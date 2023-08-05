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
                __result = chassis.GetVariant();
                return;
            }
            if (idType == 0) // assembly variant
            {
                if (SimpleMechAssembly_Main.Settings.MAIntegration_OverrideOnly)
                    __result = chassis.GetVariantOverride() ?? __result;
                else
                    __result = chassis.GetVariant();
            }
        }
        private static void GetPrefabIdInternalV_Postfix(VehicleChassisDef chassis, int idType, ref string __result)
        {
            if (idType == 0) // assembly variant
            {
                __result = chassis.GetVariant();
            }
        }

        internal static void TryPatch(HarmonyInstance h)
        {
            Assembly a = AccessExtensionPatcher.GetLoadedAssemblyByName("MechAffinity");
            if (a == null)
            {
                SimpleMechAssembly_Main.Log.Log("MechAffinity not found");
                return;
            }

            Type am = a.GetType("MechAffinity.PilotAffinityManager");
            try
            {
                SimpleMechAssembly_Main.Log.Log("loading MechAffinity...");

                MethodInfo original = am.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance).Where((m) => m.Name == "getPrefabIdInternal" && m.GetParameters()[0].ParameterType == typeof(ChassisDef)).Single();
                SimpleMechAssembly_Main.Log.Log($"patching {original.FullName()}");
                h.Patch(original, null, new HarmonyMethod(AccessTools.DeclaredMethod(typeof(MAIntegration), nameof(GetPrefabIdInternal_Postfix))));


                original = am.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance).Where((m) => m.Name == "getPrefabIdInternal" && m.GetParameters()[0].ParameterType == typeof(VehicleChassisDef)).Single();
                SimpleMechAssembly_Main.Log.Log($"patching {original.FullName()}");
                h.Patch(original, null, new HarmonyMethod(AccessTools.DeclaredMethod(typeof(MAIntegration), nameof(GetPrefabIdInternalV_Postfix))));

                SimpleMechAssembly_Main.Log.Log("MechAffinity patched");
            }
            catch (Exception e)
            {
                FileLog.Log(e.ToString());
            }
        }
    }
}
