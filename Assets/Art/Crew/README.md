# Animated crew

Use the prefabs in `Assets/Prefabs/Crew`: Commander, Medic, Pilot and Engineer.
Each is a realistic 2D side-view character with transparent background, a
SpriteRenderer, Animator and CrewCharacter component. Original reference pictures
with their backgrounds are kept separately in `SourceReferences`.

Each character sheet has four columns and two rows: four Idle frames above four
Walk frames. Native Unity animation clips loop automatically. Idle is the default.
Set the Animator bool `Walking`, or call `CrewCharacter.SetWalking(true/false)`,
to switch animation. `SetFacingLeft(true/false)` mirrors the sprite. These methods
change visuals only; they do not move the character or implement role abilities.

Drag a prefab into the desired scene. The existing SampleScene was left unchanged;
no role-selection interface, movement controls or gameplay was added.

`Tools > Crew > Rebuild Animated Characters` regenerates sprite slicing, clips,
controllers and prefabs from the sheets. This replaces edits to those generated
assets. Sprite cells use a shared pivot and scale, with uncompressed RGBA textures,
bilinear filtering and no mipmaps.

Assets were made with the built-in imagegen tool using the original role images
as references, followed by an alpha-contour cleanup pass. The prompt templates
used for all four roles are in `generation-prompts.md`.

Validation: C# compiled against installed Unity 6000.6 assemblies. Sheet alpha,
eight sprite rectangles per character, asset references and loop/transition
settings were checked. Playback in Unity could not be verified from the execution
environment because the Unity Licensing Client was unavailable.


Interior animations use Assets/Resources/InteriorCrew: one coherent 4-column x 3-row sheet per role (Idle, Walk, Climb). InteriorCrewImporter generates a JSON foot baseline for every frame and a shared pixels-per-unit value, so changing states preserves the same costume, scale and floor contact. Original prefab controllers remain available; boarding uses the consistent interior sheets.
