# TODO:

TASK INDEX (July 5, 2026) - grouped by completion state.
RULE: a task is DONE only at 100%.
Anything started but not 100% complete is HANGING and therefore NOT done - no exceptions.
HANGING comes first (ordered by shortest path to done), then NOT STARTED.
Details live in the task plans:
Popularity.md appendix A-L (marketing), Automate.md sections 1-10 (release automation),
Infrastructure.md (machines/credentials), and the sections below in this file (code).
 
HANGING (started, NOT 100% done):

       Files committed July 4, 2026 (5 comparison pages, sitemap.xml, robots.txt x3,
       Compare section with per-link analytics), but NOT LIVE.
       Remaining: deploy via the Automate 2 deploy script's `web` target -
       decided July 5, 2026: this deploy IS the first test of that script,
       because `web` is the safest target (static site, no secrets - the appsettings.json
       risk exists only on the Blazor.Web target) and success is externally verifiable
       by fetching https://openhabittracker.net.
       DONE July 5, 2026: Automation/deploy.ps1 written; `web` preview ran against the live
       FTP and listed exactly the E files as new/changed (plus one-time same-size
       timestamp-only re-uploads from the fresh git checkout - harmless).
       Remaining (USER): review the preview, run `Automation\deploy.ps1 web -Commit`,
       verify https://openhabittracker.net.
       Then: set canonical_url on the dev.to comparison article
       (dev.to post settings / front matter) to
       https://openhabittracker.net/habit-tracker-comparison.html -
       without it Google treats the two copies as duplicates and ranks dev.to.

       Service account created and invited, key downloaded -
       but the key still sits in Downloads and fastlane is not installed.
       Remaining: move the key to Automation/secrets/play-service-account.json,
       install fastlane, run validate_play_store_json_key, run supply init
       (init also settles D's open locale-code questions: pt-PT vs pt-BR etc.),
       then one listings-only upload.

    3. Automate 5 - App Store metadata tooling (USER, on the Mac mini).
       The .p8 API key exists (Mac mini + Synology); nothing executed yet.
       Remaining: copy the .p8 to Automation/secrets/, install fastlane, deliver init,
       precheck, metadata upload (exact commands: Automate.md section 5).

    4. Automate 6 - msstore-cli setup (USER).
       TENANT DONE July 5, 2026 - via the ORIGINAL billing-enrollment flow after all
       (business name + payment card, employees = 1); the corrected path failed live
       with AADSTS16000 and a disabled "Create" tenant button
       (full account in Automate.md section 6, EXECUTED block).
       USER reports setup complete.
       Remaining: verify on the Windows PC with `msstore info` + `msstore apps list`,
       run whatever of steps c-e / 2-3 is still missing, then the one listings-only
       dry cycle (get -> updateMetadata -> publish).
       Known risk: the issue #79 tenant bug can still appear at reconfigure time.

    5. Popularity D - localized store listings (finishes via items 2, 3, 4).
       All 20-language texts done and committed July 4, 2026
       (fastlane/ 20 Play + 18 Apple locales, metainfo.xml, Automation/sync-listings.py);
       translations written by Fable itself, no further check needed.
       Remaining: the uploads - Play via item 2, Apple via item 3, Microsoft via item 4.
       Flathub needs nothing (localized metainfo.xml ships with the next Flatpak release);
       IzzyOnDroid reads the committed fastlane folder for free once listed there.

    6. Popularity A - in-app review prompt (USER - verification pending).
       Code-complete, all-platform builds verified.
       Remaining: observe the prompt once per store; each store has its own path:
       - iOS/macCatalyst: plain dev/debug build on simulator or device -
         SKStoreReviewController shows the real dialog in development builds
         (suppressed in TestFlight); complete a habit 10 times, no code change needed.
       - Android, option 1 (temporary code change): pass isTestOrDebugMode: true inside
         #if ANDROID in OpenHabitTracker.Blazor.Maui/AppReview.cs to get the
         FakeReviewManager dialog in a debug/sideloaded build - do NOT ship that.
       - Android, option 2 (test/internal release): install via Play Internal App Sharing -
         the real Play dialog appears without a production release.
       - Windows: no debug or test path - the dialog appears only in a
         Microsoft Store-installed build, so this leg alone waits for the next store release.
       Details: Popularity.md A STATUS + as-built item 5.

    7. Automate 3 - gh-release script (DEFERRED to the 1.2.3 release - decided July 5, 2026).
       Automation/github-release.ps1 written + previewed July 5, 2026: all guards pass,
       notes pulled from VersionHistory.md, and the SHIPPED 1.2.2 APK downloaded from the
       website to Automation/artifacts/shipped-1.2.2.apk (the MAUI bin APK is a July 3
       post-release build - do not attach it; details in Automate.md section 3 STATUS).
       DECIDED July 5, 2026: skip the 1.2.2 backfill - the FIRST GitHub release ships with
       1.2.3, same release cycle as the multi-arch image + Umbrel digest pin;
       shipped-1.2.2.apk stays unused, the 1.2.3 run attaches that release's own APK.
       Remaining (USER, at the 1.2.3 release):
       `Automation\github-release.ps1 1.2.3 <path-to-1.2.3-apk> -Commit`
       creates the GitHub release IzzyOnDroid needs; unblocks item 8.

    8. Popularity C - IzzyOnDroid inclusion request (USER).
       Draft DONE July 5, 2026: drafts/izzyondroid-app-request.md (gitignored).
       Researched live: inclusion policy, the AppRequest template fields (mirrored from
       issue #344), and that the app is not yet listed (package URL 404s).
       All fields prefilled, including the AI-assistance disclosure - review the flagged
       decision in the draft header before filing.
       Remaining (USER): run item 7's -Commit command first (the request links the release -
       now AT THE 1.2.3 RELEASE, per item 7's July 5, 2026 defer decision),
       read the policy in full, then file at codeberg.org/IzzyOnDroid/repodata/issues/new
       with title "[AppRequest] OpenHabitTracker".

       RESEARCH DONE July 5, 2026 (Fable session): execution-ready specs for every store
       are in Popularity.md section B - Unraid, TrueNAS (added), Umbrel, CasaOS,
       PaaS tier (Coolify/Dokploy/CapRover), PikaPods (email pitch, 20% revenue share).
       Runtipi DROPPED (official store closed to new apps).
       Audience shares estimated; CHECK THE DOOR FIRST rule added to the B ground rules;
       H/I channel lists audited the same day ("This Week in .NET" was dead since 2017).
       ALSO DONE July 5, 2026: Dockerfile + Automation/docker-release.ps1 rewritten for
       multi-arch (amd64+arm64 buildx cross-compile, preview verified) - Umbrel requires
       arm64; first real multi-arch push ships with 1.2.3 (decided: no 1.2.2 overwrite).
       ARTIFACTS DONE July 6, 2026 (all YAML validated; details in Popularity.md B STATUS):
       Unraid's PERMANENT files committed to the main repo (ca_profile.xml +
       templates/openhabittracker.xml - the app repo IS the template repo);
       everything else staged one-time in gitignored drafts/store-templates/<store>/
       with a SUBMIT-HOWTO.md per store; PikaPods email at drafts/pikapods-pitch.md.
       Staging decision July 6, 2026: no new repos, no forks of our own repos,
       no lasting branches - their repos get forked only at submission time.
       VERIFIED July 6, 2026 (Windows session, second pass): TrueNAS render with their
       real library PASSED (rendered-reference.yaml saved; run_as-less questions.yaml
       confirmed fine); Coolify SERVICE_PASSWORD_64_ confirmed; Dokploy helpers confirmed
       and the blueprint CORRECTED to the real [config.env] wiring;
       Microsoft pitch URLs filled.
       (render already proven; their hash step needs a Linux filesystem - renameat2);
       fill the Umbrel digest at the 1.2.3 release.
       Remaining (USER): the submissions - Unraid portal (ready NOW),
       PikaPods email (NOW), CasaOS/Coolify/Dokploy/CapRover PRs (ready NOW,
       door-check CasaOS first), TrueNAS PR after the local test, Umbrel PR at 1.2.3;
       execution order in the Popularity.md B audience estimate.

NOT STARTED (the queue, in dependency order):

    later: first REAL runs of the remaining Automation scripts when the next release needs
    them - all were written + dry-run July 5, 2026 (Automate.md per-section STATUS lines):
    bump-version.ps1 (worktree-tested), deploy.ps1's non-web targets, deploy-pkg.sh (Mac,
    needs one-time ~/.netrc there), docker-release.ps1 (plan-tested, docker-pushrm
    installed; REWRITTEN July 5, 2026 for multi-arch amd64+arm64 buildx, preview
    re-verified - first buildx -Commit run at 1.2.3, prints the digest Umbrel pins),
    snap-release.sh + flathub-update.sh (UNTESTED, need the Kubuntu box);
    Popularity F-I drafts + G assets (video cut, image resizes);
    Domenca ticket (FTP TLS cert, text in Infrastructure.md);
    posting F-I in your own voice; store-console review clicks.

