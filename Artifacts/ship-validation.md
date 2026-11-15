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

Integrated origin/adrian commit 5b191c62: updated starfield and playback timing, and progress color transitioning from cyan to gold. Pause remains intact.

Current ladder regression: continuous W/S passes the middle deck without detaching; A/D is the only exit command beside a floor. Entry alignment and horizontal exit move at bounded speed, never teleport. No-input movement holds position. A fixed Idle pose eliminates frame-to-frame visual drift; 200 idle updates were checked for each role. New holographic role artwork is used with Commander/Pilot/Engineer/Medic click areas.

Menu/Bridge guidance: START and all four role hit areas remain clickable with no hover, focus, pressed or selected fill. The Bridge suggestion is yellow on an opaque backing/border, appears only while the check is available, and hides for the mission card, skip and completion. Briefing and skip status no longer describe negative consequences. Validated with Tools/ValidateMenuHint.cs.

Controls/Bridge reopening: verified 10-second intro dismissal, frozen intro countdown during pause, full controls always available in pause, no reappearance after expiry, repeated CLOSE/E cycles, and review of available/in-progress/completed/expired Bridge cards. Esc closes the station modal and restores input. See Tools/ValidateControlsAndBridge.cs and controls-intro.png.

Health HUD: all four roles passed 100 HP initialization, damage/healing, 0–100 clamping, rejected invalid amounts, event-driven UI updates, 75% fill geometry, amber/red thresholds and compact panel dimensions (210 x 44 reference pixels). Render: health-hud.png; check: Tools/ValidateHealthHud.cs.

## Solar Storm integration — 2026-11-15

Merged `origin/adrian2` (`f80e3e2e`) with the current main and retained the modern ship, role menu, continuous ladders, neutral Bridge card and compact health HUD.

Unity 6000.6.9f1 Play Mode checks in an isolated project:

- `ValidateSolarStorm.cs`: activation window; actual upper-to-lower ladder route; E shelter completion; success without damage; timeout, ignore and expiration apply damage exactly once; skipped windows resolve chronologically; critical exposure reduces health to zero and opens a paused failure screen; Escape cannot bypass death; replay resets missions, radiation, health and overlays; closing the start window does not interrupt an already running procedure.
- `ValidateHealthHud.cs`: all four roles, damage/healing/clamps/invalid inputs, fill geometry, colors and compact size.
- `ValidateControlsAndBridge.cs`: introductory controls, pause countdown, controls in pause, repeated Bridge CLOSE/reopen, available/running/completed/expired card review.
- `ValidatePauseAndLadders.cs`: all four roles, continuous W/S, explicit A/D exits, no teleport/air exits, frozen animation/camera/timers in pause.
- `ValidateMenuHint.cs`: no blue overlay on START or roles; neutral Bridge text and opaque yellow suggestion.
- `ValidateMenuClicks.cs`: rendered pointer raycasts for START/BACK/START, all four roles and boarding at 1600×900.

The imported MOONIES question phases have no ship campaign dispatcher yet. Their assets and data are preserved but excluded from timed station checks, preventing fake timed phases from obscuring the working missions. Updated the retained question UI's font/input module for Unity 6.

Preview: `solar-storm-health.png` (75% HP and the shelter marker).

## Radiation warning visibility — 2026-11-15

Added an opaque radiation indicator and a bordered, opaque alert banner. A radiation increase announces itself for ten visible gameplay seconds. At 2:15 the storm arrival banner directs the player to Communications; after starting the procedure it remains visible with the shelter destination, E interaction and countdown. Alerts hide under station dialogs, pause and game over, and reset on a new flight. Text is updated only when its state or displayed second changes.

Unity Play Mode `ValidateSolarStorm.cs` passes, including new checks for radiation-increase notification, readable backing, storm-arrival directions, persistent active warning, countdown changes and pause/resume visibility, plus all previous mission and HP checks. Visual capture: `radiation-alert.png`.

## Shared randomized mission catalog — 2026-11-15

Implemented all 35 entries from `Docs/Artemis2_Randomized_LEO_Mission_Task_Specification.md` as data-driven procedures: G-01–04, ENG-01–08, PIL-01–09, CMD-01–06, MED-01–08. Current user-requested override makes every discipline procedure available to every role. A run still draws five shared tasks (2 Easy, 2 Medium, 1 Hard) across at least three phases, and 2–3 distinct global events.

Each procedure contains task-specific indicators/actions, rejected unsafe choices, automatic verification steps and a completion condition. Caps are 30/60/90 active seconds. Tasks queue after randomized releases and remain available past the release-window end. Only one procedure can be active. Failures/skips have explicit HP costs; flight end at 600 seconds resolves unfinished work and displays a recovery summary. Starting location is stable 185 km LEO. No other role or prior task is required.

