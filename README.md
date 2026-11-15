# Space Crew

Open the repository folder in Unity Hub using Unity **6000.6.9f1**. Wait for package restoration and asset import, open `Assets/Scenes/SampleScene.unity`, then press Play. Click START, select a crew character and BOARD SHIP. All required artwork, sprites, animations, metadata and background video are included in the repository.

The flight begins in stable **185 km Low Earth Parking Orbit**, after launch and insertion. The ten-minute run follows LEO checkout, TLI, deep-space transit, lunar orbit, descent, surface operations, ascent/rendezvous and Earth return/recovery.

- A/D or arrows: walk inside the hull.
- W/S or up/down: climb the continuous central ladder. Release to hold position; A/D steps off beside a floor. W/S never automatically steps off.
- E or click an equipment region: interact with nearby consoles, doors, supplies, bunks, medical cabinets, life-support equipment or ladders. Clicking distant equipment sets a waypoint.
- M or MISSION TASKS: open the general mission interface and review current work.
- Mouse wheel: smooth zoom.
- Escape: close an open task interface, otherwise pause/resume. Pause freezes movement, animation, camera and all mission timers. Controls appear at boarding and remain in the pause menu.

The [mission specification](Docs/Artemis2_Randomized_LEO_Mission_Task_Specification.md) supplies **35 playable procedures**: four global events, eight engineering, nine piloting, six command and eight crew-health screening tasks. **Current intentional override: all 31 discipline tasks are shared by every selected role.** The character remains fixed during the flight. No procedure requires another role or a previous task.

The first LEO Bridge checkout is always available immediately and cannot be skipped. Its scan continues after CLOSE, and failure allows a retry. Completing it starts the phase clock and unlocks the flight event chain. Each run then draws five shared procedures (two Easy, two Medium, one Hard) spanning at least three mission phases, with an early follow-up, plus the guaranteed G-01 checkout and G-02 cabin-radiation event and optionally G-03 or G-04. Thus a run has seven or eight tasks, rather than all 35 at once. A seeded generator picks releases inside the allowed windows and reserves expected work/transition time within the 600-second flight. Active-time caps remain 30/60/90 seconds; total maximum task budget never exceeds 480 seconds.

Only one task is active. New tasks queue and remain available after their release window closes, until the flight ends. Upcoming objectives stay hidden until their release. The mission interface tracks every selected procedure for every role. Equipment tasks must be started and operated at their physical station; GO TO STATION highlights its location. The introductory G-01 scan remains accessible from the general interface. Thirty-three mapped world interaction regions also provide equipment actions independently of the mission draw. Closed internal bulkheads block walking until reopened; their markers turn red. Unsafe choices are rejected, while the active timer continues. Verification stages run automatically; action/decision stages require the player's choice. Timeout or skipping costs 5/10/25 HP for Easy/Medium/Hard tasks, shown in the briefing; the mandatory checkout can only be completed or retried. At 600 seconds unfinished active work fails, pending work expires, and the recovery summary reports completed procedures and crew vitals.

**LEO scanning continues after CLOSE and while walking away from the Bridge.** The small opaque task HUD shows scan progress or ACTION REQUIRED when a decision is ready. Reopening the task preserves its step and progress. Only Escape pause freezes scanning.

The guaranteed solar-particle event automatically irradiates the **inside of the ship** when its story trigger releases (2:15–2:25 after the Bridge checkout). It starts even if the player never accepts the procedure. At the default cabin level of 35%, an exposed crew member loses **0.70 HP per gameplay second** for up to 120 seconds. Damage continues during mission/equipment dialogs and after CLOSE, freezes in pause, applies only inside the ship and can kill the player. The HUD displays the current damage rate and an exposure warning frame. Generic announcements cannot cover the cabin-radiation warning.

Go to the central lower-deck shelter, install water/food shielding and close the shelter seal through its equipment panel. Remaining inside that protected area stops exposure; leaving it or opening the seal restores damage while the cabin field persists. Correct shelter-procedure actions configure the same physical systems and include shutting down non-essential electronics while preserving guidance and life support. Successful procedure completion protects the occupied shelter rather than erasing radiation throughout the ship. The hazard clears naturally at the end of the event. Equipment panels, markers, event announcements and physical interaction work for every role. First-aid cabinets restore 20 HP and bunks 5 HP, each limited to one use per flight.

Crew starts with 100 HP. The compact VITALS bar follows `CrewHealth.TakeDamage`/`Heal`, clamps values and updates on change. Zero HP stops the flight. Boarding again resets the plan, health, radiation, progress and interfaces.

The earlier imported MOONIES question data/UI remain available for future campaign integration; phase assets marked `startsQuestionCampaign` are excluded from the ship's timed-task catalog.

With the Unity Pipeline CLI connected to the project, run checks as `unity command --project-path . "script eval" --file Tools/<name>.cs`:

- `ValidateFlightChainAndEquipment`: mandatory/retryable first checkout, gated announcements, continuous inside-only radiation damage for every role, protection/clear/death, all 33 interaction regions, door collision and limited recovery.
- `ValidateEquipmentInterface`: rendered equipment/ladder/shelter action clicks and exposed/protected radiation captures.
- `ValidateMissionCatalog`: all 35 definitions and 2000 seeded run plans, coverage, counts, difficulty/phase mix, deadlines and budgets.
- `ValidateMissionProcedures`: all 35 procedures for all four roles; real-frame CLOSE/walk-away scan regression; queue, failure and flight-end behavior.
- `ValidateMissionInterface`: rendered pointer/raycast checks for task board, scan controls and safe/unsafe actions; captures in `Artifacts`.
- `ValidateSolarStorm`, `ValidateControlsAndBridge`, `ValidateHealthHud`, `ValidatePauseAndLadders`, `ValidateMenuHint`, `ValidateMenuClicks`: warnings, health, pause, ladders, controls and menu regressions.

A successful flight requires the crew to survive and complete every selected procedure before the 600-second phase clock ends. When the progress bar fills, the supplied final cinematic plays with its own sound, then Credits opens with RETURN TO MAIN MENU. Incomplete or fatal flights do not play the victory movie. SKIP CINEMATIC leads to Credits; playback failure also falls back to Credits instead of trapping the player.

The source `Music.mp3` is streamed as a looping 2D soundtrack throughout menus, gameplay, pause and Credits. It stops during the cinematic so only the movie's own audio is heard. The cinematic uses unscaled playback because the finished flight remains paused. The source MP4 is shipped as 1080p VP8/Vorbis WebM for Linux/Windows Unity playback; source Credits artwork is unchanged.

Ending/audio regression: `ValidateFlightEnding.cs` verifies an actual soundtrack wrap, successful/incomplete/fatal results, full natural movie playback at time scale zero, exclusive audio, Credits, rendered return/skip buttons, replay and decoder-error fallback.
