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
using BattleTech.Data;
using UnityEngine.UI;

namespace BTSimpleMechAssembly
{
    [HarmonyPatch(typeof(SimGameState), "AddMechPart")]
    class SimGameState_AddMechPart
    {
        public static bool Prefix(SimGameState __instance, string id)
        {
            __instance.AddItemStat(id, "MECHPART", false);

            if (!SimpleMechAssembly_Main.Settings.AutoQueryAssembly)
                return false;

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
            if (!SimpleMechAssembly_Main.Settings.AutoQueryAssembly)
                return;

            IsResolving = new Dictionary<string, int>();
        }

        public static void Postfix(SimGameState __instance)
        {
            if (!SimpleMechAssembly_Main.Settings.AutoQueryAssembly)
                return;

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
            if (___selectedChassis == null)
                return true;
            int bay = ___mechBay.Sim.GetFirstFreeMechBay();
            MechDef d = ___selectedChassis.GetMainMechDef(___mechBay.Sim.DataManager);
            if (___selectedChassis.MechPartCount > 0) // this is actually a part that gets assembled
            {
                int p = SimpleMechAssembly_Main.GetNumPartsForAssembly(___mechBay.Sim, d);
                if (p < ___mechBay.Sim.Constants.Story.DefaultMechPartMax)
                {
                    GenericPopupBuilder.Create($"{d.GetMechOmniVehicle()} Assembly", SimpleMechAssembly_Main.GetAssembleNotEnoughPartsText(___mechBay.Sim, d)).AddButton("Cancel", null, true, null)
                        .AddFader(new UIColorRef?(LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.PopupBackfill), 0f, true).Render();
                    return false;
                }
                ___mechBay.Sim.InterruptQueue.AddInterrupt(new SimpleMechAssembly_Main.SimpleMechAssembly_InterruptManager_AssembleMechEntry(___mechBay.Sim, d, delegate
                {
                    ___mechBay.RefreshData(false);
                }), true);
                return false;
            }
            if (___selectedChassis.MechPartCount < ___selectedChassis.MechPartMax)
                return true;
            if (___selectedChassis.IsVehicle())
            {
                bay = CUIntegration.GetFirstFreeMechBay(___mechBay.Sim, d);
                if (bay < 0)
                {
                    GenericPopupBuilder.Create("Cannot Ready Vehicle", "There are no available slots in the Vehicle Bay. You must move an active Vehicle into storage before readying this chassis.")
                        .AddFader(new UIColorRef?(LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.PopupBackfill), 0f, true).Render();
                }
                else
                {
                    GenericPopupBuilder.Create("Ready Vehicle?", $"It will take {Mathf.CeilToInt(___mechBay.Sim.Constants.Story.MechReadyTime / (float)___mechBay.Sim.MechTechSkill)} day(s) to ready this Vehicle for combat.")
                        .AddButton("Cancel", null, true, null).AddButton("Ready", delegate
                        {
                            if (___mechBay.Sim.ScrapInactiveMech(___selectedChassis.Description.Id, false))
                            {
                                SimpleMechAssembly_Main.ReadyMech(___mechBay.Sim, new MechDef(d, ___mechBay.Sim.GenerateSimGameUID(), true), bay, true);
                                ___mechBay.RefreshData(false);
                                ___mechBay.ViewBays();
                            }
                        }).AddFader(new UIColorRef?(LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.PopupBackfill), 0f, true).CancelOnEscape().Render();
                }
                return false;
            }
            if (!___selectedChassis.IsOmni())
                return true;
            if (bay < 0)
                return true;
            ___mechBay.Sim.InterruptQueue.AddInterrupt(new SimpleMechAssembly_Main.SimpleMechAssembly_InterruptManager_UnStorageOmniEntry(___mechBay.Sim, d, delegate
            {
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
            theWidget.mechPartsNumbersText.SetText(SimpleMechAssembly_Main.GetMechCountDescrString(___simState, __instance.mechDef));
        }
    }

    [HarmonyPatch(typeof(SG_Shop_ItemSelectedPanel), "FillInMechPartDetail")]
    class SG_Shop_ItemSelectedPanel_FillInMechPartDetail
    {
        public static void Postfix(SG_Shop_ItemSelectedPanel __instance, InventoryDataObject_BASE theController, SimGameState ___simState, LocalizableText ___MechPartCountText)
        {
            if (theController.mechDef == null)
                return;
            ___MechPartCountText.SetText(SimpleMechAssembly_Main.GetMechCountDescrString(___simState, theController.mechDef));
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

        public static void Postfix(MechBayChassisUnitElement __instance, Image ___mechImage, ChassisDef chassisDef, DataManager dataManager, int partsCount, int partsMax, int chassisQuantity)
        {
            if (partsMax > 0)
            {
                if (chassisDef.IsVehicle())
                    ___mechImage.color = SimpleMechAssembly_Main.Settings.storage_vehiclepart;
                else
                    ___mechImage.color = SimpleMechAssembly_Main.Settings.storage_parts;
            }
            else if (chassisDef.IsVehicle())
                ___mechImage.color = SimpleMechAssembly_Main.Settings.storage_vehicle;
            else if (chassisDef.IsOmni())
                ___mechImage.color = SimpleMechAssembly_Main.Settings.storage_omni;
            else
                ___mechImage.color = SimpleMechAssembly_Main.Settings.storage_mech;
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
