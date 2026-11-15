void Check(bool ok,string text){if(!ok)throw new System.Exception(text);}
UnityEngine.Application.runInBackground=true;
var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
var menu=UnityEngine.Object.FindAnyObjectByType<CrewMenu>();var presentation=menu.GetComponent<FlightPresentation>();ShipMission mission=null;
async System.Threading.Tasks.Task NewFlight(){if(UnityEngine.Object.FindAnyObjectByType<ShipMission>()!=null){menu.SendMessage("ReturnToMenu");await System.Threading.Tasks.Task.Delay(100);}menu.SendMessage("Confirm");await System.Threading.Tasks.Task.Delay(100);mission=UnityEngine.Object.FindAnyObjectByType<ShipMission>();mission.enabled=false;}
await NewFlight();
// Reproduce the real reported path: recovery with mixed side-task statistics.
var statuses=(System.Collections.IDictionary)typeof(ShipMission).GetField("missionStatuses",flags).GetValue(mission);
var entries=mission.RunPlan.ToArray();statuses[entries[0].definition]=ShipMissionStatus.Completed;
statuses[entries[1].definition]=ShipMissionStatus.Skipped;statuses[entries[2].definition]=ShipMissionStatus.Failed;
Check(!mission.RunSucceeded&&presentation.CurrentPhase==FlightPresentation.Phase.Game,"Flight won before recovery");
typeof(ShipMission).GetField("missionClock",flags).SetValue(mission,599.9f);
// Avoid introducing new cabin events while approaching recovery; event logic is separately tested.
var announced=(System.Collections.Generic.HashSet<ShipMissionDefinition>)typeof(ShipMission).GetField("announcedMissions",flags).GetValue(mission);foreach(var e in entries)announced.Add(e.definition);
mission.Health.TakeDamage(30);mission.enabled=true;
float deadline=UnityEngine.Time.realtimeSinceStartup+5;
while(!mission.RunFinished&&UnityEngine.Time.realtimeSinceStartup<deadline)await System.Threading.Tasks.Task.Delay(50);
Check(mission.RunFinished&&mission.MissionSeconds==600&&mission.RunSucceeded,"Real Update does not win at the full progress bar with mixed task results");
Check(presentation.CurrentPhase==FlightPresentation.Phase.Cinematic,"Recovery is diverted to the statistics screen");
var results=(UnityEngine.GameObject)typeof(CrewMenu).GetField("gameOverScreen",flags).GetValue(menu);var hud=(UnityEngine.GameObject)typeof(CrewMenu).GetField("missionHud",flags).GetValue(menu);
Check(!results.activeSelf&&!hud.activeSelf&&UnityEngine.Time.timeScale==0&&!presentation.MusicPlaying,"Statistics/HUD cover ending or music continues");
var movie=presentation.CinematicPlayer;bool decoded=false;deadline=UnityEngine.Time.realtimeSinceStartup+30;
while(presentation.CurrentPhase==FlightPresentation.Phase.Cinematic&&UnityEngine.Time.realtimeSinceStartup<deadline){if(movie.frame>3)decoded=true;await System.Threading.Tasks.Task.Delay(250);}
Check(decoded&&presentation.CurrentPhase==FlightPresentation.Phase.Credits&&presentation.MusicPlaying,"Full real recovery movie did not reach Credits with music resumed");
// Restore and verify death still takes the failure path even at the end of the timer.
await NewFlight();mission.Health.TakeDamage(100);typeof(ShipMission).GetField("missionClock",flags).SetValue(mission,600f);mission.SendMessage("FinishRun");
Check(!mission.RunSucceeded&&presentation.CurrentPhase==FlightPresentation.Phase.Game,"A dead crew receives the victory movie");
menu.SendMessage("ReturnToMenu");return "PASS: real Update reaches 600s with living crew and mixed skipped/failed/pending side tasks; full progress triggers actual movie rather than statistics; HUD/music hidden, full decoded playback to Credits and music resume; death still excluded";
