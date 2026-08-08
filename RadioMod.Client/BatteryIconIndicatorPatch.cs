using System;
using EFT.InventoryLogic;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using UnityEngine;

namespace RadioMod.Client
{
    /// <summary>
    /// Shows battery presence/charge directly on the radio's own inventory icon, reusing the same
    /// numeric readout the game already draws for weapon durability (aggregated across all attached
    /// parts). <see cref="GridItemView.UpdateInfo"/> leaves that readout blank for radios today: none
    /// of its vanilla branches match — no RepairableComponent, and the resource it looks for lives on
    /// the battery items sitting in the radio's slots, not on the radio itself. This patch fills that
    /// gap for radios only, and only once the battery mod is actually installed and the radio has
    /// slots to report on.
    /// </summary>
    internal static class BatteryIconIndicatorPatch
    {
        private const string AaBatteryTplId = "5672cb124bdc2d1a0f8b4568";
        private const string Cr123ABatteryTplId = "590a358486f77429692b2790";
        private const string Cr2032BatteryTplId = "5672cb304bdc2dc2088b456a";

        internal static void Apply(Harmony harmony)
        {
            try
            {
                var target = AccessTools.Method(typeof(GridItemView), "UpdateInfo");
                if (target == null)
                {
                    Plugin.LogAttributeDiagnostic("PRT: battery-icon patch skipped — method not found");
                    return;
                }

                var postfix = AccessTools.Method(typeof(BatteryIconIndicatorPatch), nameof(UpdateInfoPostfix));
                harmony.Patch(target, postfix: new HarmonyMethod(postfix));
                Plugin.LogAttributeDiagnostic("PRT: battery-icon patch applied");
            }
            catch (Exception ex)
            {
                Plugin.LogAttributeDiagnostic("PRT: battery-icon patch failed: " + ex.Message);
            }
        }

        private static bool IsBatterySlot(Slot slot)
        {
            if (slot?.Filters == null)
            {
                return false;
            }

            foreach (var filter in slot.Filters)
            {
                if (filter?.Filter == null)
                {
                    continue;
                }

                foreach (var id in filter.Filter)
                {
                    string tpl = id.ToString();
                    if (tpl == AaBatteryTplId || tpl == Cr123ABatteryTplId || tpl == Cr2032BatteryTplId)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        // Runs on every icon refresh (not just creation), same as ItemViewStats.SetStaticInfo — keep
        // this cheap and never log from here, it is not a one-shot call site.
        private static void UpdateInfoPostfix(GridItemView __instance)
        {
            try
            {
                Item rawItem = __instance.Item;
                bool isRadioTpl = rawItem != null && TierAttributePatch.IsRadio(rawItem.StringTemplateId);

                if (!(rawItem is CompoundItem radio)
                    || !isRadioTpl
                    || !TierAttributePatch.IsBatteryModPresent())
                {
                    return;
                }

                float value = 0f;
                float max = 0f;
                int totalSlots = 0;
                int filledSlots = 0;

                foreach (Slot slot in radio.Slots)
                {
                    if (!IsBatterySlot(slot))
                    {
                        continue;
                    }

                    totalSlots++;
                    Item cell = slot.ContainedItem;
                    if (cell == null)
                    {
                        continue;
                    }

                    if (cell.TryGetItemComponent(out ResourceComponent resource) && resource.MaxResource > 0f)
                    {
                        filledSlots++;
                        value += resource.Value;
                        max += resource.MaxResource;
                    }
                }

                if (totalSlots == 0)
                {
                    return;
                }

                if (filledSlots < totalSlots)
                {
                    // Incomplete set — a missing slot means the radio won't power on in-raid
                    // regardless of how charged the batteries that ARE in are (see
                    // TryGetRadioBatteryCharge in Plugin.cs), so the count of batteries actually
                    // present is the more useful reading here than a charge number.
                    __instance.SetItemValue(GridItemView.EItemValueFormat.TwoValues, true, "#ff0000", filledSlots, totalSlots);
                }
                else
                {
                    float roundedValue = Mathf.Round(value);
                    float roundedMax = Mathf.Round(max);
                    string color = roundedValue <= 0f
                        ? "#ff0000"
                        : ((max > 0f && value / max <= 0.15f) ? "#ff9900" : "#dadabc");

                    __instance.SetItemValue(GridItemView.EItemValueFormat.TwoValues, true, color, roundedValue, roundedMax);
                }

                // SetItemValue only writes the text; UpdateInfo already decided the value label's
                // visibility earlier in the same call, back when the string was still empty for a
                // radio (no vanilla branch matches it), so it left the element hidden. Force it back
                // on now that there is something real to show.
                __instance.SetValueVisibility(true);
            }
            catch (Exception)
            {
                // Cosmetic only — never break the icon.
            }
        }
    }
}
