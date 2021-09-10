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
using UnityEngine;

namespace BTSimpleMechAssembly
{
    static class SimpleMechAssembly_Main
    {
        public static SimpleMechAssembly_Settings Settings;
        public static ILog Log;

        public static int GetNumPartsForAssembly(SimGameState s, MechDef m)
        {
            Dictionary<string, bool> has = new Dictionary<string, bool>();
            int p = 0;
            foreach (MechDef d in GetAllAssemblyVariants(s, m))
            {
                if (has.ContainsKey(d.Description.Id))
                    continue;
                int v = s.GetItemCount(d.Description.Id, "MECHPART", SimGameState.ItemCountType.UNDAMAGED_ONLY);
                if (v < 0)
                {
                    Log.LogError($"warning: mechpart inventory count at {v} for {d.Description.Id}");
                }
                p += v;
                has.Add(d.Description.Id, true);
            }
            return p;
        }

        public static bool AreMechsCrossVariantCompartible(MechDef a, MechDef b)
        {
            if (Settings.CrossAssemblyExcludedMechs.Contains(a.Description.Id) && !a.Chassis.ChassisTags.Contains("chassis_ExcludeCrossAssembly"))
                return false; // a excluded
            if (Settings.CrossAssemblyExcludedMechs.Contains(b.Description.Id) && !b.Chassis.ChassisTags.Contains("chassis_ExcludeCrossAssembly"))
                return false; // b excluded
            string va = a.Chassis.GetVariant();
            string vb = b.Chassis.GetVariant();
            if (a.Chassis.ChassisTags.Contains($"chassis_CrossAssemblyAllowedWith_{vb}")
                       || a.Chassis.ChassisTags.Contains($"chassis_CrossAssemblyAllowedWith_{b.Chassis.VariantName}")
                       || b.Chassis.ChassisTags.Contains($"chassis_CrossAssemblyAllowedWith_{va}")
                       || b.Chassis.ChassisTags.Contains($"chassis_CrossAssemblyAllowedWith_{a.Chassis.VariantName}"))
                return true; // tag enabled
            if (string.IsNullOrEmpty(va) || !va.Equals(vb))
                return false; // wrong or invalid variant
            if (a.Chassis.MovementCapDef == null)
            {
                a.Chassis.RefreshMovementCaps();
                if (a.Chassis.MovementCapDef == null)
                {
                    Log.LogError(string.Format("{0} {1} (a) has no MovementCapDef, aborting speed comparison", a.Chassis.Description.UIName, a.Chassis.VariantName));
                    return false;
                }
            }
            if (b.Chassis.MovementCapDef == null)
            {
                b.Chassis.RefreshMovementCaps();
                if (b.Chassis.MovementCapDef == null)
                {
                    Log.LogError(string.Format("{0} {1} (b) has no MovementCapDef, aborting speed comparison", b.Chassis.Description.UIName, b.Chassis.VariantName));
                    return false;
                }
            }
            if (Settings.CrossAssemblySpeedMatch && (a.Chassis.MovementCapDef.MaxWalkDistance != b.Chassis.MovementCapDef.MaxWalkDistance))
                return false; // speed missmatch
            if (Settings.CrossAssemblyTonnsMatch && (a.Chassis.Tonnage != b.Chassis.Tonnage))
                return false; // tonnage missmatch
            foreach (string tag in Settings.CrossAssemblyTagsMatch)
            {
                if (a.Chassis.ChassisTags.Contains(tag) != b.Chassis.ChassisTags.Contains(tag))
                {
                    return false; // tag missmatch
                }
            }
            foreach (string it in Settings.CrossAssemblyInventoryMatch)
            {
                if (CountMechInventory(a, it) != CountMechInventory(b, it))
                    return false; // inventory mismatch
            }
            return true;
        }

        public static int CountMechInventory(MechDef d, string it)
        {
            return d.Inventory.Count((i) => i.ComponentDefID.Equals(it));
        }

