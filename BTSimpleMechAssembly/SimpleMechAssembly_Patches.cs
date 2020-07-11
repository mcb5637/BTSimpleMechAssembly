using BattleTech;
using BattleTech.UI;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Harmony;
using BattleTech.UI.TMProWrapper;
using BattleTech.Save.SaveGameStructure;
using System.Reflection.Emit;
using HBS;

namespace BTSimpleMechAssembly
{
    [HarmonyPatch(typeof(SimGameState), "AddMechPart")]
    class SimGameState_AddMechPart
    {
        public static bool Prefix(SimGameState __instance, string id)
        {
            __instance.AddItemStat(id, "MECHPART", false);

            if (SimGameState_ResolveCompleteContract.IsResolving != null) // save added mechs for later completion
            {
                if (!SimGameState_ResolveCompleteContract.IsResolving.ContainsKey(id))
                    SimGameState_ResolveCompleteContract.IsResolving.Add(id, 0);
                SimGameState_ResolveCompleteContract.IsResolving[id]++;
            }
            else // no contract -> direct assembly
            {
                MechDef d = __instance.DataManager.MechDefs.Get(id);
                int p = SimpleMechAssembly_Main.GetNumPartsForAssembly(__instance, d);
                if (p >= __instance.Constants.Story.DefaultMechPartMax)
                    __instance.InterruptQueue.AddInterrupt(new SimpleMechAssembly_Main.SimpleMechAssembly_InterruptManager_AssembleMechEntry(__instance, d, null), true);
            }

            return false; // completely replace
        }
    }

    [HarmonyPatch(typeof(SimGameState), "ResolveCompleteContract")]
    class SimGameState_ResolveCompleteContract
    {
        public static Dictionary<string, int> IsResolving = null;

        public static void Prefix()
        {
            IsResolving = new Dictionary<string, int>();
        }

        public static void Postfix(SimGameState __instance)
        {
            List<string> VariantsDone = new List<string>();
            foreach (KeyValuePair<string, int> kv in IsResolving)
            {
                MechDef d = __instance.DataManager.MechDefs.Get(kv.Key);
                if (VariantsDone.Contains(d.Description.Id))
                    continue;
                int p = SimpleMechAssembly_Main.GetNumPartsForAssembly(__instance, d);
                if (p >= __instance.Constants.Story.DefaultMechPartMax)
                {
                    __instance.InterruptQueue.AddInterrupt(new SimpleMechAssembly_Main.SimpleMechAssembly_InterruptManager_AssembleMechEntry(__instance, d, null), true);
                    foreach (MechDef m in SimpleMechAssembly_Main.GetAllAssemblyVariants(__instance, d))
                        if (!VariantsDone.Contains(m.Description.Id))
                            VariantsDone.Add(m.Description.Id);
                }
            }
            IsResolving = null;
        }
    }

    [HarmonyPatch(typeof(RewardsPopup), "AddItemsToInventory")]
    class RewardsPopup_AddItemsToInventory
    {
        public static void Prefix()
        {
            SimGameState_ResolveCompleteContract.Prefix();
        }

        public static void Postfix(SimGameState ___sim)
        {
            SimGameState_ResolveCompleteContract.Postfix(___sim);
        }
    }

