using System;
using System.Linq;
using System.Reflection;
using EFT.InventoryLogic;
using HarmonyLib;
using UnityEngine;

namespace RadioMod.Client
{
    /// <summary>
    /// Gates the "fits existing" (LinkedSearch) button on the radios' item card behind whether the
    /// battery mod is installed.
    ///
    /// The button list itself is a single static collection shared by every item in the game, so it
    /// cannot be edited without affecting everything. Visibility is instead decided per item by
    /// <c>IsActive</c> on the interactions object, which is abstract — the concrete implementation
    /// is an obfuscated type whose name changes between game builds. It is therefore located at
    /// runtime by its base type rather than referenced directly.
    ///
    /// With the battery mod installed, the button already works with no help from us: it calls the
    /// vanilla ExternalRagfairSearch(EFilterType.LinkedSearch, radio.TemplateId), which resolves
    /// against the radio's own server-side Slots/Filters — the same battery filters set up in
    /// RadioItemMod.cs — and correctly surfaces compatible batteries on the flea market. Without the
    /// mod the radio has no slots at all, so that search would just come back empty; the button is
    /// hidden only in that case.
    /// </summary>
    internal static class LinkedSearchHidePatch
    {
        private static FieldInfo _itemField;
        private static Type _dropdownRowType;

        internal static void Apply(Harmony harmony)
        {
            try
            {
                Type baseType = typeof(EFT.UI.ContextInteractions<EItemInfoButton>);

                Type[] implementations = baseType.Assembly.GetTypes()
                    .Where(t => !t.IsAbstract && baseType.IsAssignableFrom(t))
                    .ToArray();

                if (implementations.Length == 0)
                {
                    Plugin.LogAttributeDiagnostic("PRT: LinkedSearch patch skipped — no concrete interactions type found");
                    return;
                }

                MethodInfo postfix = typeof(LinkedSearchHidePatch)
                    .GetMethod(nameof(IsActivePostfix), BindingFlags.NonPublic | BindingFlags.Static);

                int patched = 0;
                foreach (Type type in implementations)
                {
                    MethodInfo target = AccessTools.Method(type, "IsActive", new[] { typeof(EItemInfoButton) });
                    if (target == null || target.IsAbstract)
                    {
                        continue;
                    }

                    harmony.Patch(target, postfix: new HarmonyMethod(postfix));
                    patched++;
                }

                Plugin.LogAttributeDiagnostic("PRT: LinkedSearch patch applied to " + patched + " interaction type(s)");

                PatchCompatibilityRow(harmony);
                PatchIconOverlays(harmony);
                PatchModTypeBadges(harmony);
            }
            catch (Exception ex)
            {
                // Purely cosmetic — never let it break startup.
                Plugin.LogAttributeDiagnostic("PRT: LinkedSearch patch failed: " + ex.Message);
            }
        }

        /// <summary>
        /// Removes the "compatible with available" row from the radios' item card.
        ///
        /// That row is not one of the item's own attributes — the specification panel builds it on
        /// the fly for anything of type Mod, listing the weapons the item fits. Radios fit nothing,
        /// so the row is always empty noise. It is dropped right after the panel rebuilds itself.
        /// </summary>
        private static void PatchCompatibilityRow(Harmony harmony)
        {
            try
            {
                Type panelType = AccessTools.TypeByName("EFT.UI.ItemSpecificationPanel");
                // 4.1 de-obfuscated this: the panel rebuild that spawns the compatible-attribute
                // dropdown rows used to be method_5, which in this build is an unrelated attribute
                // predicate — patching it by the old name would silently do nothing.
                MethodInfo target = AccessTools.Method(panelType, "RecreateAttributeBars");
                if (target == null)
                {
                    Plugin.LogAttributeDiagnostic("PRT: compatibility-row patch skipped — method not found");
                    return;
                }

                MethodInfo postfix = typeof(LinkedSearchHidePatch)
                    .GetMethod(nameof(SpecificationPanelPostfix), BindingFlags.NonPublic | BindingFlags.Static);

                harmony.Patch(target, postfix: new HarmonyMethod(postfix));
                Plugin.LogAttributeDiagnostic("PRT: compatibility-row patch applied");
            }
            catch (Exception ex)
            {
                Plugin.LogAttributeDiagnostic("PRT: compatibility-row patch failed: " + ex.Message);
            }
        }

        /// <summary>
        /// Clears the small corner markers the inventory icon inherits from being a Mod: the
        /// on/off togglable lamp and the "missing parts" overlay. Neither means anything for a
        /// radio, and both were only introduced by re-parenting it for battery slots.
        /// </summary>
        private static void PatchIconOverlays(Harmony harmony)
        {
            try
            {
                Type viewType = AccessTools.TypeByName("EFT.UI.DragAndDrop.GridItemView");
                MethodInfo target = AccessTools.Method(viewType, "NewGridItemView");
                if (target == null)
                {
                    Plugin.LogAttributeDiagnostic("PRT: icon-overlay patch skipped — method not found");
                    return;
                }

                MethodInfo postfix = typeof(LinkedSearchHidePatch)
                    .GetMethod(nameof(GridItemViewPostfix), BindingFlags.NonPublic | BindingFlags.Static);

                harmony.Patch(target, postfix: new HarmonyMethod(postfix));
                Plugin.LogAttributeDiagnostic("PRT: icon-overlay patch applied");
            }
            catch (Exception ex)
            {
                Plugin.LogAttributeDiagnostic("PRT: icon-overlay patch failed: " + ex.Message);
            }
        }