HOW TO PROCEED (July 5, 2026) - the pre-1.2.3 sequencing at a glance:

    USER, independent of everything else:
    - Item 1 (Popularity E): Automation\deploy.ps1 web -Commit, verify https://openhabittracker.net,
      then set canonical_url on the dev.to comparison article.
      Highest-leverage single action on this list, takes minutes.
    - Item 6 (Popularity A) iOS leg: complete a habit 10 times in a dev build on the Mac -
      SKStoreReviewController shows the dialog in development builds.
    - (Popularity C) Review the AI-disclosure framing in drafts/izzyondroid-app-request.md
      (read now, file at 1.2.3).
    - Optional: the Domenca FTP TLS ticket (text in Infrastructure.md).

    USER, the three store-tooling setups - together they unblock item 5 (Popularity D):
      item 3 (Automate 5, Mac mini),
      item 4 (Automate 6, verify msstore info + apps list).
    - Side effect: once item 2 works, the Android leg of item 6 is one
      Internal App Sharing upload away.

    - DONE July 6, 2026: item 9 (Popularity B) artifacts, all stores
      (see item 9 + Popularity.md B STATUS).
    - DONE July 6, 2026: Popularity F drafts - drafts/reddit-selfhosted.md +
      drafts/reddit-anti-streak.md (rewrite in own voice; post r/selfhosted after item 1,
      one sub per week, reply to comments for 24h).
    - DONE July 6, 2026: Popularity G/H/I drafts - drafts/producthunt-assets.md
      (text assets; gallery images + 30s video cut stay in the "later" bucket),
      drafts/microsoft-pitch.md (fill the two dev.to URLs before sending),
      drafts/newsletter-pitches.md (send spaced apart, door-check each).
    - DONE July 6, 2026: final review pass - 3 factual fixes applied
      (Umbrel reminders claim, TrueNAS run_as risk note, MariusHosting walkthrough source).
    the TrueNAS full ci.py deploy test (Linux PC only - Windows already proved the
    render on July 6) and the Umbrel digest fill (at 1.2.3).

    THEN the 1.2.3 release unlocks the whole deferred bucket in one cycle:
    multi-arch push + Umbrel digest pin + Umbrel PR, GitHub release (item 7),
    IzzyOnDroid filing (item 8), Flathub metainfo ships, Windows review-prompt
    observation (item 6).

