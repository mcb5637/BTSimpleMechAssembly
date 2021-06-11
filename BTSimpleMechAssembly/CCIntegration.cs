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

        public static void LoadDelegates()
        {
            try
            {
                Assembly a = AccessExtensionPatcher.GetLoadedAssemblybyName("CustomComponents");
                if (a == null)
                    return;

                // do reflection magic to get delegates to CustomComponents funcs
                Type t = a.GetType("CustomComponents.Flags");
                AccessExtensionPatcher.GetDelegateFromAssembly(a, "CustomComponents.MechDefExtensions", "GetComponent", ref GetCCFlagsMechDef, null, (mi, _) => mi.MakeGenericMethod(t));
                AccessExtensionPatcher.GetDelegateFromAssembly(a, "CustomComponents.ChassisDefExtensions", "GetComponent", ref GetCCFlagsChassisDef, null, (mi, _) => mi.MakeGenericMethod(t));
                AccessExtensionPatcher.GetDelegateFromAssembly(a, "CustomComponents.MechComponentDefExtensions", "GetComponent", ref GetCCFlagsMCDef, null, (mi, _) => mi.MakeGenericMethod(t));

                // do more magic to get no_salvage flag out of it
                if (t != null)
                {
                    MethodInfo m = t.GetMethods().Where((i) => i.Name.Equals("get_NotSalvagable")).Single();
                    DynamicMethod dm = new DynamicMethod("get_NotSalvagable", typeof(bool), new Type[] { typeof(object) });
                    ILGenerator g = dm.GetILGenerator();
                    g.Emit(OpCodes.Ldarg_0);
                    g.Emit(OpCodes.Castclass, t);
                    g.Emit(OpCodes.Call, m);
                    g.Emit(OpCodes.Ret);
                    CCFlagsGetNotSalvageable = (Func<object, bool>)dm.CreateDelegate(typeof(Func<object, bool>));
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

    }
}
