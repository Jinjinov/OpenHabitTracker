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

---------------------------------------------------------------------------------------------------
---------------------------------------------------------------------------------------------------
---------------------------------------------------------------------------------------------------

# APPENDIX: Execution instructions for Claude (Sonnet / Opus / any future session)

Everything below repeats and expands the overview above. The overview stays short on purpose;
this appendix is the full execution context so a future session does not have to re-derive it.

## Ground rules for the executing model

- READ THIS WHOLE FILE FIRST. The audit data above (June 12, 2026) is the baseline.
- The user prepares everything; the USER posts/submits manually. Never post to Reddit,
  Product Hunt, forums, or store consoles autonomously. Produce artifacts (files, drafts,
  PRs-ready branches) and stop.
- VERIFY ALL EXTERNAL SCHEMAS AND POLICIES AGAINST LIVE DOCS before writing files.
  Training data on app-store manifest formats, character limits, and submission processes
  is reliably stale. For each store/template task below, first fetch the current docs
  and at least one recently-merged example.
- Before claiming "X was never tried", grep this repo (especially OpenHabitTracker.Web/index.html)
  for platform links. Reddit blocks crawlers — use the PullPush API
  (https://api.pullpush.io/reddit/search/submission/?author=Jinjinov) or subreddit RSS
  (https://www.reddit.com/r/OpenHabitTracker.rss with a browser User-Agent via curl).
- Coding conventions (from OpenHabitTracker.md): NEVER use `var` — always explicit type.
  Use target-typed `new()`: `List<string> items = new();`
- Any new user-facing string requires a key in ALL 20 JSON files in
  OpenHabitTracker/Localization/Resources/ (file per language: en.json, de.json, ...).
  Slovenian grammatical gender: "habit" = "navada" (feminine) — agreement matters.
- The user works on Linux and Windows (two machines, two Claude instances).
  This file is the shared source of truth for marketing work.
- Hobby project: prefer the simplest implementation that works. No new infrastructure,
  no paid services, no solutions that need ongoing maintenance.

---------------------------------------------------------------------------------------------------

## A. In-app review prompt (#5) — IMPLEMENTATION SPEC

Goal: raise Play Store / App Store review count (currently 8 reviews on 1K+ Play installs).

Architecture — follow the existing platform-abstraction pattern (IOpenFile / ISaveFile /
INavBarFragment, see OpenHabitTracker.md "Dependency injection wiring"):

1. New interface in the core or Blazor shared project, e.g.:
       public interface IAppRating { Task RequestReview(); bool IsSupported { get; } }
2. Real implementation ONLY in OpenHabitTracker.Blazor.Maui (the only store-distributed host):
   - Preferred: NuGet `Plugin.StoreReview` (James Montemagno) — `CrossStoreReview.Current.RequestReview()`
     wraps Google Play In-App Review (Android) and SKStoreReviewController (iOS/macCatalyst).
     VERIFY on nuget.org that it supports the current MAUI/.NET version before adding;
     if unmaintained, fall back to direct bindings:
     Android: Xamarin.Google.Android.Play.Review.Ktx / Microsoft.Maui ApplicationModel,
     iOS: UIKit SKStoreReviewController.RequestReview(UIWindowScene).
   - Windows (MSIX/Microsoft Store): StoreContext.RequestRateAndReviewAppAsync — only works
     when installed from Microsoft Store; guard accordingly. OK to skip in v1 (low review impact).
3. No-op implementation (IsSupported=false, RequestReview returns CompletedTask) registered in
   ALL other entry points: Wasm, Web, Photino, WinForms, Wpf. Registration goes in each
   entry point's Program.cs / MauiProgram.cs next to the existing IOpenFile/ISaveFile lines.

Trigger logic:
- Trigger: after the user completes a habit for the Nth time (N = 10) across all sessions.
- DO NOT add columns to SettingsEntity for this — that forces a DB migration (see TODO.md
  for how carefully migrations are treated). Use MAUI Preferences
  (Microsoft.Maui.Storage.Preferences) inside the MAUI implementation:
  keys "ReviewPromptCompletionCount" (int) and "ReviewPromptShown" (bool).
  Since Preferences only exists in MAUI, put the counting inside the MAUI IAppRating
  implementation (e.g. a method `RecordHabitCompletion()` on the interface, no-op elsewhere).
- Call site: where a habit is marked done — HabitService / HabitComponent
  (find the MarkAsDone / done-time-add path; verify current method names before editing).
- Show at most ONCE ever (per install). Native APIs are quota-limited anyway
  (Apple ~3x/year, Google quota undocumented) and both policies discourage pre-prompt
  gating ("Enjoying the app?" filters) — call RequestReview() directly, no custom dialog.
  This also means: NO new localization strings needed if there is no custom UI. If the user
  later wants a gentle pre-prompt, that adds strings to all 20 JSON files.

Definition of done: solution builds for at least one desktop target + `dotnet build` of the
MAUI project for android target; no `var`; all 6 entry points register IAppRating.

---------------------------------------------------------------------------------------------------

## B. Self-hosting app store templates (#4) — PER-STORE SPECS

Shared facts for all four templates (from DockerHub.md / docker-compose.yml):
- Image: `jinjinov/openhabittracker:latest` (also ghcr.io/jinjinov/openhabittracker)
- Internal port 8080; suggested external 5050
- Env vars: AppSettings__UserName, AppSettings__Email, AppSettings__Password,
  AppSettings__JwtSecret (JWT secret should be a generated 32-byte base64 value —
  use each store's secret-generation mechanism, see below)
- Volume: /app/.OpenHabitTracker (SQLite data)
- Single-user app; login at /login; logs at /watchdog; OpenAPI UI at /scalar/v1
- Icon: net.openhabittracker.OpenHabitTracker.svg in repo root (stores need PNG —
  render e.g. 256x256 PNG; check each store's required size)
- Short description: "Take Markdown notes, plan tasks, track habits — free, ad-free,
  open source, all data on your server. Native apps for Windows/Linux/Android/iOS/macOS sync to it."

1. Unraid Community Apps:
   - Create GitHub repo `Jinjinov/unraid-templates` containing `openhabittracker.xml`
     (Unraid CA template XML: Repository, WebUI http://[IP]:[PORT:8080]/, Network bridge,
     env vars as <Config> entries, volume mapping, Icon URL, Overview, Support URL,
     Project URL, Category: Productivity:).
   - Submission: post in the Community Applications "template repository request" thread on
     the Unraid forums (VERIFY current process at forums.unraid.net).
2. Umbrel:
   - PR to github.com/getumbrel/umbrel-apps: new folder `openhabittracker/` with
     `umbrel-app.yml` (manifest: id, name, tagline, category, version, port, gallery images,
     dependencies, releaseNotes) + `docker-compose.yml` using their `app_proxy` pattern.
   - JWT secret: Umbrel derives deterministic secrets from $APP_SEED — check a recent
     merged app PR for the current idiom.
3. CasaOS:
   - PR to github.com/IceWhaleTech/CasaOS-AppStore: docker-compose.yml with `x-casaos`
     extension block (architectures, main, description, tagline, icon, screenshots,
     port_map, scheme). Multi-language fields supported — reuse the 20 translations
     for description/tagline (en, de, es, fr, it, ja, ko, nl, pl, pt, sv, zh at minimum;
     check which locale codes CasaOS accepts).
4. Runtipi:
   - PR to github.com/runtipi/runtipi-appstore: folder with `config.json`
     (form_fields support `"type": "random"` for generated secrets — use for JwtSecret),
     `docker-compose.json` (their custom compose schema), `metadata/description.md`,
     `metadata/logo.jpg`.

For EVERY store: fetch the current schema/docs AND one recently-merged app PR first;
copy the living idiom, not the documented one. Validate locally where a linter exists.
Output: four ready-to-submit branches/folders + a short SUBMIT-HOWTO note per store for the user.

---------------------------------------------------------------------------------------------------

## C. APK on GitHub releases + IzzyOnDroid (#3)

- Per Release.md, a signed APK is already produced each release and FTP-uploaded to the server.
  Smallest viable change: add one step to Release.md's checklist:
      gh release create <tag> --title "<version>" --notes-file <notes>   (if not created)
      gh release upload <tag> <path-to-apk>
  A GitHub Actions workflow is NOT recommended v1 — it would need the Android keystore
  as a repo secret; manual upload of the already-built APK is simpler and safer.
- IzzyOnDroid inclusion: requirements & request process — VERIFY at
  https://apt.izzysoft.de/fdroid/index/info and the IzzyOnDroid repo (GitLab).
  Known constraints to check: APK attached to GitHub releases, FOSS license (GPL-3.0 OK),
  APK size limit (~30 MB policy for full history retention — MAUI APKs can be large;
  check actual APK size first; oversized apps get shortened version history, usually still accepted),
  no proprietary tracking libs (none in this app).
- Draft the inclusion request text for the user to submit.

---------------------------------------------------------------------------------------------------

## D. Localized store listings (#6)

- Create folder `StoreListings/` in repo root; one file per language:
  `StoreListings/<lang>.md` (en, de, es, sl, fr, pt, it, ja, zh, ko, nl, da, no, sv, fi, pl, cs, sk, hr, sr).
- Each file contains clearly labeled sections with CHARACTER LIMITS respected
  (verify current limits; as of last check):
  - Google Play: app name 30, short description 80, full description 4000
  - Apple App Store: name 30, subtitle 30, promotional text 170, description 4000, keywords 100
  - Microsoft Store: description up to 10000 (use the 4000-char text)
- Source material: README.md feature list, OpenHabitTracker.md "Key features",
  index.html marketing copy, and — for terminology consistency —
  OpenHabitTracker/Localization/Resources/<lang>.json (use the SAME translated words for
  "habit", "note", "task", "category", "priority" that the app UI uses).
- Slovenian: "navada" is feminine — gender agreement throughout.
- Tone: factual, no hype. Key selling points in order: free + ad-free, no account,
  data stays on device, open source, all platforms, Markdown notes, Google Keep import,
  optional self-hosted sync.
- User then pastes manually: Microsoft Partner Center, Google Play Console,
  App Store Connect (per Release.md, these are manual web-UI updates). APKPure auto-syncs from Play.

---------------------------------------------------------------------------------------------------

## E. SEO comparison pages on openhabittracker.net (#7)

- Site is a static site in OpenHabitTracker.Web/, FTP-uploaded (see Release.md).
  Match the existing index.html: same Bootstrap setup, same gtag analytics pattern
  (add per-page event functions like the existing redditVisit()/gitHubDiscussionsVisit()).
- Pages to create:
  1. loop-habit-tracker-alternative.html — target queries: "Loop Habit Tracker iPhone",
     "Loop Habit Tracker for desktop", "Loop Habit Tracker alternative". Loop is Android-only,
     ~8k GitHub stars, huge user base wanting iOS/desktop. Honest comparison table
     (Loop wins on: widgets, graphs maturity; OHT wins on: platforms, notes+tasks, self-host sync).
  2. google-keep-alternative.html — target: "open source Google Keep alternative".
     Hook: built-in Google Keep Takeout import (Backup project).
  3. Optionally: habitica-alternative.html, streaks-alternative.html.
- Source material: the dev.to comparison article (link in Articles.md, Mar 22 2026) — fetch it.
- Each page: unique <title> (< 60 chars) and meta description (< 155 chars), one <h1>
  containing the target phrase, comparison table, 2-3 screenshots from
  OpenHabitTracker.Web/images/, store badges + PWA link, canonical link tag.
- Add links to the new pages from index.html (footer or a "Compare" section) — orphan pages
  don't rank. Create/update sitemap.xml if absent. Update "© 2026" if needed (Release.md habit).

---------------------------------------------------------------------------------------------------

## F. Reddit post skeletons (#1, #2) — DRAFTS ONLY, user posts

Hard rules (derived from the user's own 2021-2026 post-history data, table above):
- Hook in the title, brand name never first. Text post, not link post. Screenshots inline.
- ONE subreddit per week max. Never identical text in two subs. Check each sub's
  current self-promo rules BEFORE drafting (they change; r/selfhosted has flair requirements).
- User must reply to comments for 24h after posting — remind them of this.
- Release announcements go ONLY to r/OpenHabitTracker.

Drafts to produce (one file each, e.g. in a `drafts/` folder NOT committed, or pasted in chat):
1. r/selfhosted — title like: "Self-hosted habit/task/notes tracker - one docker-compose,
   native apps on all 6 platforms sync to it, no account required". Body: what it is (2 lines),
   2-3 screenshots, docker-compose block, REST API + Scalar UI mention, link to GitHub last.
2. r/getdisciplined or r/productivity — anti-streak angle: "I built a habit tracker without
   streak guilt - it shows how overdue you are instead". Explain elapsed-vs-interval ratio.
   This framing scored 59 pts for the user in 2021. Discussion question at the end.
3. r/dotnet — architecture story follow-up (the 129-pt 2023 post's audience):
   only when there is new technical substance (e.g. .NET 10 upgrade, new host).
The user's voice matters: drafts are skeletons; user should rewrite until it sounds like them.

---------------------------------------------------------------------------------------------------

## G. Product Hunt relaunch (#8) — PREP ONLY, user launches

- Previous launch (2024): 3 upvotes, 4 followers — PH allows relaunch after 6 months /
  major version. VERIFY current relaunch policy on the PH help pages.
- Assets to prepare: tagline (< 60 chars, e.g. "Notes, tasks and habits - private,
  free, on every platform"), gallery images 1270x760 (reuse/resize
  OpenHabitTracker.Web/images/), a 30s demo (cut from OpenHabitTracker.Web/videos/
  desktop-main + mobile-main; ffmpeg CRF notes are in DeveloperNotes.md),
  maker's first comment (the story: predecessor app Ididit, 5 years of daily use,
  privacy motivation — source: dev.to "What led me to creating OpenHabitTracker").
- Timing: launch 12:01 AM Pacific, Tuesday-Thursday.

---------------------------------------------------------------------------------------------------

## H. Microsoft community pitch (#9) — DRAFT ONLY, user sends

- Angle: production app, ONE Blazor codebase, SIX hosts (WASM, Server, MAUI, Photino,
  WinForms, WPF), zero #if platform conditionals, 8 distribution channels.
  Exactly the showcase .NET marketing wants for Blazor Hybrid.
- Channels (verify current submission paths):
  - .NET Community Standup (ASP.NET / MAUI standups take community links)
  - James Montemagno (MAUI advocacy)
  - "This Week in .NET" / .NET blog community spotlight
- Supporting links: the two technical dev.to articles (Mar 29 + Apr 4, 2026 — in Articles.md),
  GitHub repo, openhabittracker.net.
- Draft a short email/submission: 3 paragraphs max, lead with the 6-hosts fact,
  offer to demo or write a guest post.

---------------------------------------------------------------------------------------------------

## I. Newsletters / blogs (#10) — DRAFT ONLY

- selfh.st (Self-Host Weekly) — has a software submission path; verify on selfh.st.
- Noted.lol — pitched via their contact form; they covered HabitTrove (a competitor),
  so the pitch is "here's the same category with 6 native clients and no account".
- Both pitches: 5 sentences, screenshots links, docker-compose, differentiators.

---------------------------------------------------------------------------------------------------

## Suggested execution order for a single work session

1. A (review prompt) — most code, most verifiable, do while fresh
2. B (store templates) — schema work, needs live-doc fetching
3. C (gh release upload step + IzzyOnDroid draft) — small
4. D (localized listings) — long but mechanical
5. E (SEO pages) — needs the dev.to article fetched
6. F/G/H/I (drafts) — fill remaining time, lowest verification burden

Items A-E produce repo artifacts that survive any model/plan change.
Items F-I are text the user must personally rewrite and send — they age fine.
