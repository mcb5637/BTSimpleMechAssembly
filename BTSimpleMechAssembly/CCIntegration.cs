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
using System.Runtime.CompilerServices;

namespace BTSimpleMechAssembly
{
    static class CCIntegration
    {
        internal static Func<VehicleChassisDef, IVAssemblyVariant> GetCCVehicleAssemblyVariant = (_) => null;
        internal static Func<ChassisDef, IAssemblyVariant> GetCCAssemblyVariant = (_) => null;
        private static Action<Type[]> RegisterCCTypes = (_) => { };
        private static Type VAssemblyVariantType = null, AssemblyVariantType = null;
        internal static Func<MechDef, bool> MechDefIsDead = (_) => false;

        private static MethodInfo MechComponentGetFlags = null;
        private static MethodInfo FlagsGetNoSalvage = null;
        private static MethodInfo MechComponentGetLootable = null;
        private static MethodInfo LootableGetItem = null;

        public static void LoadDelegates(HarmonyInstance h)
        {
            try
            {
                System.Reflection.Assembly a = AccessExtensionPatcher.GetLoadedAssemblyByName("CustomComponents");
                if (a == null)
                {
                    Assembly.Log.Log("CustomComponents not found");
                    return;
                }

                Assembly.Log.Log("loading CustomComponents...");
                // do reflection magic to get delegates to CustomComponents funcs
                AccessExtensionPatcher.GetDelegateFromAssembly(a, "CustomComponents.Registry", "RegisterSimpleCustomComponents", ref RegisterCCTypes, (mi) => mi.GetParameters().First().Name=="types", null, Assembly.Log.Log);
                AccessExtensionPatcher.GetDelegateFromAssembly(a, "CustomComponents.Contract_GenerateSalvage", "IsDestroyed", ref MechDefIsDead, null, null, Assembly.Log.Log);

                // do more magic to access customs
                Assembly.Log.Log("loading flags...");
                Type getccflags = a.GetType("CustomComponents.FlagExtensions");
                if (getccflags != null)
                {
                    MechComponentGetFlags = getccflags.GetMethods().SingleOrDefault((m) => m.Name == "CCFlags");
                    Assembly.Log.Log($"found new flags: {MechComponentGetFlags.FullName()}");
                }
                if (MechComponentGetFlags == null)
                {
                    Type ccflags = a.GetType("CustomComponents.Flags");
                    MechComponentGetFlags = a.GetType("CCustomComponents.MechComponentDefExtensions").GetMethods().SingleOrDefault((m) => m.Name == "GetComponent")?.MakeGenericMethod(ccflags);
                    Assembly.Log.Log($"found old flags: {MechComponentGetFlags.FullName()}");
                }
                if (MechComponentGetFlags != null)
                {
                    FlagsGetNoSalvage = MechComponentGetFlags.ReturnType.GetProperties().SingleOrDefault((p) => p.Name == "NoSalvage")?.GetGetMethod();
                    Assembly.Log.Log($"salvage from flags: {FlagsGetNoSalvage.FullName()}");
                }
                if (MechComponentGetFlags != null && FlagsGetNoSalvage != null)
                {
                    Assembly.Log.Log("patching IsCCNoSalvage");
                    h.Patch(AccessTools.Method(typeof(CCIntegration), nameof(IsCCNoSalvage), new Type[] {typeof(MechComponentDef)}), null, null, new HarmonyMethod(typeof(CCIntegration), nameof(IsCCNoSalvage_Trans)));
                }


                Assembly.Log.Log("loading lootable...");
                Type getlootable = a.GetType("CustomComponents.MechComponentDefExtensions");
                if (getlootable != null)
                {
                    Type cclootable = a.GetType("CustomComponents.LootableDefault");
                    MechComponentGetLootable = getlootable.GetMethods().SingleOrDefault((m) => m.Name == "GetComponent")?.MakeGenericMethod(cclootable);
                    Assembly.Log.Log($"found lootable: {MechComponentGetLootable.FullName()}");
                    if (MechComponentGetLootable != null)
                    {
                        LootableGetItem = MechComponentGetLootable.ReturnType.GetProperties().SingleOrDefault((p) => p.Name == "ItemID")?.GetGetMethod();
                        Assembly.Log.Log($"item from lootable: {LootableGetItem.FullName()}");
                        if (LootableGetItem != null)
                        {
                            Assembly.Log.Log("patching GetCCLootableItem");
                            h.Patch(AccessTools.Method(typeof(CCIntegration), nameof(GetCCLootableItem)), null, null, new HarmonyMethod(typeof(CCIntegration), nameof(GetCCLootableItem_Trans)));
                        }
                    }
                }

                // do a lot more magic to register AssemblyVariant & VAssemblyVariant
                Assembly.Log.Log("Generating Customs");
                ConstructorInfo custcomatctor = a.GetType("CustomComponents.CustomComponentAttribute").GetConstructor(new Type[] { typeof(string) });
                Type simplecustom = a.GetType("CustomComponents.SimpleCustom`1");
                AssemblyVariantType = AccessExtensionPatcher.GenerateType("AssemblyVariant", simplecustom.MakeGenericType(typeof(ChassisDef)),
                    new Type[] { typeof(IAssemblyVariant) },
                    new CustomAttributeBuilder[] { new CustomAttributeBuilder(custcomatctor, new object[] { "AssemblyVariant" }) });
                VAssemblyVariantType = AccessExtensionPatcher.GenerateType("VAssemblyVariant", simplecustom.MakeGenericType(typeof(VehicleChassisDef)),
                    new Type[] { typeof(IVAssemblyVariant) },
                    new CustomAttributeBuilder[] { new CustomAttributeBuilder(custcomatctor, new object[] { "VAssemblyVariant" }) });

                Assembly.Log.Log("Registering Customs");
                RegisterCCTypes(new Type[] { AssemblyVariantType, VAssemblyVariantType });

                AccessExtensionPatcher.GetDelegateFromAssembly(a, "CustomComponents.ChassisDefExtensions", "GetComponent", ref GetCCAssemblyVariant, null, (mi, _) => mi.MakeGenericMethod(AssemblyVariantType), Assembly.Log.Log);
                if (!AccessExtensionPatcher.GetDelegateFromAssembly(a, "CustomComponents.VehicleExtentions", "GetComponent", ref GetCCVehicleAssemblyVariant, null, (mi, _) => mi.MakeGenericMethod(VAssemblyVariantType), Assembly.Log.Log))
                {
                    if (Assembly.Settings.FakeVehilceTag != null)
                    {
                        Assembly.Log.LogWarning("warning: SMA FakeVehilceTag is set, but CustomComponents does not support VehicleDef Customs. Upgrade your CustomComponents to use Vehicle CrossAssembly");
                    }
                }
            }
            catch (Exception e)
            {
                Assembly.Log.LogException(e);
            }
        }

