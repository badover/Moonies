# Artemis 2 — Randomized LEO Mission Task Specification

## 1. Mission start and run length

- **Run duration:** approximately 10 minutes (600 seconds).
- **Starting location:** the spacecraft begins in a stable **Low Earth Orbit (LEO), at approximately 185 km altitude**, in the Low Earth Parking Orbit described in the mission source.
- **Starting state:** launch, ascent, Max-Q, stage separation, and initial orbital insertion have already been completed. The player starts aboard the spacecraft in orbit around Earth, preparing for the Trans-Lunar Injection (TLI) departure sequence.
- **Mission progression:** LEO checkout and departure → Trans-Lunar Injection → deep-space transit → lunar orbit → lunar descent and surface operations → lunar ascent and rendezvous → Trans-Earth Injection and return → Earth reentry and recovery.
- **Compressed timeline:** the full mission is represented in a 10-minute gameplay run. The phases remain in chronological order even though the real mission would take much longer.
- **Player role:** the player selects Engineer, Pilot, Commander, or Medic before the run. The selected role cannot be changed during the run.

The game does **not** begin on the launchpad or during atmospheric ascent. Launch-phase tasks are excluded from the player task pools.

## 2. Exact player event count per run

Every run generates the following player-controlled workload:

1. **Exactly 5 role-specific tasks**, randomly selected from the pool belonging to the player's chosen role.
2. **Either 2 or 3 global events**, randomly selected from the Global Event Pool.

Therefore, every run contains **exactly 7 or 8 player-controlled events**. The five role-specific tasks and the two or three global events are all performed by the player.

### Selection rules

- Select role-specific tasks **without replacement**. No role-specific task may appear twice in one run.
- Select global events **without replacement**. No global event may appear twice in one run.
- Select the number of global events randomly: each run gets either 2 or 3.
- Do not add AI-controlled events to the 7–8 event count. Other-role AI activity, if present elsewhere in the game, is outside this specification and is not included in the pools below.
- The selected role-specific tasks must all belong to the player's chosen role. A player must never be given another role's task as a required player interaction.

### Role-specific difficulty mix

The five selected role tasks must contain:

- **2 Easy** tasks.
- **2 Medium** tasks.
- **1 Hard** task.

The random selector must draw from the matching difficulty groups in the selected role's pool. This keeps the workload varied while guaranteeing that a run does not consist entirely of easy or difficult tasks.

### Available role-pool sizes

| Selected role | Easy options | Medium options | Hard options | Total pool |
|---|---:|---:|---:|---:|
| Engineer | 2 | 3 | 3 | 8 |
| Pilot | 2 | 4 | 3 | 9 |
| Commander | 2 | 3 | 1 | 6 |
| Medic | 2 | 5 | 1 | 8 |

## 3. Task duration and complexity

| Complexity | Expected completion time | Maximum active time | Design expectation |
|---|---:|---:|---|
| **Easy** | 15–30 seconds | 30 seconds | One simple check, calibration, alignment, or straightforward decision. |
| **Medium** | 30–60 seconds | 60 seconds | A short sequence, a few connected actions, or a decision requiring comparison of information. |
| **Hard** | 60–90 seconds | 90 seconds | A multi-step diagnosis, repair, or procedure with meaningful choices. |

The duration includes all required player actions for that event. **No event may require more than 90 seconds of active player time.** A multi-step event must be shortened or divided into separate independently completable events if it exceeds that limit.

Only one player-controlled event should require active interaction at a time. Randomized event times must be staggered so the player is not forced to complete overlapping tasks.

## 4. Mission phase timing

The phases below define when tasks and global events are allowed to appear. Exact trigger times may vary between runs, but an event must never occur before its mission phase is reached.

| Mission time | Phase | Relevant activity |
|---|---|---|
| **0:00–1:20** | LEO checkout | Begin in the 185 km Low Earth Parking Orbit; verify spacecraft systems, telemetry, docking configuration, and departure readiness. |
| **1:20–2:15** | TLI and Earth departure | Complete the final attitude setup and execute the Trans-Lunar Injection burn. |
| **2:15–3:50** | Deep-space transit | Conduct course corrections, radiation and life-support checks, and any in-transit contingency actions. |
| **3:50–4:55** | Lunar orbit and separation preparation | Execute lunar-orbit insertion, correct orbital disturbances, and verify transfer-tunnel/docking safety before separation. |
| **4:55–5:45** | Lunar descent and touchdown | Respond to landing-computer alerts, avoid surface hazards, and complete landing. |
| **5:45–6:50** | Lunar surface operations | Conduct surface science, equipment handling, crew health checks, and the stay/no-stay decision. |
| **6:50–7:50** | Lunar ascent and rendezvous | Prepare the ascent stage, restore engine arming, shed non-essential mass, ascend, rendezvous, and dispose of the unused stage. |
| **7:50–10:00** | Earth return and recovery | Execute Trans-Earth Injection, configure passive thermal control, perform reentry, splashdown, and recovery checks. |

