using BattleTech;
using Harmony;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using AccessExtension;

namespace BTSimpleMechAssembly
{
    class CustomMech_GetActorInfoFromVisLevel
    {
        public static string Get(AbstractActor a)
        {
            return string.IsNullOrEmpty(a.Description.UIName) ? a.Description.Name : a.Description.UIName;
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo o = AccessTools.Property(typeof(AbstractActor), nameof(AbstractActor.DisplayName)).GetMethod;
            MethodInfo r = AccessTools.Method(typeof(CustomMech_GetActorInfoFromVisLevel), nameof(Get));
            foreach (var c in instructions)
            {
                if ((c.opcode == OpCodes.Call || c.opcode == OpCodes.Callvirt) && (c.operand as MethodInfo) == o)
                {
                    c.operand = r;
                }
                yield return c;
            }
        }

        public static void TryPatch(HarmonyInstance h)
        {
            if (!SimpleMechAssembly_Main.Settings.CUVehicle_CombatHudDisplayName)
                return;
            Assembly a = AccessExtensionPatcher.GetLoadedAssemblyByName("CustomUnits");
            if (a == null)
            {
                SimpleMechAssembly_Main.Log.Log("CustomUnits.dll not found");
                return;
            }
            Type t = a.GetType("CustomUnits.CustomMech");
            if (t == null)
            {
                SimpleMechAssembly_Main.Log.Log("CustomUnits.CustomMech not found");
                return;
            }
            MethodInfo i = t.GetMethod("GetActorInfoFromVisLevel", BindingFlags.Public | BindingFlags.Instance);
            if (i == null)
            {
                SimpleMechAssembly_Main.Log.Log("CustomUnits.CustomMech.GetActorInfoFromVisLevel not found");
                return;
            }
            h.Patch(i, null, null, new HarmonyMethod(AccessTools.Method(typeof(CustomMech_GetActorInfoFromVisLevel), nameof(Transpiler))));
            SimpleMechAssembly_Main.Log.Log("CustomUnits.CustomMech.GetActorInfoFromVisLevel patched");
        }
    }
}