NOTE: everything in HANGING is Sonnet-grade mechanical work (Popularity.md section J).
Item 9's Fable-grade part (store research + schema verification) is DONE July 5, 2026.
BUDGET FLIP (July 5, 2026: weekly quota reset at 18:00 with 18% used;
Fable access ends July 7, 2026 EOD): rationing is superseded -
run everything still open on Fable while it lasts
(item 9 artifacts, then Popularity F-I drafts, then the final review pass);
details in the UPDATE block at the top of Popularity.md section J.

---------------------------------------------------------------------------------------------------

DEVICE-SCOPED vs USER-SCOPED data in SettingsEntity - watch list:

    SettingsEntity mixes user-scoped preferences (theme, culture, sort/filter settings -
    fine to sync/export) with DEVICE-scoped state that must never leave the device:
        - RefreshToken (auth session for this install - see security item above)
        - RememberMe (per-device choice)
        - BaseUrl (arguably user config - borderline, decide when it matters)
    Anything device-scoped that does not need the DB should use MAUI Preferences
    (see Popularity.md appendix section A - the in-app review prompt flag ReviewPromptShown
    uses Preferences for exactly this reason: a synced/exported flag would suppress
    the second store's review prompt and survive backup-restore when it shouldn't).
    FUTURE: the planned reminders feature (exact repeating reminders, like Google Keep) -
    "which device shows notifications" is genuinely device-scoped and will face the
    same decision. Decide the storage before implementing, not after.

---------------------------------------------------------------------------------------------------

find out why `padding-left: 12px !important;` is needed on iOS - why `padding-left: env(safe-area-inset-left) !important;` doesn't work

---------------------------------------------------------------------------------------------------

to lazy load Times / TimesDone and remove `// TODO:: remove temp fix` and keep habit ratio in UI:
call LoadTimesDone on Habit Initialize - sort needs it, every calendar needs it, ...
    save TotalTimeSpent
    save AverageInterval
    on Habit Initialize - load only last week (last X days, displayed in small calendar)
    call LoadTimesDone for large calendar

LAZY LOADING:
    Per-instance lazy loads in services stay exactly as they are — triggered by user interaction,
    loading only what is needed. Loaded objects are registered in the dict.
    WHY Times lazy loading is critical:
    In Ididit (predecessor app), all TimesDone were loaded upfront. After 5 years of daily use
    with ~50 habits done 1-2× per day, the Times table grew to ~180,000 records and became slow.
    Per-habit lazy loading of Times was introduced specifically to solve this.
    Items are small (handful per habit/task) and not a performance concern.
    The temp fix in LoadHabits() calls LoadTimes() (bulk load of ALL times) — this is the WRONG
    direction and reintroduces the exact performance problem lazy loading was meant to solve.
    the habit list UI depends on AverageInterval and TotalTimeSpent being computed
    from TimesDone — without them the ratio badges show division-by-zero results.

TASK — remove temp fix (prerequisite: persist aggregates to DB):
    HabitModel computes TotalTimeSpent and AverageInterval in OnTimesDoneChanged()
    from the full TimesDone list. The habit list renders these via GetRatio() for the
    ratio badge and elapsed time display. Without TimesDone loaded, they are TimeSpan.Zero
    and ElapsedTimeToAverageIntervalRatio / AverageIntervalToRepeatIntervalRatio divide by zero.
    The // TODO:: save it? comments in HabitModel.cs already identify the solution:
    - add TotalTimeSpent and AverageInterval as persisted fields on HabitEntity
    - compute and save them on every AddTimeDone, RemoveTimeDone, UpdateTimeDone
    - once persisted, LoadHabits() can render the full list without loading any Times
    - then remove LoadTimes() and the TimesDone wiring from LoadHabits() (remove temp fix)
    - load only last N days of Times at startup for the small calendar display
    - load full Times per-habit on selection for the large calendar
    This requires a DB migration

if you fix this: `// TODO:: remove temp fix (needed to get TimesDoneByDay, TotalTimeSpent, AverageTimeSpent, AverageInterval)`
and TimesDone are actually lazy loaded, there will be a bug:
    FilterHabits NotOn vs. Before/On/After inconsistency with null TimesDone:
    File: QueryExtensions.cs, FilterHabits DoneAtCompare block
    When a habit's TimesDone has not been lazy-loaded yet (is null), the four date comparisons behave differently:
        NotOn:          x.TimesDone?.Any(...) != true  →  null != true  →  true   →  habit INCLUDED
        Before/On/After: x.TimesDone?.Any(...) == true  →  null == true  →  false  →  habit EXCLUDED
    Users see different result sets depending on which habits have been selected 
    (and thus had their TimesDone loaded) in the current session.

---------------------------------------------------------------------------------------------------

`StateHasChanged()` in `OnAfterRenderAsync(bool firstRender)` in `Habits.razor` — double-render and missing resize handling:

