using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
