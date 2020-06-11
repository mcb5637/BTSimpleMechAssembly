using BattleTech;
using Harmony;
using HBS.Logging;
using HBS.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BTSimpleMechAssembly
{
    class SimpleMechAssembly_StructurePointBasedSalvage
    {
        public static bool Prefix(Contract __instance, List<UnitResult> enemyMechs, List<VehicleDef> enemyVehicles, List<UnitResult> lostUnits,
            ref List<SalvageDef> ___finalPotentialSalvage)
        {
            ILog log = SimpleMechAssembly_Main.Log;
            if (__instance.BattleTechGame.Simulation == null)
            {
                log.LogError("trying to generarte salvage without a simgame");
                return false;
            }
            SimGameState s = __instance.BattleTechGame.Simulation;
            ___finalPotentialSalvage = new List<SalvageDef>();
            Traverse trav = Traverse.Create(__instance);
            trav.Property("SalvagedChassis").SetValue(new List<SalvageDef>());
            trav.Property("LostMechs").SetValue(new List<MechDef>());
            trav.Property("SalvageResults").SetValue(new List<SalvageDef>());

            foreach (UnitResult u in lostUnits)
            {
                log.Log(string.Format("player mech {0} was lost", u.mech.Description.Name));
                if (u.mech.GetLocationLoadoutDef(ChassisLocations.CenterTorso).CurrentInternalStructure > 0f)
                {
                    log.Log(string.Format("ct not desytroyed, auto recovery"));
                    u.mechLost = false;
                }
                else if (s.NetworkRandom.Float(0f, 1f) <= s.Constants.Salvage.DestroyedMechRecoveryChance)
                {
                    log.Log(string.Format("recovery roll succeeded"));
                    u.mechLost = false;
                }
                else
                {
                    log.Log(string.Format("recovery roll failed, unit goes into salvage pool"));
                    u.mechLost = true;
                    GenerateSalvageForMech(__instance, u, s, ___finalPotentialSalvage);
                }
            }


            foreach (UnitResult u in enemyMechs)
            {
                if (!(u.pilot.IsIncapacitated || u.mech.IsDestroyed || u.mech.Inventory.Any((x) => x.Def != null && x.Def.CriticalComponent && x.DamageLevel == ComponentDamageLevel.Destroyed)))
                    continue;
                GenerateSalvageForMech(__instance, u, s, ___finalPotentialSalvage);
            }

            foreach (VehicleDef d in enemyVehicles)
            {
                foreach (VehicleComponentRef r in d.Inventory)
                {
                    AddUpgradeToSalvage(__instance, r.Def, s, ___finalPotentialSalvage);
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

            return false;
        }

        private static readonly ChassisLocations[] LP = new ChassisLocations[] { ChassisLocations.LeftArm, ChassisLocations.LeftLeg, ChassisLocations.LeftTorso, ChassisLocations.RightArm, ChassisLocations.RightLeg, ChassisLocations.RightTorso };
        private static readonly ChassisLocations[] HP = new ChassisLocations[] { ChassisLocations.CenterTorso };

        private static void GenerateSalvageForMech(Contract __instance, UnitResult u, SimGameState s, List<SalvageDef> ___finalPotentialSalvage)
        {
            ILog log = SimpleMechAssembly_Main.Log;
            log.Log("generating salvage for mech " + u.mech.Description.Name);
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
            int minparts = 1;
            float parts = left * maxparts;
            log.Log(string.Format("calculated parts {0}", parts));
            float fract = parts - (float) Math.Floor(parts);
            float rand = s.NetworkRandom.Float(0f, 1f);
            if (parts < minparts)
            {
                log.Log(string.Format("below min parts, getting min parts instead"));
                AddMechPartSalvage(__instance, u.mech, s, minparts, ___finalPotentialSalvage);
            }
            else if (fract > rand)
            {
                log.Log(string.Format("rolled low on parts, getting {0} + 1 ({1}>{2})", Math.Floor(parts), fract, rand));
                AddMechPartSalvage(__instance, u.mech, s, (int) Math.Ceiling(parts), ___finalPotentialSalvage);
            }
            else
            {
                log.Log(string.Format("rolled high on parts, getting {0} ({1}<={2})", Math.Floor(parts), fract, rand));
                AddMechPartSalvage(__instance, u.mech, s, (int)Math.Ceiling(parts), ___finalPotentialSalvage);
            }

            foreach (MechComponentRef r in u.mech.Inventory)
            {
                if (r.DamageLevel != ComponentDamageLevel.Destroyed && (string.IsNullOrEmpty(s.Constants.Salvage.UniqueSalvageTag) || !r.Def.ComponentTags.Contains(s.Constants.Salvage.UniqueSalvageTag))
                    && !r.IsFixed)
                {
                    if (u.mech.GetLocationLoadoutDef(r.MountedLocation).CurrentInternalStructure > 0f)
                    {
                        log.Log(string.Format("added salvage {0} from nondestroyed loc", r.Def.Description.Id));
                        AddUpgradeToSalvage(__instance, r.Def, s, ___finalPotentialSalvage);
                    }
                }
            }
        }

        private static void AddMechPartSalvage(Contract __instance, MechDef d, SimGameState s, int num, List<SalvageDef> sal)
        {
            object[] arg = new object[] { s.Constants, d, num, sal };
            Traverse.Create(__instance).Method("CreateAndAddMechPart", arg).GetValue(arg);
        }

        private static void AddUpgradeToSalvage(Contract __instance, MechComponentDef d, SimGameState s, List<SalvageDef> sal)
        {
            if (SimpleMechAssembly_Main.Settings.StructurePointBasedSalvageVanillaComponents)
            {
                object[] args = new object[] { sal, d, ComponentDamageLevel.Functional, false, s.Constants, s.NetworkRandom, true };
                Traverse.Create(__instance).Method("AddMechComponentToSalvage", args).GetValue(args);
                return;
            }

            if (d.ComponentTags.Contains("BLACKLISTED"))
            {
                SimpleMechAssembly_Main.Log.LogError("skipping, cause its blacklisted");
                return;
            }
            SalvageDef salvageDef = new SalvageDef();
            salvageDef.MechComponentDef = d;
            salvageDef.Description = new DescriptionDef(d.Description);
            salvageDef.RewardID = __instance.GenerateRewardUID();
            salvageDef.Type = SalvageDef.SalvageType.COMPONENT;
            salvageDef.ComponentType = d.ComponentType;
            salvageDef.Damaged = false;
            salvageDef.Weight = s.Constants.Salvage.DefaultComponentWeight;
            salvageDef.Count = 1;
            object[] arg = new object[] { salvageDef };
            if (Traverse.Create(__instance).Method("IsSalvageInContent", arg).GetValue<bool>(arg))
            {
                sal.Add(salvageDef);
                //Traverse.Create(__instance).Method("AddToFinalSalvage", arg).GetValue(arg);
            }
            else
                SimpleMechAssembly_Main.Log.LogError("failed to add upgrade");
        }
    }
}
