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
        public string StorageColorParts = "blue";
        public string StorageColorMech = "yellow";
        public string StorageColorOmni = "green";
        public bool UseOnlyCCSalvageFlag = false;
        public bool UseOnlyCCAssemblyOptions = false;
        public string FakeVehilceTag = null;
        public bool ShowAllVariantsInPopup = false;

        [JsonIgnore]
        internal Color storage_parts = Color.blue;
        [JsonIgnore]
        internal Color storage_mech = Color.yellow;
        [JsonIgnore]
        internal Color storage_omni = Color.green;

        public void ParseColors()
        {
            ColorUtility.TryParseHtmlString(StorageColorParts, out storage_parts);
            ColorUtility.TryParseHtmlString(StorageColorMech, out storage_mech);
            ColorUtility.TryParseHtmlString(StorageColorOmni, out storage_omni);
        }
    }
}
