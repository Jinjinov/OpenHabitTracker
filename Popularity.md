# Popularity / Marketing TODO

All suggestions for making OpenHabitTracker more popular, ordered by impact-per-hour.
Based on a June 2026 audit of online presence.

---------------------------------------------------------------------------------------------------

## Current presence snapshot (June 2026)

- GitHub: 250 stars, 19 forks (repo since Nov 2023)
- Google Play: 1K+ downloads, 4.2 stars, only 8 reviews
- Flathub: ~274 downloads/month
- Hacker News: 3 self-posts, 2-3 points each, 0 comments
- AlternativeTo: listed, 5 likes, 0 comments
- awesome-selfhosted: listed (asset!)
- Product Hunt: launched 2024, 3 upvotes, 4 followers - fizzled
- dev.to: 9 articles, single-digit reactions
- Reddit: ~40 posts since 2021 + own r/OpenHabitTracker sub

Diagnosis: distribution channels are excellent (8+ stores), discovery is the bottleneck.
The problem is not missing channels - it is title/framing on the channels already used.

---------------------------------------------------------------------------------------------------

## What the Reddit history proves (own data, 2021-2026)

Posts that worked - led with the hook:

| Post | Score |
|---|---|
| 2023 r/dotnet - "I made a Blazor app that runs on 6 platforms" | 129 pts |
| 2023 r/Blazor - same | 67 pts |
| 2021 r/getdisciplined - "free habit tracker that tracks an average time..." | 59 pts |
| 2024 r/Blazor - "How to create a Blazor app for WASM, Windows..." | 42 pts |
| 2021 r/opensource - "I made an open source habit tracker app" | 38 pts |

Posts that flopped - led with the brand name:

| Post | Score |
|---|---|
| 2024-2025 "OpenHabitTracker 1.0.8 / 1.0.9 / 1.1.0 / 1.1.2 is here" (many subs) | 1-3 pts |
| 2025 r/selfhosted - "I created OpenHabitTracker" | 1 pt, 0 comments |
| 2025 x5 subs same day - "I improved OpenHabitTracker" | 1 pt each |

Rules derived from this:

1. Never title a post with the product name. Title with the differentiator.
   Readers who have never heard of OpenHabitTracker have zero reason to click a version announcement.
2. Never cross-post identical text to multiple subs in one day - repeated same-domain
   links in quick succession get algorithmically buried, and each community can tell
   the post was not written for them. One sub per week, each post native to that audience.
3. Release announcements belong only in r/OpenHabitTracker.
4. Dev content consistently outperforms product content (42-129 pts vs 1-3 pts) -
   the Blazor/.NET community responds well; consumer framing is what needs fixing.

---------------------------------------------------------------------------------------------------

## 1. Re-do r/selfhosted with a hook title, not a brand title

Highest-leverage single action. Beaver Habits, HabitTrove, and HabitSync all grew
primarily through r/selfhosted - HabitTrove got a Noted.lol write-up off the back of it.

- Title pitches the differentiator, e.g.:
  "Self-hosted habit/task tracker - one docker-compose, native apps on all 6 platforms sync to it, no account required"
- Text post (not link post), 2-3 screenshots inline, docker-compose snippet in the body
- Mention the REST API with OpenAPI/Scalar UI - self-hosters love scriptability
- Answer every comment for the first 24 hours
- Then separate, tailored posts weeks apart:
  - r/dotnet - the 6-UI-hosts architecture story
  - r/degoogle - Google Keep import + local-only data
  - r/androidapps - privacy angle
  - r/opensource - GPL-3.0, no account, no telemetry

## 2. Anti-streak angle for r/getdisciplined / r/productivity

The elapsed-time-vs-interval ratio is genuinely different and already scored 59 pts once (2021).
"Streak anxiety" is a recurring complaint in those subs.

- Frame as: "I built a habit tracker without streak guilt - it shows how overdue you are instead"
- Discussion starter, not an ad

## 3. Submit to IzzyOnDroid

F-Droid proper is impossible with MAUI (documented in r/OpenHabitTracker post), but
IzzyOnDroid ships prebuilt APKs straight from GitHub releases - sidesteps exactly that problem.

- The APK is already produced each release (currently FTP-uploaded to the server) -
  attach it to GitHub releases too, then file one inclusion request
- The degoogled-Android crowd that refuses Google Play is exactly the target audience

## 4. Submit Docker templates to self-hosting app stores

Each is a small JSON/YAML template submission, an afternoon each.
Competitors (Beaver Habits) are already in Unraid CA.

- Unraid Community Apps
- Umbrel
- CasaOS
- Runtipi

## 5. Add an in-app review prompt

8 reviews on 1K+ Play installs leaves store ranking on the table - review count is a major
ranking signal. Compounds forever at zero ongoing cost.

- After the Nth habit completion (N ~ 10, a user who is clearly active),
  show a one-time "Enjoying OpenHabitTracker?" prompt
- Use the native in-app review APIs (Google Play In-App Review, SKStoreReviewController)

## 6. Localize the store listings

The app speaks 20 languages but the store listings may not. Store search is per-locale -
a German listing makes the app discoverable to German searches.

- All translated strings already exist in Localization/Resources
- Per Release.md, listings are manual via web UI: Microsoft Partner Center,
  Google Play Console, App Store Connect, Docker Hub
- Mostly copy-paste into the store consoles

## 7. Turn the comparison article into SEO pages on openhabittracker.net

The "vs Habitica, Loop, Streaks, Everyday" piece lives on dev.to where it earns dev.to SEO.

- Loop Habit Tracker is Android-only with ~8k stars; people constantly search for
  "Loop Habit Tracker for iPhone/desktop" - a page targeting exactly that query
  is a permanent free traffic source
- Same for "open source Google Keep alternative" (the importer already exists!)

## 8. Relaunch on Product Hunt

The 2024 launch got 3 upvotes - effectively a blank slate. PH explicitly allows
relaunches for major updates after 6 months.

- Relaunch with 1.2.x / 1.3.0
- Prepared assets, a 30-second GIF, Tuesday-Thursday timing
- Lead with the privacy / no-account angle
- Even a mediocre launch yields backlinks and AlternativeTo/SaaSHub citation boost

## 9. Pitch the Blazor story to Microsoft's community channels

A production app sharing one Blazor codebase across WASM/MAUI/Photino/WinForms/WPF/Server
is a showcase Microsoft actively looks for. Plays to the demonstrated strength
(dev content outperforms product content).

- Email James Montemagno or the .NET Community Standup (they take submissions)
- Submit to "This Week in .NET"
- One feature there is worth hundreds of GitHub stars; stars are social proof
  that feeds everything else

## 10. Pitch self-hosting blogs / newsletters

- Noted.lol writes up self-hosted apps (covered HabitTrove)
- selfh.st (Self-Host Weekly) has a new-software section - submit

---------------------------------------------------------------------------------------------------

## Skip list (deliberately not doing)

- More dev.to articles - audience proved too small (9 articles, single-digit reactions)
- Paid anything
- Mastodon/X presence-building - ongoing cost, slow payoff for a hobby project
- Bare HN Show posts - already tried 3x with 0 comments; if retried, use the
  r/selfhosted-style text with screenshots instead of a bare link, Tue-Thu morning ET
- F-Droid proper - impossible with MAUI (use IzzyOnDroid instead, see #3)

---------------------------------------------------------------------------------------------------

## If only two things get done

1. r/selfhosted post with hook title (#1)
2. IzzyOnDroid submission (#3)

Both are one-evening efforts aimed at communities actively searching for exactly this app.
