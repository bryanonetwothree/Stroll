# Bundled pets and imported artwork

The bundled cartoon St Bernard, **Bernie**, was generated for this project using OpenAI's built-in image generation on 2026-10-16. It was prompted as an original dog, without a reference to an existing character. The artwork is AI-generated; this is not a claim that it has exclusive copyright protection or guaranteed legal clearance in every jurisdiction.

To the extent the project contributors hold rights in these bundled assets, permission to use, copy, modify, distribute and commercially reuse them is granted under the accompanying MIT licence. No separately licensed third-party character or Kenney artwork is bundled. Imported user artwork is not covered by this grant.

Bundled files:

- `assets/walk.png`: three-pose horizontal sprite strip, 256-pixel square cells.
- `assets/jump.png`: separate 256-pixel square jump pose.
- `assets/app.ico`: application icon derived from the first walking pose.

Source generation prompt:

> Use case: illustration-story. Asset type: original 2D game pet sprite sheet for a tiny Windows eye-break app. Create an original adorable cartoon St Bernard dog, warm chestnut and white coat, dark floppy ears and dark eye mask, big soft muzzle, short chunky legs, no collar or accessories. Clean bold dark brown outlines, flat warm colours with very restrained cel shading, legible at 140 pixels wide. True transparent background. Sheet layout: a precisely aligned 2-column by 2-row grid of four separate full-body same-size side-profile sprites facing RIGHT. Each dog centered in its quadrant with generous transparent gap, no overlap; same body proportions, markings, scale and baseline. Top left: walking pose A, legs in alternating extended stride. Top right: walking pose B, legs in opposite stride. Bottom left: walking pose C, legs under body in passing position. Bottom right: jumping pose, legs tucked, body horizontal, cheerful. All four dogs fully in frame. No ground, no scene, no text, no labels, no grid lines, no shadows underneath; only four dog sprites on transparency. Not based on any existing character.

The generated sheet was separated into cells, resized for the renderer, and packaged without a runtime image generation service. The default walk is a modest pose cycle rather than bespoke frame-by-frame animation.

For your own pet, import only files you have permission to use. A file that is acceptable for private use is not automatically licensed for public bundling. Do not commit someone else's film/book/game character artwork to this repository without the relevant permission.


## V2 pets

V2 also embeds the supplied generated John and Smooth St Bernard walking and
jumping poses. Their original prompts and provenance are under `assets/pets/`.
The same bundled-art permission above applies to these contributed assets.

- John: `assets/pets/john/walk.png` is a compact strip assembled from the supplied
  `walk-a.png`, `walk-passing.png`, and `walk-b.png`; `John-jumping.png` is the hop pose.
  The supplied `John-walking.gif` is empty and is not used by the app.
- Smooth St Bernard: `walking.gif` and `jumping.png` from its asset folder.

Only these runtime assets are embedded; source sheets and reference documentation
are not loaded by the application.