### Timing randomization

- Each selected event has an allowed phase window, shown in its pool entry.
- The event generator chooses a trigger time randomly within that window. The window determines when the event becomes available; the task may be completed after the window closes.
- If another player task is active when an event becomes available, place the new event in the pending queue. Its active-time limit starts only when the player begins it.
- Only one player task may be active at a time. Pending events must remain available until completed or until the mission ends; they must not disappear merely because another task was active.
- Keep mission chronology fixed. Randomization changes which tasks appear and their trigger times, not the order of mission phases.
- The five role tasks have a maximum combined active duration of 270 seconds. Three global events have a maximum combined active duration of 210 seconds. The maximum planned player-task time is therefore 480 seconds, leaving at least 120 seconds of the 600-second run for movement and transitions.

## 5. Global event pool

Select **2 or 3** events from this pool per run. Every global event must be completable by the player regardless of the selected role. It must not require a role-specific capability, a role change, or another crew member to perform a mandatory step.

| ID | Global event | Complexity / duration | Allowed time window | Player task and decision |
|---|---|---|---|---|
| **G-01** | LEO systems and telemetry checkout | Easy · 15–30 s | 0:05–1:20 | From the general mission interface, verify that the spacecraft is stable in LEO, confirm the Earth/Mission Control telemetry link, and check that the departure systems report ready. This is an in-orbit checkout, not a launch or pre-launch briefing. |
| **G-02** | Solar particle event: crew shelter | Hard · 60–90 s | 2:15–3:50 | Move the crew to the protected central equipment area, arrange available water and food stores as shielding, power down non-essential electronics, and confirm the shelter configuration. All actions are available to every role. |
| **G-03** | Transfer-tunnel pressure and docking safety check | Medium · 30–60 s | 3:50–4:55 | Inspect the docking latch-lock and transfer-tunnel pressure indicators. If either is outside its safe state, hold separation, perform the available latch/pressure verification, and release only when the indicators are safe. The player performs the complete check. |
| **G-04** | Lunar surface stay / no-stay decision | Medium · 30–60 s | 5:45–6:50 | Review the descent-stage propellant pressure warning, run the available venting and avionics safety checks, and decide whether conditions permit the crew to remain on the surface or require an early departure. The decision and checks are available to every role. |

### Global-event selection constraints

- Choose 2 or 3 distinct global events from G-01 to G-04.
- The random selection must respect each event's allowed mission phase.
- If 3 global events are selected, do not select more than one Hard global event. The current pool contains only one Hard global event.
- Global events count toward the player's 7–8 event total. They are not background/AI events.

## 6. Engineer task pool

When Engineer is selected, randomly select exactly **2 Easy, 2 Medium, and 1 Hard** task from this pool. The player must be able to complete the entire selected task independently.

| ID | Task | Complexity / duration | Allowed time window | Player task and decision |
|---|---|---|---|---|
| **ENG-01** | Propellant settling / ullage verification | Easy · 15–30 s | 0:05–1:20 | Inspect the propellant-settling indicators, verify that the ullage sequence is required, activate the settling sequence, and confirm that liquid propellant is positioned correctly before the main engine burn. Do not authorize ignition while the propellant is unsettled. |
| **ENG-02** | Ascent-engine arming breaker recovery | Easy · 15–30 s | 6:50–7:50 | Inspect the damaged engine-arming breaker, choose the safe non-conductive recovery tool, restore the circuit, and verify the engine-arming indicator. Unsafe conductive or liquid fixes must be rejected. |
| **ENG-03** | Backup propellant valve-bank switch | Medium · 30–60 s | 7:50–8:35 | Inspect the primary propellant-valve warning, verify the redundant valve bank, switch to the backup bank, and confirm that the feed path is ready for the Trans-Earth Injection burn. |
| **ENG-04** | Passive thermal-control configuration | Medium · 30–60 s | 8:00–9:10 | Set the spacecraft to the specified Sun-relative orientation, initiate the slow passive thermal-control roll, and verify that the roll rate is within the target range. This is an independent engineering control task; it does not require a Pilot task. |
| **ENG-05** | CO₂ scrubber canister adapter repair | Hard · 60–90 s | 2:15–3:50 | Inspect the incompatible backup canister and available materials, identify the correct adapter arrangement, assemble the improvised connection, and verify that airflow is restored and the CO₂ reading begins returning toward safe limits. The player completes the entire repair. |
| **ENG-06** | Spacesuit PLSS radiator dust blockage | Hard · 60–90 s | 5:45–6:50 | Inspect the clogged radiator louvers and suit bearings, select the anti-static cleaning tool, clear the affected surfaces, restore the cooling-flow setting, and verify that suit temperature is stabilizing. |
| **ENG-07** | RTG fuel-rod extraction and science-station deployment | Hard · 60–90 s | 5:45–6:50 | Inspect the thermally bound fuel rod and deployment cask, select the insulated long-handled tool, gently rock and rotate the rod to release the binding, insert it into the RTG, and confirm that the science station is positioned at a safe distance from the lander. The task is the mechanical equipment procedure; any separate crew radiation screening is not a prerequisite. |
| **ENG-08** | Guidance-computer data-load isolation | Medium · 30–60 s | 4:55–5:45 | Inspect the computer-load display during the 1202 alarm, identify the non-essential radar or science-data streams contributing to the overload, isolate those streams, and verify that processor load returns to the safe range while core guidance remains active. This is an engineering systems task, independent of the Commander's operational decision. |

