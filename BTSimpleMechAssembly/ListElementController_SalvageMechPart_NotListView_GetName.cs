using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Harmony;
using BattleTech.UI;

namespace BTSimpleMechAssembly
{
    [HarmonyPatch(typeof(ListElementController_SalvageMechPart_NotListView), "GetName")]
    class ListElementController_SalvageMechPart_NotListView_GetName
    {
        public static bool Prefix(ListElementController_SalvageMechPart_NotListView __instance, ref string __result)
        {
            if (__instance.mechDef != null && __instance.mechDef.IsVehicle())
            {
                __result = __instance.mechDef.Chassis.Description.Name;
                return false;
            }
            return true;
        }
    }
}
