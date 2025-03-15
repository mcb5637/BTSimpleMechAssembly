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
    static class Assembly
    {
        public static Settings Settings;
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

        public static bool IsCrossAssemblyExcluded(this MechDef d)
        {
            if (!Settings.UseOnlyCCAssemblyOptions)
            {
                if (Settings.CrossAssemblyExcludedMechs.Contains(d.Description.Id))
                    return true;
                if (d.Chassis.ChassisTags.Contains("chassis_ExcludeCrossAssembly"))
                    return true;
            }
            if (d.IsVehicle())
            {
                VehicleChassisDef vd = d.Chassis.GetVehicleChassisDefFromFakeVehicle();
                if (vd == null)
                    return false;
                IVAssemblyVariant vv = CCIntegration.GetCCVehicleAssemblyVariant(vd);
                if (vv == null)
                    return false;
                return vv.Exclude;
            }
            IAssemblyVariant v = CCIntegration.GetCCAssemblyVariant(d.Chassis);
            if (v != null)
            {
                return v.Exclude;
            }
            return false;
        }

        public static bool IsCrossAssemblyOverrideEnabled(MechDef a, MechDef b, string va, string vb)
        {
            if (!Settings.UseOnlyCCAssemblyOptions)
            {
                if (a.Chassis.ChassisTags.Contains($"chassis_CrossAssemblyAllowedWith_{vb}"))
                    return true;
                if (b.Chassis.ChassisTags.Contains($"chassis_CrossAssemblyAllowedWith_{va}"))
                    return true;
                if (a.Chassis.ChassisTags.Contains($"chassis_CrossAssemblyAllowedWith_{b.Chassis.VariantName}"))
                    return true;
                if (b.Chassis.ChassisTags.Contains($"chassis_CrossAssemblyAllowedWith_{a.Chassis.VariantName}"))
                    return true;
                if (a.Chassis.ChassisTags.Contains($"chassis_CrossAssemblyAllowedWith_{b.Description.Id}"))
                    return true;
                if (b.Chassis.ChassisTags.Contains($"chassis_CrossAssemblyAllowedWith_{a.Description.Id}"))
                    return true;
            }
            IAssemblyVariant av = CCIntegration.GetCCAssemblyVariant(a.Chassis);
            if (av != null && av.AssemblyAllowedWith != null)
            {
                if (av.AssemblyAllowedWith.Contains(vb))
                    return true;
                if (av.AssemblyAllowedWith.Contains(b.Chassis.VariantName))
                    return true;
                if (av.AssemblyAllowedWith.Contains(b.Description.Id))
                    return true;
            }
            IAssemblyVariant bv = CCIntegration.GetCCAssemblyVariant(b.Chassis);
            if (bv != null && bv.AssemblyAllowedWith != null)
            {
                if (bv.AssemblyAllowedWith.Contains(va))
                    return true;
                if (bv.AssemblyAllowedWith.Contains(a.Chassis.VariantName))
                    return true;
                if (bv.AssemblyAllowedWith.Contains(a.Description.Id))
                    return true;
            }
            return false;
        }
        
        public static bool AreMechsCrossVariantCompartible(MechDef a, MechDef b)
        {
            // a excluded had been checked before
            if (b.IsCrossAssemblyExcluded())
                return false; // b excluded
            string va = a.Chassis.GetVariant();
            string vb = b.Chassis.GetVariant();
            if (IsCrossAssemblyOverrideEnabled(a, b, va, vb))
                return true; // override enabled
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
                if (a.CountMechInventory(it) != b.CountMechInventory(it))
                    return false; // inventory mismatch
            }
            return true;
        }
        public static bool AreOmniMechsCompartible(MechDef a, MechDef b)
        {
            // a omni & a excluded had been checked before
            if (!b.Chassis.IsOmni()) // no omni
                return false;
            if (b.IsCrossAssemblyExcluded())
                return false; // b excluded
            string va = a.Chassis.GetVariant();
            string vb = b.Chassis.GetVariant();
            if (IsCrossAssemblyOverrideEnabled(a, b, va, vb))
                return true; // override enabled
            if (string.IsNullOrEmpty(va) || !va.Equals(vb))
                return false; // wrong or invalid variant
            return true;
        }
        public static bool AreVehicleMechsCompatible(MechDef a, MechDef b)
        {
            // aexcluded already checked, as well as a+b vehicle
            if (b.IsCrossAssemblyExcluded())
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
            if (IsCrossAssemblyAllowed(s))
            {
                return GetAllNonOmniVariants(s, m);
            }
            return new List<MechDef>() { m };
        }
        public static IEnumerable<MechDef> GetAllNonOmniVariants(SimGameState s, MechDef m)
        {
            yield return m;
            if (IsCrossAssemblyAllowed(s) && !m.IsCrossAssemblyExcluded())
            {
                foreach (KeyValuePair<string, MechDef> kv in s.DataManager.MechDefs)
                {
                    if (!m.Chassis.Description.Id.Equals(kv.Value.Chassis.Description.Id) && !kv.Value.IsVehicle() && !kv.Value.IsMechDefCustom()
                        && (Settings.AllowNonMainVariants || kv.Value.IsMechDefMain()) && AreMechsCrossVariantCompartible(m, kv.Value))
                        yield return kv.Value;
                }
            }
        }
        public static IEnumerable<MechDef> GetAllVehicleMechVariants(SimGameState s, MechDef m)
        {
            //FileLog.Log($"getting vehicle variants for {m.Description.Id}");
            yield return m;
            if (IsCrossAssemblyAllowed(s) && !m.IsCrossAssemblyExcluded())
            {
                foreach (KeyValuePair<string, MechDef> kv in s.DataManager.MechDefs)
                {
                    if (!m.Chassis.Description.Id.Equals(kv.Value.Chassis.Description.Id) && kv.Value.IsVehicle() && !kv.Value.IsMechDefCustom()
                        && (Settings.AllowNonMainVariants || kv.Value.IsMechDefMain()) && AreVehicleMechsCompatible(m, kv.Value))
                    {
                        //FileLog.Log($"variant found {kv.Value.Description.Id}");
                        yield return kv.Value;
                    }
                }
            }
        }
        public static IEnumerable<MechDef> GetAllOmniVariants(SimGameState s, MechDef m)
        {
            if (!m.Chassis.IsOmni()) // no omni, return empty list
                yield break;
            yield return m;
            if (m.IsCrossAssemblyExcluded())
                yield break;
            foreach (KeyValuePair<string, MechDef> kv in s.DataManager.MechDefs)
            {
                if (!m.Chassis.Description.Id.Equals(kv.Value.Chassis.Description.Id) && !kv.Value.IsVehicle() && !kv.Value.IsMechDefCustom()
                        && (Settings.AllowNonMainVariants || kv.Value.IsMechDefMain()) && AreOmniMechsCompartible(m, kv.Value))
                    yield return kv.Value;
            }
        }

        public static bool IsVariantKnown(SimGameState s, MechDef d)
        {
            if (!Settings.UseOnlyCCAssemblyOptions)
            {
                if (d.Chassis.ChassisTags.Contains("chassis_KnownOmniVariant"))
                return true;
            }
            IAssemblyVariant v = CCIntegration.GetCCAssemblyVariant(d.Chassis);
            if (v != null)
            {
                if (v.KnownOmniVariant)
                    return true;
            }
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
                {
                    if (Settings.ShowAllVariantsInPopup)
                        pop.Body += $"unknown: [[DM.MechDefs[{m.Description.Id}],{m.Chassis.Description.UIName} {m.Chassis.VariantName}]]\n";
                    continue;
                }
                pop.AddButton($"{var.Chassis.VariantName}", delegate
                {
                    Log.Log("ready omni as: " + var.Description.Id);
                    s.ScrapInactiveMech(d.Chassis.Description.Id, false);
                    ReadyMech(s, new MechDef(var, s.GenerateSimGameUID(), false), mechbay);
                    onClose?.Invoke();
                }, true, null);
                int com = GetNumberOfMechsOwnedOfType(s, m);
                int cost = m.GetMechSellCost(s, true);
                pop.Body += $"[[DM.MechDefs[{m.Description.Id}],{m.Chassis.Description.UIName} {m.Chassis.VariantName}]] ({com} Complete) ({SimGameState.GetCBillString(cost)})\n";
            }
            pop.AddFader(new UIColorRef?(LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.PopupBackfill), 0f, true);
            OverridePopupSize(pop.Render());
        }

        public static string GetAssembleNotEnoughPartsText(SimGameState s, MechDef d)
        {
            IEnumerable<MechDef> mechs = GetAllAssemblyVariants(s, d);
            string desc = $"Yang: I do not have enough parts to assemble a {d.GetMechOmniVehicle()} out of it.";
            if (!Settings.ShowAllVariantsInPopup)
                return desc;
            desc += "\nKeep your eyes open for the following Variants:\n";
            foreach (MechDef m in mechs)
            {
                int count = s.GetItemCount(m.Description.Id, "MECHPART", SimGameState.ItemCountType.UNDAMAGED_ONLY);
                int com = GetNumberOfMechsOwnedOfType(s, m);
                if (count <= 0 && !CheckOmniKnown(s, d, m))
                {
                    desc += $"no parts: [[DM.MechDefs[{m.Description.Id}],{m.Chassis.Description.UIName} {m.Chassis.VariantName}]] ({com} Complete)\n";
                    continue;
                }
                desc += $"[[DM.MechDefs[{m.Description.Id}],{m.Chassis.Description.UIName} {m.Chassis.VariantName}]] ({count} Parts/{com} Complete)\n";
            }
            return desc;
        }

        public static void ReadyMech(SimGameState s, MechDef d, int baySlot, bool donotoverridetime=false)
        {
            d.RunSingleAutoFixer();
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
            string desc = $"Yang: Concerning the [[DM.MechDefs[{d.Description.Id}],{d.Chassis.Description.UIName} {d.Chassis.VariantName}]]: {d.Chassis.YangsThoughts}\n\n We have Parts of the following {d.GetMechOmniVehicle()} variants. What should I build?\n";
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
                int cost = m.GetMechSellCost(s, !s.Constants.Salvage.EquipMechOnSalvage);
                if (count <= 0 && !CheckOmniKnown(s, d, m))
                {
                    if (Settings.ShowAllVariantsInPopup)
                        pop.Body += $"no parts: [[DM.MechDefs[{m.Description.Id}],{m.Chassis.Description.UIName} {m.Chassis.VariantName}]] ({com} Complete)\n";
                    continue;
                }
                if (GetNumPartsForAssembly(s, var) >= s.Constants.Story.DefaultMechPartMax)
                {
                    pop.AddButton(string.Format("{0}", var.Chassis.VariantName), delegate
                    {
                        PerformMechAssemblyStorePopup(s, var, onClose);
                    }, true, null);
                }
                pop.Body += $"[[DM.MechDefs[{m.Description.Id}],{m.Chassis.Description.UIName} {m.Chassis.VariantName}]] ({count} Parts/{com} Complete) ({SimGameState.GetCBillString(cost)})\n";
            }
            pop.AddFader(new UIColorRef?(LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.PopupBackfill), 0f, true);
            OverridePopupSize(pop.Render());
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
                pop.Body += "Should I put it into storage or ready it for combat?";
                pop.AddButton("ready it", delegate
                {
                    if (Settings.AssembledMechsNeedReadying)
                    {
                        ReadyMech(s, toAdd, mechbay);
                        CallMessages(s, toAdd);
                    }
                    else
                    {
                        toAdd.RunSingleAutoFixer();
                        s.AddMech(mechbay, toAdd, true, false, false);
                        CallMessages(s, toAdd);
                    }
                    Log.Log("added to bay " + mechbay);
                    onClose?.Invoke();
                }, true, null);
            }
            if (s.IsSellingAllowed())
            {
                int cost = toAdd.GetMechSellCost(s);
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

        public static string GetMechCountDescrString(SimGameState s, MechDef d)
        {
            int pieces = s.GetItemCount(d.Description.Id, "MECHPART", SimGameState.ItemCountType.UNDAMAGED_ONLY);
            int needed = s.Constants.Story.DefaultMechPartMax;
            int varpieces = GetNumPartsForAssembly(s, d);
            int owned = GetNumberOfMechsOwnedOfType(s, d);
            string ownedorknown;
            if (owned == 0 && d.Chassis.IsOmni() && IsVariantKnown(s, d))
                ownedorknown = "K";
            else
                ownedorknown = owned.ToString();
            return $"{pieces}({varpieces})/{needed}({ownedorknown})";
        }

        public static IEnumerable<Transform> GetChildren(this Transform t)
        {
            foreach (Transform c in t)
                yield return c;
        }

        private static float? PopupSizeOrig = null;
        public static void OverridePopupSize(GenericPopup p)
        {
            float sizex = Settings.AssemblyPopupSizeIncrease;
            Transform rep = p.transform.GetChildren().First((x) => x.name == "Representation");
            Transform expanderViewport = rep.GetChildren().First((x) => x.name == "ExpanderViewport");
            RectTransform expanderViewportTrans = expanderViewport.GetComponent<RectTransform>();
            if (PopupSizeOrig == null)
                PopupSizeOrig = expanderViewportTrans.sizeDelta.x;
            else if (expanderViewportTrans.sizeDelta.x > (float)PopupSizeOrig)
                return;
            doTransf(expanderViewportTrans, sizex);
            Transform containerLayout = expanderViewport.GetChildren().First((x) => x.name == "popupContainerLayout");
            doTransf(containerLayout.GetComponent<RectTransform>(), sizex);
            doTransf(containerLayout.GetChildren().First((x) => x.name == "popUpTitle").GetComponent<RectTransform>(), sizex);
            doTransf(containerLayout.GetChildren().First((x) => x.name == "Text_content").GetComponent<RectTransform>(), sizex);
            doTransf(containerLayout.GetChildren().First((x) => x.name == "popup_buttonLayout").GetComponent<RectTransform>(), sizex);

            void doTransf(RectTransform r, float x)
            {
                Vector2 v = r.sizeDelta;
                v.x += x;
                r.sizeDelta = v;
            }
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
