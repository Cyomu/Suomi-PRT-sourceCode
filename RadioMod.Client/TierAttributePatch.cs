using System.Collections.Generic;
using System.Linq;
using EFT.InventoryLogic;
using HarmonyLib;

namespace RadioMod.Client
{
    [HarmonyPatch(typeof(Item), MethodType.Constructor, new[] { typeof(string), typeof(ItemTemplate) })]
    internal static class TierAttributePatch
    {
        private static readonly Dictionary<string, string> RadioTiers = new Dictionary<string, string>
        {
            { "6d6f645f726164696f303033", "C" },  // Kenwood TH-21BT
            { "6d6f645f726164696f303130", "C" },  // Realistic TRC-83
            { "6d6f645f726164696f303031", "C+" }, // Baofeng UV-5R
            { "6d6f645f726164696f303131", "C+" }, // Alinco (Fake)
            { "6d6f645f726164696f303034", "B" },  // Motorola T460
            { "6d6f645f726164696f303132", "B" },  // Kenwood ProTalk XLS
            { "6d6f645f726164696f303035", "B+" }, // Yaesu VX-8DR
            { "6d6f645f726164696f303133", "B+" }, // Motorola MTH800
            { "6d6f645f726164696f303036", "A" },  // Motorola DP4800
            { "6d6f645f726164696f303037", "A" },  // Motorola DP4601e
            { "6d6f645f726164696f303038", "A+" }, // Motorola XTS5000
            { "6d6f645f726164696f303032", "A+" }, // Р-187П1 «Азарт»
            { "6d6f645f726164696f303039", "S" },  // Harris AN/PRC-152
        };

        /// <summary>
        /// Battery type and runtime per radio, mirroring the entries registered with the battery
        /// mod. Shown as native attribute rows rather than description text.
        /// </summary>
        private static readonly Dictionary<string, (string Type, int Count, double Hours)> RadioBatteries =
            new Dictionary<string, (string, int, double)>
            {
                { "6d6f645f726164696f303033", ("AA", 1, 8) },
                { "6d6f645f726164696f303130", ("AA", 1, 8) },
                { "6d6f645f726164696f303031", ("AA", 2, 7) },
                { "6d6f645f726164696f303131", ("AA", 2, 7) },
                { "6d6f645f726164696f303034", ("AA", 1, 6) },
                { "6d6f645f726164696f303132", ("AA", 2, 6) },
                { "6d6f645f726164696f303035", ("CR123A", 1, 5) },
                { "6d6f645f726164696f303133", ("CR123A", 1, 5) },
                { "6d6f645f726164696f303036", ("CR123A", 2, 4) },
                { "6d6f645f726164696f303037", ("CR123A", 2, 4) },
                { "6d6f645f726164696f303038", ("CR123A", 2, 3) },
                { "6d6f645f726164696f303032", ("CR123A", 2, 3) },
                { "6d6f645f726164696f303039", ("CR123A", 2, 2.5) },
            };

        private static bool? _batteryModPresent;

        internal static bool IsRadio(string tplId)
        {
            return tplId != null && RadioTiers.ContainsKey(tplId);
        }

        /// <summary>Battery rows only make sense when a battery mod is actually running.</summary>
        private static bool BatteryModPresent()
        {
            if (_batteryModPresent == null)
            {
                _batteryModPresent = System.AppDomain.CurrentDomain.GetAssemblies()
                    .Any(a => a.GetName().Name == "ozen-BatteriesNotIncluded");
            }

            return _batteryModPresent.Value;
        }

        /// <summary>Shared with the other battery-aware patches so they don't each re-scan the AppDomain.</summary>
        internal static bool IsBatteryModPresent() => BatteryModPresent();

        /// <summary>Number of battery slots the given radio has, if it's a radio with any registered.</summary>
        internal static bool TryGetBatterySlotCount(string tplId, out int count)
        {
            if (tplId != null && RadioBatteries.TryGetValue(tplId, out var battery))
            {
                count = battery.Count;
                return true;
            }

            count = 0;
            return false;
        }

        private static string FormatRuntime(double hours)
        {
            int minutes = (int)System.Math.Round(hours * 60.0);
            return (minutes / 60) + "h " + (minutes % 60).ToString("00") + "m";
        }

        private static void AddAttribute(Item item, string label, string value)
        {
            item.Attributes.Add(new ItemAttributeClass(EItemAttributeId.Undefined)
            {
                DisplayNameFunc = () => label,
                Base = () => 0f,
                StringValue = () => value,
                FullStringValue = () => value,
                DisplayType = () => EItemAttributeDisplayType.Compact
            });
        }

        private static void Postfix(Item __instance)
        {
            if (__instance == null || !RadioTiers.TryGetValue(__instance.StringTemplateId, out string tier))
            {
                return;
            }

            AddAttribute(__instance,
                Plugin.L("ТИР", "TIER", "STUFE", "NIVEL", "NIVEAU", "POZIOM", "LIVELLO", "ÚROVEŇ"),
                tier);

            if (!BatteryModPresent() || !RadioBatteries.TryGetValue(__instance.StringTemplateId, out var battery))
            {
                return;
            }

            AddAttribute(__instance,
                Plugin.L("ПИТАНИЕ", "POWER", "STROM", "ENERGÍA", "ALIMENTATION", "ZASILANIE", "ALIMENTAZIONE", "NAPÁJENÍ"),
                battery.Count + "x " + battery.Type);

            AddAttribute(__instance,
                Plugin.L("ВРЕМЯ РАБОТЫ", "RUNTIME", "LAUFZEIT", "AUTONOMÍA", "AUTONOMIE", "CZAS PRACY", "AUTONOMIA", "VÝDRŽ"),
                FormatRuntime(battery.Hours));
        }
    }

    /// <summary>
    /// The radios sit in the Mod branch so they can carry battery slots, and Mod's own constructor
    /// adds weapon-attachment attributes (raid-moddable, effective distance, loudness, accuracy…).
    /// Those run after the base Item constructor, which is why stripping them there had no effect.
    /// This patch removes them at the point they are actually created.
    /// </summary>
    [HarmonyPatch(typeof(Mod), MethodType.Constructor, new[] { typeof(string), typeof(ModTemplate) })]
    internal static class RadioModAttributeCleanupPatch
    {
        private static readonly EItemAttributeId[] WeaponOnlyAttributes =
        {
            EItemAttributeId.RaidModdable,
            EItemAttributeId.EffectiveDistance,
            EItemAttributeId.Loudness,
            EItemAttributeId.Accuracy,
            EItemAttributeId.Ergonomics,
            EItemAttributeId.Recoil,
        };

        private static void Postfix(Mod __instance)
        {
            if (__instance == null || !TierAttributePatch.IsRadio(__instance.StringTemplateId))
            {
                return;
            }

            __instance.Attributes.RemoveAll(a => WeaponOnlyAttributes.Any(id => Equals(a.Id, id)));

            // The blue corner square on the inventory icon is the togglable on/off marker, drawn
            // whenever the item carries a TogglableComponent. Mod items get one; the radios are
            // switched from the mod's own hotkey, not from the inventory, so it is just noise.
            __instance.Components.RemoveAll(c => c is TogglableComponent);
        }
    }
}
