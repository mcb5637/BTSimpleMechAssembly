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

[assembly: AssemblyVersion("1.2.4.0")]

namespace BTSimpleMechAssembly
{
    class Main
    {
        public static void Init(string directory, string settingsJSON)
        {
            Assembly.Log = Logger.GetLogger("BTSimpleMechAssembly");
            try
            {
                Assembly.Settings = JsonConvert.DeserializeObject<Settings>(settingsJSON);
            }
            catch (Exception e)
            {
                Assembly.Log.LogException("error reading settings, using defaults", e);
                Assembly.Settings = new Settings();
            }
            if (Assembly.Settings.LogLevelLog)
                Logger.SetLoggerLevel("BTSimpleMechAssembly", LogLevel.Log);
            var harmony = HarmonyInstance.Create("com.github.mcb5637.BTSimpleMechAssembly");
            harmony.PatchAll(System.Reflection.Assembly.GetExecutingAssembly());
            AccessExtensionPatcher.PatchAll(harmony, System.Reflection.Assembly.GetExecutingAssembly());
            if (Assembly.Settings.StructurePointBasedSalvageActive)
            {
                try
                {
                    harmony.Patch(typeof(Contract).GetMethod("GenerateSalvage", BindingFlags.NonPublic | BindingFlags.Instance),
                                null,
                                new HarmonyMethod(typeof(Salvage).GetMethod("Postfix")),
                                new HarmonyMethod(typeof(Salvage).GetMethod("Transpiler")));
                    harmony.Patch(AccessTools.DeclaredMethod(typeof(Contract), "AddMechComponentToSalvage"), null, null,
                        new HarmonyMethod(AccessTools.DeclaredMethod(typeof(Salvage), "CheckSalvageTranspiler")));
                    harmony.Patch(AccessTools.DeclaredMethod(typeof(Contract), "CreateAndAddMechPart"), null, null,
                        new HarmonyMethod(AccessTools.DeclaredMethod(typeof(Salvage), "CheckMechSalvageTranspiler")));
                }
                catch (Exception e)
                {
                    FileLog.Log(e.ToString());
                }
            }
        }

        public static void FinishedLoading()
        {
            var h = HarmonyInstance.Create("com.github.mcb5637.BTSimpleMechAssembly");
            CCIntegration.LoadDelegates(h);
            CUIntegration.LoadDelegates();
            MAIntegration.TryPatch(h);
            CustomMech_GetActorInfoFromVisLevel.TryPatch(h);
        }
    }
}
