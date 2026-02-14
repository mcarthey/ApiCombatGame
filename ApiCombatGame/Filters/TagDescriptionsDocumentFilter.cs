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
            CreateTag("Leaderboard", "See who dominates the rankings. Global leaderboards sorted by API (Arena Power Index) rating.", "trophy", "#f59e0b", 5),
            CreateTag("Strategy Marketplace", "Browse, upload, purchase, and rate community-created battle strategies.", "store", "#10b981", 6),
            CreateTag("Guild", "Create and manage guilds. Invite members, assign roles, coordinate via chat, and share strategies.", "shield", "#f59e0b", 7),
            CreateTag("Guild Boss", "Cooperative raid encounters. Rally your guild to defeat powerful bosses for shared rewards.", "dragon", "#dc2626", 8),
            CreateTag("Challenges", "Daily personalized objectives that reward currency and experience. Reset every 24 hours.", "target", "#f97316", 9),
            CreateTag("Mastery", "Track unit mastery progression. The more you battle with a unit, the stronger the bond.", "star", "#a855f7", 10),
            CreateTag("Modifiers", "Weekly environmental effects that shake up the meta. Adapt or fall behind.", "bolt", "#eab308", 11),
            CreateTag("Replays", "Create and share battle replays. Study your victories and learn from defeats.", "film", "#64748b", 12),
            CreateTag("AI Practice", "Fight AI opponents to learn, experiment, and test strategies without risking your rating.", "robot", "#8b5cf6", 13),
            CreateTag("Ranked Seasons", "Compete in 8-week ranked seasons. Climb from Bronze to Legend and earn exclusive end-of-season rewards.", "trophy", "#f59e0b", 14),
            CreateTag("Loot", "Random loot drops from battles. Collect currency packs, XP boosts, rare titles, and critical gold jackpots.", "gift", "#f97316", 15),
            CreateTag("Referral", "Invite friends and earn rewards. Share your referral code for bonus gold when they sign up.", "share", "#10b981", 16),
            CreateTag("Unit Customization", "Gold sinks: rename units, reroll stats, and apply permanent golden upgrades.", "sparkles", "#eab308", 17),
            CreateTag("Rival", "Auto-assigned rivals for competitive motivation. Beat your nemesis for bonus gold.", "target", "#dc2626", 18),
            CreateTag("Battle Pass", "Seasonal 30-level progression with free and premium reward tracks. Earn XP from battles, challenges, and daily play.", "star", "#f59e0b", 19),
            CreateTag("Guild Wars", "Weekly guild vs guild matchups. Your ranked wins earn points — the winning guild gets a treasury bonus.", "swords", "#dc2626", 20),
            CreateTag("Tournament", "Weekly single-elimination tournaments. Pay entry fee, compete in brackets, win big prizes.", "trophy", "#f59e0b", 21),
            CreateTag("Cosmetics", "Premium cosmetic shop. Customize your profile, units, and battle effects with gems or gold.", "sparkles", "#a855f7", 22),
            CreateTag("Premium Plus", "Exclusive perks for Premium Plus subscribers. Multipliers, badges, monthly gems, and exclusive content.", "crown", "#f59e0b", 23),
            CreateTag("Activity Feed", "Your game journey in a timeline. Track battles, achievements, level-ups, and view other players' activity.", "activity", "#06b6d4", 24),
            CreateTag("Education", "Learn API concepts through guided gameplay. Create curricula, enroll students, and track progress.", "book", "#3b82f6", 25),
            CreateTag("SDK", "Developer tools: quick-start guide, endpoint catalog, code snippets, and game status. No auth required.", "code", "#64748b", 26),
            CreateTag("Discord", "Link your Discord account, register webhooks for game events, and enable bot commands.", "chat", "#5865F2", 27),
            CreateTag("Creators", "Content creator program. Apply, get verified, track content performance, and earn gem revenue share.", "star", "#f59e0b", 28)
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
