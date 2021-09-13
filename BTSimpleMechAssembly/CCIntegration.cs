using BattleTech;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AccessExtension;
using Harmony;
using System.Reflection;
using System.Reflection.Emit;

namespace BTSimpleMechAssembly
{
    static class CCIntegration
    {
        internal static Func<MechDef, object> GetCCFlagsMechDef = (_) => null;
        internal static Func<ChassisDef, object> GetCCFlagsChassisDef = (_) => null;
        internal static Func<MechComponentDef, object> GetCCFlagsMCDef = (_) => null;
        internal static Func<object, bool> CCFlagsGetNotSalvageable = (_) => false;
        internal static Func<VehicleChassisDef, IVAssemblyVariant> GetCCVehicleAssemblyVariant = (_) => null;
        internal static Func<ChassisDef, IAssemblyVariant> GetCCAssemblyVariant = (_) => null;
        private static Action<Type[]> RegisterCCTypes = (_) => { };
        private static Type VAssemblyVariantType = null, AssemblyVariantType = null;

        public static void LoadDelegates()
        {
            try
            {
                Assembly a = AccessExtensionPatcher.GetLoadedAssemblybyName("CustomComponents");
                if (a == null)
                {
                    SimpleMechAssembly_Main.Log.Log("CustomComponents not found");
                    return;
                }

                SimpleMechAssembly_Main.Log.Log("loading CustomComponents...");
                // do reflection magic to get delegates to CustomComponents funcs
                Type ccflags = a.GetType("CustomComponents.Flags");
                AccessExtensionPatcher.GetDelegateFromAssembly(a, "CustomComponents.MechDefExtensions", "GetComponent", ref GetCCFlagsMechDef, null, (mi, _) => mi.MakeGenericMethod(ccflags), SimpleMechAssembly_Main.Log.Log);
                AccessExtensionPatcher.GetDelegateFromAssembly(a, "CustomComponents.ChassisDefExtensions", "GetComponent", ref GetCCFlagsChassisDef, null, (mi, _) => mi.MakeGenericMethod(ccflags), SimpleMechAssembly_Main.Log.Log);
                AccessExtensionPatcher.GetDelegateFromAssembly(a, "CustomComponents.MechComponentDefExtensions", "GetComponent", ref GetCCFlagsMCDef, null, (mi, _) => mi.MakeGenericMethod(ccflags), SimpleMechAssembly_Main.Log.Log);
                AccessExtensionPatcher.GetDelegateFromAssembly(a, "CustomComponents.Registry", "RegisterSimpleCustomComponents", ref RegisterCCTypes, (mi) => mi.GetParameters().First().Name=="types", null, SimpleMechAssembly_Main.Log.Log);

                // do more magic to get no_salvage flag out of it
                if (ccflags != null)
                {
                    SimpleMechAssembly_Main.Log.Log("generating CCFalgs.GetNotSalvageable");
                    MethodInfo m = ccflags.GetMethods().Where((i) => i.Name.Equals("get_NotSalvagable")).Single();
                    DynamicMethod dm = new DynamicMethod("get_NotSalvagable", typeof(bool), new Type[] { typeof(object) });
                    ILGenerator g = dm.GetILGenerator();
                    g.Emit(OpCodes.Ldarg_0);
                    g.Emit(OpCodes.Castclass, ccflags);
                    g.Emit(OpCodes.Call, m);
                    g.Emit(OpCodes.Ret);
                    CCFlagsGetNotSalvageable = (Func<object, bool>)dm.CreateDelegate(typeof(Func<object, bool>));
                }

                // do a lot more magic to register AssemblyVariant & VAssemblyVariant
                SimpleMechAssembly_Main.Log.Log("Generating Customs");
                ConstructorInfo custcomatctor = a.GetType("CustomComponents.CustomComponentAttribute").GetConstructor(new Type[] { typeof(string) });
                Type simplecustom = a.GetType("CustomComponents.SimpleCustom`1");
                AssemblyVariantType = AccessExtensionPatcher.GenerateType("AssemblyVariant", simplecustom.MakeGenericType(typeof(ChassisDef)),
                    new Type[] { typeof(IAssemblyVariant) },
                    new CustomAttributeBuilder[] { new CustomAttributeBuilder(custcomatctor, new object[] { "AssemblyVariant" }) });
                VAssemblyVariantType = AccessExtensionPatcher.GenerateType("VAssemblyVariant", simplecustom.MakeGenericType(typeof(VehicleChassisDef)),
                    new Type[] { typeof(IVAssemblyVariant) },
                    new CustomAttributeBuilder[] { new CustomAttributeBuilder(custcomatctor, new object[] { "VAssemblyVariant" }) });

                SimpleMechAssembly_Main.Log.Log("Registering Customs");
                RegisterCCTypes(new Type[] { AssemblyVariantType, VAssemblyVariantType });

                AccessExtensionPatcher.GetDelegateFromAssembly(a, "CustomComponents.ChassisDefExtensions", "GetComponent", ref GetCCAssemblyVariant, null, (mi, _) => mi.MakeGenericMethod(AssemblyVariantType), SimpleMechAssembly_Main.Log.Log);
                if (!AccessExtensionPatcher.GetDelegateFromAssembly(a, "CustomComponents.VehicleExtentions", "GetComponent", ref GetCCVehicleAssemblyVariant, null, (mi, _) => mi.MakeGenericMethod(VAssemblyVariantType), SimpleMechAssembly_Main.Log.Log))
                {
                    if (SimpleMechAssembly_Main.Settings.FakeVehilceTag != null)
                    {
                        SimpleMechAssembly_Main.Log.LogWarning("warning: SMA FakeVehilceTag is set, but CustomComponents does not support VehicleDef Customs. Upgrade your CustomComponents to use Vehicle CrossAssembly");
                    }
                }
            }
            catch (Exception e)
            {
                FileLog.Log(e.ToString());
            }
        }


        public static bool IsCCNoSalvage(this MechDef d)
        {
            object f = GetCCFlagsMechDef(d);
            if (f != null)
                return CCFlagsGetNotSalvageable(f);
            return false;
        }
        public static bool IsCCNoSalvage(this ChassisDef d)
        {
            object f = GetCCFlagsChassisDef(d);
            if (f != null)
                return CCFlagsGetNotSalvageable(f);
            return false;
        }
        public static bool IsCCNoSalvage(this MechComponentDef d)
        {
            object f = GetCCFlagsMCDef(d);
            if (f != null)
                return CCFlagsGetNotSalvageable(f);
            return false;
        }

        public static string GetVariant(this ChassisDef d)
        {
            if (d.IsVehicle())
            {
                VehicleChassisDef vd = d.GetVehicleChassisDefFromFakeVehicle();
                if (vd == null)
                    return d.Description.Id;
                IVAssemblyVariant iVAssemblyVariant = GetCCVehicleAssemblyVariant(vd);
                //FileLog.Log($"vehicle {d.Description.Id} -> {iVAssemblyVariant?.PrefabID ?? d.Description.Id} null:{iVAssemblyVariant==null}");
                return iVAssemblyVariant?.PrefabID ?? d.Description.Id;
            }
            return GetCCAssemblyVariant(d)?.PrefabID ?? d.Description.UIName;
        }
    }

    interface IVAssemblyVariant
    {
        string PrefabID { get; set; }
        bool Exclude { get; set; }
    }
    interface IAssemblyVariant
    {
        string PrefabID { get; set; }
        bool Exclude { get; set; }
        string[] AssemblyAllowedWith { get; set; }
        bool KnownOmniVariant { get; set; }
    }
}
