using AccessExtension;
using BattleTech;
using Harmony;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace BTSimpleMechAssembly
{
    static class MAIntegration
    {
        private static void GetPrefabIdInternal_Postfix(ChassisDef chassis, int idType, ref string __result)
        {
            if (chassis.IsVehicle())
            {
                __result = chassis.GetVariant(true);
                return;
            }
            if (idType == 0) // assembly variant
            {
                if (Assembly.Settings.MAIntegration_OverrideOnly)
                    __result = chassis.GetVariantOverride(true) ?? __result;
                else
                    __result = chassis.GetVariant(true);
            }
        }
        private static void GetPrefabIdInternalV_Postfix(VehicleChassisDef chassis, int idType, ref string __result)
        {
            if (idType == 0) // assembly variant
            {
                __result = chassis.GetVariant(true);
            }
        }

        internal static void TryPatch(HarmonyInstance h)
        {
            System.Reflection.Assembly a = AccessExtensionPatcher.GetLoadedAssemblyByName("MechAffinity");
            if (a == null)
            {
                Assembly.Log.Log("MechAffinity not found");
                return;
            }

            Type am = a.GetType("MechAffinity.PilotAffinityManager");
            try
            {
                Assembly.Log.Log("loading MechAffinity...");

                MethodInfo original = am.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance).Where((m) => m.Name == "getPrefabIdInternal" && m.GetParameters()[0].ParameterType == typeof(ChassisDef)).Single();
                Assembly.Log.Log($"patching {original.FullName()}");
                h.Patch(original, null, new HarmonyMethod(AccessTools.DeclaredMethod(typeof(MAIntegration), nameof(GetPrefabIdInternal_Postfix))));


                original = am.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance).Where((m) => m.Name == "getPrefabIdInternal" && m.GetParameters()[0].ParameterType == typeof(VehicleChassisDef)).Single();
                Assembly.Log.Log($"patching {original.FullName()}");
                h.Patch(original, null, new HarmonyMethod(AccessTools.DeclaredMethod(typeof(MAIntegration), nameof(GetPrefabIdInternalV_Postfix))));

                Assembly.Log.Log("MechAffinity patched");
            }
            catch (Exception e)
            {
                FileLog.Log(e.ToString());
            }
        }
    }
}