## 7. Pilot task pool

When Pilot is selected, randomly select exactly **2 Easy, 2 Medium, and 1 Hard** task from this pool. All flight-control actions must be self-contained and completable by the Pilot alone.

| ID | Task | Complexity / duration | Allowed time window | Player task and decision |
|---|---|---|---|---|
| **PIL-01** | LEO docking-target alignment | Easy · 15–30 s | 0:05–1:20 | Align the spacecraft and lunar-module docking references, reduce the displayed relative closing rate to the safe capture range, and confirm target alignment. This is a short in-orbit alignment task, not a launch task. |
| **PIL-02** | Pre-TLI attitude alignment | Easy · 15–30 s | 1:20–2:15 | Align the spacecraft to the displayed TLI attitude reference, stabilize the attitude indicators, and confirm that the vehicle is ready for the planned departure burn. This is a short setup task adapted from the LEO ullage/TLI sequence. |
| **PIL-03** | Trans-Lunar Injection burn timing | Medium · 30–60 s | 1:20–2:15 | Review the target burn profile, start the TLI burn, monitor the velocity/energy indicator, and shut down when the target is reached. The player controls the full timing and cutoff sequence. |
| **PIL-04** | Mid-course correction | Medium · 30–60 s | 2:15–3:50 | Compare the onboard star-angle reference with the course-error indicator, select the correction direction and magnitude, and complete the short correction burn. |
| **PIL-05** | Lunar Orbit Insertion burn execution | Medium · 30–60 s | 3:50–4:55 | Verify the preloaded retrograde burn parameters, align the inertial reference, execute the autonomous LOI burn during the planned loss-of-signal window, and confirm lunar-orbit capture. |
| **PIL-06** | Lunar mascon orbit correction | Medium · 30–60 s | 3:50–4:55 | Read the changing perilune/apolune indicators, identify the orbit-decay trend, select the appropriate stabilizing correction, and confirm that the orbit is within the safe band. |
| **PIL-07** | Lunar hazard avoidance and landing | Hard · 60–90 s | 4:55–5:45 | Identify the unsafe landing area from the surface view, select semi-manual descent control, translate toward safe terrain, and complete the landing within the displayed fuel and tilt limits. |
| **PIL-08** | Orbital rendezvous | Hard · 60–90 s | 6:50–7:50 | Read the relative orbit and phase-angle indicators, select the lower-orbit catch-up approach, execute the transfer burn at the indicated point, and confirm rendezvous with the command/service module. |
| **PIL-09** | Reentry corridor and recovery sequence | Hard · 60–90 s | 9:00–10:00 | Maintain the specified reentry corridor, monitor the entry profile, deploy the main parachutes at the indicated condition, and confirm that the capsule reaches the correct post-splashdown orientation for recovery. |

## 8. Commander task pool

When Commander is selected, randomly select exactly **2 Easy, 2 Medium, and 1 Hard** task from this pool. Each task is an independent operational decision or authorization; it must not require another role to execute a mandatory follow-up.

