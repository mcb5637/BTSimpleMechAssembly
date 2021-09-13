using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleTech;
using UnityEngine;

namespace BTSimpleMechAssembly
{
    static class Ext
    {

        public static bool IsVehicle(this MechDef d)
        {
            return d.Chassis.IsVehicle();
        }
        public static bool IsVehicle(this ChassisDef d)
        {
            return SimpleMechAssembly_Main.Settings.FakeVehilceTag != null && d.ChassisTags.Contains(SimpleMechAssembly_Main.Settings.FakeVehilceTag);
        }

        public static bool IsOmni(this ChassisDef d)
        {
            return SimpleMechAssembly_Main.Settings.OmniMechTag != null && d.ChassisTags.Contains(SimpleMechAssembly_Main.Settings.OmniMechTag);
        }

        public static string GetMechOmniVehicle(this MechDef d)
        {
            if (d.IsVehicle())
                return "Vehicle";
            else if (IsOmni(d.Chassis))
                return "OmniMech";
            else
                return "Mech";
        }

        public static bool IsMechDefCustom(this MechDef d)
        {
            return d.Description.Id.Contains("mechdef_CUSTOM_");
        }

        public static int CountMechInventory(this MechDef d, string it)
        {
            return d.Inventory.Count((i) => i.ComponentDefID.Equals(it));
        }

        public static bool IsSellingAllowed(this SimGameState s)
        {
            if (!s.CurSystem.CanUseSystemStore())
                return false;
            if (s.TravelState != SimGameTravelStatus.IN_SYSTEM)
                return false;
            return true;
        }

        public static int GetMechSellCost(this MechDef m, SimGameState s)
        {
            int c = m.Chassis.Description.Cost;
            if (m.IsVehicle())
                return c;
            foreach (MechComponentRef r in m.Inventory)
            {
                if (!r.IsFixed)
                {
                    c += r.Def.Description.Cost;
                }
            }
            c = Mathf.FloorToInt(c * s.Constants.Finances.ShopSellModifier);
            return c;
        }
    }
}