        public static bool AreOmniMechsCompartible(MechDef a, MechDef b)
        {
            if (Settings.OmniMechTag == null)
                return false;
            if (!a.Chassis.ChassisTags.Contains(Settings.OmniMechTag)) // no omni
                return false;
            if (!b.Chassis.ChassisTags.Contains(Settings.OmniMechTag)) // no omni
                return false;
            if (Settings.CrossAssemblyExcludedMechs.Contains(a.Description.Id) && !a.Chassis.ChassisTags.Contains("chassis_ExcludeCrossAssembly"))
                return false; // a excluded
            if (Settings.CrossAssemblyExcludedMechs.Contains(b.Description.Id) && !b.Chassis.ChassisTags.Contains("chassis_ExcludeCrossAssembly"))
                return false; // b excluded
            string va = a.Chassis.GetVariant();
            string vb = b.Chassis.GetVariant();
            if (a.Chassis.ChassisTags.Contains($"chassis_CrossAssemblyAllowedWith_{vb}")
                       || a.Chassis.ChassisTags.Contains($"chassis_CrossAssemblyAllowedWith_{b.Chassis.VariantName}")
                       || b.Chassis.ChassisTags.Contains($"chassis_CrossAssemblyAllowedWith_{va}")
                       || b.Chassis.ChassisTags.Contains($"chassis_CrossAssemblyAllowedWith_{a.Chassis.VariantName}"))
                return true; // tag enabled
            if (a.Chassis.ChassisTags.Contains($"chassis_CrossAssemblyAllowedWith_{vb}")
                       || a.Chassis.ChassisTags.Contains($"chassis_CrossAssemblyAllowedWith_{b.Chassis.VariantName}")
                       || b.Chassis.ChassisTags.Contains($"chassis_CrossAssemblyAllowedWith_{va}")
                       || b.Chassis.ChassisTags.Contains($"chassis_CrossAssemblyAllowedWith_{a.Chassis.VariantName}"))
                return true; // tag enabled
            if (string.IsNullOrEmpty(va) || !va.Equals(vb))
                return false; // wrong or invalid variant
            return true;
        }

        public static bool AreVehicleMechsCompatible(MechDef a, MechDef b)
        {
            if (Settings.CrossAssemblyExcludedMechs.Contains(a.Description.Id) && !a.Chassis.ChassisTags.Contains("chassis_ExcludeCrossAssembly"))
                return false; // a excluded
            if (Settings.CrossAssemblyExcludedMechs.Contains(b.Description.Id) && !b.Chassis.ChassisTags.Contains("chassis_ExcludeCrossAssembly"))
                return false; // b excluded
            string va = a.Chassis.GetVariant();
            string vb = b.Chassis.GetVariant();
            return va != null && vb != null && va.Equals(vb);
        }


        public static IEnumerable<MechDef> GetAllAssemblyVariants(SimGameState s, MechDef m)
        {
            if (m.IsVehicle())
            {
                return GetAllVehicleMechVariants(s, m);
            }
            if (m.Chassis.IsOmni())
            {
                return GetAllOmniVariants(s, m);
            }
            if (IsCrossAssemblyAllowed(s) && !Settings.CrossAssemblyExcludedMechs.Contains(m.Description.Id) && !m.Chassis.ChassisTags.Contains("chassis_ExcludeCrossAssembly"))
            {
                return GetAllNonOmniVariants(s, m);
            }
            return new List<MechDef>() { m };
        }
        public static IEnumerable<MechDef> GetAllNonOmniVariants(SimGameState s, MechDef m)
        {
            yield return m;
            if (IsCrossAssemblyAllowed(s) && !Settings.CrossAssemblyExcludedMechs.Contains(m.Description.Id) && !m.Chassis.ChassisTags.Contains("chassis_ExcludeCrossAssembly"))
            {
                foreach (KeyValuePair<string, MechDef> kv in s.DataManager.MechDefs)
                {
                    if (!m.Chassis.VariantName.Equals(kv.Value.Chassis.VariantName) && !kv.Value.IsMechDefCustom() && AreMechsCrossVariantCompartible(m, kv.Value))
                        yield return kv.Value;
                }
            }
        }
        public static IEnumerable<MechDef> GetAllVehicleMechVariants(SimGameState s, MechDef m)
        {
            //FileLog.Log($"getting vehicle variants for {m.Description.Id}");
            yield return m;
            if (IsCrossAssemblyAllowed(s) && !Settings.CrossAssemblyExcludedMechs.Contains(m.Description.Id) && !m.Chassis.ChassisTags.Contains("chassis_ExcludeCrossAssembly"))
            {
                foreach (KeyValuePair<string, MechDef> kv in s.DataManager.MechDefs)
                {
                    if (!m.Chassis.Description.Id.Equals(kv.Value.Chassis.Description.Id) && kv.Value.IsVehicle() && !kv.Value.IsMechDefCustom() && AreVehicleMechsCompatible(m, kv.Value))
                        yield return kv.Value;
                }
            }
        }

