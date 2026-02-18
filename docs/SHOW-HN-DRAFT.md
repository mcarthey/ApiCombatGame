# Show HN Draft

**Title:** Show HN: API Combat – A PvP game with no UI, played entirely through REST API

**URL:** https://apicombat.com

**Text:**

I teach .NET at a technical college (WCTC) and needed a way to make API consumption actually fun to learn. So I built a turn-based combat game where the only interface is a REST API. No frontend. You bring your own client — curl, Python script, React app, whatever can send HTTP requests.

Register, get a JWT, build a roster of units, set battle strategies in JSON, and queue fights. Battles resolve server-side turn-by-turn. You get back a full replay log.

```
curl -X POST https://apicombat.com/api/v1/auth/register \
  -H "Content-Type: application/json" \
  -d '{"username":"HNReader","email":"you@example.com","password":"SecurePass1!"}'
```

Rating uses an Elo system with dev-meme tiers (Rubber Duck through I Use Arch btw). There are 100+ endpoints covering battles, teams, guilds, tournaments, loot, and a battle pass.

Tech: ASP.NET Core 8, MSSQL, JWT auth, hosted on SmarterASP.NET. OpenAPI 3.0 spec available.

- Live: https://apicombat.com (also https://apicombat.dev)
- API docs: https://apicombat.com/api-docs/v1
- Python starter client: https://github.com/api-combat-game/python-starter
- Leaderboard: https://apicombat.com/Leaderboard
- Discord: https://discord.gg/jfSCSfAN49

My students use it as their final project — they build clients in whatever language they're learning. Turns out "build a game client" is a better assignment than "build another todo app."

Happy to answer any questions about the architecture or game design.
