# Recovery ending regression

The statistics-only result came from an extra victory gate: the player had to complete every randomly selected side task for the cinematic to run. Reaching 10:00 alive with skipped, failed or pending tasks instead showed the statistics screen.

Recovery now wins when the mandatory Bridge checkout is complete, the crew is alive, and the phase clock reaches 600 seconds. Side-task results retain their health/statistics effects but cannot hide the cinematic and Credits. Death and an unfinished flight remain excluded.

`ValidateRecoveryEnding.cs` reproduces the reported path with mixed task results and enables the real `ShipMission.Update` just before 600 seconds. It verifies automatic timer completion, full progress, hidden statistics/HUD, actual full video decoding at timeScale zero with game music stopped, natural transition to Credits and resumed music. It also verifies the death path is still excluded.

`ValidateFlightEnding.cs` now explicitly expects the cinematic after missed side work, while retaining its complete-playback, looping audio, Credits/main-menu clicks, skip, replay and decoder-fallback checks.
