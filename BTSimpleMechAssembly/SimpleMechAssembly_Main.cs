using BattleTech;
using BattleTech.UI;
using Harmony;
using HBS.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BTSimpleMechAssembly
{
    class SimpleMechAssembly_Main
    {
        public static SimpleMechAssembly_Settings Settings;
        public static ILog Log;

        public static int GetNumPartsForAssembly(SimGameState s, MechDef m)
        {
            List<MechDef> vars = GetAllAssemblyVariants(s, m);
            int p = 0;
            foreach (MechDef d in vars)
            {
                p += s.GetItemCount(d.Description.Id, "MECHPART", SimGameState.ItemCountType.UNDAMAGED_ONLY);
            }
            return p;
        }

        public static List<MechDef> GetAllAssemblyVariants(SimGameState s, MechDef m)
        {
            List<MechDef> r = new List<MechDef>();
            r.Add(m);
            if (IsCrossAssemblyAllowed(s) && !Settings.CrossAssemblyExcludedMechs.Contains(m.Description.Id) && !m.Chassis.ChassisTags.Contains("chassis_ExcludeCrossAssembly"))
            {
                foreach (KeyValuePair<string, MechDef> kv in s.DataManager.MechDefs)
                {
                    if (m.Description.Id.Equals(kv.Value.Description.Id))
                        continue; // base variant
                    if (string.IsNullOrEmpty(kv.Value.Chassis.Description.UIName) || !kv.Value.Chassis.Description.UIName.Equals(m.Chassis.Description.UIName))
                        continue; // wrong or invalid variant
                    if (m.Chassis.MovementCapDef==null)
                    {
                        Log.LogError(string.Format("{0} {1} (m) has no MovementCapDef, aborting speed comparison", m.Chassis.Description.UIName, m.Chassis.VariantName));
                        continue;
                    }
                    if (kv.Value.Chassis.MovementCapDef == null)
                    {
                        Log.LogError(string.Format("{0} {1} (kv.Value) has no MovementCapDef, aborting speed comparison", kv.Value.Chassis.Description.UIName, kv.Value.Chassis.VariantName));
                        continue;
                    }
                    if (Settings.CrossAssemblySpeedMatch && (m.Chassis.MovementCapDef.MaxWalkDistance != kv.Value.Chassis.MovementCapDef.MaxWalkDistance))
                        continue; // speed missmatch
                    if (Settings.CrossAssemblyTonnsMatch && (m.Chassis.Tonnage != kv.Value.Chassis.Tonnage))
                        continue; // tonnage missmatch
                    bool cont = false;
                    foreach (string tag in Settings.CrossAssemblyTagsMatch)
                    {
                        if (m.Chassis.ChassisTags.Contains(tag) != kv.Value.Chassis.ChassisTags.Contains(tag))
                        {
                            cont = true;
                            break;
                        }
                    }
                    if (cont)
                        continue; // tag mismatch (endo/ferro)
                    if (Settings.CrossAssemblyExcludedMechs.Contains(kv.Value.Description.Id))
                        continue; // excluded
                    if (kv.Value.Chassis.ChassisTags.Contains("chassis_ExcludeCrossAssembly"))
                        continue;
                    r.Add(kv.Value);
                }
            }
            if (Settings.OmniMechTag != null && m.Chassis.ChassisTags.Contains(Settings.OmniMechTag))
            {
                r = r.Union(GetAllOmniVariants(s, m)).ToList();
            }

            return r;
        }

        public static List<MechDef> GetAllOmniVariants(SimGameState s, MechDef m)
        {
            List<MechDef> r = new List<MechDef>();
            if (Settings.OmniMechTag == null)
                return r;
            if (!m.Chassis.ChassisTags.Contains(Settings.OmniMechTag)) // no omni, return empty list
                return r;
            r.Add(m);
            if (Settings.CrossAssemblyExcludedMechs.Contains(m.Description.Id) || m.Chassis.ChassisTags.Contains("chassis_ExcludeCrossAssembly")) // excluded
                return r;
            foreach (KeyValuePair<string, MechDef> kv in s.DataManager.MechDefs)
            {
                if (m.Description.Id.Equals(kv.Value.Description.Id))
                    continue; // base variant
                if (string.IsNullOrEmpty(kv.Value.Chassis.Description.UIName) || !kv.Value.Chassis.Description.UIName.Equals(m.Chassis.Description.UIName))
                    continue; // wrong or invalid variant
                if (Settings.CrossAssemblyExcludedMechs.Contains(kv.Value.Description.Id))
                    continue; // excluded
                if (kv.Value.Chassis.ChassisTags.Contains("chassis_ExcludeCrossAssembly"))
                    continue;
                if (!kv.Value.Chassis.ChassisTags.Contains(Settings.OmniMechTag)) // no omni
                    continue;
                r.Add(kv.Value);
            }
            return r;
        }

        public static bool IsVariantKnown(SimGameState s, MechDef d)
        {
            foreach (KeyValuePair<int, MechDef> a in s.ActiveMechs)
            {
                if (d.ChassisID == a.Value.ChassisID)
                {
                    return true;
                }
            }
            foreach (KeyValuePair<int, MechDef> a in s.ReadyingMechs)
            {
                if (d.ChassisID == a.Value.ChassisID)
                {
                    return true;
                }
            }
            Traverse c = Traverse.Create(s);
            object[] args = new object[] { d.Chassis.Description.Id, "MECHPART" };
            string id = c.Method("GetItemStatID", args).GetValue<string>(args);
            if (s.CompanyStats.ContainsStatistic(id))
            {
                return true;
            }
            args[1] = d.GetType();
            id = c.Method("GetItemStatID", args).GetValue<string>(args);
            if (s.CompanyStats.ContainsStatistic(id))
            {
                return true;
            }
            return false;
        }

        private static bool CheckOmniKnown(SimGameState s, MechDef baseV, MechDef variant)
        {
            return Settings.OmniMechTag != null && baseV.Chassis.ChassisTags.Contains(Settings.OmniMechTag) && variant.Chassis.ChassisTags.Contains(Settings.OmniMechTag) && IsVariantKnown(s, variant);
        }

        public static int GetNumberOfMechsOwnedOfType(SimGameState s, MechDef m)
        {
            int com = s.GetItemCount(m.Chassis.Description.Id, m.GetType(), SimGameState.ItemCountType.UNDAMAGED_ONLY);
            foreach (KeyValuePair<int, MechDef> a in s.ActiveMechs)
            {
                if (m.ChassisID == a.Value.ChassisID)
                {
                    com++;
                }
            }
            foreach (KeyValuePair<int, MechDef> a in s.ReadyingMechs)
            {
                if (m.ChassisID == a.Value.ChassisID)
                {
                    com++;
                }
            }
            return com;
        }

        public static bool IsCrossAssemblyAllowed(SimGameState s)
        {
            if (Settings.CrossAssemblyUpgradeRequired==null)
                return true;
            if (Settings.CrossAssemblyAlwaysAllowIfSimulation && s.Constants.Story.MaximumDebt == 42)
                return true;
            return s.PurchasedArgoUpgrades.Contains(Settings.CrossAssemblyUpgradeRequired);
        }

        public static void UnStorageOmniMechPopup(SimGameState s, MechDef d, MechBayPanel refresh)
        {
            if (Settings.OmniMechTag == null)
                throw new InvalidOperationException("omnimechs disabled");
            int mechbay = s.GetFirstFreeMechBay();
            if (mechbay < 0)
                return;
            List<MechDef> mechs = GetAllOmniVariants(s, d);
            string desc = "Yang: We know the following Omni variants. What should i ready this mech as?\n\n";
            foreach (MechDef m in mechs)
            {
                if (!CheckOmniKnown(s, d, m))
                    continue;
                int com = GetNumberOfMechsOwnedOfType(s, m);
                desc += string.Format("{0} {1} ({2} Complete)\n", m.Chassis.Description.UIName, m.Chassis.VariantName, com);
            }
            GenericPopupBuilder pop = GenericPopupBuilder.Create("Ready Mech?", desc);
            pop.AddButton("nothing", null, true, null);
            foreach (MechDef m in mechs)
            {
                MechDef var = m; // new var to keep it for lambda
                if (!CheckOmniKnown(s, d, m))
                    continue;
                pop.AddButton(string.Format("{0} {1}", var.Chassis.Description.UIName, var.Chassis.VariantName), delegate
                {
                    Log.Log("ready omni as: " + var.Description.Id);
                    s.ScrapInactiveMech(d.Chassis.Description.Id, false);
                    ReadyMech(s, new MechDef(var, s.GenerateSimGameUID(), false), mechbay);
                    if (refresh!=null)
                    {
                        refresh.RefreshData(false);
                        refresh.ViewBays();
                    }
                }, true, null);
            }
            pop.Render();
        }

        public static void ReadyMech(SimGameState s, MechDef d, int baySlot)
        {
            WorkOrderEntry_ReadyMech workOrderEntry_ReadyMech = new WorkOrderEntry_ReadyMech(string.Format("ReadyMech-{0}", d.GUID), string.Format("Readying 'Mech - {0}", new object[]
                {
                    d.Chassis.Description.Name
                }), s.Constants.Story.MechReadyTime, baySlot, d, string.Format(s.Constants.Story.MechReadiedWorkOrderCompletedText, new object[]
                {
                    d.Chassis.Description.Name
                }));
            s.MechLabQueue.Add(workOrderEntry_ReadyMech);
            s.ReadyingMechs[baySlot] = d;
            s.RoomManager.AddWorkQueueEntry(workOrderEntry_ReadyMech);
            s.UpdateMechLabWorkQueue(false);
            AudioEventManager.PlayAudioEvent("audioeventdef_simgame_vo_barks", "workqueue_readymech", WwiseManager.GlobalAudioObject, null);
        }

        public static void QueryMechAssemblyPopup(SimGameState s, MechDef d, MechBayPanel refresh = null)
        {
            if (GetNumPartsForAssembly(s, d) < s.Constants.Story.DefaultMechPartMax)
                return;
            List<MechDef> mechs = GetAllAssemblyVariants(s, d);
            string desc = "Yang: We have Parts for the following mech variants. What should i build?\n\n";
            foreach (MechDef m in mechs)
            {
                int count = s.GetItemCount(m.Description.Id, "MECHPART", SimGameState.ItemCountType.UNDAMAGED_ONLY);
                if (count <= 0)
                    continue;
                int com = GetNumberOfMechsOwnedOfType(s, m);
                desc += string.Format("{0} {1} ({2} Parts/{3} Complete)\n", m.Chassis.Description.UIName, m.Chassis.VariantName, count, com);
            }
            GenericPopupBuilder pop = GenericPopupBuilder.Create("Assemble Mech?", desc);
            pop.AddButton("nothing", null, true, null);
            foreach (MechDef m in mechs)
            {
                MechDef var = m; // new var to keep it for lambda
                int count = s.GetItemCount(var.Description.Id, "MECHPART", SimGameState.ItemCountType.UNDAMAGED_ONLY);
                if (count <= 0 && !CheckOmniKnown(s, d, m))
                    continue;
                pop.AddButton(string.Format("{0} {1}", var.Chassis.Description.UIName, var.Chassis.VariantName), delegate
                {
                    PerformMechAssemblyStorePopup(s, var, refresh);
                }, true, null);
            }
            pop.Render();
        }

        public static void PerformMechAssemblyStorePopup(SimGameState s, MechDef d, MechBayPanel refresh)
        {
            MechDef toAdd = PerformMechAssembly(s, d);
            int mechbay = s.GetFirstFreeMechBay();
            if (mechbay < 0) // no space - direct storage
            {
                StoreMech(s, toAdd);
                Log.Log("no space, direct storage");
                if (refresh != null)
                    refresh.RefreshData(false);
                GenericPopupBuilder pop = GenericPopupBuilder.Create("Mech Assembled", string.Format("Yang: {0}\n\nWe have no space for a new mech, so it goes into storage.", d.Chassis.YangsThoughts));
                pop.AddButton("ok", null, true, null);
                pop.Render();
            }
            else
            {
                GenericPopupBuilder pop = GenericPopupBuilder.Create("Mech Assembled", string.Format("Yang: {0}\n\nShould i put it into storage or ready it for combat?.", d.Chassis.YangsThoughts));
                pop.AddButton("storage", delegate
                {
                    StoreMech(s, toAdd);
                    Log.Log("direct storage");
                    if (refresh != null)
                        refresh.RefreshData(false);
                }, true, null);
                pop.AddButton("ready it", delegate
                {
                    if (Settings.AssembledMechsNeedReadying)
                        ReadyMech(s, toAdd, mechbay);
                    else
                        s.AddMech(mechbay, toAdd, true, false, false);
                    Log.Log("added to bay " + mechbay);
                    if (refresh != null)
                        refresh.RefreshData(false);
                }, true, null);
                pop.Render();
            }
        }

        private static void StoreMech(SimGameState s, MechDef d)
        {
            s.UnreadyMech(-1, d);
            s.CompanyStats.ModifyStat<int>("Mission", 0, "COMPANY_MechsAdded", StatCollection.StatOperation.Int_Add, 1, -1, true);
        }

        public static MechDef PerformMechAssembly(SimGameState s, MechDef d)
        {
            Log.Log("mech assembly: " + d.Description.Id);
            List<MechDef> mechs = GetAllAssemblyVariants(s, d);
            int requiredParts = s.Constants.Story.DefaultMechPartMax;
            requiredParts -= MechAssemblyRemoveParts(s, d, requiredParts, 0); // use all base variant parts
            if (requiredParts > 0)
            {
                foreach (MechDef v in mechs)
                {
                    requiredParts -= MechAssemblyRemoveParts(s, v, requiredParts, 1); // try to leave 1 part of each variant
                }
            }
            if (requiredParts > 0)
            {
                foreach (MechDef v in mechs)
                {
                    requiredParts -= MechAssemblyRemoveParts(s, v, requiredParts, 0); // use last part of variant
                }
            }
            if (requiredParts > 0)
                throw new InvalidOperationException("not enough parts! your parts are now lost!"); // should never happen, we checked before if we have enough
            return new MechDef(d, s.GenerateSimGameUID(), s.Constants.Salvage.EquipMechOnSalvage);
        }

        private static int MechAssemblyRemoveParts(SimGameState s, MechDef d, int required, int min)
        {
            int curr = s.GetItemCount(d.Description.Id, "MECHPART", SimGameState.ItemCountType.UNDAMAGED_ONLY);
            int removing = required;
            if (curr < required)
                removing = curr;
            if ((curr - removing) < min)
                removing -= min - (curr - removing);
            if (removing < 0)
                removing = 0;
            // the string variant of removeitem is private...
            //string stat = string.Format("{0}.{1}.{2}", "Item", "MECHPART", d.Description.Id);
            object[] args = new object[] { d.Description.Id, "MECHPART", false };
            Traverse method = Traverse.Create(s).Method("RemoveItemStat", args);
            for (int i = 0; i < removing; i++)
            {
                //s.CompanyStats.ModifyStat("SimGameState", 0, stat, StatCollection.StatOperation.Int_Subtract, 1, -1, true);
               method.GetValue(args);
            }
            Log.LogDebug("using parts " + d.Description.Id + " " + removing);
            return removing;
        }
    }
}
