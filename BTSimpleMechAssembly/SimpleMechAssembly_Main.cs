using BattleTech;
using BattleTech.UI;
using Harmony;
using HBS;
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
            Dictionary<string, bool> has = new Dictionary<string, bool>();
            int p = 0;
            foreach (MechDef d in vars)
            {
                if (has.ContainsKey(d.Description.Id))
                    continue;
                p += s.GetItemCount(d.Description.Id, "MECHPART", SimGameState.ItemCountType.UNDAMAGED_ONLY);
                has.Add(d.Description.Id, true);
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
                    if (Settings.CrossAssemblyExcludedMechs.Contains(kv.Value.Description.Id))
                        continue; // excluded
                    if (kv.Value.Chassis.ChassisTags.Contains("chassis_ExcludeCrossAssembly"))
                        continue;
                    if (m.Chassis.ChassisTags.Contains($"chassis_CrossAssemblyAllowedWith_{kv.Value.Chassis.Description.UIName}")
                        || m.Chassis.ChassisTags.Contains($"chassis_CrossAssemblyAllowedWith_{kv.Value.Chassis.VariantName}")
                        || kv.Value.Chassis.ChassisTags.Contains($"chassis_CrossAssemblyAllowedWith_{m.Chassis.Description.UIName}")
                        || kv.Value.Chassis.ChassisTags.Contains($"chassis_CrossAssemblyAllowedWith_{m.Chassis.VariantName}"))
                    {
                        r.Add(kv.Value);
                        continue;
                    }
                    if (string.IsNullOrEmpty(kv.Value.Chassis.Description.UIName) || !kv.Value.Chassis.Description.UIName.Equals(m.Chassis.Description.UIName))
                        continue; // wrong or invalid variant
                    if (m.Chassis.MovementCapDef==null)
                    {
                        m.Chassis.RefreshMovementCaps();
                        if (m.Chassis.MovementCapDef == null)
                        {
                            Log.LogError(string.Format("{0} {1} (m) has no MovementCapDef, aborting speed comparison", m.Chassis.Description.UIName, m.Chassis.VariantName));
                            continue;
                        }
                    }
                    if (kv.Value.Chassis.MovementCapDef == null)
                    {
                        kv.Value.Chassis.RefreshMovementCaps();
                        if (kv.Value.Chassis.MovementCapDef == null)
                        {
                            Log.LogError(string.Format("{0} {1} (kv.Value) has no MovementCapDef, aborting speed comparison", kv.Value.Chassis.Description.UIName, kv.Value.Chassis.VariantName));
                            continue;
                        }
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
                if (Settings.CrossAssemblyExcludedMechs.Contains(kv.Value.Description.Id))
                    continue; // excluded
                if (kv.Value.Chassis.ChassisTags.Contains("chassis_ExcludeCrossAssembly"))
                    continue;
                if (m.Chassis.ChassisTags.Contains($"chassis_CrossAssemblyAllowedWith_{kv.Value.Chassis.Description.UIName}")
                    || m.Chassis.ChassisTags.Contains($"chassis_CrossAssemblyAllowedWith_{kv.Value.Chassis.VariantName}")
                    || kv.Value.Chassis.ChassisTags.Contains($"chassis_CrossAssemblyAllowedWith_{m.Chassis.Description.UIName}")
                    || kv.Value.Chassis.ChassisTags.Contains($"chassis_CrossAssemblyAllowedWith_{m.Chassis.VariantName}"))
                {
                    r.Add(kv.Value);
                    continue;
                }
                if (string.IsNullOrEmpty(kv.Value.Chassis.Description.UIName) || !kv.Value.Chassis.Description.UIName.Equals(m.Chassis.Description.UIName))
                    continue; // wrong or invalid variant
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
            string desc = "Yang: We know the following Omni variants. Which should I build?\n\n";
            foreach (MechDef m in mechs)
            {
                if (!CheckOmniKnown(s, d, m))
                    continue;
                int com = GetNumberOfMechsOwnedOfType(s, m);
                desc += string.Format("[[DM.MechDefs[{3}],{0} {1}]] ({2} Complete)\n", m.Chassis.Description.UIName, m.Chassis.VariantName, com, m.Description.Id);
            }
            GenericPopupBuilder pop = GenericPopupBuilder.Create("Ready Mech?", desc);
            pop.AddButton("-", null, true, null);
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
            pop.AddFader(new UIColorRef?(LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.PopupBackfill), 0f, true);
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

        public static void VariantPopup(string desc, SimGameState s, IEnumerable<MechDef> variantsToList, MechBayPanel refresh = null, SimpleMechAssembly_InterruptManager_AssembleMechEntry close = null)
        {
            GenericPopupBuilder pop = GenericPopupBuilder.Create("Assemble Mech?", desc);
            bool hasMultipleVariants = variantsToList.Count() > 1;
            string closeButtonText = hasMultipleVariants ? "-" : "Not now";
            
            pop.AddButton(closeButtonText, delegate
            {
                if (close != null)
                    close.NewClose();
            }, true, null);


            foreach (MechDef m in variantsToList)
            {
                string buttonText = hasMultipleVariants ? string.Format("{0}", m.Chassis.VariantName) : "Yes";
                pop.AddButton(buttonText, delegate
                {
                    PerformMechAssemblyStorePopup(s, m, refresh, close);
                }, true, null);
            }

            pop.CancelOnEscape();
            pop.AddFader(new UIColorRef?(LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.PopupBackfill), 0f, true);
            pop.Render();
        }

        public static void QueryMechAssemblyPopup(SimGameState s, MechDef d, MechBayPanel refresh = null, SimpleMechAssembly_InterruptManager_AssembleMechEntry close =null)
        {
            if (GetNumPartsForAssembly(s, d) < s.Constants.Story.DefaultMechPartMax)
            {
                if (close != null)
                    close.NewClose();
                return;
            }
            IEnumerable<MechDef> ownedMechVariantParts = GetAllAssemblyVariants(s, d)
                .Where(m => s.GetItemCount(
                    m.Description.Id, "MECHPART", SimGameState.ItemCountType.UNDAMAGED_ONLY) > 0 || CheckOmniKnown(s, d, m));

            int selectedMechParts = s.GetItemCount(d.Description.Id, "MECHPART", SimGameState.ItemCountType.UNDAMAGED_ONLY);
            int numberOfChassisOwned = GetNumberOfMechsOwnedOfType(s, d);
            string desc = $"Yang: {d.Chassis.YangsThoughts}\n\n";

            IEnumerable<string> additionalVariants = ownedMechVariantParts
                .Where(m => m.Description.Id != d.Description.Id)
                .Select(m  =>
                {
                    int count = s.GetItemCount(m.Description.Id, "MECHPART", SimGameState.ItemCountType.UNDAMAGED_ONLY);
                    int com = GetNumberOfMechsOwnedOfType(s, m);
                    return $"[[DM.MechDefs[{m.Description.Id}], {m.Chassis.VariantName}]] ({count} Parts/{com} Complete)";
                });

            string selectedMechDisplayTitle = $"[[DM.MechDefs[{d.Description.Id}],{d.Chassis.VariantName}]] ({selectedMechParts} Parts/{numberOfChassisOwned} Complete)";

            if (additionalVariants.Count() > 1)
            {
                desc += $"We can build the {selectedMechDisplayTitle}, " +
                    $"or one of these other variants of the {d.Chassis.Description.UIName}:\n\n";
                desc += string.Join("\n", additionalVariants);
                VariantPopup(desc, s, ownedMechVariantParts, refresh, close);
            } else if (additionalVariants.Count() == 1)
            {
                desc += $"We can build the {selectedMechDisplayTitle}, or the\n" +
                    $"{string.Join("", additionalVariants)} variant of the {d.Chassis.Description.UIName}.";
                VariantPopup(desc, s, ownedMechVariantParts, refresh, close);
            }
            else
            {
                desc += $"Should I build the {selectedMechDisplayTitle}?";
                VariantPopup(desc, s, new[] { d }, refresh, close);
            }
        }

        public static void PerformMechAssemblyStorePopup(SimGameState s, MechDef d, MechBayPanel refresh, SimpleMechAssembly_InterruptManager_AssembleMechEntry close)
        {
            MechDef toAdd = PerformMechAssembly(s, d);
            int mechbay = s.GetFirstFreeMechBay();
            if (mechbay < 0) // no space - direct storage
            {
                StoreMech(s, toAdd);
                Log.Log("no space, direct storage");
                if (refresh != null)
                    refresh.RefreshData(false);
                GenericPopupBuilder pop = GenericPopupBuilder.Create("Mech Assembled",
                    string.Format("Yang: [[DM.MechDefs[{3}],{1} {2}]] finished!\n{0}\n\nWe have no space for a new mech, so it goes into storage.",
                    d.Chassis.YangsThoughts, d.Chassis.Description.UIName, d.Chassis.VariantName, d.Description.Id));
                pop.AddButton("ok", delegate
                {
                    if (close != null)
                        close.NewClose();
                }, true, null);
                pop.AddFader(new UIColorRef?(LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.PopupBackfill), 0f, true);
                pop.Render();
            }
            else
            {
                GenericPopupBuilder pop = GenericPopupBuilder.Create("Mech Assembled",
                    string.Format("Yang: [[DM.MechDefs[{3}],{1} {2}]] finished!\n{0}\n\nShould I put it into storage, or ready it for combat?",
                    d.Chassis.YangsThoughts, d.Chassis.Description.UIName, d.Chassis.VariantName, d.Description.Id));
                pop.AddButton("storage", delegate
                {
                    StoreMech(s, toAdd);
                    Log.Log("direct storage");
                    if (refresh != null)
                        refresh.RefreshData(false);
                    if (close != null)
                        close.NewClose();
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
                    if (close != null)
                        close.NewClose();
                }, true, null);
                pop.AddFader(new UIColorRef?(LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.PopupBackfill), 0f, true);
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

        public class SimpleMechAssembly_InterruptManager_AssembleMechEntry : SimGameInterruptManager.Entry
        {
            public readonly SimGameState s;
            public readonly MechDef d;
            public readonly MechBayPanel refresh;

            public SimpleMechAssembly_InterruptManager_AssembleMechEntry(SimGameState s, MechDef d, MechBayPanel refresh)
            {
                type = SimGameInterruptManager.InterruptType.GenericPopup;
                this.s = s;
                this.d = d;
                this.refresh = refresh;
            }

            public override bool IsUnique()
            {
                return false;
            }

            public override bool IsVisible()
            {
                return true;
            }

            public override bool NeedsFader()
            {
                return false;
            }

            public override void Render()
            {
                QueryMechAssemblyPopup(s, d, refresh, this);
            }

            public void NewClose()
            {
                Traverse.Create(this).Method("Close").GetValue();
            }
        }
    }
}