WHY the StateHasChanged() exists (OnAfterRenderAsync, firstRender || dynamicComponentVisibilityChanged):
    Blazor renders the component → DOM appears in browser → JS can now measure the element.
    JsInterop.GetElementDimensions(columnRef) runs and sets columnWidth.
    But the already-rendered template skipped the habits list because `columnWidth == 0`
    (the guard `@if (HabitService.Habits is not null && columnWidth != 0)` on line 108).
    StateHasChanged() forces a second render with the real columnWidth so CalendarComponent
    receives a non-zero ColumnWidth and computes `daysInRow = ColumnWidth / 50` correctly.
    The double-render is structurally unavoidable with this approach — it is the honest
    consequence of needing a browser measurement before the first meaningful render.
    The StateHasChanged() is NOT a hack; it is the correct response to that constraint.

THE REAL GAP — window resize is not handled:
    columnWidth is measured once on firstRender and again when the sidebar opens/closes
    (tracked via dynamicComponentVisibilityChanged from the DynamicComponentType cascade).
    Window resize, browser zoom, and mobile orientation changes are NOT handled.
    After resize, columnWidth stays stale — the calendar shows the wrong number of day cells,
    overflowing or leaving dead space. This is a real bug, most visible on desktop and MAUI/WPF/WinForms.
    Main.razor also only calls GetWindowDimensions() on firstRender — _windowDimensions is equally stale.

WHY the TODO alternatives don't work:
    @media for each day:
        CSS media queries can show/hide elements but cannot change how many <button> elements
        Blazor renders. You would need to pre-render all possible daysInRow counts and hide
        the extras — wasteful DOM and still fragile.
    Get <body> dimensions once at startup, calculate column width from that:
        Main.razor already does GetWindowDimensions() on firstRender.
        Column width != window width: you would need to subtract sidebar (350px when open),
        Bootstrap column gaps, and HorizontalMargin padding. That math breaks every time layout changes.
    Move the measurement up the hierarchy, before Habits renders for the first time:
        Main.razor already blocks child rendering until _windowDimensions is set (the
        `@if (_windowDimensions is not null)` gate). If column width were measured there too,
        Habits.razor would have a non-zero columnWidth on its very first render — no double-render.
        The problem: columnRef lives inside Habits.razor, so Main.razor cannot hold a reference to it.
        You would have to measure the <main> container instead and cascade that width down — close
        but not the same as the column's clientWidth, which depends on sidebar state, Bootstrap
        column gaps, and HorizontalMargin padding. That approximation breaks every time layout changes.
    Don't render habits if columnWidth == 0:
        Already done — that is exactly what the guard on line 108 does.
        The guard is necessary and correct; it does not remove the need for StateHasChanged().

