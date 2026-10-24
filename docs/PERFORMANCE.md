# Windows development-session measurements

Measured 2026-10-17 on Windows build 26200.9457 (25H2), .NET Framework 4.8,
20 logical processors, one 2560 × 1440 screen, default John at 160 pixels,
65 pixels/second, solid background and fades enabled. No settings or tray window
was open during samples; measurements are of the desktop test process hosting
the production overlay. This session has no desktop screen-capture access.

| Pets | CPU (% whole machine) | Private memory (MiB) | Working set (MiB) | Paint events / 10 s |
| --- | ---: | ---: | ---: | ---: |
| 1 | 0.25 | 37.3 | 52.0 | 214 |
| 3 | 0.49 | 37.2 | 52.0 | 214 |
| 5 | 0.55 | 37.9 | 52.7 | 214 |

The executable is approximately 0.58 MiB, including all three bundled pets.
No browser runtime or third-party application packages were added.

Method: `./test-desktop.ps1 -Performance`. Each count gets a 2-second warmup,
then a 10-second sample of process CPU time, divided by elapsed time and logical
processor count. Memory comes from Windows process private bytes and working set
at sample end, after a warmup collection. Counts run in one process; runtime caches
can carry across samples. Paint events confirm the overlay was rendering. The
harness also pumps messages and therefore contributes some CPU/memory overhead.

These short single-screen samples do not establish long-term idle use, visible
compositor/fade cost, startup peaks, physical-machine performance, multi-monitor
or mixed-DPI behavior, or arbitrary imported GIF/background costs. Repeat on the
normal interactive desktop, and use Task Manager for a five-minute idle and break
check before treating performance as validated for a release.

Copies share a single walking/jump image pair, stopwatch and animation timer.
Secondary overlays use a one-second timer after fades. Source sprite sheets are
not decoded; image and timer resources are disposed when overlays close.

These samples cover the initial v2 single-character copies (John).
Mixed-character selection added later loads one artwork pair per distinct selected
character; duplicate copies still share decoded images. Mixed selections are
covered by rendering tests, but these numbers do not measure their added memory.