        private static readonly string[] OverlayFields = { "_togglableIcon", "_missingLayout" };

        private static void GridItemViewPostfix(object __result, Item item)
        {
            try
            {
                if (item == null || __result == null || !TierAttributePatch.IsRadio(item.StringTemplateId))
                {
                    return;
                }

                foreach (string fieldName in OverlayFields)
                {
                    FieldInfo field = AccessTools.Field(__result.GetType(), fieldName);
                    if (field?.GetValue(__result) is Component overlay)
                    {
                        overlay.gameObject.SetActive(false);
                    }
                }

                // The bottom-left corner badge itself is handled by PatchModTypeBadges below —
                // it is re-drawn by ItemViewStats.SetStaticInfo on every icon refresh, so toggling
                // it once here (right after the icon is constructed) does not stick.

            }
            catch (Exception)
            {
                // Cosmetic only.
            }
        }

        private static void SpecificationPanelPostfix(object __instance)
        {
            try
            {
                Item item = ResolveItem(__instance);
                if (item == null || !TierAttributePatch.IsRadio(item.StringTemplateId))
                {
                    return;
                }

                // The attribute list is a local inside the rebuild method and the enum has no
                // "hidden" value, so the row cannot be suppressed at the data level. The rendered
                // widget is switched off instead — for a radio the only dropdown row is the
                // weapon-compatibility one, which is always empty.
                if (!(__instance is Component panel))
                {
                    return;
                }

                if (_dropdownRowType == null)
                {
                    _dropdownRowType = AccessTools.TypeByName("EFT.UI.CompactCharacteristicDropdownPanel");
                }

                if (_dropdownRowType == null)
                {
                    return;
                }

                foreach (Component row in panel.GetComponentsInChildren(_dropdownRowType, includeInactive: true))
                {
                    row.gameObject.SetActive(false);
                }
            }
            catch (Exception)
            {
                // Cosmetic only — never break the item card.
            }
        }

        /// <summary>
        /// Hides the generic "mod type" badges (the corner square) that the icon draws for any item
        /// whose runtime class is a Mod — decided by <c>ItemViewStats.SetStaticInfo</c>, which runs
        /// on every icon refresh. This is why disabling the widgets once, right after the icon is
        /// built, did not stick: this method re-enables them afterwards based on the item's C# type,
        /// which is Mod for the radios now that they carry battery slots. The fix has to sit here,
        /// at the actual decision point, rather than chase yet another one-shot toggle.
        /// </summary>
        private static void PatchModTypeBadges(Harmony harmony)
        {
            try
            {
                Type statsType = AccessTools.TypeByName("ItemViewStats");
                MethodInfo target = AccessTools.Method(statsType, "SetStaticInfo", new[] { typeof(Item), typeof(bool) });
                if (target == null)
                {
                    Plugin.LogAttributeDiagnostic("PRT: mod-type-badge patch skipped — method not found");
                    return;
                }

                MethodInfo postfix = typeof(LinkedSearchHidePatch)
                    .GetMethod(nameof(SetStaticInfoPostfix), BindingFlags.NonPublic | BindingFlags.Static);

                harmony.Patch(target, postfix: new HarmonyMethod(postfix));
                Plugin.LogAttributeDiagnostic("PRT: mod-type-badge patch applied");
            }
            catch (Exception ex)
            {
                Plugin.LogAttributeDiagnostic("PRT: mod-type-badge patch failed: " + ex.Message);
            }
        }

        private static readonly string[] ModTypeBadgeFields = { "_modIcon", "_modTypeIcon", "_specialIcon", "_modTypeIconParent" };

        private static void SetStaticInfoPostfix(object __instance, Item item)
        {
            if (item == null || !TierAttributePatch.IsRadio(item.StringTemplateId))
            {
                return;
            }

            try
            {
                foreach (string fieldName in ModTypeBadgeFields)
                {
                    FieldInfo field = AccessTools.Field(__instance.GetType(), fieldName);
                    object value = field?.GetValue(__instance);

                    GameObject go = value as GameObject;
                    if (go == null && value is Component comp)
                    {
                        go = comp.gameObject;
                    }

                    go?.SetActive(false);
                }
            }
            catch (Exception)
            {
                // Cosmetic only.
            }
        }

        /// <summary>
        /// Finds the Item the interactions object is bound to. The field name is obfuscated, so it
        /// is resolved by type once and cached.
        /// </summary>
        private static Item ResolveItem(object instance)
        {
            Type type = instance.GetType();

            if (_itemField == null || !_itemField.DeclaringType.IsInstanceOfType(instance))
            {
                _itemField = null;
                for (Type t = type; t != null && _itemField == null; t = t.BaseType)
                {
                    _itemField = t
                        .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                        .FirstOrDefault(f => typeof(Item).IsAssignableFrom(f.FieldType));
                }
            }

            return _itemField?.GetValue(instance) as Item;
        }

        private static void IsActivePostfix(object __instance, EItemInfoButton button, ref bool __result)
        {
            if (button != EItemInfoButton.LinkedSearch || !__result)
            {
                return;
            }

            try
            {
                Item item = ResolveItem(__instance);
                if (item == null || !TierAttributePatch.IsRadio(item.StringTemplateId))
                {
                    return;
                }

                if (!TierAttributePatch.IsBatteryModPresent())
                {
                    __result = false;
                }
            }
            catch (Exception)
            {
                // Leave the button visible rather than risking the card failing to draw.
            }
        }
    }
}
