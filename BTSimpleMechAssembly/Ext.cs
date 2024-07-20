using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleTech;
using BattleTech.Data;
using Harmony;
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
            return Assembly.Settings.FakeVehilceTag != null && d.ChassisTags.Contains(Assembly.Settings.FakeVehilceTag);
        }

        public static bool IsOmni(this ChassisDef d)
        {
            return Assembly.Settings.OmniMechTag != null && d.ChassisTags.Contains(Assembly.Settings.OmniMechTag);
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

        public static bool IsMechDefMain(this MechDef d)
        {
            // remove mechdef_ and chassisdef_, then compare
            return d.Description.Id.Substring(8).Equals(d.Chassis.Description.Id.Substring(11));
        }

        public static MechDef GetMainMechDef(this ChassisDef d, DataManager dm)
        {
            return dm.MechDefs.Get(d.Description.Id.Replace("chassisdef", "mechdef"));
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

        public static int GetMechSellCost(this MechDef m, SimGameState s, bool stripped = false)
        {
            int c = m.Chassis.Description.Cost;
            if (!m.IsVehicle() && !stripped)
            {
                foreach (MechComponentRef r in m.Inventory)
                {
                    if (!r.IsFixed)
                    {
                        c += r.Def.Description.Cost;
                    }
                }
            }
            c = Mathf.FloorToInt(c * s.Constants.Finances.ShopSellModifier);
            return c;
        }

        public static MechComponentDef GetComponentDefFromID(this DataManager s, string id)
        {
            if (s.AmmoBoxDefs.TryGet(id, out AmmunitionBoxDef adef))
                return adef;
            if (s.HeatSinkDefs.TryGet(id, out HeatSinkDef hdef))
                return hdef;
            if (s.JumpJetDefs.TryGet(id, out JumpJetDef jdef))
                return jdef;
            if (s.UpgradeDefs.TryGet(id, out UpgradeDef udef))
                return udef;
            if (s.WeaponDefs.TryGet(id, out WeaponDef wdef))
                return wdef;
            return null;
        }
    }
}