        public static IEnumerable<MechDef> GetAllOmniVariants(SimGameState s, MechDef m)
        {
            if (!m.Chassis.IsOmni()) // no omni, return empty list
                yield break;
            yield return m;
            foreach (KeyValuePair<string, MechDef> kv in s.DataManager.MechDefs)
            {
                if (!m.Chassis.VariantName.Equals(kv.Value.Chassis.VariantName) && !kv.Value.IsMechDefCustom() && AreOmniMechsCompartible(m, kv.Value))
                    yield return kv.Value;
            }
        }

        public static bool IsVariantKnown(SimGameState s, MechDef d)
        {
            if (d.Chassis.ChassisTags.Contains("chassis_KnownOmniVariant"))
                return true;
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
            string id = s.GetItemStatID(d.Description.Id, "MECHPART");
            if (s.CompanyStats.ContainsStatistic(id))
            {
                return true;
            }
            id = s.GetItemStatID(d.Chassis.Description, d.GetType());
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
            if (Settings.BTXCrossAssemblyAlwaysAllowIfSimulation && s.Constants.Story.MaximumDebt >= 42)
                return true;
            return s.PurchasedArgoUpgrades.Contains(Settings.CrossAssemblyUpgradeRequired);
        }

        public static void UnStorageOmniMechPopup(SimGameState s, MechDef d, Action onClose)
        {
            if (Settings.OmniMechTag == null)
            {
                onClose?.Invoke();
                throw new InvalidOperationException("omnimechs disabled");
            }
            int mechbay = s.GetFirstFreeMechBay();
            if (mechbay < 0)
            {
                onClose?.Invoke();
                return;
            }
            IEnumerable<MechDef> mechs = GetAllOmniVariants(s, d);
            string desc = "Yang: We know the following Omni variants. What should I ready this 'Mech as?\n\n";
            GenericPopupBuilder pop = GenericPopupBuilder.Create("Ready 'Mech?", desc);
            pop.AddButton("nothing", onClose, true, null);
            foreach (MechDef m in mechs)
            {
                MechDef var = m; // new var to keep it for lambda
                if (!CheckOmniKnown(s, d, m))
                    continue;
                pop.AddButton($"{var.Chassis.VariantName}", delegate
                {
                    Log.Log("ready omni as: " + var.Description.Id);
                    s.ScrapInactiveMech(d.Chassis.Description.Id, false);
                    ReadyMech(s, new MechDef(var, s.GenerateSimGameUID(), false), mechbay);
                    onClose?.Invoke();
                }, true, null);
                int com = GetNumberOfMechsOwnedOfType(s, m);
                pop.Body += $"[[DM.MechDefs[{m.Description.Id}],{m.Chassis.Description.UIName} {m.Chassis.VariantName}]] ({com} Complete)\n";
            }
            pop.AddFader(new UIColorRef?(LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.PopupBackfill), 0f, true);
            pop.Render();
        }

        public static void ReadyMech(SimGameState s, MechDef d, int baySlot, bool donotoverridetime=false)
        {
            int mechReadyTime = s.Constants.Story.MechReadyTime;
            if (Settings.AssembledMechsReadyingFlatCost > 0 && !donotoverridetime)
            {
                mechReadyTime = Settings.AssembledMechsReadyingFlatCost + Settings.AssembledMechsReadyingPerNonFixedComponentCost * d.Inventory.Where((a) => !a.IsFixed).Count();
            }
            WorkOrderEntry_ReadyMech workOrderEntry_ReadyMech = new WorkOrderEntry_ReadyMech(string.Format("ReadyMech-{0}", d.GUID), $"Readying {d.GetMechOmniVehicle()} - {d.Chassis.Description.Name}",
                mechReadyTime, baySlot, d, string.Format(s.Constants.Story.MechReadiedWorkOrderCompletedText, new object[]
                {
                    d.Chassis.Description.Name
                }));
            s.MechLabQueue.Add(workOrderEntry_ReadyMech);
            s.ReadyingMechs[baySlot] = d;
            s.RoomManager.AddWorkQueueEntry(workOrderEntry_ReadyMech);
            s.UpdateMechLabWorkQueue(false);
            AudioEventManager.PlayAudioEvent("audioeventdef_simgame_vo_barks", "workqueue_readymech", WwiseManager.GlobalAudioObject, null);
            s.CompanyStats.ModifyStat("Mission", 0, "COMPANY_MechsAdded", StatCollection.StatOperation.Int_Add, 1, -1, true);
        }

