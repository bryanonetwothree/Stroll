# Validation

## Automated

`tests/CoreTests.cs` is a dependency-free console harness for the production `BreakClock` and `PetRoute` implementations. It checks the agreed repeated-snooze sequence, skip, full break completion, interrupted break restart, custom timings, advance warning de-duplication and disabled warnings, delayed ticks, pause/resume, wake reset entry point, settings validation, orientation and all 15 edge combinations. Route sampling checks finite positions, screen bounds, hopping and reversing over 400 seconds for each combination.

`build.ps1 -Test` builds the Windows application and runs that harness. The workflow does the same on a Windows runner when pushed to GitHub.

## V2 checks executed on Windows (2026-10-17)

- Roslyn build succeeded targeting native Windows Forms/.NET Framework 4.8, without application packages.
- Core harness: 120,102 assertions passed, including all existing clock/routes, preset selection and updates, draft isolation, JSON round trip, v1 migration and name updates, mixed choices, fade duration limits, count limits, pet deletion and restore, deletion persistence, and countdown suspension during fade-in.
- App integration: 11 checks passed using real prompt/overlay buttons, pause/resume and the wake-event handler. This invokes the handler; it does not physically suspend the machine.
- V2 desktop harness: 26 checks passed, covering every bundled walking/jump asset, fade start/intermediate/completion/disposal, fade-off behavior, rounded region, card clamping and drag-release callback, secondary-screen timer cadence, saved preset/count through settings controls, renamed checkbox choices, mixed character rendering with shared copies, a longer slider duration applied to both fades, distinct bundled John/imported J-Yosef entries, and deletion through both UI lists.
- Settings tabs, prompt, colour wheel and overlay rendered to bitmaps and inspected. This session cannot capture the visible desktop; bitmap rendering does not prove native composition or actual mouse dragging.
- CPU/private memory/working set sampled with 1, 3 and 5 pets. See [PERFORMANCE.md](PERFORMANCE.md).

Run `./test-desktop.ps1` for the desktop harnesses, or add `-Performance` for short
CPU/memory samples. It builds first, writes layout images under `dist/smoke`, and
does not save settings or alter startup registration. Run on an interactive Windows
desktop for the strongest rendering evidence. Performance uses 2 seconds of warmup
and 10 seconds of sampling per count, with a paint-event check to ensure rendering
is active. Desktop tests remain optional and are not part of the headless CI job.

## Windows acceptance checklist

- [ ] Run portable app on Windows 10 with .NET Framework 4.8; confirm tray icon, settings and Exit.
- [ ] Work 1 minute, break .1 minute, snooze 1 minute. Snooze twice, then Break; verify six full seconds and a fresh work timer.
- [ ] Skip due prompt; verify a full new work interval. Closing prompt or overlay should snooze.
- [ ] Enable a 10-second warning, then disable it. Check Windows notification settings if the balloon is suppressed.
- [ ] Pause/resume working, snoozing and during a break. Preview while paused should preserve the paused state.
- [ ] Sleep while working, snoozing, at the prompt and during a break. Waking should remove all stale UI and start a fresh work interval.
- [ ] Lock/unlock Windows; verify the documented fresh work interval.
- [ ] Enable startup, sign out/in; confirm only tray icon appears. Disable startup and repeat.
- [ ] Colour wheel: choose a hue, grey, white and black; test hex entry. Upload portrait/wide backgrounds.
- [ ] Import transparent PNG and GIF, with/without a separate jump file. Delete original import location and repeat preview.
- [ ] Save a custom named pet, restart, select it again; verify artwork and optional jump persist. Verify upgrading v1 keeps the previous pet and timings.
- [ ] Preview 1, 3 and 5 pets; verify shared animation, path rotation/hops and readable controls.
- [ ] Toggle fades, snooze during fade-in, and restart a break during fade-out. Full countdown follows fade-in; Exit/wake/display rebuild must leave no stale overlays.
- [ ] Drag reminder text and empty space to all boundaries, restart and change resolution; check retained, bounded placement and usable buttons at 100/150/200% scaling.
- [ ] Test corrupt/oversized images and absent imported files; app should reject or fall back without crashing.
- [ ] Test all four single edges, adjoining pairs, three edges, four edges and opposite pairs. Verify the visible hop, rotation and reversal.
- [ ] Connect two monitors, including one left of primary (negative coordinates); cover both, controls/pet on primary.
- [ ] Test 100%, 150%, 200% and mixed scaling; check border coverage, readable controls and settings scrollability.
- [ ] Disconnect/reconnect/change primary display during a break; overlays rebuild without leaving orphan windows.
- [ ] Check ordinary apps, fullscreen video and games; Esc and Alt+F4 remain usable.
- [ ] Measure Task Manager CPU/private working set with settings closed for 5 minutes and during a break. Record Windows version, display sizes and asset sizes. No performance numbers are claimed until measured.
- [ ] Start twice; the second instance should report that the first is in the tray.
- [ ] Verify no outbound network requests from the app process.

## Release gate

Complete the Windows checks before promoting from preview to stable. The included GitHub workflow has not been executed unless someone has pushed the project to a repository and inspected its result.

Pet management regression checks cover the prior bundled-John naming collision,
deduplicated catalog entries, deleting saved and bundled pets, removing screen
selections with a fallback, preserving deletion across JSON reloads, draft
isolation, restoring only built-ins, and retaining at least one available pet.

Windows shell integration: native `.lnk` creation passed; the Windows shell
independently read back the executable target and `Stroll.EyeBreak` AppUserModelID.
App integration also verifies relaunch requests open exactly one settings window
and preserve the work interval. Tests create shortcuts only in `dist/smoke`.

- [ ] Search Start for Stroll; pin to Start and taskbar using Windows menus.
- [ ] Close settings to tray; launch pinned shortcut and verify settings opens.
- [ ] Exit completely; launch pinned shortcut and verify normal startup.
- [ ] Move the portable folder; launch manually to repair the Start shortcut.

The process and shortcut share an AppUserModelID as described in
[Microsoft shell documentation](https://learn.microsoft.com/en-us/windows/win32/shell/appids).
Actual Explorer pin/unpin menus still need an interactive-desktop check.

## Start catalog issue observed locally

The shortcut's file, executable target, icon and AppUserModelID validate, but
Windows Get-StartApps still does not return Stroll in this user session. A shell
change notification, Start-menu host refresh, standard shortcut and per-user
Programs location did not establish that it appears in All apps. Do not treat
shortcut metadata validation as proof of Start-menu listing or successful pinning.
AddToStartMenu now notifies the shell of shortcut creation/update. The shortcut
folder is the direct route to Windows pinning while the catalog issue remains.
