# Production Launch Checklist

Pre-flight checks before advertising and accepting public players.

> **Status:** Technical items verified, HN launch prep in progress (February 18, 2026)
> **Target:** Hacker News launch Friday February 20, 2026
> **Marketing action items:** See [MARKETING-STRATEGY.md](MARKETING-STRATEGY.md) § "Immediate Action Plan"

---

## Infrastructure

- [x] Production database (`SQL1002.site4now.net`) is accessible and migrations applied
- [x] App pool (`mcarthey-001nyy`) configured as 64-bit, dedicated, OutOfProcess
- [x] `appsettings.Production.json` has correct connection string, JWT secret, and base URL
- [x] HTTPS enforced (UseHttpsRedirection middleware)
- [x] Health check endpoint (`/health`) returns 200

## Security

- [x] JWT secret is unique, strong, and not committed to source control
- [x] reCAPTCHA v3 configured for both Login and Register (site key + secret key)
- [x] Password reset tokens are URL-safe, single-use, 1-hour expiry
- [x] Deleted accounts have PII anonymized (username, email, password hash cleared)
- [x] Rate limiting active on all API endpoints (Free: 60/min, Premium: 120/min, Premium+: 300/min)
- [x] Global exception middleware catches unhandled errors (no stack traces to users)
- [x] CORS configured for production domain only (`apicombat.com` + `www.apicombat.com`)

## Legal & Compliance

- [x] Terms of Service page live at `/Terms`
- [x] Privacy Policy page live at `/Privacy`
- [x] Cookie consent banner appears on first visit
- [x] Account deletion available in Settings (GDPR right to erasure)
- [x] Registration page links to both Terms and Privacy Policy
- [x] Age requirement (13+) stated in Terms of Service

## Email

- [x] SMTP credentials configured (`mail5013.site4now.net:587`)
- [x] FromAddress matches authenticated SMTP account (`support@apicombat.com`)
- [x] Welcome email sends on registration
- [x] Email verification sends on registration
- [x] Password reset email sends with correct reset link
- [x] Account deletion confirmation email sends
- [x] Contact form sends both notification and thank-you emails
- [x] All emails render correctly in Gmail, Outlook, Apple Mail (check CTA button colors)

## Features

- [x] Registration creates account with 3 starter units and 1,000 currency
- [x] Login works with email + password (reCAPTCHA validated)
- [x] Password reset flow: forgot → email → reset → login with new password
- [x] Email verification: banner on dashboard, resend button, verify link works
- [x] Account deletion: password confirmation, soft delete, sign out, confirmation email
- [x] Leaderboard page shows top 50 real players (no bots, no deleted)
- [x] Landing page renders with social proof and UTM param passthrough
- [x] Admin dashboard excludes bots from all metrics
- [x] Battle processing background service is running
- [x] Daily challenge generation is working
- [x] Mobile navigation works (hamburger menu with slide-out drawer)

## SEO & Marketing

- [x] Favicon displays in browser tab (SVG)
- [x] OpenGraph meta tags on all pages (title, description, image)
- [x] Canonical URLs set on key pages
- [x] Sitemap.xml generates at `/sitemap.xml` (9 URLs: /, api-docs, Leaderboard, Register, About, Login, Contact, Terms, Privacy)
- [x] Landing page has `noindex, nofollow` (ad traffic only)
- [x] About page live at `/About`
- [x] Domain `apicombat.com` resolves to production server

## Monitoring & Support

- [x] Application logs writing to AppLog table
- [x] Admin log viewer accessible at `/Admin/Logs`
- [x] Error pages configured (404, 500)
- [x] Admin dashboard overview metrics accurate (DAU, MRR, signups)
- [x] Activity ledger tracking all numeric state changes (46 audit points across 17 services)
- [x] Support timeline on admin PlayerDetail page (filterable by property)
- [x] Rating reconciliation tool on Admin/Tools (preview + execute)
- [ ] Verify admin can monitor errors in real-time during launch (keep `/Admin/Logs` open)

## HN Traffic Readiness

- [ ] Verify site4now SQL Server connection limit (EF Core defaults to 100 pool)
- [ ] Test cold start time after app pool recycle (MigrateAsync + seed data under load)
- [ ] Confirm rate limiting holds under concurrent requests (Free: 60/min)
- [ ] Have a plan for scaling if traffic exceeds shared hosting capacity

## Pre-Launch Smoke Test

- [x] `dotnet build` — 0 errors, 0 warnings
- [x] All automated tests pass (574 tests)
- [ ] Deploy to production via MSDeploy (recycle app pool first)
- [ ] Verify production health check after deploy
- [ ] Verify LedgerEntries migration applied on production
- [ ] Manual smoke test: register → fight battle → check Support Timeline shows entries
- [ ] Manual smoke test: buy cosmetic → admin PlayerDetail → filter Currency → verify ledger
- [ ] Manual smoke test: admin adjust rating → verify in Support Timeline as "AdminAction"
- [ ] Test registration flow on production
- [ ] Test password reset flow on production
- [ ] Full manual smoke test sequence (see [MANUAL_TEST.md](MANUAL_TEST.md))