FIX — ResizeObserver:
    The browser ResizeObserver API fires whenever an observed element's size changes:
    on first observation, on window resize, on zoom, on sidebar open/close.

    No feedback loop risk: OnWidthChanged triggers a re-render which can change the column's
    height (more/fewer habit rows) but never its width — width is determined by the Bootstrap
    parent grid, not by content inside. ResizeObserver will not fire on its own callback's side effects.

    Three required safety measures (without these: crash on navigation-during-resize, flood on Blazor Server, crash on old iOS):

    1. Debounce + width-change guard — ResizeObserver fires up to 60x/sec during window drag-resize.
       On Blazor Server each invokeMethodAsync goes over SignalR, flooding the connection.
       Only invoke .NET if the width actually changed (columns snap to discrete pixel widths):

    2. IAsyncDisposable cleanup — if the user navigates away while a resize callback is in-flight,
       the component is disposed but invokeMethodAsync fires anyway → ObjectDisposedException on
       the .NET side and a JS error. Requires DisposeAsync to disconnect the observer and dispose
       the DotNetObjectReference. The try-catch in JS must use .catch(() => {}) not a synchronous
       try-catch block — invokeMethodAsync returns a Promise, so a sync catch doesn't catch async
       rejections and the ObjectDisposedException still surfaces as an unhandled promise rejection.

    3. ResizeObserver availability guard — undefined on iOS < 13.4 and old Android WebViews.
       Without the guard the JS call throws. With it, columnWidth stays 0 and habits don't render
       on those devices — same behavior as today, not worse.

    RISK — ElementReference validity during DisposeAsync:
        When DisposeAsync calls UnobserveElementWidth(columnRef), Blazor must resolve the
        ElementReference struct to the actual JS DOM element to do the Map lookup. If the DOM
        element has already been removed by the time DisposeAsync runs, Blazor cannot resolve it
        and the Map lookup returns undefined — the ResizeObserver is never disconnected and the
        DotNetObjectReference leaks. In practice Blazor runs DisposeAsync before removing the DOM,
        so this is likely safe — but the exact ordering is not guaranteed by the framework.

        Safe alternative: key the Map by a string ID instead of the element object. C# generates
        a GUID at observe-time, stores it as a field, passes it to both observe and unobserve.
        No DOM resolution needed in unobserve — works regardless of element lifetime.

    Add to jsInterop.js:

        const _resizeObservers = new Map();

        export function observeElementWidth(id, element, dotnetRef) {
            if (typeof ResizeObserver === 'undefined') return;  // 3. old iOS/Android guard
            if (_resizeObservers.has(id)) return;               // guard against double-observe
            let lastWidth = 0;
            const ro = new ResizeObserver(entries => {
                for (let entry of entries) {
                    const w = Math.round(entry.contentRect.width);
                    if (w !== lastWidth) {                      // 1. width-change guard
                        lastWidth = w;
                        dotnetRef.invokeMethodAsync('OnWidthChanged', w)  // 2. swallow navigation race
                            .catch(() => {});                   //    must be .catch, not try-catch (Promise)
                    }
                }
            });
            ro.observe(element);
            _resizeObservers.set(id, { ro, dotnetRef });
        }

        export function unobserveElementWidth(id) {             // string key — no DOM resolution needed
            const entry = _resizeObservers.get(id);
            if (entry) {
                entry.ro.disconnect();
                entry.dotnetRef.dispose();                      // 2. release .NET GC reference
                _resizeObservers.delete(id);
            }
        }

    Changes required (verified against current code):

    jsInterop.js — add observeElementWidth and unobserveElementWidth (see snippet above)

    IJsInterop.cs — add two method signatures:
        ValueTask ObserveElementWidth(string id, ElementReference element, DotNetObjectReference<Habits> dotnetRef);
        ValueTask UnobserveElementWidth(string id);
        (or use object for dotnetRef to keep the interface generic across pages)

    JsInterop.cs — add implementations matching the existing lazy-module pattern

    Habits.razor:
        - Add @implements IAsyncDisposable at the top of the file (required for Blazor to call
          DisposeAsync on component teardown — without this directive the method exists but is
          never invoked by the framework)
        - Do NOT add @inject IPreRenderService — OnAfterRenderAsync is never called during SSR
          prerendering (Microsoft docs: "OnAfterRender and OnAfterRenderAsync aren't called during
          the prerendering process"). The guard would be redundant. RuntimeClientData uses it
          because OnInitializedAsync IS called during prerendering — a different lifecycle method.
        - Add private DotNetObjectReference<Habits>? _dotNetRef;  (must store to dispose in DisposeAsync)
        - Add private readonly string _observerId = Guid.NewGuid().ToString();  (stable key for Map)
        - In OnAfterRenderAsync: replace the GetElementDimensions + StateHasChanged block with:
            if (firstRender)
            {
                _dotNetRef = DotNetObjectReference.Create(this);
                await JsInterop.ObserveElementWidth(_observerId, columnRef, _dotNetRef);
            }
        - Add [JSInvokable] public async Task OnWidthChanged(int width):
            [JSInvokable]
            public async Task OnWidthChanged(int width)
            {
                columnWidth = width;
                await InvokeAsync(StateHasChanged);
            }
          NOT void — void is wrong for two reasons:
          (a) InvokeAsync(StateHasChanged) is the correct pattern for callbacks that arrive from
              outside the Blazor render cycle (timers, events, JS callbacks), as recommended by
              Microsoft docs. Blazor Server does route [JSInvokable] calls through the circuit's
              synchronization context, so StateHasChanged() directly is not strictly unsafe there —
              but InvokeAsync is the defensive, idiomatic pattern regardless of host.
          (b) void means the JS Promise returned by invokeMethodAsync resolves immediately without
              waiting for the C# work to complete — errors inside would become unhandled rejections
              that the .catch(() => {}) in observeElementWidth cannot reliably suppress.
              Returning Task lets Blazor marshal it as a Promise so the .catch is effective.
        - Implement IAsyncDisposable — dispose the DotNetObjectReference BEFORE the JS call so it
          always runs even if the JS call throws (e.g. JSDisconnectedException on circuit teardown):
            public async ValueTask DisposeAsync()
            {
                _dotNetRef?.Dispose();
                try { await JsInterop.UnobserveElementWidth(_observerId); } catch { }
            }
          The try-catch is needed because JsInterop.UnobserveElementWidth can throw if the JS
          runtime is already gone (browser tab closed, Blazor Server circuit disconnected).
          During normal in-app navigation JsInterop is alive (it is scoped to the circuit/session,
          not the component), so this only matters at app teardown — but at that point the JS
          context is gone anyway and the cleanup doesn't matter. The try-catch makes it safe and
          silent in all cases.
          NOTE: the JS unobserveElementWidth also calls dotnetRef.dispose() — this double-dispose
          is safe because DotNetObjectReference.Dispose() is idempotent (implementation uses
          ConcurrentDictionary.TryRemove which silently no-ops on a missing key). Not formally
          guaranteed by the docs — an implementation detail, but stable in practice.
        - Remove the dynamicComponentVisibilityChanged tracking — ResizeObserver fires on sidebar
          toggle automatically because the column reflows when the sidebar appears/disappears.
          This means removing:
            • the _dynamicComponentTypeName field declaration
            • the bool dynamicComponentVisibilityChanged local variable
            • the _dynamicComponentTypeName = DynamicComponentType?.Name assignment
            • the || dynamicComponentVisibilityChanged condition in the if statement

    Notes.razor — no changes needed: confirmed no columnRef, no columnWidth, no CalendarComponent
    Tasks.razor — no changes needed: confirmed no columnRef, no columnWidth, no CalendarComponent

    Home.razor embeds all three pages simultaneously (IsEmbedded=true). Each component instance
    has its own columnRef DOM element → own ResizeObserver entry in the Map → no conflict.
    The _resizeObservers.has(id) guard prevents double-observe if OnAfterRenderAsync fires
    more than once with firstRender=true (can happen in Blazor Server SSR with prerendering).

    Platform safety summary:
        WASM                      — safe, invokeMethodAsync is in-process, re-renders cheap
        Blazor Server             — safe with width-change guard (SignalR flood avoided) and
                                    InvokeAsync(StateHasChanged) (synchronization context safe)
        MAUI iOS                  — safe for iOS >= 13.4; guard handles older devices
        MAUI Android              — safe, Chrome WebView supports it since Android 5.0
        WinForms / WPF / Photino  — safe, WebView2 is Chromium; Photino Linux uses WebKit2GTK >= 2019

    The InvokeAsync(StateHasChanged) call stays in OnWidthChanged — it is still needed because
    the callback arrives from JS outside the Blazor render cycle. But it is now driven by real
    size changes, not a one-shot post-render measurement. The columnWidth != 0 guard on line 108
    also stays and becomes semantically correct: it means "no measurement received yet" rather
    than "first render hasn't completed the JS round-trip".

    KNOWN LIMITATION — Blazor Server circuit reconnection leaks a stale Map entry:
        If a Blazor Server circuit drops abruptly (network interruption), Blazor may not call
        DisposeAsync on the old component before the circuit dies. The old _resizeObservers entry
        is never cleaned up — the old ResizeObserver keeps firing, invokeMethodAsync fails (old
        circuit is gone), and .catch(() => {}) silently swallows each failure. The stale entry
        sits in the Map until page reload.
        Severity: low. The primary deploy target is WASM. On Blazor Server the leak is bounded
        (one entry per dropped circuit, harmless callbacks, cleared on page reload).

    BEHAVIORAL NOTE — initial width delivery is now asynchronous:
        Old code: GetElementDimensions is awaited inside OnAfterRenderAsync → columnWidth is set
        → StateHasChanged() forces re-render, all within one OnAfterRenderAsync invocation.
        New code: ObserveElementWidth registers the observer, OnAfterRenderAsync returns with
        columnWidth still 0, then ResizeObserver fires asynchronously in a later task →
        OnWidthChanged → InvokeAsync(StateHasChanged) → re-render.
        The double-render still happens but via a different mechanism with an additional async hop.
        In practice the ResizeObserver fires fast enough that users won't see a flash of missing
        content. But it is a behavioral difference from today worth being aware of.

    DECISION REQUIRED — contentRect.width vs clientWidth:
        The existing getElementDimensions uses element.clientWidth.
        The new observeElementWidth uses entry.contentRect.width (then Math.round()).
        These differ when the column has horizontal padding:
            clientWidth      = content width + padding  (always integer)
            contentRect.width = content width only       (can be fractional)
        The column element is <div class="col child-column px-0 px-md-{HorizontalMargin}">.
        On screens >= md, Bootstrap px-{n} adds padding. With HorizontalMargin=3 (px-3),
        that is 16px per side = 32px total.
        CalendarComponent computes daysInRow = ColumnWidth / 50 (since July 4, 2026 additionally
        capped by the MaxSmallCalendarDays setting when > 0; the cap does not affect this
        decision — it applies after the width division). Example with 800px column:
            clientWidth      → 800 → 16 days
            contentRect.width → 768 → 15 days  (800 - 32px padding)
        contentRect.width is arguably more correct — it is the width available to the calendar
        buttons (padding is not usable space). But it is a behavioral change from today.
        Decide which to use before implementing, then be consistent in the JS snippet.

---------------------------------------------------------------------------------------------------

1.
QueryParameters:
    `ClientData.GetHabits/GetNotes/GetTasks` each have a TODO: "first filter with queryParameters, then use _dataAccess"
    Currently all records are loaded into memory first, then filtered in C# — the intent is to push filters down to the data layer
    `_dataAccess` calls would receive query parameters (search term, category, priority, date range) and return only matching records
    This would eliminate the need for large in-memory filter blocks and reduce data transferred from the data source

2.
exact repeating reminders, like Google Keep

3.
drag & drop reorder - manual sort - 1000000 sort index
- sort categories?
- sort items?

4.
show only not done of highest priority

5.
TSV export/import is several features behind — Record class missing DisplayMetric, TargetQuantity, and TimesDone/Quantity.

---------------------------------------------------------------------------------------------------

1.
upgrade to .NET 10

2.
upgrade NuGet versions

3.
TODO:: research: high priority - large feature
copy Loop Habit Tracker UI - all required data is already in the DB
    - History (done count grouped by week, month, quarter, year)
    - Calendar (continuous year calendar, no breaks in months: 7 days -> 7 rows (horizontal scroll) or 7 columns (vertical scroll))
    - Frequency (by day of the week - continuous calendar, without dates, done count grouped by days of the week)

---------------------------------------------------------------------------------------------------

1.
TODO:: research: low priority (DB migration)
save/load settings:
    - add string Name to Settings
    - add a way to create a new preset
    - add a way to load a preset
    - add a way to rename a preset
    - add a way to delete a preset
    - always load last used preset
        - add long SelectedSettingsId to Settings
        - load Settings[0], then Settings[Settings[0].SelectedSettingsId]
    - add string? Name to SettingsEntity + SettingsModel (null = unnamed/default row)
    - add long SelectedSettingsId to SettingsEntity + SettingsModel (default = own Id = Settings[0].Id)
    - Settings[0] is the root row; load Settings[0], then load Settings[Settings[0].SelectedSettingsId]
    - ClientState.LoadSettings() currently takes settings[0] — extend to also load Settings[SelectedSettingsId]
    - ClientState.UpdateSettings() uses Settings.Id to fetch and update — no change needed
    - UI in Settings.razor: preset selector dropdown + icons for create (bi-plus) / rename (bi-pencil) / delete (bi-trash)
    - on create: AddSettings() new row, set Settings[0].SelectedSettingsId = new row Id, UpdateSettings()
    - on load: set Settings[0].SelectedSettingsId = selected row Id, UpdateSettings(), reload
    - on rename: set Name on selected row, UpdateSettings()
    - on delete: DeleteSettings() selected row, set Settings[0].SelectedSettingsId = Settings[0].Id, reload
    - "URL settings" row: reserved row with Name = "URL", overwritten on each URL param navigation

multiple saved settings, with optional "URL references a preset by name", otherwise the URL params overwrite the "URL settings" saved setting:

2.
TODO:: research: low priority
search/filter/sort query parameters in the URL - Blazor
    - QueryParameters class already exists with all filter/sort fields
    - serialize QueryParameters to URL query string via NavigationManager
    - on page load: parse URL params, find or overwrite "URL settings" row, set SelectedSettingsId to it
    - if URL references a preset by name: find matching Name in Settings rows, set SelectedSettingsId to it

---------------------------------------------------------------------------------------------------

3.
refresh local if remote has changed:

set `_lastRefreshAt = DateTime.UtcNow;` on local changes, so a local change won't trigger an update of the local UI

---------------------------------------------------------------------------------------------------

4.
Data.razor -> "Online sync" -> "Log in"
Sync between `DataLocation.Local` and `DataLocation.Remote` in `ClientState.SetDataLocation()`
method to copy one db context to another

    WARNING (see DEVICE-SCOPED watch list at the top of this file): the generic CopyData
    below loops over ALL entity types - it would copy SettingsEntity wholesale, including
    RefreshToken and RememberMe, between contexts/devices. When implementing, exclude
    SettingsEntity from the copy (or copy it with device-scoped fields blanked).

    public void CopyData(DbContext source, DbContext destination)
    {
        foreach (var entityType in source.Model.GetEntityTypes())
        {
            var sourceSet = source.Set(entityType.ClrType);
            var destinationSet = destination.Set(entityType.ClrType);
            // Retrieve all records without tracking.
            var data = sourceSet.AsNoTracking().ToList();
            // Add records to the destination context.
            destinationSet.AddRange(data);
        }
        destination.SaveChanges();
    }

    using (var sqliteContext = new MyDbContext(sqliteOptions))
    using (var sqlServerContext = new MyDbContext(sqlServerOptions))
    {
        // Copy data from SQLite to SQL Server.
        CopyData(sqliteContext, sqlServerContext);
    }

    // Or to copy in the opposite direction:
    using (var sqliteContext = new MyDbContext(sqliteOptions))
    using (var sqlServerContext = new MyDbContext(sqlServerOptions))
    {
        // Copy data from SQL Server to SQLite.
        CopyData(sqlServerContext, sqliteContext);
    }

---------------------------------------------------------------------------------------------------

5.
make every ...Id a required field in EF Core - Debug.Assert(Id != 0) before Add / Update

---------------------------------------------------------------------------------------------------
6.
Android:
    save SQLite DB in an external folder
    can be part of Google Drive, OneDrive, iCloud, Dropbox

AndroidManifest.xml
MANAGE_EXTERNAL_STORAGE

    <uses-permission android:name="android.permission.READ_EXTERNAL_STORAGE" />
    <uses-permission android:name="android.permission.WRITE_EXTERNAL_STORAGE" />

Android: get permission to save SQLite DB in an external folder that can be part of Google Drive, OneDrive, iCloud, Dropbox

    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using Android.Content.PM;
    using Android.OS;
    using Xamarin.Essentials;
    using Android;
    using Android.Content.PM;
    using Android.Support.V4.App;
    using Android.Support.V4.Content;

    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) // LocalApplicationData, ApplicationData, UserProfile, Personal, MyDocuments, Desktop, DesktopDirectory
    {
        return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    }
    if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) // LocalApplicationData, ApplicationData, UserProfile, Personal, MyDocuments, Desktop, DesktopDirectory
    {
        return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    }
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Android))
    {
        if (ContextCompat.CheckSelfPermission(Android.App.Application.Context, Manifest.Permission.WriteExternalStorage) != (int)Permission.Granted)
        {
            ActivityCompat.RequestPermissions(MainActivity.Instance, new string[] { Manifest.Permission.WriteExternalStorage }, 1);
        }
        path = Path.Combine(Android.OS.Environment.ExternalStorageDirectory.AbsolutePath, "MyAppFolder");
        return Path.Combine(Android.OS.Environment.ExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDocuments).AbsolutePath, "MyAppFolder");
    }
    if (RuntimeInformation.IsOSPlatform(OSPlatform.iOS))
    {
        return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    }

