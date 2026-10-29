# Stroll — quick start

1. Extract this whole folder somewhere permanent, such as `Documents\Stroll`.
2. Double-click `Stroll.exe`. Keep `Stroll.exe.config` beside it.
3. Start with 20 minutes work, 5 minutes rest and 3 minutes snooze, or choose your own.
4. Open **Pet & path** to upload art and select edges. John is ready by default; your saved Yosef appears as J-Yosef. Use **Pets on screen** to check the pets to show together and set a total of 1-5. Use **Edit selected** in **Pet & path** to change artwork. After importing artwork, name it and click **Save named pet**, then **Save settings**.
5. Choose **Preview break** to try the screen; Esc closes preview.
6. Choose **Save settings**. Close the window; Stroll continues in the tray.

Use the globe button at the top right to switch between English and Cantonese (Hong Kong). The choice is saved with settings. The default break-screen background is `#F2EAAA`; **Set to default** restores it.

When the prompt appears:

- **Break:** start the full break, covering every monitor.
- **Snooze:** ask again after your chosen delay; repeat as often as needed.
- **Skip:** start another complete work interval.

In **Break screen**, turn fades on or off and adjust their shared duration slider. Drag the rounded reminder by its text to reposition it. The full break countdown starts after fade-in.

During the break, look beyond your display. The countdown continues even if you do nothing. Finishing it starts the next work interval. Esc or Alt+F4 snoozes. The tray menu has Pause/Resume and Exit.

Optional startup is under **Timing**. Enable it only after putting the program in its permanent folder. No installation or administrator access is needed to run the app.

This is an unsigned early preview. Windows-specific startup, sleep/wake, display scaling still need on-device validation. Short Windows CPU/RAM samples are in docs/PERFORMANCE.md. The source package includes a checklist and build instructions.

Select a pet and choose **Delete selected** under **Pets on screen** (or **Delete pet** under **Pet & path**), then **Save settings**. **Restore built-ins** brings bundled pets back. Imported artwork files are retained.

To pin: after launching the current build, search **Stroll** in Windows Start,
right-click and choose **Pin to Start** or **Pin to taskbar**. **Timing > Add to
Start menu** creates/repairs the shortcut and shows its folder. A pinned launch
reopens the running settings window; closing settings keeps Stroll in the tray.
Keep the app folder in a permanent location. After moving it, launch manually
to update the Start shortcut; unpin/re-pin stale taskbar shortcuts if needed.

If Stroll is missing from Start and the Programs folder, double-click
Add-Stroll-to-Start.cmd beside Stroll.exe (or in the source project folder).
Run it yourself in your normal Windows session. It creates the shortcut,
checks that Stroll.lnk exists, and opens its location. Alternatively, use
Stroll > Timing > Add to Start menu.
