# Mission clarity integration

Source: `origin/adrian3`, commit `f4e32b09` ("missions are simplified i think"). Reviewed the earlier `adrian` and `adrian2` branches as well.

Ported the simplified titles, briefings, prompts, answer labels and results for all 35 procedures. Retained Adrian's extra decision time for G-01 (40 seconds) and CMD-02, ENG-02, PIL-01 (35 seconds each). Other deadlines, answers, stages, station coordinates, role access and failure effects are unchanged. The generated maximum task budget remains below 480 seconds.

Adapted the mission-location hints, station/deck directions, dedicated shelter reminder and separate check/task countdowns to the current UI. Player-facing reminders omit internal mission IDs. Hints hide under dialogs, task board, pause and game over. The shelter step still explicitly tells the player to close the panel to walk.

The current local starfield, music, cinematic/Credits flow, mandatory Bridge chain, physical equipment, cabin radiation/HP, ladder movement and current published README are preserved. The branch's separate YouTube/CEF background implementation and bundled package changes are outside this mission-clarity port.

Unity 6000.6.9f1 Play Mode validation passed:

- `ValidateSimpleMissionHints`: check/task countdown accuracy, live CLOSE scanning, clear station/deck hints without internal IDs, tracked physical access, simplified actions, exclusive solar hint, dialog/pause hiding, continuing cabin HP damage and music.
- `ValidateMissionCatalog`: all 35 definitions and 2000 seeded plans, adjusted deadline bounds, counts/mix/phase diversity, uniqueness and run budgets.
- `ValidateMissionProcedures`: all 35 procedures completed for all four roles; correct/unsafe choices, background scans, queues and run end.
- `ValidateFlightChainAndEquipment`, `ValidateSolarStorm`: mandatory/retryable checkout, event chain, all equipment, real cabin damage, protection, pause, death and replay.
- `ValidateMissionInterface`, `ValidateMenuHint`, `ValidateMenuClicks`, `ValidateControlsAndBridge`, `ValidateHealthHud`, `ValidatePauseAndLadders`: rendered button interactions and existing gameplay/UI regressions.
- `ValidateFlightEnding`: actual complete cinematic playback with exclusive audio, looping soundtrack, Credits, main-menu/skip clicks, replay and failure fallback.

Updated captures: `mission-task-board.png`, `mission-procedure.png`, `scan-after-close.png`.
