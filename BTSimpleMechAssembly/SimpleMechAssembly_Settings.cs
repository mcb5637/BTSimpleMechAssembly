using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BTSimpleMechAssembly
{
    class SimpleMechAssembly_Settings
    {
        public string CrossAssemblyUpgradeRequired = null;
        public bool BTXCrossAssemblyAlwaysAllowIfSimulation = false;
        public bool CrossAssemblySpeedMatch = true;
        public bool CrossAssemblyTonnsMatch = true;
        public string[] CrossAssemblyTagsMatch = new string[] {};
        public string[] CrossAssemblyExcludedMechs = new string[] {};
        public string[] CrossAssemblyInventoryMatch = new string[] { };
        public string OmniMechTag = null;
        public bool AssembledMechsNeedReadying = false;
        public bool StructurePointBasedSalvageActive = false;
        public float StructurePointBasedSalvageLowPriorityFactor = 1f;
        public float StructurePointBasedSalvageHighPriorityFactor = 2f;
        public int StructurePointBasedSalvageMaxPartsFromMech = 3;
        public int StructurePointBasedSalvageMinPartsFromMech = 1;
        public string[] StructurePointBasedSalvageSalvageBlacklist = new string[] { };
        public float StructurePointBasedSalvageTurretComponentSalvageChance = 1;
        public Dictionary<string, string> StructurePointBasedSalvageMechPartSalvageRedirect = new Dictionary<string, string>();
        public bool LogLevelLog = false;
        public int AssembledMechsReadyingFlatCost = 1;
        public int AssembledMechsReadyingPerNonFixedComponentCost = 0;
        public bool AutoQueryAssembly = true;
        public string StorageColorParts
        {
            set => ColorUtility.TryParseHtmlString(value, out storage_parts);
            get => ColorUtility.ToHtmlStringRGB(storage_parts);
        }
        public string StorageColorMech
        {
            set => ColorUtility.TryParseHtmlString(value, out storage_mech);
            get => ColorUtility.ToHtmlStringRGB(storage_mech);
        }
        public string StorageColorOmni
        {
            set => ColorUtility.TryParseHtmlString(value, out storage_omni);
            get => ColorUtility.ToHtmlStringRGB(storage_omni);
        }
        public string StorageColorVehicle
        {
            set => ColorUtility.TryParseHtmlString(value, out storage_vehicle);
            get => ColorUtility.ToHtmlStringRGB(storage_vehicle);
        }
        public string StorageColorVehiclePart
        {
            set => ColorUtility.TryParseHtmlString(value, out storage_vehiclepart);
            get => ColorUtility.ToHtmlStringRGB(storage_vehiclepart);
        }
        public bool UseOnlyCCSalvageFlag = false;
        public bool UseOnlyCCAssemblyOptions = false;
        public string FakeVehilceTag = null;
        public bool ShowAllVariantsInPopup = false;

        [JsonIgnore]
        internal Color storage_parts = Color.white;
        [JsonIgnore]
        internal Color storage_mech = Color.white;
        [JsonIgnore]
        internal Color storage_omni = Color.white;
        [JsonIgnore]
        internal Color storage_vehicle = Color.white;
        [JsonIgnore]
        internal Color storage_vehiclepart = Color.white;
    }
}
