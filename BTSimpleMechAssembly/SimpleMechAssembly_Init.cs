using AccessExtension;
using BattleTech;
using BattleTech.UI;
using Harmony;
using HBS.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

[assembly: AssemblyVersion("1.2.2.1")]

namespace BTSimpleMechAssembly
{
    class SimpleMechAssembly_Init
    {
        public static void Init(string directory, string settingsJSON)
        {
            SimpleMechAssembly_Main.Log = Logger.GetLogger("BTSimpleMechAssembly");
            try
            {
                SimpleMechAssembly_Main.Settings = JsonConvert.DeserializeObject<SimpleMechAssembly_Settings>(settingsJSON);
            }
            catch (Exception e)
            {
                SimpleMechAssembly_Main.Log.LogException("error reading settings, using defaults", e);
                SimpleMechAssembly_Main.Settings = new SimpleMechAssembly_Settings();
            }
            if (SimpleMechAssembly_Main.Settings.LogLevelLog)
                Logger.SetLoggerLevel("BTSimpleMechAssembly", LogLevel.Log);
            var harmony = HarmonyInstance.Create("com.github.mcb5637.BTSimpleMechAssembly");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            AccessExtensionPatcher.PatchAll(harmony, Assembly.GetExecutingAssembly());
            if (SimpleMechAssembly_Main.Settings.StructurePointBasedSalvageActive)
            {
                try
                {
                    harmony.Patch(typeof(Contract).GetMethod("GenerateSalvage", BindingFlags.NonPublic | BindingFlags.Instance),
                                null,
                                new HarmonyMethod(typeof(SimpleMechAssembly_StructurePointBasedSalvage).GetMethod("Postfix")),
                                new HarmonyMethod(typeof(SimpleMechAssembly_StructurePointBasedSalvage).GetMethod("Transpiler")));
                    harmony.Patch(AccessTools.DeclaredMethod(typeof(Contract), "AddMechComponentToSalvage"), null, null,
                        new HarmonyMethod(AccessTools.DeclaredMethod(typeof(SimpleMechAssembly_StructurePointBasedSalvage), "CheckSalvageTranspiler")));
                    harmony.Patch(AccessTools.DeclaredMethod(typeof(Contract), "CreateAndAddMechPart"), null, null,
                        new HarmonyMethod(AccessTools.DeclaredMethod(typeof(SimpleMechAssembly_StructurePointBasedSalvage), "CheckMechSalvageTranspiler")));
                }
                catch (Exception e)
                {
                    FileLog.Log(e.ToString());
                }
            }
        }

        public static void FinishedLoading()
        {
            CCIntegration.LoadDelegates();
            CUIntegration.LoadDelegates();
            var h = HarmonyInstance.Create("com.github.mcb5637.BTSimpleMechAssembly");
            MAIntegration.TryPatch(h);
            CustomMech_GetActorInfoFromVisLevel.TryPatch(h);
        }
    }
}
