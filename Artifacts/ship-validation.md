Modern ship integration validation

Verified in Unity 6000.6.9f1, Play Mode, in an isolated project copy.

- Rendered raycast/pointer tests: START, BACK, START again, all four role buttons at 1600 x 900.
- AfterStart JPG is explicitly imported as Texture2D, not Cubemap.
- Modern ship is transparent outside the hull, uses the project starfield and English room labels, and has no Artemis logo.
- Every role uses one atlas for Idle, Walk and Climb; per-frame alpha foot baselines are generated during import.
- All roles passed hull limits, central ladder ascent/descent/pause, deck landings and propulsion barrier checks.
- Zoom converges without overshooting; wheel events expressed as 1 or 120 produce the same result. Mipmapped ship/crew textures replace quantized sampling.
- Remote progress-bar commit 04ed5750 is preserved in the merge. Verified visible fill geometry at 50%, completion at 100%, and timer reset on boarding.
- Timer text only updates when the displayed second changes.
- No C# or shader compiler errors in the final verification.

Renders: ship-check.png, ship-ladder.png, role-selection.png, modern-with-progress.png.
