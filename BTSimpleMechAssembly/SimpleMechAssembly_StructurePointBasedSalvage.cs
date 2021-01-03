using BattleTech;
using BattleTech.UI;
using Harmony;
using HBS.Logging;
using HBS.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BTSimpleMechAssembly
{
    class SimpleMechAssembly_StructurePointBasedSalvage
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> code)
        {
            yield return new CodeInstruction(OpCodes.Ret);
        }

        public static void Postfix(Contract __instance, List<UnitResult> enemyMechs, List<VehicleDef> enemyVehicles, List<UnitResult> lostUnits,
            ref List<SalvageDef> ___finalPotentialSalvage)
        {
            ILog log = SimpleMechAssembly_Main.Log;
            if (__instance.BattleTechGame.Simulation == null)
            {
                log.LogError("trying to generarte salvage without a simgame");
                return;
            }
            SimGameState s = __instance.BattleTechGame.Simulation;
            ___finalPotentialSalvage = new List<SalvageDef>();
            Traverse trav = Traverse.Create(__instance);
            trav.Property("SalvagedChassis").SetValue(new List<SalvageDef>());
            trav.Property("LostMechs").SetValue(new List<MechDef>());
            trav.Property("SalvageResults").SetValue(new List<SalvageDef>());

            foreach (UnitResult u in lostUnits)
            {
                log.Log($"player mech {u.mech.Description.Name} was lost");
                if (u.mech.GetLocationLoadoutDef(ChassisLocations.CenterTorso).CurrentInternalStructure > 0f)
                {
                    log.Log("ct not desytroyed, auto recovery");
                    u.mechLost = false;
                }
                else if (!IsPlayerMech(u.mech, s))
                {
                    log.Log("contract added player mech, auto recovery");
                    u.mechLost = false;
                }
                else if (s.NetworkRandom.Float(0f, 1f) <= s.Constants.Salvage.DestroyedMechRecoveryChance)
                {
                    log.Log("recovery roll succeeded");
                    u.mechLost = false;
                }
                else
                {
                    log.Log("recovery roll failed, unit goes into salvage pool");
                    u.mechLost = true;
                    GenerateSalvageForMech(__instance, u, s, ___finalPotentialSalvage);
                }
            }

            foreach (UnitResult u in enemyMechs)
            {
                if (!(u.pilot.IsIncapacitated || u.pilot.HasEjected || u.mech.IsDestroyed || u.mech.Inventory.Any((x) => x.Def != null && x.Def.CriticalComponent && x.DamageLevel == ComponentDamageLevel.Destroyed)))
                {
                    log.Log($"skipping salvage for mech {u.mech.Description.UIName} {u.mech.Chassis.VariantName}, cause its not dead");
                    continue;
                }
                GenerateSalvageForMech(__instance, u, s, ___finalPotentialSalvage);
            }

            foreach (VehicleDef d in enemyVehicles)
            {
                log.Log($"generating salvage for vehicle {d.Chassis.Description.Name}");
                foreach (VehicleComponentRef r in d.Inventory)
                {
                    log.Log($"added salvage {r.Def.Description.Id}");
                    AddUpgradeToSalvage(__instance, r.Def, s, ___finalPotentialSalvage);
                }
            }

            if (SimpleMechAssembly_Main.Settings.StructurePointBasedSalvageTurretComponentSalvageChance > 0)
            {
                foreach (Turret t in __instance.BattleTechGame.Combat.AllEnemies.OfType<Turret>().Where(t => t.IsDead))
                {
                    log.Log($"generating salvage for turret {t.TurretDef.Description.Name}");
                    foreach (TurretComponentRef r in t.TurretDef.Inventory)
                    {
                        float rand = s.NetworkRandom.Float(0f, 1f);
                        if (rand < SimpleMechAssembly_Main.Settings.StructurePointBasedSalvageTurretComponentSalvageChance)
                        {
                            log.Log($"added salvage {r.Def.Description.Id} ({rand}<{SimpleMechAssembly_Main.Settings.StructurePointBasedSalvageTurretComponentSalvageChance})");
                            AddUpgradeToSalvage(__instance, r.Def, s, ___finalPotentialSalvage);
                        }
                        else
                        {
                            log.Log($"missed salvage {r.Def.Description.Id} ({rand}>={SimpleMechAssembly_Main.Settings.StructurePointBasedSalvageTurretComponentSalvageChance})");
                        }
                    }
                }
            }

            int salvagepotential = __instance.SalvagePotential;
            float schanc = s.Constants.Salvage.VictorySalvageChance;
            float mlos = s.Constants.Salvage.VictorySalvageLostPerMechDestroyed;
            if (__instance.State == Contract.ContractState.Failed)
            {
                schanc = s.Constants.Salvage.DefeatSalvageChance;
                mlos = s.Constants.Salvage.DefeatSalvageLostPerMechDestroyed;
            }
            else if (__instance.State == Contract.ContractState.Retreated)
            {
                schanc = s.Constants.Salvage.RetreatSalvageChance;
                mlos = s.Constants.Salvage.RetreatSalvageLostPerMechDestroyed;
            }
            float mod = schanc;
            float persalpot = salvagepotential * __instance.PercentageContractSalvage;
            if (salvagepotential > 0)
                persalpot += s.Constants.Finances.ContractFloorSalvageBonus;
            mod = Mathf.Max(0f, schanc - mlos * lostUnits.Count);
            Traverse.Create(__instance).Property("FinalSalvageCount").SetValue(Mathf.FloorToInt(persalpot * mod));
            Traverse.Create(__instance).Property("FinalPrioritySalvageCount").SetValue(Math.Min(8, Mathf.FloorToInt(__instance.FinalSalvageCount * s.Constants.Salvage.PrioritySalvageModifier)));

        }

        private static readonly ChassisLocations[] LP = new ChassisLocations[] { ChassisLocations.LeftArm, ChassisLocations.LeftLeg, ChassisLocations.LeftTorso, ChassisLocations.RightArm, ChassisLocations.RightLeg, ChassisLocations.RightTorso };
        private static readonly ChassisLocations[] HP = new ChassisLocations[] { ChassisLocations.CenterTorso };

        private static void GenerateSalvageForMech(Contract __instance, UnitResult u, SimGameState s, List<SalvageDef> ___finalPotentialSalvage)
        {
            ILog log = SimpleMechAssembly_Main.Log;
            log.Log($"generating salvage for mech {u.mech.Chassis.Description.UIName} {u.mech.Chassis.VariantName}");
            float maxstruct = 0;
            float currstruct = 0;
            foreach (ChassisLocations c in LP)
            {
                currstruct += u.mech.GetLocationLoadoutDef(c).CurrentInternalStructure * SimpleMechAssembly_Main.Settings.StructurePointBasedSalvageLowPriorityFactor;
                maxstruct += u.mech.GetChassisLocationDef(c).InternalStructure * SimpleMechAssembly_Main.Settings.StructurePointBasedSalvageLowPriorityFactor;
            }
            foreach (ChassisLocations c in HP)
            {
                currstruct += u.mech.GetLocationLoadoutDef(c).CurrentInternalStructure * SimpleMechAssembly_Main.Settings.StructurePointBasedSalvageHighPriorityFactor;
                maxstruct += u.mech.GetChassisLocationDef(c).InternalStructure * SimpleMechAssembly_Main.Settings.StructurePointBasedSalvageHighPriorityFactor;
            }
            float left = currstruct / maxstruct;
            int maxparts = Math.Min(s.Constants.Story.DefaultMechPartMax, SimpleMechAssembly_Main.Settings.StructurePointBasedSalvageMaxPartsFromMech);
            int minparts = SimpleMechAssembly_Main.Settings.StructurePointBasedSalvageMinPartsFromMech;
            float parts = left * maxparts;
            log.Log($"calculated parts {parts}, ct is {u.mech.GetChassisLocationDef(ChassisLocations.CenterTorso).InternalStructure * SimpleMechAssembly_Main.Settings.StructurePointBasedSalvageHighPriorityFactor / maxstruct} of total points");
            float fract = parts - (float) Math.Floor(parts);
            float rand = s.NetworkRandom.Float(0f, 1f);
            MechDef toSalvage = GetSalvageRedirect(s, u.mech);
            if (parts < minparts)
            {
                log.Log("below min parts, getting min parts instead");
                AddMechPartSalvage(__instance, toSalvage, s, minparts, ___finalPotentialSalvage);
            }
            else if (fract > rand)
            {
                log.Log($"rolled low on parts, getting {Math.Floor(parts)} + 1 ({fract}>{rand})");
                AddMechPartSalvage(__instance, toSalvage, s, (int) Math.Ceiling(parts), ___finalPotentialSalvage);
            }
            else
            {
                log.Log($"rolled high on parts, getting {Math.Floor(parts)} ({fract}<={rand})");
                AddMechPartSalvage(__instance, toSalvage, s, (int)Math.Ceiling(parts), ___finalPotentialSalvage);
            }

            foreach (MechComponentRef r in u.mech.Inventory)
            {
                if (r.DamageLevel != ComponentDamageLevel.Destroyed && (string.IsNullOrEmpty(s.Constants.Salvage.UniqueSalvageTag) || !r.Def.ComponentTags.Contains(s.Constants.Salvage.UniqueSalvageTag))
                    && !r.IsFixed)
                {
                    if (u.mech.GetLocationLoadoutDef(r.MountedLocation).CurrentInternalStructure > 0f)
                    {
                        log.Log($"added salvage {r.Def.Description.Id} from nondestroyed loc");
                        AddUpgradeToSalvage(__instance, r.Def, s, ___finalPotentialSalvage);
                    }
                }
            }
        }

        private static void AddMechPartSalvage(Contract __instance, MechDef d, SimGameState s, int num, List<SalvageDef> sal)
        {
            if (SimpleMechAssembly_Main.Settings.StructurePointBasedSalvageSalvageBlacklist.Contains(d.Description.Id))
            {
                SimpleMechAssembly_Main.Log.LogError("skipping, cause its blacklisted by mod.json");
                return;
            }
            object[] arg = new object[] { s.Constants, d, num, sal };
            Traverse.Create(__instance).Method("CreateAndAddMechPart", arg).GetValue(arg);
        }

        private static void AddUpgradeToSalvage(Contract __instance, MechComponentDef d, SimGameState s, List<SalvageDef> sal)
        {
            if (SimpleMechAssembly_Main.Settings.StructurePointBasedSalvageSalvageBlacklist.Contains(d.Description.Id))
            {
                SimpleMechAssembly_Main.Log.LogError("skipping, cause its blacklisted by mod.json");
                return;
            }
            try
            {
                object[] args = new object[] { sal, d, ComponentDamageLevel.Functional, false, s.Constants, s.NetworkRandom, true };
                Traverse.Create(__instance).Method("AddMechComponentToSalvage", args).GetValue(args);
            }
            catch (Exception e)
            {
                SimpleMechAssembly_Main.Log.LogError("failed to add mech component");
                SimpleMechAssembly_Main.Log.LogException(e);
                GenericPopupBuilder.Create("SMA add component error", "please report").AddButton("ok", null, true, null).Render();
            }
        }

        public static bool IsPlayerMech(MechDef m, SimGameState s)
        {
            return s.ActiveMechs.Values.Select((a) => a.GUID).Contains(m.GUID);
        }

        public static MechDef GetSalvageRedirect(SimGameState s, MechDef m)
        {
            if (SimpleMechAssembly_Main.Settings.StructurePointBasedSalvageMechPartSalvageRedirect.TryGetValue(m.Description.Id, out string red))
            {
                if (s.DataManager.MechDefs.TryGet(red, out MechDef n) && n != null)
                {
                    SimpleMechAssembly_Main.Log.Log($"mechpart salvage redirection: {m.Description.Id}->{red}");
                    return n;
                }
            }
            string c = m.MechTags.FirstOrDefault((x) => x.StartsWith("mech_MechPartSalvageRedirect_"));
            if (c != null)
            {
                c = c.Replace("mech_MechPartSalvageRedirect_", "");
                if (s.DataManager.MechDefs.TryGet(c, out MechDef n) && n != null)
                {
                    SimpleMechAssembly_Main.Log.Log($"mechpart salvage redirection: {m.Description.Id}->{c}");
                    return n;
                }
            }
            return m;
        }
    }
}