---------------------------------------------------------------------------------------------------
---------------------------------------------------------------------------------------------------
---------------------------------------------------------------------------------------------------

when all habit items are done, habit is done automatically ??? pros & cons ?
when all task items are done, task is done automatically ??? pros & cons ?

repeat:
    - weekly: which day in week
    - monthly: which day (or week/day - second monday) in month
    - yearly: which day (date) in year

textarea Tabs
    - make markdown Tabs look the same as in textarea
        - currently Tabs are ignored in markdown (except under a "- list row")
        - if `DisplayNoteContentAsMarkdown` is `false`, tabs are already displayed properly with `style="white-space: pre-wrap;"`
        - there is no way to know if user is using tabs to create a code block

horizontal calendar with vertical weeks

---------------------------------------------------------------------------------------------------

read Settings from DB before Run() - NO!!! - !!! Transient / Scoped / Singleton !!! - Scoped instances before and after Run() are not the same

unify into one property ??? Task `CompletedAt` / Habit `LastTimeDoneAt` --> `DateTime? DoneAt` ??? NO!!!

---------------------------------------------------------------------------------------------------

common `Router`
    OpenHabitTracker.Blazor - Routes.razor
    OpenHabitTracker.Blazor.Wasm - App.razor - CascadingAuthenticationState, AuthorizeRouteView, NotAuthorized