        public static void QueryMechAssemblyPopup(SimGameState s, MechDef d, Action onClose = null)
        {
            if (GetNumPartsForAssembly(s, d) < s.Constants.Story.DefaultMechPartMax)
            {
                onClose?.Invoke();
                return;
            }
            IEnumerable<MechDef> mechs = GetAllAssemblyVariants(s, d);
            string desc = $"Yang: Concerning the [[DM.MechDefs[{d.Description.Id}],{d.Chassis.Description.UIName} {d.Chassis.VariantName}]]: {d.Chassis.YangsThoughts}\n\n We have Parts for the following {d.GetMechOmniVehicle()} variants. What should I build?\n";
            GenericPopupBuilder pop = GenericPopupBuilder.Create($"Assemble {d.GetMechOmniVehicle()}?", desc);
            pop.AddButton("-", delegate
            {
                onClose?.Invoke();
            }, true, null);
            foreach (MechDef m in mechs)
            {
                MechDef var = m; // new var to keep it for lambda
                int count = s.GetItemCount(var.Description.Id, "MECHPART", SimGameState.ItemCountType.UNDAMAGED_ONLY);
                int com = GetNumberOfMechsOwnedOfType(s, m);
                if (count <= 0 && !CheckOmniKnown(s, d, m))
                {
                    pop.Body += $"[[DM.MechDefs[{m.Description.Id}],{m.Chassis.Description.UIName} {m.Chassis.VariantName}]] (unavailable, {count} Parts/{com} Complete)\n";
                    continue;
                }
                pop.AddButton(string.Format("{0}", var.Chassis.VariantName), delegate
                {
                    PerformMechAssemblyStorePopup(s, var, onClose);
                }, true, null);
                pop.Body += $"[[DM.MechDefs[{m.Description.Id}],{m.Chassis.Description.UIName} {m.Chassis.VariantName}]] ({count} Parts/{com} Complete)\n";
            }
            pop.AddFader(new UIColorRef?(LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.PopupBackfill), 0f, true);
            pop.Render();
        }

        public static void PerformMechAssemblyStorePopup(SimGameState s, MechDef d, Action onClose)
        {
            WwiseManager.PostEvent(AudioEventList_ui.ui_sim_popup_newChassis, WwiseManager.GlobalAudioObject, null, null);
            MechDef toAdd = PerformMechAssembly(s, d);
            int mechbay;
            if (toAdd.IsVehicle())
                mechbay = CUIntegration.GetFirstFreeMechBay(s, d); // vehicle bay, +100 or something similar
            else
                mechbay = s.GetFirstFreeMechBay();
            GenericPopupBuilder pop = GenericPopupBuilder.Create($"{d.GetMechOmniVehicle()} Assembled", $"Yang: [[DM.MechDefs[{d.Description.Id}],{d.Chassis.Description.UIName} {d.Chassis.VariantName}]] finished!\n{d.Chassis.YangsThoughts}\n\n");
            pop.AddButton("storage", delegate
            {
                StoreMech(s, toAdd);
                CallMessages(s, toAdd);
                Log.Log("direct storage");
                onClose?.Invoke();
            }, true, null);
            if (mechbay < 0) // no space - direct storage
            {
                pop.Body += $"We have no space for a new {d.GetMechOmniVehicle()}, so it goes into storage.";
            }
            else
            {
                pop.Body += "Should i put it into storage or ready it for combat?";
                pop.AddButton("ready it", delegate
                {
                    if (Settings.AssembledMechsNeedReadying)
                    {
                        ReadyMech(s, toAdd, mechbay);
                        CallMessages(s, toAdd);
                    }
                    else
                    {
                        s.AddMech(mechbay, toAdd, true, false, false);
                        CallMessages(s, toAdd);
                    }
                    Log.Log("added to bay " + mechbay);
                    onClose?.Invoke();
                }, true, null);
            }
            if (IsSellingAllowed(s))
            {
                int cost = GetMechSellCost(s, toAdd);
                pop.Body += $"\n\nDarius: We could also sell it for {SimGameState.GetCBillString(cost)}, although Yang would certanly not like it.";
                pop.AddButton("sell it", delegate
                {
                    s.AddFunds(cost, "Store", true, true);
                    Log.Log("sold for " + cost);
                    s.CompanyStats.ModifyStat("Mission", 0, "COMPANY_MechsAdded", StatCollection.StatOperation.Int_Add, 1, -1, true);
                    CallMessages(s, toAdd);
                    onClose?.Invoke();
                }, true, null);
            }
            pop.AddFader(new UIColorRef?(LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.PopupBackfill), 0f, true);
            pop.Render();
        }

