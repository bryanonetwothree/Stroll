# Stroll v2 — agreed requirements

Implemented as the v0.2 preview. See docs/TESTING.md and docs/PERFORMANCE.md for executed checks and remaining Windows acceptance checks.

User requirements:
1. Save named pets and select them later as presets, like Use Bernie.
2. Choose how many pets appear at once.
3. John is the default pet.
4. Fade the break overlay in and out.
5. Make the reminder card draggable with rounded corners; give the app a chill, relaxing feel.

Maintain the agreed existing behaviour: configurable work/break/snooze timings;
Break/Snooze/Skip prompt; only Break starts the break countdown; repeated snoozes;
Skip starts a fresh work interval; manual pause; no activity detection; reset after
wake; optional Windows startup; optional configurable warning; multi-monitor
coverage; custom static/GIF pets and optional jump artwork; selected edge paths,
rotation and visible hops between opposite edges.

Suggested implementation choices, still adjustable: one John by default; an initial
1–5 pets range; about 400 ms fades with an off option; countdown begins after
fade-in; remember a draggable card's position within the screen boundaries.

Stay with native Windows Forms/.NET Framework 4.8. Share animation timing and
artwork between instances. Decode selected assets as needed; animate only during
breaks. Measure actual Windows CPU/memory, including 1, 3 and 5 pets.

Assets: the original Bernie is in assets/walk.png and assets/jump.png.
The second smooth-coat dog is in assets/pets/smooth-saint-bernard/.
John (third pet, neck mane) is in assets/pets/john/.

Use README.md and docs/TESTING.md for the current build and validation instructions.
Windows-specific sleep/wake, startup, scaling and real performance need local checks.

Follow-up refinements: Bernie is S-Yosef; John/Yosef is J-Yosef. Pets on screen
uses checkboxes to mix selected characters, with a total count for extra copies.
The walking and optional jumping controls have a divider. One duration slider
sets both fade-in and fade-out (0.1-3 seconds).

Naming correction: the bundled John stays John; the saved/imported Yosef becomes
J-Yosef. Both lists share one deduplicated catalog. Delete removes saved or bundled
entries and screen selections; Restore built-ins brings back bundled entries.