OpenHabitTracker.Blazor.Server:
    - @page "/Error"
    - app.UseExceptionHandler("/Error");

---------------------------------------------------------------------------------------------------

    Google Keep
        - title
        - pin
        - note
        - reminder
            - date
            - time
            - place
            - repeat
                - Does not repeat
                - Daily
                - Weekly
                - Monthly
                - Yearly
                - Custom:
                    - Forever
                    - Until a date
                    - For a number of events
        - collaborator
        - background
        - (app) take photo
        - add image
        - archive
        - delete
        - add label
        - add drawing
        - (app) recording
        - make copy
        - show checkboxes
        - (app) send (share)
        - copy to Google Docs
        - version history
        - undo
        - redo
        - close
        - (app):
            - h1
            - h2
            - normal text
            - bold
            - italic
            - underline
            - clear (\) text (T) formatting

---------------------------------------------------------------------------------------------------
---------------------------------------------------------------------------------------------------
---------------------------------------------------------------------------------------------------

ASAP tasks: when, where, contact/company name, address, phone number, working hours, website, email

email: copy task list as HTML with checkboxes to clipboard
sms, message: copy task list with Unicode checkboxes

---------------------------------------------------------------------------------------------------

virtualized container

benchmark: method time & render time
method trace logging - benchmark method performance
https://learn.microsoft.com/en-us/aspnet/core/blazor/performance

