using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ApiCombatGame.Filters;

public class TagDescriptionsDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        swaggerDoc.Tags = new List<OpenApiTag>
        {
            CreateTag("Auth", "Player registration, login, and token management. Your journey begins here.", "shield", "#3b82f6", 1),
            CreateTag("Player", "View your profile, manage your unit roster, and unlock new combatants.", "user", "#8b5cf6", 2),
            CreateTag("Team", "Assemble squads of up to 5 units, assign battle strategies, and prepare for war.", "users", "#06b6d4", 3),
            CreateTag("Battle", "Enter the arena. Queue for matches, track ongoing battles, and review detailed results.", "swords", "#ef4444", 4),
            CreateTag("Leaderboard", "See who dominates the rankings. Global leaderboards sorted by Elo rating.", "trophy", "#f59e0b", 5),
            CreateTag("Strategy Marketplace", "Browse, upload, purchase, and rate community-created battle strategies.", "store", "#10b981", 6),
            CreateTag("Guild", "Create and manage guilds. Invite members, assign roles, coordinate via chat, and share strategies.", "shield", "#f59e0b", 7),
            CreateTag("Guild Boss", "Cooperative raid encounters. Rally your guild to defeat powerful bosses for shared rewards.", "dragon", "#dc2626", 8),
            CreateTag("Challenges", "Daily personalized objectives that reward currency and experience. Reset every 24 hours.", "target", "#f97316", 9),
            CreateTag("Mastery", "Track unit mastery progression. The more you battle with a unit, the stronger the bond.", "star", "#a855f7", 10),
            CreateTag("Modifiers", "Weekly environmental effects that shake up the meta. Adapt or fall behind.", "bolt", "#eab308", 11),
            CreateTag("Replays", "Create and share battle replays. Study your victories and learn from defeats.", "film", "#64748b", 12)
        };
    }

    private static OpenApiTag CreateTag(string name, string description, string icon, string color, int order)
    {
        return new OpenApiTag
        {
            Name = name,
            Description = description,
            Extensions =
            {
                ["x-icon"] = new OpenApiString(icon),
                ["x-color"] = new OpenApiString(color),
                ["x-order"] = new OpenApiInteger(order)
            }
        };
    }
}
