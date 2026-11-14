Ship movement validation

Validated in Unity 6000.6.9f1, Play Mode, an isolated project copy using the project Pipeline CLI.

- All four crew roles board with the interior material and reduced scale (0.64).
- Both horizontal hull limits hold, including the sloped cockpit floor and the wall before the service tanks.
- Three deck levels connect through the mapped ladders.
- Climb sprites change; movement and animation pause without ladder input; descent reverses movement and the cycle.
- Animator resumes Idle/Walk on leaving a ladder.
- Wheel events reported as either 1 or 120 produce the same 22% zoom change, within 2.5–12 limits.
- Runtime renders inspected: ship-check.png and ship-ladder.png.
- No C# compilation errors or shader errors in the verification log.
