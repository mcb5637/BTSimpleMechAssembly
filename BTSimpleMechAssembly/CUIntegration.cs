﻿using AccessExtension;
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
        private static Func<SimGameState, MechDef, int> GetFirstFreeMechBayD = null;
        private static Func<SimGameState, MechDef, int?, int> GetFirstFreeMechBayDNew = null;

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
                if (!AccessExtensionPatcher.GetDelegateFromAssembly("CustomUnits", "CustomUnits.SimGameState_AddMech", "GetFirstFreeMechBay", ref GetFirstFreeMechBayD, (m) => m.GetParameters().Length == 2, null, Assembly.Log.Log)
                    && !AccessExtensionPatcher.GetDelegateFromAssembly("CustomUnits", "CustomUnits.SimGameState_AddMech", "GetFirstFreeMechBay", ref GetFirstFreeMechBayDNew, (m) => m.GetParameters().Length == 3 && m.GetParameters()[1].ParameterType == typeof(MechDef), null, Assembly.Log.Log))
                {
                    if (Assembly.Settings.SalvageAndAssembleVehicles)
                    {
                        Assembly.Log.LogWarning("warning: SMA SalvageAndAssembleVehicles is set, but CustomUnits is missing. unsetting it now.");
                        Assembly.Settings.SalvageAndAssembleVehicles = false;
                    }
                }
            }
            catch (Exception e)
            {
                Assembly.Log.LogException(e);
            }
            Assembly.Log.Log($"FakeVehilceTag={Assembly.Settings.FakeVehilceTag ?? "null"}, SalvageAndAssembleVehicles={Assembly.Settings.SalvageAndAssembleVehicles}");
        }

        public static VehicleDef GetVehicleDefFromFakeVehicle(this MechDef a)
        {
            return a.DataManager.VehicleDefs.Get(a.Description.Id);
        }
        public static VehicleChassisDef GetVehicleChassisDefFromFakeVehicle(this ChassisDef a)
        {
            return a.DataManager.VehicleChassisDefs.Get(a.Description.Id);
        }
        public static MechDef GetFakeVehicle(this VehicleDef v)
        {
            return v.DataManager.MechDefs.Get(v.Description.Id);
        }
    }
}
