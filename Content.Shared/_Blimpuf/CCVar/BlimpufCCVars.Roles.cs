using Robust.Shared.Configuration;

namespace Content.Shared._Blimpuf.CCVar;

public sealed partial class BlimpufCCVars
{
    /// <summary>
    /// Comma-separated discordJobTimeOverride prototype IDs. Empty disables Discord playtime overrides.
    /// </summary>
    public static readonly CVarDef<string> DiscordJobTimeOverrides =
        CVarDef.Create("game.discord_job_time_overrides", "", CVar.SERVER | CVar.REPLICATED);
}