    [HarmonyPatch(typeof(MechBayChassisInfoWidget), "OnReadyClicked")]
    class MechBayChassisInfoWidget_OnReadyClicked
    {
        public static bool Prefix(MechBayChassisInfoWidget __instance, ChassisDef ___selectedChassis, MechBayPanel ___mechBay)
        {
            if (___selectedChassis==null)
                return true;
            int bay = ___mechBay.Sim.GetFirstFreeMechBay();
            string id = ___selectedChassis.Description.Id.Replace("chassisdef", "mechdef");
            MechDef d = ___mechBay.Sim.DataManager.MechDefs.Get(id);
            if (___selectedChassis.MechPartCount > 0) // this is actually a part that gets assembled
            {
                int p = SimpleMechAssembly_Main.GetNumPartsForAssembly(___mechBay.Sim, d);
                if (p < ___mechBay.Sim.Constants.Story.DefaultMechPartMax)
                {
                    GenericPopupBuilder.Create("Mech Assembly", "Yang: I do not have enough parts to assemble a mech out of it.").AddButton("Cancel", null, true, null)
                        .AddFader(new UIColorRef?(LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.PopupBackfill), 0f, true).Render();
                    return false;
                }
                ___mechBay.Sim.InterruptQueue.AddInterrupt(new SimpleMechAssembly_Main.SimpleMechAssembly_InterruptManager_AssembleMechEntry(___mechBay.Sim, d, delegate {
                    ___mechBay.RefreshData(false);
                }), true);
                return false;
            }
            if (___selectedChassis.MechPartCount < ___selectedChassis.MechPartMax)
                return true;
            if (SimpleMechAssembly_Main.Settings.OmniMechTag == null || !___selectedChassis.ChassisTags.Contains(SimpleMechAssembly_Main.Settings.OmniMechTag))
                return true;
            if (bay < 0)
                return true;
            ___mechBay.Sim.InterruptQueue.AddInterrupt(new SimpleMechAssembly_Main.SimpleMechAssembly_InterruptManager_UnStorageOmniEntry(___mechBay.Sim, d, delegate {
                ___mechBay.RefreshData(false);
            }));
            return false;
        }
    }

    [HarmonyPatch(typeof(MechBayChassisInfoWidget), "SetDescriptions")]
    class MechBayChassisInfoWidget_SetDescriptions
    {
        public static void Postfix(MechBayChassisInfoWidget __instance, ChassisDef ___selectedChassis, GameObject ___readyBtnObj, GameObject ___partsCountObj)
        {
            if (___selectedChassis == null)
                return;
            if (___selectedChassis.MechPartCount > 0)
            {
                ___readyBtnObj.SetActive(true);
                ___partsCountObj.SetActive(false);
            }
        }
    }

    [HarmonyPatch(typeof(ListElementController_SalvageMechPart_NotListView), "RefreshInfoOnWidget")]
    class ListElementController_SalvageMechPart_NotListView_RefreshInfoOnWidget
    {
        public static void Postfix(ListElementController_SalvageMechPart_NotListView __instance, InventoryItemElement_NotListView theWidget, SimGameState ___simState)
        {
            int pieces = ___simState.GetItemCount(__instance.mechDef.Description.Id, "MECHPART", SimGameState.ItemCountType.UNDAMAGED_ONLY);
            int needed = ___simState.Constants.Story.DefaultMechPartMax;
            int varpieces = SimpleMechAssembly_Main.GetNumPartsForAssembly(___simState, __instance.mechDef);
            int owned = SimpleMechAssembly_Main.GetNumberOfMechsOwnedOfType(___simState, __instance.mechDef);
            theWidget.mechPartsNumbersText.SetText(string.Format("{0}({1})/{3}({2})", pieces, varpieces, owned, needed));
        }
    }

    [HarmonyPatch(typeof(SG_Shop_ItemSelectedPanel), "FillInMechPartDetail")]
    class SG_Shop_ItemSelectedPanel_FillInMechPartDetail
    {
        public static void Postfix(SG_Shop_ItemSelectedPanel __instance, InventoryDataObject_BASE theController, SimGameState ___simState, LocalizableText ___MechPartCountText)
        {
            if (theController.mechDef == null)
                return;
            int pieces = ___simState.GetItemCount(theController.mechDef.Description.Id, "MECHPART", SimGameState.ItemCountType.UNDAMAGED_ONLY);
            int needed = ___simState.Constants.Story.DefaultMechPartMax;
            int varpieces = SimpleMechAssembly_Main.GetNumPartsForAssembly(___simState, theController.mechDef);
            int owned = SimpleMechAssembly_Main.GetNumberOfMechsOwnedOfType(___simState, theController.mechDef);
            ___MechPartCountText.SetText(string.Format("{0}({1})/{3}({2})", pieces, varpieces, owned, needed));
        }
    }

