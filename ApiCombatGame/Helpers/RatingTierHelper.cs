namespace ApiCombatGame.Helpers;

/// <summary>
/// Maps a player's API (Arena Power Index) rating to dev-meme rank tiers.
/// </summary>
public static class RatingTierHelper
{
    public static (string Name, string Flavor, string CssClass) GetTier(int rating) => rating switch
    {
        >= 1900 => ("I Use Arch btw", "You mass-assigned everything. Legendary.", "api-tier-arch"),
        >= 1700 => ("Wizard", "Any sufficiently advanced code...", "api-tier-wizard"),
        >= 1500 => ("10x Dev", "Mythical creature spotted", "api-tier-10x"),
        >= 1300 => ("Bug Hunter", "It's not a bug, it's a feature", "api-tier-bughunter"),
        >= 1100 => ("Code Monkey", "It works. Don't touch it.", "api-tier-codemonkey"),
        >= 800  => ("Copy Pasta", "ctrl+c, ctrl+v, ship it", "api-tier-copypasta"),
        _       => ("Rubber Duck", "At least someone's listening", "api-tier-rubberduck")
    };
}
