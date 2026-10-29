# Stroll

A small native Windows eye-break companion. Work for a while, look away for a while, and let John the cartoon St Bernard take a stroll.

**Version 0.3.0 — v3 preview.** Windows 10/11 with .NET Framework 4.8. No browser runtime, NuGet dependencies, accounts, telemetry, keyboard/mouse hooks or activity detection. The app itself makes no network requests.

## Run

1. Extract the portable ZIP to a permanent folder.
2. Launch **Stroll.exe**. Settings opens on the first manual launch; closing it leaves Stroll in the system tray.
3. Set your timings, choose **Preview break**, then **Save settings**.
4. For pinning, search **Stroll** in Windows Start, right-click it and choose **Pin to Start** or **Pin to taskbar**. Manual launch adds/updates its Start-menu shortcut. You can also use **Timing > Add to Start menu** to create it and open its folder. Launching the shortcut again reopens settings in the running app.
If Windows does not list Stroll yet, use **Timing > Add to Start menu** to open the shortcut folder and use the shortcut context menu directly. A local Start catalog issue remains under investigation; a created shortcut does not prove that All apps has refreshed.

5. Right-click the tray icon for Pause/Resume, Take a break now, Restart work interval, Settings and Exit. Windows may put the icon under the hidden-icons arrow.

Keep `Stroll.exe.config` next to the executable. .NET Framework 4.8 is included with Windows 10 May 2019 Update and later. Older Windows 10 installations need Microsoft's .NET Framework 4.8 runtime. Startup is off by default and requires no administrator access.

## The timing rules

Defaults: **20-minute work interval, 5-minute break, 3-minute snooze, optional 30-second advance warning**.

| Event/action | Result |
| --- | --- |
| Work countdown ends | Small topmost prompt: Break / Snooze / Skip. No break starts until you press Break. |
| Break | Cover every monitor, fade in, then start a full break countdown. |
| Snooze | Dismiss, wait the chosen snooze delay, then ask again. Repeat as needed. |
| Skip | Dismiss and start a fresh work interval. |
| Finish the break | Close overlays and start a fresh work interval. |
| Snooze during a break | Dismiss and ask again later; the next break starts at its full duration. |
| Pause / Resume | Freeze and restore the current timer, including a snooze or break. |
| Wake from sleep / unlock Windows | Clear pending UI and start a fresh work interval. |
| Save settings | Apply changes and start a fresh work interval. |
| Preview | Show the draft for up to 30 seconds; pause the live timer during preview, then restore it. |

You do not need to move the mouse to keep the clock running. Warning lead time and warning on/off are adjustable. If the warning lead is longer than an interval, the warning arrives on the first tick. Windows notification settings/Focus Assist can hide tray balloon warnings; the due prompt is a separate app window.

## Personalise

- Full colour wheel with brightness control and hex entry, or upload a background image. Images fill/crop to each screen. The central reminder has a solid panel for legibility.
- John is the default for new settings. Choose John, S-Yosef, J-Yosef, Smooth St Bernard, or a saved pet in **Pet presets**, then **Edit selected** to edit its artwork. Choose the pets to display using the checkboxes under **Pets on screen**. Existing settings keep their artwork; names update automatically (Yosef to J-Yosef, Bernie to S-Yosef); the bundled John stays John. Mudkip and Dorae are not bundled due to character-rights concerns.
- Give imported artwork a name and click **Save named pet** to add or update a preset. Finish with **Save settings** to keep it for later. Presets remember name, walking artwork, and optional jump artwork; count, size, speed and path stay independent.
- Select a pet and click **Delete pet** in **Pet & path**, or **Delete selected** in **Pets on screen**. Finish with **Save settings**. Deletion removes its entry and screen selection, retaining imported artwork files. Bundled pets can be brought back with **Restore built-ins**. At least one pet stays available.
- Choose **1–5 pets on screen**. Check which built-in or saved pets appear together in **Pets on screen**. The total count grows to fit your checked choices; extra copies repeat those pets. Copies of the same artwork share decoded images and follow the path at staggered positions.
- Turn the gentle fades on or off and use one **Fade in / out duration** slider (0.1 to 3 seconds per direction) in **Break screen**. Drag the rounded reminder by its text or empty space; its last primary-screen position is saved and clamped to the available screen. Preview positions remain in the draft until you save.
- The default solid break-screen background is `#F2EAAA`. **Set to default** restores it and clears any background image. Use the globe button at the upper right to switch between English and Cantonese (Hong Kong); the choice applies across all four settings tabs and the break screens.
- Rename your pet; import a PNG/JPG/BMP or animated GIF. Right-facing art with a transparent background looks best. Static art has a gentle bob. GIF frame timings are respected, with a minimum frame duration of 40 ms.
- Optionally import a separate jumping image/GIF. With a custom pet and no jump file, its main image/animation follows the hop arc. A custom pet never intentionally uses S-Yosef's jump pose.
- Select **Bottom, Right, Top, Left**, any combination. Pet rotates onto walls and upside down at the top. One edge or adjacent edges form a back-and-forth path. All four make a loop. Opposite edges use a visible hop across the screen.
- Adjust pet size (64–300 pixels) and walking speed (20–160 pixels/second). The pets and controls stay on the primary monitor; all other monitors show the reminder/countdown. Display changes rebuild overlays.

