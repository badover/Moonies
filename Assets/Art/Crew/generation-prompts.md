# Image generation prompts

Tool: built-in `image_gen.imagegen`, `transparent_background: true` for every call.
Four separate calls per pass. Substitute `Commander`, `Medic`, `Pilot`, or
`Engineer` for `{role}`. The generation pass references the corresponding original
PNG in `SourceReferences`; cleanup references that role's generated sheet.

## Generation

Use case: identity-preserve. Asset type: production 2D side-view astronaut animation spritesheet for Unity. Input image: visual reference of {role}; retain this role's suit colors, helmet, equipment and realistic proportions. Create ONE transparent PNG spritesheet with EXACTLY 4 equal columns and 2 equal rows, EIGHT full-body images of this SAME astronaut. All face RIGHT in side profile (slight three-quarter is acceptable but SAME angle every frame), orthographic, realistic detailed fabric, metal and dark visor, sober realistic 2D game art, no cartoon outlines. Engineer is WHITE with yellow tools and backpack; other roles keep orange suit and reference equipment. Row 1: four subtle breathing idle frames, arms relaxed with reference equipment, same planted feet. Row 2: four chronological in-place walking frames: left contact, passing, right contact, opposite passing. Natural restrained walking, bending knees and elbows. Fixed camera, identical body size, same gear throughout. Each full body centered WITHIN ITS OWN equal grid cell, feet same baseline, head same height apart from tiny natural bob. Generous empty transparent margin within each cell. No overlaps, no cropped helmet/boots/gear. Genuine alpha transparency, NO floor, NO cast shadow, NO background, NO white or gray matte, NO checkerboard drawn into image, NO text labels, NO grid lines. Highest priority: 4x2 regular grid, consistent identity and scale, convincing realistic side-view astronaut frames.

## Alpha cleanup

Use case: background-extraction. Edit target: this existing {role} 4 columns x 2 rows animation sprite sheet. Clean up ONLY the alpha extraction and contour defects. Remove the conspicuous red/yellow/white/gray halos, speckled noise and stray fragments outside the silhouettes of all eight astronauts. Empty space must be truly fully transparent. Astronaut interiors must be solid opaque with crisp natural antialiased edges. Keep the exact same eight poses, same 4x2 cell placement, same size of each astronaut, same role equipment, colors, realistic rendering, boots and helmet intact. No new poses, no new equipment, no background, no shadows, no text. Output a clean game-ready transparent RGBA PNG sprite sheet. All grid cell margins must be clean transparent. Do not add or change any artwork inside the astronaut silhouettes.