Fixed G-01 scanning stopping after CLOSE / leaving the Bridge: automatic verification runs independently of dialog visibility and player distance. A compact opaque HUD shows scan percentage/progress or ACTION REQUIRED. Reopening preserves the current step; Escape pause freezes both run and procedure clocks. Mission Tasks / M opens the live general interface. Future objectives are hidden until release.

Unity 6000.6.9f1 validation:

- `ValidateMissionCatalog`: all definitions, valid actions/time caps, all-role access, 2000 reproducible random plans with correct counts/difficulty/phase mix, valid releases, uniqueness, expected completion budget; all 35 tasks selected, including late reentry.
- `ValidateMissionProcedures`: all 35 procedures completed for all four roles; actual shelter ladder route, shielding and essential-power sequence; unsafe commands rejected; successful completion preserves HP. G-01 regression verifies real-frame progress after CLOSE and walking away, live review, background departure scanning and Escape pause. Also checks queued tasks after windows, exclusive active work, one-shot timeout damage and actual Update-driven ten-minute flight completion.
- `ValidateMissionInterface`: rendered pointer/raycasts for task board, rows, Start Scan, Close, unsafe/safe action buttons and final confirmation. Confirms hidden upcoming tasks and opaque closed-scan HUD. Captures at 1600×900: `mission-task-board.png`, `mission-procedure.png`, `scan-after-close.png`.
- `ValidateSolarStorm`, `ValidateControlsAndBridge`, `ValidateMenuHint`, `ValidateMenuClicks`, `ValidateHealthHud`, `ValidatePauseAndLadders`: passed warning/countdown, station review, controls, menu, HP and all-role movement/pause regressions. Older fixed-mission tests updated to isolate their target task from the randomized production draw.

This implementation presents flight/repair/crew-screening procedures through the ship's mission console. The existing ship scene remains the play space as phases advance.

## Mandatory checkout, physical equipment and cabin exposure — 2026-11-15

User-requested progression override: G-01 is guaranteed, available immediately, unskippable and retryable. Its completion unlocks the phase clock / subsequent events; an early shared follow-up releases within its allowed LEO window. G-02 is guaranteed as an actual cabin hazard rather than an optional indicator activated only by accepting a task. Selected task counts/difficulty/phase diversity remain intact. Other role tasks remain shared.

Added 33 world interaction regions across the ship. E, nearby-equipment button and world-point clicks open console/door/supply/medical/bunk/life-support/laboratory/waste/ladder panels. Distant clicks provide a waypoint. Equipment tasks require their physical station. Closed doors block walking; reopening restores passage. First aid/bunk recovery is limited per flight. Shielding, shelter seal and power actions share the state used by the shelter procedure.

Cabin field: 35% level, 0.70 HP/s exposed, 120 seconds maximum; starts at story release without acceptance. Damage continues while walking or reading dialogs and after mission failure, freezes in pause, and stops outside the ship, inside a shielded/sealed central shelter, or when the field clears. Leaving protection restores damage. Zero HP stops gameplay. Added exposed/protected HUD states, rate text and warning edges. Generic notices cannot cover hazard warnings. Interface opening holds the character's walk/climb animation.

Unity 6000.6.9f1 checks:

- `ValidateMissionCatalog`: 2000 generated plans; mandatory G-01 at zero, guaranteed G-02, counts, unique selections, caps, phase diversity and all-35 coverage.
- `ValidateFlightChainAndEquipment`: all four roles, first-checkout retry/gating, follow-up announcements, station restrictions, automatic field, actual HP loss, paused/protected/outside/clear behavior; all 33 world-point interaction paths, real door collisions/reopening, limited medical recovery, real-frame damage while a dialog is open, radiation death and replay reset.
- `ValidateEquipmentInterface`: rendered pointer raycasts for first aid, station task, ladder grip, shielding/seal and Close; exposed/protected captures and assertion that phase notices do not cover radiation.
- Existing mission-procedure, solar-storm, interface, controls/Bridge, menu, HP and ladder checks updated for mandatory checkout and physical stations.

Captures: `equipment-panel.png`, `cabin-radiation.png`, `cabin-radiation-protected.png`.

Final verification repeated after deleting the isolated project's imported-asset cache and performing a clean Unity import. All ten catalog/procedure/chain/equipment/solar/controls/menu/health/ladder check scripts passed on the fresh import. The exposed capture shows cabin damage at 0.70 HP/s with 69% vitals; the protected capture retains that health with SHIELDED / no HP loss. Phase notices are hidden while radiation is present.
