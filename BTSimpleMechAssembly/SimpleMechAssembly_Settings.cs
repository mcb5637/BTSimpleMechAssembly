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
        public bool CrossAssemblySpeedMatch = true;
        public bool CrossAssemblyTonnsMatch = true;
        public string[] CrossAssemblyTagsMatch = new string[] {};
        public string[] CrossAssemblyExcludedMechs = new string[] {};
        public string OmniMechTag = null;
        public bool AssembledMechsNeedReadying = false;
    }
}
