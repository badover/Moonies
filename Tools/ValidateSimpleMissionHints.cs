void Check(bool ok,string text){if(!ok)throw new System.Exception(text);}
var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
var menu=UnityEngine.Object.FindAnyObjectByType<CrewMenu>();
if(UnityEngine.Object.FindAnyObjectByType<ShipMission>()!=null){menu.SendMessage("ReturnToMenu");await System.Threading.Tasks.Task.Delay(80);}menu.SendMessage("Confirm");await System.Threading.Tasks.Task.Delay(100);
var mission=UnityEngine.Object.FindAnyObjectByType<ShipMission>();mission.enabled=false;
var intro=UnityEngine.Resources.Load<ShipMissionDefinition>("Missions/G01_LEO_Check");
var engineering=UnityEngine.Resources.LoadAll<ShipMissionDefinition>("Missions").Single(x=>x.missionId=="ENG-01");
var storm=UnityEngine.Resources.Load<ShipMissionDefinition>("Missions/G02_Solar_Storm");
void Set(string field,object value)=>typeof(ShipMission).GetField(field,flags).SetValue(mission,value);
UnityEngine.GameObject Panel(string field)=>(UnityEngine.GameObject)typeof(CrewMenu).GetField(field,flags).GetValue(menu);
UnityEngine.UI.Text Text(string field)=>(UnityEngine.UI.Text)typeof(CrewMenu).GetField(field,flags).GetValue(menu);
Set("runPlan",new[]{new MissionRunPlan.Entry{definition=intro,trigger=0},new MissionRunPlan.Entry{definition=engineering,trigger=5},new MissionRunPlan.Entry{definition=storm,trigger=135}});Set("missionDefinitions",new[]{intro,engineering,storm});
menu.SendMessage("OpenMissionCard",intro);menu.SendMessage("StartSelectedMission");
Check(Text("missionCardStatus").text.Contains("CHECK 8s")&&Text("missionCardStatus").text.Contains("TASK 40s"),"Scan wait and overall deadline are not distinct");
mission.SendMessage("UpdateActiveMission",1.25f);menu.SendMessage("UpdateMissionCard");Check(Text("missionCardStatus").text.Contains("CHECK 7s")&&Text("missionCardStatus").text.Contains("TASK 39s"),"Countdown values do not follow actual timers");
menu.SendMessage("CloseMissionCard");float progress=mission.ActiveMissionProgress;mission.SendMessage("UpdateActiveMission",.5f);Check(mission.ActiveMissionProgress>progress,"Simplification stopped background scanning");
while(mission.ActiveMission!=null){var step=mission.CurrentStep;if(step.automatic)mission.SendMessage("UpdateActiveMission",step.durationSeconds);else Check(mission.ChooseProcedureOption(step.correctChoice),"Simplified correct action fails");}
Set("missionClock",5f);menu.SendMessage("UpdateBridgeSuggestion");Check(Panel("bridgeSuggestion").activeSelf,"Next task has no location hint");
string hint=Text("missionSuggestionText").text;Check(hint.Contains(engineering.title)&&hint.Contains("Engineering console")&&hint.Contains("lower deck")&&!hint.Contains("ENG-01"),"Location hint lacks clear destination or exposes debug ID");
menu.SendMessage("OnMissionAvailable",engineering);Check(Text("shipNoticeText").text.Contains("lower deck")&&Text("shipNoticeText").text.Contains("press E"),"Task announcement lacks directions");
menu.SendMessage("OpenMissionCard",engineering);Check(!Panel("bridgeSuggestion").activeSelf&&Text("missionStartButton").text=="GO TO STATION","Hints overlap card or remote equipment is not distinguished");menu.SendMessage("StartSelectedMission");Check(mission.ActiveMission==null&&mission.TrackedEquipment!=null,"Remote task start bypasses physical equipment");
var player=mission.GetComponentInChildren<CrewCharacter>();player.transform.position=(UnityEngine.Vector3)engineering.WorldPosition+UnityEngine.Vector3.back;Set("deck",engineering.deck);
Check(mission.TryInteract(),"Station interaction lost");menu.SendMessage("StartSelectedMission");Check(mission.ChooseProcedureOption(mission.CurrentStep.correctChoice),"Simplified ullage action fails");menu.SendMessage("UpdateMissionCard");Check(Text("missionCardStatus").text.Contains("CHECK 16s"),"Automatic settling timer missing");menu.SendMessage("CloseMissionCard");
while(mission.ActiveMission!=null){var step=mission.CurrentStep;if(step.automatic)mission.SendMessage("UpdateActiveMission",step.durationSeconds);else Check(mission.ChooseProcedureOption(step.correctChoice),"Settling confirmation fails");}
Set("missionClock",135f);mission.SendMessage("UpdateFlightEvents");menu.SendMessage("UpdateBridgeSuggestion");menu.SendMessage("UpdateStormSuggestion");
Check(!Panel("bridgeSuggestion").activeSelf&&Panel("solarStormHint").activeSelf,"General and storm hints overlap or storm directions missing");Check(Panel("solarStormHint").GetComponentInChildren<UnityEngine.UI.Text>().text.Contains("lower deck")&&!Panel("solarStormHint").GetComponentInChildren<UnityEngine.UI.Text>().text.Contains("G-02"),"Shelter directions are unclear/debug-labelled");
mission.SendMessage("UpdateCabinRadiation",5f);Check(mission.Health.Current<100,"Mission simplification removed cabin HP damage");
menu.SendMessage("OpenMissionCard",storm);Check(!Panel("solarStormHint").activeSelf,"Shelter hint covers briefing");menu.SendMessage("CloseMissionCard");menu.SendMessage("TogglePause");Check(!Panel("solarStormHint").activeSelf&&!Panel("bridgeSuggestion").activeSelf,"Location hints cover pause");menu.SendMessage("TogglePause");
Check(menu.GetComponent<FlightPresentation>().MusicPlaying,"Soundtrack lost during simplification");menu.SendMessage("ReturnToMenu");
return "PASS: separate scan/task countdowns, ongoing CLOSE scans, clear station/deck directions without debug IDs, tracked physical station access, simplified actions, exclusive solar hint, card/pause visibility, cabin HP damage and soundtrack retained";