| ID | Task | Complexity / duration | Allowed time window | Player task and decision |
|---|---|---|---|---|
| **CMD-01** | Guidance-computer 1202 alarm response | Easy · 15–30 s | 4:55–5:45 | Review the alarm status, distinguish core guidance functions from non-essential data streams, and authorize shedding non-essential sensor/radar data while preserving active landing guidance. |
| **CMD-02** | Lunar ascent mass-jettison checklist | Easy · 15–30 s | 6:50–7:50 | Review the ascent mass limit, identify non-essential equipment to leave on the surface, protect the collected lunar samples, update the mass checklist, and authorize liftoff preparation. |
| **CMD-03** | Radiation-belt operational priorities | Medium · 30–60 s | 2:15–3:50 | Review the radiation warning, choose which non-essential instruments to power down, and select the available water/food shielding arrangement for the crew and critical equipment. This is a complete operational/resource decision. |
| **CMD-04** | Autonomous lunar-orbit insertion authorization | Medium · 30–60 s | 3:50–4:55 | Review the planned loss-of-signal period, verify the preloaded LOI burn parameters and autonomous execution plan, and authorize the sequence before the spacecraft passes behind the Moon. This does not require the player to be the Pilot. |
| **CMD-05** | Lunar surface science and thermal-resource priorities | Medium · 30–60 s | 5:45–6:50 | Review the cooling-water reserve and remaining surface timeline, prioritize the available science activities, and set safe duty-cycle limits so science work does not consume the reserve needed for crew survival. |
| **CMD-06** | Ascent-stage controlled disposal decision | Hard · 60–90 s | 7:30–8:20 | Review the active spacecraft and seismometer positions, choose a controlled disposal trajectory that avoids the crewed vehicle and deployed science equipment, and confirm the intended impact plan. |

## 9. Medic task pool

The source mission script does not define a Medic role or provide a full set of medical procedures. The tasks below are therefore **crew-health and exposure-assessment adaptations** of scenarios in that script. They must remain screening, monitoring, and crew-safety tasks; do not present them as clinical diagnosis or treatment procedures.

When Medic is selected, randomly select exactly **2 Easy, 2 Medium, and 1 Hard** task from this pool. The Medic must be able to complete the entire selected task independently. Mechanical repairs remain separate Engineer tasks and are never prerequisites for a Medic task.

| ID | Task | Complexity / duration | Allowed time window | Player task and decision |
|---|---|---|---|---|
| **MED-01** | CO₂ exposure and symptom assessment | Easy · 15–30 s | 2:15–3:50 | Check the cabin CO₂ reading and the listed symptoms associated with elevated CO₂, assess the crew's exposure status, and select the appropriate monitoring priority. The scrubber repair is not part of this task. |
| **MED-02** | Surface heat-stress and hydration assessment | Easy · 15–30 s | 5:45–6:50 | Review cabin/suit temperature and cooling-water reserve indicators alongside crew heat and hydration status. Select a safe work/rest or hydration response while preserving the required cooling reserve. |
| **MED-03** | Radiation exposure screening | Medium · 30–60 s | 2:15–3:50 | Compare radiation-counter readings with crew exposure indicators, identify the highest-risk exposure status, and record the appropriate observation priority. The Medic does not need to operate the spacecraft's power or attitude systems. |
| **MED-04** | RTG handling radiation-safety check | Medium · 30–60 s | 5:45–6:50 | Review the radioactive-source warning, establish the safe handling boundary and placement distance, check crew exposure indicators, and confirm that the crew is clear of the source area. Mechanical fuel-rod extraction is not required. |
| **MED-05** | Post-splashdown crew assessment and safe egress | Medium · 30–60 s | 9:00–10:00 | Verify that the capsule is upright before egress, confirm that the exit route is safe, and complete a rapid crew-condition assessment before recovery. |
| **MED-06** | EVA overheating and dust-exposure response | Hard · 60–90 s | 5:45–6:50 | Review the astronaut's suit-temperature and cooling indicators, assess the crew member's condition, decide whether to continue or terminate the EVA, and enforce the walkback limit. Detailed PLSS hardware repair is not part of this Medic task. |
| **MED-07** | Post-solar-event crew exposure follow-up | Medium · 30–60 s | 3:00–3:50 | Review the event radiation log and crew exposure indicators, identify anyone requiring closer observation, and set the follow-up monitoring priority. This is a standalone crew assessment; the player does not need to complete the global shelter event first. |
| **MED-08** | LEO crew readiness baseline | Medium · 30–60 s | 0:05–1:20 | Review the crew's baseline hydration, fatigue, and general condition indicators before the TLI departure sequence. Identify any crew member needing observation and record the readiness status. This is a game-level screening task, not a clinical diagnosis. |

## 10. Randomization and scheduling rules

### Role task selection

