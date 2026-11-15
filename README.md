# Space Crew

Open the repository folder in Unity Hub using Unity **6000.6.9f1**. Wait for package restoration and asset import, open `Assets/Scenes/SampleScene.unity`, then press Play. Click START, select a crew character and BOARD SHIP. All required artwork, sprites, animations, metadata and background video are included in the repository.

The flight begins in stable **185 km Low Earth Parking Orbit**, after launch and insertion. The ten-minute run follows LEO checkout, TLI, deep-space transit, lunar orbit, descent, surface operations, ascent/rendezvous and Earth return/recovery.

- A/D or arrows: walk inside the hull.
- W/S or up/down: climb the continuous central ladder. Release to hold position; A/D steps off beside a floor. W/S never automatically steps off.
- E: interact with a station or reach the marked central shelter.
- M or MISSION TASKS: open the general mission interface and review current work.
- Mouse wheel: smooth zoom.
- Escape: close an open task interface, otherwise pause/resume. Pause freezes movement, animation, camera and all mission timers. Controls appear at boarding and remain in the pause menu.

The [mission specification](Docs/Artemis2_Randomized_LEO_Mission_Task_Specification.md) supplies **35 playable procedures**: four global events, eight engineering, nine piloting, six command and eight crew-health screening tasks. **Current intentional override: all 31 discipline tasks are shared by every selected role.** The character remains fixed during the flight. No procedure requires another role or a previous task.

Each run draws five shared procedures (two Easy, two Medium, one Hard) spanning at least three mission phases, plus two or three distinct global events. Thus a run has seven or eight tasks, rather than all 35 at once. A seeded generator picks releases inside the allowed windows and reserves expected work/transition time within the 600-second flight. Active-time caps remain 30/60/90 seconds; total maximum task budget never exceeds 480 seconds.

Only one task is active. New tasks queue and remain available after their release window closes, until the flight ends. Upcoming objectives stay hidden until their release. The general mission interface provides every procedure to every role; stations can also open nearby tasks. Unsafe choices are rejected, while the active timer continues. Verification stages run automatically; action/decision stages require the player's choice. Timeout or skipping costs 5/10/25 HP for Easy/Medium/Hard tasks, shown in the briefing. At 600 seconds unfinished active work fails, pending work expires, and the recovery summary reports completed procedures and crew vitals.

**LEO scanning continues after CLOSE and while walking away from the Bridge.** The small opaque task HUD shows scan progress or ACTION REQUIRED when a decision is ready. Reopening the task preserves its step and progress. Only Escape pause freezes scanning.

The solar-particle procedure includes reaching the protected central equipment area on the lower deck, arranging water/food shielding, shutting down non-essential electronics while preserving life support/guidance, automatic verification and final shelter confirmation. Reaching the marker alone does not complete the event. Arrival and active warnings are opaque and include directions/countdown. Starting or ignoring the event raises the exposure indicator; shelter completion clears the local hazard, while failure raises it further and costs HP.

Crew starts with 100 HP. The compact VITALS bar follows `CrewHealth.TakeDamage`/`Heal`, clamps values and updates on change. Zero HP stops the flight. Boarding again resets the plan, health, radiation, progress and interfaces.

The earlier imported MOONIES question data/UI remain available for future campaign integration; phase assets marked `startsQuestionCampaign` are excluded from the ship's timed-task catalog.

With the Unity Pipeline CLI connected to the project, run checks as `unity command --project-path . "script eval" --file Tools/<name>.cs`:

- `ValidateMissionCatalog`: all 35 definitions and 2000 seeded run plans, coverage, counts, difficulty/phase mix, deadlines and budgets.
- `ValidateMissionProcedures`: all 35 procedures for all four roles; real-frame CLOSE/walk-away scan regression; queue, failure and flight-end behavior.
- `ValidateMissionInterface`: rendered pointer/raycast checks for task board, scan controls and safe/unsafe actions; captures in `Artifacts`.
- `ValidateSolarStorm`, `ValidateControlsAndBridge`, `ValidateHealthHud`, `ValidatePauseAndLadders`, `ValidateMenuHint`, `ValidateMenuClicks`: warnings, health, pause, ladders, controls and menu regressions.