Import limits protect memory: 20 MB/file, 24 megapixels/background, 2048 pixels/side for pets, and 64 million pixel-frames/animation. Importing a new walking pet clears its previous jump file to avoid mismatched characters. John and S-Yosef use compact three-pose walking strips and separate jump poses. Smooth St Bernard uses a bundled GIF. Only the selected character is decoded.

## Lightweight design

One 1-second UI timer checks deadlines using a monotonic stopwatch. No polling for active apps, idle time, windows, keyboard or mouse. The settings status timer exists only while settings is open. One 25 FPS timer drives all pets on the primary break overlay and invalidates their old/new regions; secondary screens update their countdown once per second after fading. Pet copies share decoded artwork by file and one animation clock; different selected pets load only their own walking and optional jump images. Artwork is decoded when an overlay opens and disposed after fade-out. All animation timers stop when their overlays close. Background images are cached per monitor, so very large/many displays increase memory use.

Windows process measurements for 1, 3 and 5 pets are recorded in [docs/PERFORMANCE.md](docs/PERFORMANCE.md); these are short development-session samples, not performance guarantees. This release is not a Windows lock screen: Esc/Alt+F4 snoozes, and system/security surfaces can take precedence over the overlay. Fullscreen exclusive applications require on-device testing.

## Build from source

Use Windows with Visual Studio 2022 Build Tools (or newer), **.NET desktop build tools**, and .NET Framework 4.8 reference assemblies. Run:

```powershell
.\build.ps1 -Test
```

Or double-click `build.cmd`. Output is `dist/Stroll.exe` plus its config. The script locates Roslyn using `vswhere`; the inbox .NET Framework compiler is too old for this source's C# 7 features. No third-party application packages are needed.

The included GitHub Actions workflow builds, runs the tests, and offers a portable ZIP under **Actions → successful run → Artifacts**. Version tags also publish the tested portable ZIP and SHA-256 checksum as a preview release. For laptop downloads, use the portable ZIP on this repository's Releases page. Build and package locally with `./package.ps1`.

## Data and removal

Settings and imported artwork live under `%LOCALAPPDATA%\Stroll`. Imports are copied there, so moving/deleting the original file will not break your pet. Old imports are retained; remove unneeded files manually after exiting Stroll. Invalid settings fall back to defaults; the previous saved config is kept as `settings.json.bak`.

Startup uses your account's `HKCU\Software\Microsoft\Windows\CurrentVersion\Run\Stroll` entry and stores the current executable path. Disable startup before moving the executable, then enable it again from its new location. To remove: disable startup, Exit from the tray, delete the app folder, remove `Stroll.lnk` from your Start Menu Programs folder (open it using `shell:programs`), unpin any Start/taskbar entries, and optionally delete `%LOCALAPPDATA%\Stroll`.

## Validation and contribution

See [docs/TESTING.md](docs/TESTING.md) for automated coverage, desktop validation and the Windows acceptance checklist. The app is not yet signed or independently verified on Windows. Keep executable releases labelled as previews until those checks are complete.

Source: MIT; bundled generated artwork: see [ASSETS.md](ASSETS.md). Users are responsible for having permission to use and redistribute their own imported characters.


