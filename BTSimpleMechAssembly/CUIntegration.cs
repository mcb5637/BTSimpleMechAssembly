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
        private static Func<SimGameState, MechDef, int> GetFirstFreeMechBayD = (s, m) => -1;
        private static Func<SimGameState, MechDef, int?, int> GetFirstFreeMechBayDNew = (s, m, d) => -1;

        public static int GetFirstFreeMechBay(SimGameState s, MechDef m)
        {
            if (GetFirstFreeMechBayD != null)
                return GetFirstFreeMechBayD(s, m);
            if (GetFirstFreeMechBayDNew != null)
                return GetFirstFreeMechBayDNew(s, m, null);
            return s.GetFirstFreeMechBay();
        }

        public static void LoadDelegates()
        {
            try
            {
                if (!AccessExtensionPatcher.GetDelegateFromAssembly("CustomUnits", "CustomUnits.SimGameState_AddMech", "GetFirstFreeMechBay", ref GetFirstFreeMechBayD, (m) => m.GetParameters().Length == 2, null, SimpleMechAssembly_Main.Log.Log)
                    && !AccessExtensionPatcher.GetDelegateFromAssembly("CustomUnits", "CustomUnits.SimGameState_AddMech", "GetFirstFreeMechBay", ref GetFirstFreeMechBayDNew, (m) => m.GetParameters().Length == 3 && m.GetParameters()[1].ParameterType == typeof(MechDef), null, SimpleMechAssembly_Main.Log.Log))
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