1. Read the player's locked role.
2. Load only that role's task pool.
3. Randomly select exactly **2 Easy**, **2 Medium**, and **1 Hard** task, without replacement.
4. Confirm that the five selected tasks belong to valid mission phases and can be scheduled inside their listed time windows.
5. The five selected tasks must span **at least 3 different mission phases**. If the draw does not meet this pacing constraint, redraw from the same difficulty group.
6. If the selected set cannot be scheduled within the 10-minute run budget, redraw the conflicting selection. Do not reduce the number of tasks.

### Global event selection

1. Randomly choose whether the run will contain **2 or 3** global events.
2. Select that number of distinct events from G-01 to G-04, without replacement.
3. Schedule each event only in its allowed phase window.
4. Global events are player-controlled and count toward the run's exact total of 7 or 8 interactions.

### Event timing

- Randomize the exact trigger time within the selected event's allowed time window. The window determines when the event becomes available; the task may be completed after the window closes.
- Preserve the mission's chronological phase order. Do not display a task before its underlying mission phase or condition exists.
- If another player task is active when an event becomes available, place the new event in the pending queue. Its active-time limit starts only when the player begins it.
- Only one player task may be active at a time. Pending events must remain available until completed or until the mission ends; they must not disappear merely because another task was active.
- Do not let randomization change the selected task's objective, required steps, success condition, or failure condition.
- The five role tasks have a maximum combined active duration of 270 seconds. Three global events have a maximum combined active duration of 210 seconds. The maximum planned player-task time is therefore 480 seconds, leaving at least 120 seconds of the 600-second run for movement and transitions.
- A task that has not been completed by its maximum active time is treated as timed out according to that task's failure rule. The timeout must not exceed 30 seconds for Easy, 60 seconds for Medium, or 90 seconds for Hard.

## 11. Single-player independence rules

- The player chooses one role before the run and remains in that role for the entire mission.
- Every selected role-specific task must be fully completable by the player in that role alone.
- Every global event must be fully completable by any of the four roles.
- A task may involve multiple stations or several steps, but all required interactions must be available to the player regardless of any other crew member's actions.
- No selected task may require the player to switch roles, wait for another role, or depend on another role's event being completed.
- Do not create multi-role problem chains. Separate tasks inspired by the same incident are independent alternatives, not mandatory stages of one shared repair.
- Failure or timeout consequences must be defined within the task itself. A failure must not create a new obligation that can only be solved by a different role.

## 12. Exclusions and interpretation

- **Exclude all pre-launch and atmospheric launch tasks:** launchpad cryogenic fueling/de-icing, launchpad countdown, gravity-turn ascent, Max-Q throttle control, stage-separation/Pogo events, and initial orbital insertion. The run starts after these activities, in LEO.
- **Retain LEO and later mission phases:** the first playable events may involve orbital checkout, ullage/propellant settling, docking configuration, and TLI preparation.
- **Adapt Act 1:** only the telemetry/readiness verification concept is retained as the in-orbit LEO systems and telemetry checkout. The pre-launch briefing is not part of the run.
- **Adapt Pilot LEO setup:** docking-target alignment and pre-TLI attitude alignment are short in-orbit tasks suitable for the established starting state.
- **Medic content remains adapted:** Medic tasks are crew screening and exposure-management activities based on the script's crew-health hazards, not original clinical procedures from the source.
- **AI-only events are not specified here:** any optional AI activity involving the other roles is separate background simulation. It is not included in the task pools, is not player-controlled, and does not count toward the exact 7–8 player events.

## 13. Per-run validation checklist

A generated run is valid only if all of the following are true:

- [ ] The mission starts in a stable **185 km Low Earth Parking Orbit**.
- [ ] No launchpad, atmospheric ascent, Max-Q, or pre-orbit task is scheduled.
- [ ] The player has one fixed role for the entire run.
- [ ] Exactly **5** tasks are selected from that role's pool.
- [ ] The five role tasks contain exactly **2 Easy, 2 Medium, and 1 Hard** task.
- [ ] Exactly **2 or 3** distinct global events are selected.
- [ ] The total number of player-controlled events is exactly **7 or 8**.
- [ ] No AI-only event is counted toward the player's 7–8 events.
- [ ] The five role-specific tasks span at least 3 mission phases.
- [ ] All selected events become available within their mission phase windows and in chronological order.
- [ ] No two active player-controlled events overlap.
- [ ] Easy tasks take no more than 30 seconds, Medium no more than 60 seconds, and Hard no more than 90 seconds.
- [ ] Every task is independently completable by the selected role or, for a global event, by any role.
- [ ] No task requires role switching, another role's completion, or an AI-controlled action as a prerequisite.
