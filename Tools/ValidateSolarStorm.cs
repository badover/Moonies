void Check(bool ok,string message){if(!ok)throw new System.Exception(message);}
var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
var menu=UnityEngine.Object.FindAnyObjectByType<CrewMenu>();
var storm=UnityEngine.Resources.Load<ShipMissionDefinition>("Missions/G02_Solar_Storm");
ShipMission mission=null;
void Set(string name,object value)=>typeof(ShipMission).GetField(name,flags).SetValue(mission,value);
async System.Threading.Tasks.Task NewFlight(){
 if(UnityEngine.Object.FindAnyObjectByType<ShipMission>()!=null){menu.SendMessage("ReturnToMenu");await System.Threading.Tasks.Task.Delay(80);}
 menu.SendMessage("Confirm");await System.Threading.Tasks.Task.Delay(100);mission=UnityEngine.Object.FindAnyObjectByType<ShipMission>();mission.enabled=false;
 Set("runPlan",new[]{new MissionRunPlan.Entry{definition=storm,trigger=135}});Set("missionDefinitions",new[]{storm});
 Check(mission.Health.Current==100&&mission.RadiationLevel==0&&!mission.IsPaused,"Replay state not reset");
}
await NewFlight();
var alert=(UnityEngine.GameObject)typeof(CrewMenu).GetField("stormSuggestion",flags).GetValue(menu);
var title=(UnityEngine.UI.Text)typeof(CrewMenu).GetField("stormAlertTitle",flags).GetValue(menu);
var instruction=(UnityEngine.UI.Text)typeof(CrewMenu).GetField("stormAlertInstruction",flags).GetValue(menu);
Set("missionClock",134f);menu.SendMessage("UpdateStormSuggestion");Check(!alert.activeSelf,"Storm appears before release");
Set("missionClock",135f);menu.SendMessage("UpdateStormSuggestion");Check(alert.activeInHierarchy&&title.text=="SOLAR STORM DETECTED"&&instruction.text.Contains("MISSION TASKS"),"Storm release has no visible response direction");
Check(alert.GetComponent<UnityEngine.UI.Image>().color.a==1&&alert.transform.Find("Alert Opaque Backing").GetComponent<UnityEngine.UI.Image>().color.a==1,"Warning is not opaque");
menu.SendMessage("OpenMissionCard",storm);Check(!alert.activeSelf&&!mission.IsPaused,"Card overlaps alert or pauses world");
menu.SendMessage("StartSelectedMission");menu.SendMessage("CloseMissionCard");menu.SendMessage("UpdateRadiationStatus");menu.SendMessage("UpdateStormSuggestion");
Check(alert.activeSelf&&title.text.Contains("SHELTER")&&instruction.text.Contains("90s"),"Active warning not persistent");
var radiation=(UnityEngine.UI.Text)typeof(CrewMenu).GetField("radiationStatus",flags).GetValue(menu);Check(radiation.text.Contains("35%")&&radiation.text.Contains("ELEVATED"),"Storm does not register radiation exposure");
mission.SendMessage("UpdateActiveMission",1f);menu.SendMessage("UpdateStormSuggestion");Check(instruction.text.Contains("89s"),"Warning countdown not advancing");
menu.SendMessage("TogglePause");float remaining=mission.ActiveMissionSecondsRemaining;mission.SendMessage("UpdateActiveMission",10f);Check(!alert.activeSelf&&mission.ActiveMissionSecondsRemaining==remaining,"Pause fails to hide/freeze storm");menu.SendMessage("TogglePause");menu.SendMessage("UpdateStormSuggestion");Check(alert.activeSelf,"Warning lost after pause");
mission.SendMessage("UpdateActiveMission",91f);menu.SendMessage("UpdateRadiationStatus");Check(mission.GetMissionStatus(storm)==ShipMissionStatus.Failed&&mission.Health.Current==75&&mission.RadiationLevel==60,"Storm timeout consequence wrong");mission.SendMessage("UpdateActiveMission",91f);Check(mission.Health.Current==75,"Repeated timeout damage");
await NewFlight();Set("missionClock",500f);Check(mission.GetMissionStatus(storm)==ShipMissionStatus.Available,"Queued storm expired after release window");Check(mission.SkipMission(storm)&&mission.Health.Current==75&&mission.RadiationLevel==35,"Skip consequence wrong");Check(!mission.SkipMission(storm)&&mission.Health.Current==75,"Repeated skip damage");
menu.SendMessage("UpdateRadiationStatus");menu.SendMessage("UpdateStormSuggestion");Check(alert.activeSelf&&title.text=="RADIATION LEVEL INCREASED","Radiation rise not announced");
mission.Health.TakeDamage(75);Check(mission.IsPaused&&((UnityEngine.GameObject)typeof(CrewMenu).GetField("gameOverScreen",flags).GetValue(menu)).activeInHierarchy,"Zero HP does not stop play");menu.SendMessage("TogglePause");Check(mission.IsPaused,"Escape bypasses death");
await NewFlight();Check(!alert.activeSelf,"Old storm warning leaked into replay");menu.SendMessage("ReturnToMenu");
return "PASS: scheduled arrival, opaque warning and directions, live radiation exposure, persistent countdown, paused timers, one-shot timeout/skip consequences, pending storm after release window, radiation increase notice, death and replay reset";
