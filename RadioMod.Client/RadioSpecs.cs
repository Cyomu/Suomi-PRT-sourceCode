using System.Collections.Generic;

namespace RadioMod.Client
{
    /// <summary>
    /// Real-world specifications for the thirteen radios: who made them, when, on what band, at what
    /// power. Reference material only — the balance lives in <see cref="RadioProfiles"/> and is not
    /// touched by anything here.
    ///
    /// Deliberately a separate table rather than extra fields on <c>RadioProfile</c>. Those two sets
    /// of numbers have different owners: the profile is tuned by ear against gameplay, while this is
    /// looked up from datasheets and verified by a human. Mixing them would make it impossible to
    /// tell which numbers may be freely retuned.
    ///
    /// Collected and checked by the user against manufacturer datasheets, radiomuseum/rigpix and,
    /// for the military sets, public documentation. Where the sources disagree or say nothing, the
    /// field is left unknown on purpose — a plausible-looking invented figure would be worse than an
    /// admitted gap in a mod whose whole point is that the radios are real.
    /// </summary>
    public partial class Plugin
    {
        /// <summary>Value could not be established from open sources.</summary>
        private const string SpecUnknown = null;

        /// <summary>No channel memory at all — the frequency is set by hand.</summary>
        private const string SpecNoMemory = "@nomemory";

        /// <summary>Channel count is not a meaningful figure for this set (TETRA talkgroups).</summary>
        private const string SpecNotFixed = "@notfixed";

        private struct RadioSpec
        {
            public string Maker;
            public string Country;
            public string Year;
            public string Band;
            public string Power;
            public string Modulation;
            public string Weight;
            public string Channels;
        }

        private static readonly Dictionary<string, RadioSpec> RadioSpecs = new Dictionary<string, RadioSpec>
        {
            [KenwoodTplId] = new RadioSpec
            {
                Maker = "Kenwood", Country = "JP", Year = "1986",
                Band = "144–148 MHz", Power = "1 W / 150 mW",
                Modulation = "FM (F3)", Weight = "290 g",
                // Not zero-as-a-number: the set has no memory at all, the frequency is dialled in
                // on thumbwheels. Printing "0" would read as a broken value.
                Channels = SpecNoMemory,
            },

            [Trc83TplId] = new RadioSpec
            {
                Maker = "Radio Shack / Realistic", Country = "US", Year = "1980",
                Band = "27 MHz (CB)", Power = "1 W",
                Modulation = "AM", Weight = "454 g", Channels = "3",
            },

            [BaofengTplId] = new RadioSpec
            {
                Maker = "Baofeng", Country = "CN", Year = "2012",
                Band = "136–174 / 400–520 MHz", Power = "5 W / 1 W",
                Modulation = "FM", Weight = "241 g", Channels = "128",
            },

            [KenwoodProTalkTplId] = new RadioSpec
            {
                Maker = "Kenwood", Country = "JP", Year = "2006",
                Band = "460–470 MHz", Power = "1.5 W / 0.5 W",
                Modulation = "FM", Weight = "155 g",
                // User-selectable channels, not the 56/64 preset frequencies underneath them.
                Channels = "6",
            },

            [T460TplId] = new RadioSpec
            {
                Maker = "Motorola", Country = "US", Year = "2015",
                Band = "462–467 MHz (FRS/GMRS)", Power = "2 W",
                Modulation = "FM", Weight = "200 g", Channels = "22",
            },

            [YaesuTplId] = new RadioSpec
            {
                Maker = "Yaesu / Vertex Standard", Country = "JP", Year = "2008",
                Band = "50 / 144 / 222 / 430 MHz", Power = "5 W (1.5 W @ 222)",
                Modulation = "FM / NFM, AM @ 50", Weight = "240 g", Channels = "900",
            },

            [Mth800TplId] = new RadioSpec
            {
                Maker = "Motorola", Country = "US", Year = "2005",
                Band = "380–430 / 440–470 MHz", Power = "1 W",
                Modulation = "TETRA (π/4-DQPSK)", Weight = "247 g",
                // A TETRA terminal works in talkgroups on a network; a channel count would be a
                // number with no meaning behind it.
                Channels = SpecNotFixed,
            },

            [Dp4601eTplId] = new RadioSpec
            {
                Maker = "Motorola Solutions", Country = "US", Year = "2016",
                Band = "VHF / UHF / 300 MHz", Power = "5 W VHF, 4 W UHF",
                Modulation = "DMR / FM", Weight = "315 g", Channels = "1000",
            },

            [Dp4800TplId] = new RadioSpec
            {
                Maker = "Motorola Solutions", Country = "US", Year = "2013",
                Band = "136–174 / 403–527 MHz", Power = "5 W VHF, 4 W UHF",
                Modulation = "DMR / FM", Weight = "425 g", Channels = "1000",
            },

            [Xts5000TplId] = new RadioSpec
            {
                Maker = "Motorola", Country = "US", Year = "2002",
                Band = "VHF / UHF / 700–800 MHz", Power = "1–6 W",
                Modulation = "P25 (IMBE/C4FM) / FM", Weight = "400 g",
                // Genuinely model-dependent, so both are shown rather than picking one.
                Channels = "48 / 1000",
            },

            [AzartTplId] = new RadioSpec
            {
                Maker = "НПО «Ангстрем»", Country = "RU", Year = "2013",
                Band = "30–512 MHz", Power = SpecUnknown,
                Modulation = "TETRA / SDR", Weight = SpecUnknown, Channels = SpecUnknown,
            },

            [HarrisTplId] = new RadioSpec
            {
                Maker = "Harris / L3Harris", Country = "US", Year = "2005",
                Band = "30–512 MHz", Power = "0.25–5 W",
                Modulation = "AM / FM / PSK / CPM", Weight = "1.2 kg",
                Channels = "1 + 99 presets",
            },

            // Alinco is deliberately absent. It is marked as a replica in the mod and its card shows
            // "specifications could not be determined" instead — an empty passport on a radio that
            // behaves strangely is characterisation, not a missing row.
        };

        private static bool TryGetRadioSpec(string tplId, out RadioSpec spec)
        {
            spec = default;
            return tplId != null && RadioSpecs.TryGetValue(tplId, out spec);
        }

        /// <summary>Renders a spec value, turning the markers into readable localised text.</summary>
        private string SpecText(string value)
        {
            if (value == SpecNoMemory)
            {
                return L("нет памяти каналов", "no channel memory", "kein Kanalspeicher", "sin memoria de canales",
                    "aucune mémoire de canaux", "brak pamięci kanałów", "nessuna memoria canali", "bez paměti kanálů");
            }

            if (value == SpecNotFixed)
            {
                return L("не применимо", "not applicable", "nicht zutreffend", "no aplicable",
                    "sans objet", "nie dotyczy", "non applicabile", "neuplatňuje se");
            }

            if (string.IsNullOrEmpty(value))
            {
                return L("нет данных", "no data", "keine Daten", "sin datos",
                    "aucune donnée", "brak danych", "nessun dato", "bez údajů");
            }

            return value;
        }
    }
}