        private static void StoreMech(SimGameState s, MechDef d)
        {
            s.UnreadyMech(-1, d);
            s.CompanyStats.ModifyStat("Mission", 0, "COMPANY_MechsAdded", StatCollection.StatOperation.Int_Add, 1, -1, true);
        }

        private static void CallMessages(SimGameState s, MechDef d)
        {
            s.MessageCenter.PublishMessage(new SimGameMechAddedMessage(d, s.Constants.Story.DefaultMechPartMax, true));
            s.MessageCenter.PublishMessage(new SimGameMechAddedMessage(d, 0, false));
        }

        public static MechDef PerformMechAssembly(SimGameState s, MechDef d)
        {
            Log.Log("mech assembly: " + d.Description.Id);
            IEnumerable<MechDef> mechs = GetAllAssemblyVariants(s, d);
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
            return new MechDef(d, s.GenerateSimGameUID(), d.IsVehicle() || s.Constants.Salvage.EquipMechOnSalvage);
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
            if (removing > curr)
            {
                removing = curr;
                Log.LogError($"warning: tried to remove more parts than in storage (st: {curr}, req: {required}, min: {min}");
            }
            // the string variant of removeitem is private...
            for (int i = 0; i < removing; i++)
            {
                s.RemoveItemStat(d.Description.Id, "MECHPART", false);
            }
            Log.LogDebug("using parts " + d.Description.Id + " " + removing);
            return removing;
        }

        public static bool IsSellingAllowed(SimGameState s)
        {
            if (!s.CurSystem.CanUseSystemStore())
                return false;
            if (s.TravelState != SimGameTravelStatus.IN_SYSTEM)
                return false;
            return true;
        }

        public static int GetMechSellCost(SimGameState s, MechDef m)
        {
            int c = m.Chassis.Description.Cost;
            if (m.IsVehicle())
                return c;
            foreach (MechComponentRef r in m.Inventory)
            {
                if (!r.IsFixed)
                {
                    c += r.Def.Description.Cost;
                }
            }
            c = Mathf.FloorToInt(c * s.Constants.Finances.ShopSellModifier);
            return c;
        }

        public static string GetMechCountDescrString(SimGameState s, MechDef d)
        {
            int pieces = s.GetItemCount(d.Description.Id, "MECHPART", SimGameState.ItemCountType.UNDAMAGED_ONLY);
            int needed = s.Constants.Story.DefaultMechPartMax;
            int varpieces = SimpleMechAssembly_Main.GetNumPartsForAssembly(s, d);
            int owned = SimpleMechAssembly_Main.GetNumberOfMechsOwnedOfType(s, d);
            string ownedorknown;
            if (owned == 0 && d.Chassis.IsOmni() && IsVariantKnown(s, d))
                ownedorknown = "K";
            else
                ownedorknown = owned.ToString();
            return $"{pieces}({varpieces})/{ownedorknown}({needed})";
        }

        public class SimpleMechAssembly_InterruptManager_AssembleMechEntry : SimGameInterruptManager.Entry
        {
            public readonly SimGameState s;
            public readonly MechDef d;
            public readonly Action onClose;

            public SimpleMechAssembly_InterruptManager_AssembleMechEntry(SimGameState s, MechDef d, Action onClose)
            {
                type = SimGameInterruptManager.InterruptType.GenericPopup;
                this.s = s;
                this.d = d;
                this.onClose = (onClose == null) ? Close : (onClose + Close);
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
                QueryMechAssemblyPopup(s, d, onClose);
            }
        }

        public class SimpleMechAssembly_InterruptManager_UnStorageOmniEntry : SimGameInterruptManager.Entry
        {
            public readonly SimGameState s;
            public readonly MechDef d;
            public readonly Action onClose;

            public SimpleMechAssembly_InterruptManager_UnStorageOmniEntry(SimGameState s, MechDef d, Action onClose)
            {
                type = SimGameInterruptManager.InterruptType.GenericPopup;
                this.s = s;
                this.d = d;
                this.onClose = (onClose == null) ? Close : (onClose + Close);
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
                UnStorageOmniMechPopup(s, d, onClose);
            }
        }
    }
}