        public static bool IsCCNoSalvage(this MechDef d)
        {
            return d.Chassis.IsCCNoSalvage();
        }
        public static bool IsCCNoSalvage(this ChassisDef d)
        {
            IAssemblyVariant v = GetCCAssemblyVariant(d);
            if (v == null)
                return false;
            return v.NoSalvage;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool IsCCNoSalvage(this MechComponentDef d)
        {
            return false;
        }
        private static IEnumerable<CodeInstruction> IsCCNoSalvage_Trans(ILGenerator il)
        {
            Label retfalse = il.DefineLabel();
            return new CodeInstruction[]
            {
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Call, MechComponentGetFlags),
                new CodeInstruction(OpCodes.Dup),
                new CodeInstruction(OpCodes.Ldnull),
                new CodeInstruction(OpCodes.Beq, retfalse),

                new CodeInstruction(OpCodes.Call, FlagsGetNoSalvage),
                new CodeInstruction(OpCodes.Ret),

                lb(new CodeInstruction(OpCodes.Pop), retfalse),
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Ret),
            };
            CodeInstruction lb(CodeInstruction c, Label l)
            {
                c.labels.Add(l);
                return c;
            }
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

        public static string GetVariantOverride(this ChassisDef d)
        {
            if (d.IsVehicle())
            {
                VehicleChassisDef vd = d.GetVehicleChassisDefFromFakeVehicle();
                if (vd == null)
                    return d.Description.Id;
                IVAssemblyVariant iVAssemblyVariant = GetCCVehicleAssemblyVariant(vd);
                //FileLog.Log($"vehicle {d.Description.Id} -> {iVAssemblyVariant?.PrefabID ?? d.Description.Id} null:{iVAssemblyVariant==null}");
                return iVAssemblyVariant?.PrefabID;
            }
            return GetCCAssemblyVariant(d)?.PrefabID;
        }

        public static string GetVariant(this VehicleChassisDef vd)
        {
            IVAssemblyVariant iVAssemblyVariant = GetCCVehicleAssemblyVariant(vd);
            //FileLog.Log($"vehicle {d.Description.Id} -> {iVAssemblyVariant?.PrefabID ?? d.Description.Id} null:{iVAssemblyVariant==null}");
            return iVAssemblyVariant?.PrefabID ?? vd.Description.Id;
        }

        public static string GetVariantOverride(this VehicleChassisDef vd)
        {
            IVAssemblyVariant iVAssemblyVariant = GetCCVehicleAssemblyVariant(vd);
            //FileLog.Log($"vehicle {d.Description.Id} -> {iVAssemblyVariant?.PrefabID ?? d.Description.Id} null:{iVAssemblyVariant==null}");
            return iVAssemblyVariant?.PrefabID;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string GetCCLootableItem(this MechComponentDef d)
        {
            return null;
        }
        private static IEnumerable<CodeInstruction> GetCCLootableItem_Trans(ILGenerator il)
        {
            Label retfalse = il.DefineLabel();
            return new CodeInstruction[]
            {
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Call, MechComponentGetLootable),
                new CodeInstruction(OpCodes.Dup),
                new CodeInstruction(OpCodes.Ldnull),
                new CodeInstruction(OpCodes.Beq, retfalse),

                new CodeInstruction(OpCodes.Call, LootableGetItem),
                new CodeInstruction(OpCodes.Ret),

                lb(new CodeInstruction(OpCodes.Pop), retfalse),
                new CodeInstruction(OpCodes.Ldnull),
                new CodeInstruction(OpCodes.Ret),
            };
            CodeInstruction lb(CodeInstruction c, Label l)
            {
                c.labels.Add(l);
                return c;
            }
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
        string Lootable { get; set; }
        bool NoSalvage { get; set; }
    }
}
