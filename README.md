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
