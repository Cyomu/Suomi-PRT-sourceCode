using System.Collections.Generic;
using SemanticVersioning;
using SPTarkov.Server.Core.Models.Spt.Mod;

namespace RadioMod.Server
{
    /// <summary>
    /// SPT 4.1 replaced the AbstractModMetadata base class with the IModMetadata interface: every
    /// property is implemented directly instead of overridden, optional ones are nullable rather
    /// than assigned null!, and IsBundleMod is gone from the contract entirely — bundles are picked
    /// up from the mod folder without being declared.
    /// </summary>
    public sealed record RadioModMetadata : IModMetadata
    {
        public string ModGuid { get; init; } = "com.suomi.makshepard.smprt";
        public string Name { get; init; } = "S&M-PRT (experimental)";
        public string Author { get; init; } = "Suomi & makshepard";
        public List<string>? Contributors { get; init; } = new List<string> { "Suomi", "makshepard" };
        public Version Version { get; init; } = new Version("1.0.2");
        public Range SptVersion { get; init; } = new Range("~4.1.0");
        public bool HasPrepatcher { get; init; }
        public List<string>? Incompatibilities { get; init; }
        public Dictionary<string, Range>? ModDependencies { get; init; }
        public string? Url { get; init; }
        public string License { get; init; } = "MIT";
    }
}
