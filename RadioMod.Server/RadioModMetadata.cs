using System.Collections.Generic;
using SemanticVersioning;
using SPTarkov.Server.Core.Models.Spt.Mod;
namespace RadioMod.Server
{
    public record RadioModMetadata : AbstractModMetadata
    {
        public override string ModGuid { get; init; } = "com.suomi.radiomod";
        public override string Name { get; init; } = "PRTPortableRadioTransmitter";
        public override string Author { get; init; } = "Suomi";
        public override List<string> Contributors { get; init; } = new List<string> { "makshepard" };
        public override Version Version { get; init; } = new Version("0.9.6");
        public override Range SptVersion { get; init; } = new Range("~4.0.0");
        public override List<string> Incompatibilities { get; init; } = null!;
        public override Dictionary<string, Range> ModDependencies { get; init; } = null!;
        public override string Url { get; init; } = null!;
        public override bool? IsBundleMod { get; init; } = true;
        public override string License { get; init; } = "MIT";
    }
}
