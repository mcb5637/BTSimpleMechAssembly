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
    static class CUIntegration
    {
        public static Func<SimGameState, MechDef, int> GetFirstFreeMechBay = (s, m) => -1;


        public static void LoadDelegates()
        {
            try
            {
                Assembly a = AccessExtensionPatcher.GetLoadedAssemblybyName("CustomUnits");

                if (!AccessExtensionPatcher.GetDelegateFromAssembly(a, "CustomUnits.SimGameState_AddMech", "GetFirstFreeMechBay", ref GetFirstFreeMechBay))
                {
                    if (SimpleMechAssembly_Main.Settings.FakeVehilceTag != null)
                    {
                        SimpleMechAssembly_Main.Log.LogWarning("warning: SMA FakeVehilceTag is set, but CustomUnits is missing. unsetting it now.");
                        SimpleMechAssembly_Main.Settings.FakeVehilceTag = null;
                    }
                }
            }
            catch (Exception e)
            {
                FileLog.Log(e.ToString());
            }
        }

        public static VehicleDef GetVehicleDefFromFakeVehicle(this MechDef a)
        {
            return a.DataManager.VehicleDefs.Get(a.Description.Id);
        }
        public static VehicleChassisDef GetVehicleChassisDefFromFakeVehicle(this ChassisDef a)
        {
            return a.DataManager.VehicleChassisDefs.Get(a.Description.Id);
        }
    }
}
