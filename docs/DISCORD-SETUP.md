# Discord Server Setup Guide — API Combat

Server name: **API Combat**

---

## Categories & Channels

### HQ (3 channels)

| Channel | Purpose | Topic / Welcome Message |
|---------|---------|------------------------|
| `#welcome` | Read-only landing pad with server rules and game links. | See [Welcome Message Draft](#welcome-message-draft) below. |
| `#announcements` | Read-only channel for patch notes, new endpoints, downtime notices, and events. | "Game updates, new endpoints, and event announcements. Stay sharp." |
| `#role-select` | Self-assign roles (rating tier, preferred language, notifications). | "Pick your tier, claim your stack. Roles are cosmetic until you prove them on the ladder." |

### Community (4 channels)

| Channel | Purpose | Topic / Welcome Message |
|---------|---------|------------------------|
| `#general` | Main hangout for anything API Combat related. | "Talk shop, talk trash, talk API. Keep it dev-friendly." |
| `#show-your-client` | Share screenshots, repos, or demos of the clients people build. | "Built something cool? POST it here. All languages welcome, even PHP." |
| `#strategies` | Discuss builds, tier lists, matchup theory, and rating climbing tips. | "Theorycraft your way from Rubber Duck to I Use Arch btw." |
| `#bugs-and-feedback` | Bug reports and feature requests straight from the community. | "Found a bug? That's a feature request. Format: endpoint, payload, expected vs actual." |

### Dev Help (2 channels)

| Channel | Purpose | Topic / Welcome Message |
|---------|---------|------------------------|
| `#getting-started` | Help for new players setting up auth, making first API calls, and understanding the docs. | "New here? Start with the docs at https://apicombat.com/api-docs/v1 then ask away." |
| `#code-help` | Troubleshooting HTTP clients, auth flows, parsing responses, and anything code-related. | "Stuck on a 401? Payload not parsing? Drop your code (use backticks) and we'll help debug." |

**Total: 9 channels across 3 categories.** One slot left if something organic emerges.

---

## Roles

### Rating Tier Roles (color-coded, cosmetic)

Assign these manually or via a bot that reads the API Combat leaderboard. Colors are suggestions — pick whatever fits the Discord theme.

| Role | Color (hex) | Description |
|------|-------------|-------------|
| Rubber Duck | `#FFD700` (gold/yellow) | The starting tier. Everyone begins here. |
| Copy Pasta | `#FFA500` (orange) | You can copy-paste curl commands. Congrats. |
| Code Monkey | `#8B4513` (brown) | You wrote an actual client. Respect. |
| Bug Hunter | `#2ECC71` (green) | You exploit edge cases for fun. |
| 10x Dev | `#3498DB` (blue) | Consistently winning. Scary good. |
| Wizard | `#9B59B6` (purple) | Top-tier. Your client basically plays itself. |
| I Use Arch btw | `#E74C3C` (red) | Peak. You have transcended. |

### Staff / Utility Roles

| Role | Purpose |
|------|---------|
| `Admin` | Server admins. Full permissions. |
| `Moderator` | Can mute, kick, manage threads. |
| `Bot` | For any bots added to the server. |

---

## Welcome Message Draft

Post this in `#welcome` as an embed (or pinned message):

> **Welcome to API Combat**
>
> The API is the game.
>
> API Combat is a PvP game played entirely through REST API endpoints. There is no GUI — you build your own client, call the endpoints, and fight your way up the Arena Power Index leaderboard.
>
> **Quick Links**
> - Game: https://apicombat.com
> - API Docs: https://apicombat.com/api-docs/v1
> - OpenAPI Spec: https://apicombat.com/openapi/v1.json
>
> **How It Works**
> 1. Register at apicombat.com and grab your JWT token.
> 2. Build a client in any language — curl counts.
> 3. Call the API to create characters, equip items, and queue for battles.
> 4. Climb the rating tiers from Rubber Duck to I Use Arch btw.
>
> **Server Rules**
> 1. Be cool. Dev humor is encouraged, toxicity is not.
> 2. No sharing exploit payloads that break the game for others — report them in `#bugs-and-feedback`.
> 3. Keep self-promo to `#show-your-client`.
> 4. Spoiler-tag any strategy content that reveals non-obvious mechanics.
> 5. Have fun. It's a game about HTTP requests. Don't take it too seriously.
>
> Head to `#role-select` to pick your tier, then say hi in `#general`.

---

## Bot Suggestions

Keep it minimal. Two bots max at launch.

| Bot | Why |
|-----|-----|
| **Carl-bot** (or MEE6) | Role-select reaction roles in `#role-select`, welcome DMs, basic moderation (auto-mod, word filters). One bot handles all of this. |
| **Custom webhook** (optional) | POST from the API Combat backend to `#announcements` whenever a patch deploys or a new event starts. No bot framework needed — just a Discord webhook URL stored in server config. |

Skip music bots, leveling bots, and anything that adds noise. The game already has a rating system.

---

## Server Settings Checklist

- [ ] Verification level: **Medium** (must have a verified email)
- [ ] Explicit content filter: **Scan messages from all members**
- [ ] Default notification setting: **Only @mentions**
- [ ] `#welcome` and `#announcements` set to read-only (only admins/bots can post)
- [ ] Slowmode on `#general`: 5 seconds (prevents spam, barely noticeable)
- [ ] Community features enabled if you want Server Discovery later
- [ ] Server icon: API Combat logo (crossed swords)
- [ ] Server banner/invite splash: dark background, tagline "The API is the game"
