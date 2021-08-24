using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleTech;

namespace BTSimpleMechAssembly
{
    static class Ext
    {

        public static bool IsVehicle(this MechDef d)
        {
            return SimpleMechAssembly_Main.Settings.FakeVehilceTag != null && d.Chassis.ChassisTags.Contains(SimpleMechAssembly_Main.Settings.FakeVehilceTag);
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
    }
}
