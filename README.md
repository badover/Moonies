# Space Crew

Open this repository folder in Unity Hub using Unity **6000.6.9f1**. Wait for package restoration and asset import, then open `Assets/Scenes/SampleScene.unity` and press Play.

Click START, select a role on the After Start screen, and click BOARD SHIP.

- A/D or arrow keys: walk inside the hull.
- W/S or up/down: climb the central ladder; release to pause, reverse to descend.
- Mouse wheel: smooth zoom.
- Escape: return to role selection.

The modern ship uses the source artwork with transparent surroundings and English room labels. All gameplay sprites, images, animations and frame metadata are included in the repository; no files outside the project are needed to run it.

The mission HUD includes a ten-minute progress timer.

For automated verification with the Unity Pipeline CLI, start Play Mode, then run `unity command --project-path . "script eval" --file Tools/ValidateMenuClicks.csx` and `unity command --project-path . "script eval" --file Tools/ValidateModernShip.csx`. They run UI pointer/raycast checks and verify all roles, floor bounds, ladders, smooth zoom and progress fill geometry. Test render captures are written to the system temporary directory.
