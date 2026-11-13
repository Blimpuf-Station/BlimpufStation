using Robust.Shared.Enums;

namespace Content.Shared._Blimpuf.Preferences;

public static class PronounFormatter
{
    public static string Format(Gender primary, Gender? secondary = null)
    {
        return Format(primary.ToString().ToLowerInvariant(), secondary?.ToString().ToLowerInvariant());
    }

    public static string Format(string primary, string? secondary = null)
    {
        // They/them fallback if primary pronoun isn't defined
        primary = primary is "male" or "female" or "neuter" ? primary : "epicene";

        return secondary is { } secondaryGender
            ? Loc.GetString("blimpuf-pronouns-pair",
                ("primary", Loc.GetString($"blimpuf-pronouns-subject-{primary}")),
                ("secondary", Loc.GetString($"blimpuf-pronouns-subject-{secondaryGender}")))
            : Loc.GetString($"humanoid-profile-editor-pronouns-{primary}-text");
    }
}