---------------------------------------------------------------------------------------------------

bUnit - Always invisible (in-memory, no browser/device)
Playwright - Configurable — headless (invisible) or headed (visible)
Appium - Always visible — real device or emulator screen

write Appium integration tests:

The only things Appium could uniquely cover that Playwright can't:
    App launch on device — does the MAUI shell start without crashing on Android/iOS/Windows?
    Android back button — does pressing the hardware back button behave correctly (navigate back vs. exit)?
    iOS swipe-back gesture — does swiping from the left edge navigate back?
    App lifecycle — does the app resume correctly after being backgrounded (e.g., data not lost after switching apps)?
    Permissions dialogs — if you ever add camera/storage/notification permissions, Appium can tap "Allow" on the native OS dialog.

https://learn.microsoft.com/en-us/samples/dotnet/maui-samples/uitest-appium-nunit/
https://github.com/dotnet/maui-samples/tree/main/8.0/UITesting/BasicAppiumNunitSample

https://devblogs.microsoft.com/dotnet/dotnet-maui-ui-testing-appium/

---------------------------------------------------------------------------------------------------

accessibility: Silent operations give no screen reader feedback (WCAG 4.1.3):
    - note save, habit marked done, item deleted — screen reader users hear nothing
    - success feedback: aria-live="polite" (role="status") region in Main.razor, write brief status text after operations
    - error feedback: role="alert" (implies aria-live="assertive") for validation errors — interrupts immediately
    PLAN:
    Step A — shared StatusService (OpenHabitTracker.Blazor/StatusService.cs):
    - Add a Scoped service: public class StatusService { public string Message { get; private set; } public event Action? OnChange; public void Set(string msg) { Message = msg; OnChange?.Invoke(); } public void Clear() { Message = string.Empty; OnChange?.Invoke(); } }
    - Register in DI: builder.Services.AddScoped<StatusService>();
    Step B — live region in Main.razor:
    - Add <div role="status" aria-live="polite" aria-atomic="true" class="visually-hidden">@StatusService.Message</div> at the bottom of the layout (inside <main> or just before </body>)
    - Subscribe to StatusService.OnChange in OnInitialized; call StateHasChanged in the handler
    - Auto-clear after 3 seconds: use a CancellationTokenSource, cancel previous timer before starting a new one
    Step C — call StatusService.Set() after each silent operation:
    - HabitComponent.razor: after MarkAsDone → StatusService.Set(Loc["Habit marked as done"])
    - NoteComponent.razor: after Save → StatusService.Set(Loc["Note saved"])
    - HabitComponent/NoteComponent/TaskComponent.razor: after Delete → StatusService.Set(Loc["Item deleted"])
    - ItemsComponent.razor: after item checkbox toggled → StatusService.Set(Loc["Item checked"] / Loc["Item unchecked"])
    Step D — validation errors (role="alert"):
    - Where form validation messages are shown, wrap in <div role="alert">...</div> (role="alert" implies aria-live="assertive" so no extra attribute needed)
    - Existing ValidationMessage components can be wrapped; no changes to the validation logic itself

---------------------------------------------------------------------------------------------------

add comments to methods - 1. for any open source contributor - 2. for GitHub Copilot
    the codebase would need to be stable first; comments on moving targets are maintenance burden

deploy Blazor Server Docker image to Raspberry Pi 5 / Synology NAS DS224+

---------------------------------------------------------------------------------------------------

what is wrong: I'm not doing the critical tasks - because I see too many unimportant tasts that are overdue and I am satisfied with completing them