    [HarmonyPatch(typeof(SG_Shop_ItemSelectedPanel), "FillInFullMechDetail")]
    class SG_Shop_ItemSelectedPanel_FillInFullMechDetail
    {
        public static void Postfix(SG_Shop_ItemSelectedPanel __instance, InventoryDataObject_BASE theController, SimGameState ___simState, LocalizableText ___MechPartCountText,
            GameObject ___FullMechWeightClassDisplayObject, GameObject ___MechPartCountDisplayObject)
        {
            ___FullMechWeightClassDisplayObject.SetActive(false);
            ___MechPartCountDisplayObject.SetActive(true);
            SG_Shop_ItemSelectedPanel_FillInMechPartDetail.Postfix(__instance, theController, ___simState, ___MechPartCountText);
        }
    }



    [HarmonyPatch(typeof(MechBayChassisUnitElement), "SetData")]
    class MechBayChassisUnitElement_SetData
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> code)
        {
            CodeInstruction prev = null;
            CodeInstruction prevprev = null;
            foreach (CodeInstruction c in code)
            {
                if (prevprev != null && prevprev.opcode == OpCodes.Ldarg_S && Convert.ToInt32(prevprev.operand) == 4
                    && prev.opcode == OpCodes.Ldarg_S && Convert.ToInt32(prev.operand) == 5
                    && (c.opcode == OpCodes.Blt_S || c.opcode == OpCodes.Blt))
                {
                    yield return prevprev;
                    yield return new CodeInstruction(OpCodes.Ldc_I4_0);
                    c.opcode = OpCodes.Bne_Un;
                    yield return c;
                    prevprev = null;
                    prev = null;
                    continue;
                }
                if (prevprev != null)
                    yield return prevprev;
                prevprev = prev;
                prev = c;
            }
            if (prevprev != null)
                yield return prevprev;
            if (prev != null)
                yield return prev;
        }
    }



    [HarmonyPatch(typeof(MechBayPanel), "OnScrapChassis")]
    class MechBayPanel_OnScrapChassis
    {

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> code) // correct the scrap amount you get, not what is shown in the popup
        {
            LinkedList<CodeInstruction> prev = new LinkedList<CodeInstruction>();
            List<CodeInstruction> cmp = new List<CodeInstruction>()
            {
                new CodeInstruction(OpCodes.Ldarg_1),
                new CodeInstruction(OpCodes.Callvirt, "Int32 get_PartsCount()"),
                new CodeInstruction(OpCodes.Ldarg_1),
                new CodeInstruction(OpCodes.Callvirt, "Int32 get_PartsMax()"),
                new CodeInstruction(OpCodes.Bge),
            };
            foreach (CodeInstruction c in code)
            {
                prev.AddLast(c);

                if (prev.Count == cmp.Count
                    && (c.opcode == OpCodes.Bge || c.opcode == OpCodes.Bge_S))
                {
                    bool match = prev.Zip(cmp, (p, cm) => (p.opcode == cm.opcode) && (cm.operand == null || p.operand.ToString().Equals(cm.operand))).Aggregate((a, b) => a && b);
                    if (match)
                    {
                        foreach (CodeInstruction o in prev.Take(2))
                        {
                            yield return o;
                        }
                        yield return new CodeInstruction(OpCodes.Ldc_I4_0);
                        c.opcode = OpCodes.Beq;
                        yield return c;
                        prev.Clear();
                        continue;
                    }
                }

                if (prev.Count == cmp.Count)
                {
                    yield return prev.First.Value;
                    prev.RemoveFirst();
                }
            }
            while (prev.Count > 0)
            {
                yield return prev.First.Value;
                prev.RemoveFirst();
            }
        }
    }
}
