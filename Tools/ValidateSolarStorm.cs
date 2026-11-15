void Check(bool ok,string message){if(!ok)throw new System.Exception(message);}
var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
var menu=UnityEngine.Object.FindAnyObjectByType<CrewMenu>();
var storm=UnityEngine.Resources.Load<ShipMissionDefinition>("Missions/G02_Solar_Storm");
var bridge=UnityEngine.Resources.Load<ShipMissionDefinition>("Missions/G01_LEO_Check");
ShipMission mission=null;
void Set(string name,object value)=>typeof(ShipMission).GetField(name,flags).SetValue(mission,value);
void At(int deck,UnityEngine.Vector2 point){var p=mission.GetComponentInChildren<CrewCharacter>();p.transform.position=new UnityEngine.Vector3(point.x,point.y,-1);Set("deck",deck);Set("activeLadder",-1);mission.SendMessage("UpdateNearbyMission");}
void Clock(float value)=>Set("missionClock",value);
void Close()=>menu.SendMessage("CloseMissionCard");
async System.Threading.Tasks.Task NewFlight(){if(mission!=null||UnityEngine.Object.FindAnyObjectByType<ShipMission>()!=null){menu.SendMessage("ReturnToMenu");await System.Threading.Tasks.Task.Delay(100);}menu.SendMessage("Confirm");await System.Threading.Tasks.Task.Delay(100);mission=UnityEngine.Object.FindAnyObjectByType<ShipMission>();mission.enabled=false;Check(mission.Health.Current==100&&mission.RadiationLevel==0&&!mission.IsPaused,"Replay state not reset");Check(!((UnityEngine.GameObject)typeof(CrewMenu).GetField("gameOverScreen",flags).GetValue(menu)).activeSelf,"Failure overlay leaked into replay");}
void CompleteBridge(){Clock(0);At(bridge.deck,bridge.WorldPosition);Check(mission.TryInteract(),"Bridge interaction failed");Check(mission.StartMission(bridge),"Bridge start failed");Close();mission.SendMessage("UpdateActiveMission",bridge.durationSeconds);Check(mission.GetMissionStatus(bridge)==ShipMissionStatus.Completed,"Bridge not completed");}
void StartStorm(){Clock(135);At(storm.deck,storm.WorldPosition);Check(mission.TryInteract(),"Storm card failed");var title=(UnityEngine.UI.Text)typeof(CrewMenu).GetField("missionCardTitle",flags).GetValue(menu);Check(title.text=="COMMUNICATIONS CONTROL","Technical mission metadata leaked");Check(mission.StartMission(storm),"Storm did not start");Close();}
await NewFlight();
var definitions=(ShipMissionDefinition[])typeof(ShipMission).GetField("missionDefinitions",flags).GetValue(mission);Check(definitions.Length==2&&!definitions.Any(x=>x.startsQuestionCampaign),"Unfinished question phases entered timed mission dispatcher");
Clock(134);Check(mission.GetMissionStatus(storm)==ShipMissionStatus.Unavailable,"Storm too early");
CompleteBridge();StartStorm();
Check(!mission.TryInteract()||mission.ActiveMission==storm,"Incorrect early completion");Close();
// Exercise the actual ladder route, rather than teleporting directly to the destination.
At(2,new UnityEngine.Vector2(ShipLayout.Ladders[1].x,ShipLayout.FloorY(2,ShipLayout.Ladders[1].x)));
for(int i=0;i<300;i++)mission.MoveCrew(0,-1,.02f);
mission.MoveCrew(1,0,.02f);
Check((int)typeof(ShipMission).GetField("deck",flags).GetValue(mission)==0,"Shelter route cannot exit on lower deck");
for(int i=0;i<17;i++)mission.MoveCrew(1,0,.02f);
Check(mission.IsNearActiveTarget,"Shelter target unreachable via ladder");
Check(mission.TryInteract()&&mission.GetMissionStatus(storm)==ShipMissionStatus.Completed&&mission.Health.Current==100,"Shelter completion damages health or fails");Check(typeof(ShipMission).GetField("targetMarker",flags).GetValue(mission)==null,"Target marker not cleaned up");
await NewFlight();CompleteBridge();StartStorm();mission.SendMessage("UpdateActiveMission",91f);
Check(mission.GetMissionStatus(storm)==ShipMissionStatus.Failed&&mission.Health.Current==75&&!mission.IsPaused,"Low-radiation timeout damage wrong");mission.SendMessage("UpdateActiveMission",91f);Check(mission.Health.Current==75,"Repeated timeout damage");
await NewFlight();CompleteBridge();Clock(231);mission.SendMessage("UpdateExpiredMissions");mission.SendMessage("UpdateExpiredMissions");Check(mission.GetMissionStatus(storm)==ShipMissionStatus.Expired&&mission.Health.Current==75,"Expiry damage duplicated or missing");
await NewFlight();At(bridge.deck,bridge.WorldPosition);Check(mission.SkipMission(bridge)&&mission.RadiationLevel==65&&mission.Health.Current==100,"Bridge radiation setup wrong");StartStorm();
float clock=(float)typeof(ShipMission).GetField("missionClock",flags).GetValue(mission);float remaining=mission.ActiveMissionSecondsRemaining;mission.SetPaused(true);mission.SendMessage("Update");Check(mission.ActiveMissionSecondsRemaining==remaining&&(float)typeof(ShipMission).GetField("missionClock",flags).GetValue(mission)==clock,"Pause advanced mission timers");mission.SetPaused(false);
mission.SendMessage("UpdateActiveMission",91f);Check(mission.Health.Current==0&&mission.IsPaused,"Critical exposure did not reduce HP to zero and pause");Check(((UnityEngine.GameObject)typeof(CrewMenu).GetField("gameOverScreen",flags).GetValue(menu)).activeInHierarchy,"Death screen missing");menu.SendMessage("TogglePause");Check(mission.IsPaused,"Escape bypassed game over");
await NewFlight();CompleteBridge();Clock(135);At(storm.deck,storm.WorldPosition);Check(mission.SkipMission(storm)&&mission.Health.Current==75,"Ignore-alert damage incorrect");Check(!mission.SkipMission(storm)&&mission.Health.Current==75,"Repeated skip causes damage");
await NewFlight();Clock(231);mission.SendMessage("UpdateExpiredMissions");Check(mission.Health.Current==0&&mission.RadiationLevel==65&&mission.IsPaused,"Skipped windows did not resolve chronologically into critical exposure");
await NewFlight();At(bridge.deck,bridge.WorldPosition);Check(mission.SkipMission(bridge),"Cannot skip initial check");Clock(135);At(storm.deck,storm.WorldPosition);Check(mission.TryInteract(),"Cannot open critical alert");menu.SendMessage("SkipSelectedMission");Check(mission.Health.Current==0&&mission.IsPaused&&!((UnityEngine.GameObject)typeof(CrewMenu).GetField("missionCard",flags).GetValue(menu)).activeSelf,"Ignore-alert UI bypassed game over");
await NewFlight();CompleteBridge();StartStorm();Clock(231);mission.SendMessage("UpdateExpiredMissions");Check(mission.ActiveMission==storm&&mission.Health.Current==100,"Start-window expiration interrupted running storm");
menu.SendMessage("ReturnToMenu");
return "PASS: two timed missions only, storm activation, neutral card, real ladder route and shelter interaction, success no damage, timeout/expiry/skip exactly once, critical-radiation death, pause, replay resets HP/radiation/overlays";
