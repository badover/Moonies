void Check(bool ok,string message){if(!ok)throw new System.Exception(message);}
UnityEngine.Application.runInBackground=true;
var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
var menu=UnityEngine.Object.FindAnyObjectByType<CrewMenu>();
var catalog=UnityEngine.Resources.LoadAll<ShipMissionDefinition>("Missions").Where(x=>!x.startsQuestionCampaign).OrderBy(x=>x.missionId=="G-01"?0:1).ToArray();
ShipMission mission=null;
void Set(string name,object value)=>typeof(ShipMission).GetField(name,flags).SetValue(mission,value);
void Fixture(bool unlocked=false){
 // Isolate individual procedures from random selection; production draws are checked separately.
 Set("runPlan",catalog.Select(x=>new MissionRunPlan.Entry{definition=x,trigger=0}).ToArray());Set("missionDefinitions",catalog);Set("missionClock",0f);
 ((System.Collections.IDictionary)typeof(ShipMission).GetField("missionStatuses",flags).GetValue(mission)).Clear();
 if(unlocked)((System.Collections.IDictionary)typeof(ShipMission).GetField("missionStatuses",flags).GetValue(mission))[catalog.Single(x=>x.missionId=="G-01")]=ShipMissionStatus.Completed;
}
async System.Threading.Tasks.Task NewFlight(int role){
 if(UnityEngine.Object.FindAnyObjectByType<ShipMission>()!=null){menu.SendMessage("ReturnToMenu");await System.Threading.Tasks.Task.Delay(80);}
 menu.SendMessage("Select",role);menu.SendMessage("Confirm");await System.Threading.Tasks.Task.Delay(100);
 mission=UnityEngine.Object.FindAnyObjectByType<ShipMission>();Check(mission!=null,"Boarding failed");mission.enabled=false;Fixture();
}
var results=new System.Collections.Generic.List<string>();
for(int role=0;role<4;role++){
 await NewFlight(role);
 foreach(var d in catalog){
  var operatorCrew=mission.GetComponentInChildren<CrewCharacter>();operatorCrew.transform.position=(UnityEngine.Vector3)d.WorldPosition+UnityEngine.Vector3.back;Set("deck",d.deck);Set("activeLadder",-1);
  Check(mission.StartMission(d),"Cannot start "+d.missionId+" as "+role);
  int guard=0;
  while(mission.ActiveMission!=null&&guard++<30){
   var step=mission.CurrentStep;Check(step!=null,"Procedure missing step");int before=mission.ProcedureStepIndex;
   if(step.automatic)mission.SendMessage("UpdateActiveMission",step.durationSeconds+.01f);
   else if(step.requiresShelter){
    var player=mission.GetComponentInChildren<CrewCharacter>();float x=ShipLayout.Ladders[1].x;player.transform.position=new UnityEngine.Vector3(x,ShipLayout.FloorY(2,x),-1);Set("deck",2);Set("activeLadder",-1);
    for(int frame=0;frame<300;frame++)mission.MoveCrew(0,-1,.02f);for(int frame=0;frame<18;frame++)mission.MoveCrew(1,0,.02f);
    Check(mission.IsNearActiveTarget&&mission.TryInteract(),"Shelter route/E interaction not usable");
    Check(mission.ActiveMission==d,"Shelter arrival completed the event without shielding/power checks");
   }else{
    if(step.options.Length>1){int wrong=(step.correctChoice+1)%step.options.Length;Check(!mission.ChooseProcedureOption(wrong)&&mission.ProcedureStepIndex==before,"Unsafe command advanced procedure");}
    Check(mission.ChooseProcedureOption(step.correctChoice),"Correct action rejected: "+d.missionId);
   }
  }
  Check(mission.GetMissionStatus(d)==ShipMissionStatus.Completed&&mission.Health.Current==100,"Procedure fails or damages crew on success: "+d.missionId);
 }
 results.Add(((CrewRole)role)+": all 35 procedures, unsafe-command rejection, actual shelter ladder route, shielding/power/confirmation and zero success damage PASS");
}
await NewFlight(2);
var scan=catalog.Single(x=>x.missionId=="G-01");menu.SendMessage("OpenMissionCard",scan);menu.SendMessage("StartSelectedMission");Check(mission.ActiveMission==scan&&!mission.IsPaused,"Starting scan pauses simulation");
mission.SendMessage("UpdateActiveMission",2f);float beforeClose=mission.ActiveMissionProgress;menu.SendMessage("CloseMissionCard");
var crew=mission.GetComponentInChildren<CrewCharacter>();crew.transform.position=new UnityEngine.Vector3(15,ShipLayout.FloorY(1,15),-1);Set("deck",1);
mission.SendMessage("UpdateActiveMission",2f);Check(mission.ActiveMissionProgress>beforeClose,"Scan stops after CLOSE or walking away");
menu.SendMessage("OpenMissionCard",scan);float beforeReview=mission.ActiveMissionProgress;Check(!mission.IsPaused&&mission.CurrentStep.automatic,"Review stops scan or loses its step");
menu.SendMessage("CloseMissionCard");mission.enabled=true;float beforeFrames=mission.ActiveMissionProgress;await System.Threading.Tasks.Task.Delay(500);Check(mission.ActiveMissionProgress>beforeFrames,"Real frames do not advance the closed scan");
menu.SendMessage("TogglePause");float paused=mission.ActiveMissionProgress;await System.Threading.Tasks.Task.Delay(200);Check(mission.ActiveMissionProgress==paused,"Escape pause fails to freeze scan");menu.SendMessage("TogglePause");mission.enabled=false;
mission.SendMessage("UpdateActiveMission",4f);Check(mission.ProcedureStepIndex==1,"Background scan did not reach its decision");
while(mission.ActiveMission!=null){var step=mission.CurrentStep;if(step.automatic){menu.SendMessage("CloseMissionCard");mission.SendMessage("UpdateActiveMission",step.durationSeconds);}else Check(mission.ChooseProcedureOption(step.correctChoice),"Scan decision failed");}
Check(mission.GetMissionStatus(scan)==ShipMissionStatus.Completed,"Scan never completes");results.Add("G-01: START SCAN, CLOSE, walk away, real-frame progression, live review, Escape pause and background departure scan PASS");
// Queued tasks survive their trigger window and cannot overlap active work.
Fixture(true);Set("missionClock",599f);var first=catalog.Single(x=>x.missionId=="G-03");var pending=catalog.Single(x=>x.missionId=="G-04");
Check(mission.GetMissionStatus(first)==ShipMissionStatus.Available&&mission.GetMissionStatus(pending)==ShipMissionStatus.Available,"Pending tasks expire with release windows");
crew.transform.position=(UnityEngine.Vector3)first.WorldPosition+UnityEngine.Vector3.back;Set("deck",first.deck);Set("activeLadder",-1);
Check(mission.StartMission(first)&&!mission.StartMission(pending)&&!mission.SkipMission(pending),"Concurrent task or skip allowed");mission.SendMessage("UpdateActiveMission",61f);
Check(mission.GetMissionStatus(first)==ShipMissionStatus.Failed&&mission.Health.Current==90&&mission.GetMissionStatus(pending)==ShipMissionStatus.Available,"Timeout/queue consequence incorrect");mission.SendMessage("UpdateActiveMission",61f);Check(mission.Health.Current==90,"Duplicate timeout damage");
crew.transform.position=(UnityEngine.Vector3)pending.WorldPosition+UnityEngine.Vector3.back;Set("deck",pending.deck);
Check(mission.StartMission(pending),"Cannot begin queued task after prior failure");menu.SendMessage("ToggleTaskBoard");Check(mission.IsInterfaceOpen&&!mission.IsPaused,"Mission board incorrectly pauses task clocks");menu.SendMessage("ToggleTaskBoard");
Set("missionClock",600f);mission.SendMessage("Update");Check(mission.RunFinished&&mission.IsPaused,"Run does not end at ten minutes");Check((bool)typeof(CrewMenu).GetField("gameOver",flags).GetValue(menu),"Run result screen absent");Check(mission.GetMissionStatus(pending)==ShipMissionStatus.Failed,"Unfinished active task not resolved at run end");
results.Add("Pending queue after release windows, exclusive active task, one-shot timeout damage, live mission board and ten-minute run result PASS");
menu.SendMessage("ReturnToMenu");return results;
