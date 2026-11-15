# Space Crew

Open this repository folder in Unity Hub using Unity **6000.6.9f1**. Wait for package restoration and asset import, then open `Assets/Scenes/SampleScene.unity` and press Play.

Click START, select a role on the holographic role screen, and click BOARD SHIP.

- A/D or arrow keys: walk inside the hull.
- W/S or up/down: climb the continuous central ladder through the middle deck; release to hold position, reverse to descend. A/D steps off beside a floor. W/S never automatically steps off.
- Mouse wheel: smooth zoom.
- Escape: pause/resume the current mission. Pause freezes movement, camera zoom, animation and the mission timer.

The modern ship uses the source artwork with transparent surroundings and English room labels. All gameplay sprites, images, animations and frame metadata are included in the repository; no files outside the project are needed to run it.

The mission HUD includes a ten-minute progress timer.

For automated verification with the Unity Pipeline CLI, start Play Mode, then run `unity command --project-path . "script eval" --file Tools/ValidateMenuClicks.cs` and `unity command --project-path . "script eval" --file Tools/ValidateModernShip.cs`. They run UI pointer/raycast checks and verify all roles, floor bounds, ladders, smooth zoom and progress fill geometry. Test render captures are written to the system temporary directory.

Pause/ladder regression check: `unity command --project-path . "script eval" --file Tools/ValidatePauseAndLadders.cs`.

Controls are shown for the first 10 seconds of gameplay and remain available in the pause menu. The introduction timer stops while paused or reading a mission card. E can reopen a station card to review its status even after its start window closes.

Crew starts with 100 health. A compact VITALS HUD follows the current crew member; CrewHealth.TakeDamage and CrewHealth.Heal clamp health to its valid range and update the HUD when it changes.

Solar Storm (G-02) is available at Communications on the upper deck from 2:15 to 3:50. Start the procedure, descend the central ladder to the bottom deck, reach the marked doors left of Life Support, then press E within 90 seconds. The objective timer freezes during pause and station dialogs. Missing, ignoring or timing out the alert deals 25 HP damage; at critical radiation it depletes the remaining health. Skipping or missing the initial Bridge check raises radiation. Zero HP opens the failure screen; boarding again resets health, radiation and missions.

The imported MOONIES question data, UI and phase assets are retained for later campaign integration. Phase assets marked `startsQuestionCampaign` are excluded from timed ship checks so unfinished question phases cannot appear as fake 30-second checks or obscure station interactions.

Solar storm/health integration regression: `unity command --project-path . "script eval" --file Tools/ValidateSolarStorm.cs`.
