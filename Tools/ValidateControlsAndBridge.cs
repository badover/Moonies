void Check(bool ok,string message){if(!ok)throw new System.Exception(message);}
var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
var menu=UnityEngine.Object.FindAnyObjectByType<CrewMenu>();
float Remaining()=>(float)typeof(CrewMenu).GetField("controlsIntroRemaining",flags).GetValue(menu);
UnityEngine.GameObject Intro()=>(UnityEngine.GameObject)typeof(CrewMenu).GetField("controlsIntro",flags).GetValue(menu);
void Click(string name){var button=UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>().Single(x=>x.name==name);UnityEngine.EventSystems.ExecuteEvents.Execute(button.gameObject,new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){button=UnityEngine.EventSystems.PointerEventData.InputButton.Left},UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler);}
if(UnityEngine.Object.FindAnyObjectByType<ShipMission>()!=null){menu.SendMessage("ReturnToMenu");await System.Threading.Tasks.Task.Delay(100);}
menu.SendMessage("Select",2);menu.SendMessage("Confirm");await System.Threading.Tasks.Task.Delay(100);
var mission=UnityEngine.Object.FindAnyObjectByType<ShipMission>();Check(mission!=null,"Boarding failed");
Check(Intro().activeInHierarchy,"Intro controls not visible at boarding");
var introText=Intro().GetComponentsInChildren<UnityEngine.UI.Text>().Single(x=>x.text.Contains("Mouse wheel"));
Check(introText.text.Contains("E  —  Interact") && introText.text.Contains("Leave ladder"),"Controls incomplete");
Check(!UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Text>().Any(x=>x.text.Contains("A / D  MOVE")),"Permanent controls strip still visible");
menu.SendMessage("UpdateControlsIntro",3f);float remaining=Remaining();
menu.SendMessage("TogglePause");Check(mission.IsPaused&&!Intro().activeSelf,"Intro not hidden by pause");
var pause=UnityEngine.GameObject.Find("Pause Screen");Check(pause.GetComponentsInChildren<UnityEngine.UI.Text>().Any(x=>x.text==introText.text),"Pause controls differ from intro");
menu.SendMessage("UpdateControlsIntro",100f);Check(Remaining()==remaining,"Intro timer advanced in pause");
Click("RESUME");Check(!mission.IsPaused&&Intro().activeSelf&&Remaining()<=remaining,"Resume resets intro timer");
menu.SendMessage("UpdateControlsIntro",20f);Check(!Intro().activeSelf&&Remaining()==0,"Intro failed to expire");
menu.SendMessage("TogglePause");Check(pause.GetComponentsInChildren<UnityEngine.UI.Text>().Any(x=>x.text==introText.text),"Controls not available after intro expired");Click("RESUME");Check(!Intro().activeSelf,"Expired intro reappeared");
var definition=UnityEngine.Resources.Load<ShipMissionDefinition>("Missions/G01_LEO_Check");var player=UnityEngine.Object.FindAnyObjectByType<CrewCharacter>();
player.transform.position=new UnityEngine.Vector3(definition.WorldPosition.x,definition.WorldPosition.y,-1);typeof(ShipMission).GetField("deck",flags).SetValue(mission,definition.deck);
var card=(UnityEngine.GameObject)typeof(CrewMenu).GetField("missionCard",flags).GetValue(menu);
for(int i=0;i<3;i++){Check(mission.TryInteract()&&card.activeSelf&&mission.IsPaused,"Cannot reopen available Bridge check");Click("CLOSE");Check(!card.activeSelf&&!mission.IsPaused&&UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject==null,"CLOSE does not restore input");}
typeof(ShipMission).GetField("missionClock",flags).SetValue(mission,definition.availableUntilSeconds+1);
Check(mission.TryInteract()&&card.activeSelf,"Cannot review Bridge after start window");
var start=(UnityEngine.UI.Text)typeof(CrewMenu).GetField("missionStartButton",flags).GetValue(menu);Check(!start.transform.parent.gameObject.activeSelf,"Expired check can be started");Click("CLOSE");Check(mission.TryInteract(),"Expired card only opens once");
// Escape from a station dialog closes that dialog rather than leaving an invisible input-blocking shade.
menu.SendMessage("TogglePause");Check(!card.activeSelf&&!mission.IsPaused,"Escape does not close station card");
typeof(ShipMission).GetField("missionClock",flags).SetValue(mission,0f);Check(mission.TryInteract(),"Available card lost after reviewing expiration");
Click("START CHECK");Check(mission.ActiveMission==definition,"Check did not start");Click("CLOSE");Check(mission.TryInteract()&&mission.ActiveMission==definition,"Cannot reopen running check");Click("CLOSE");
mission.SendMessage("UpdateActiveMission",definition.durationSeconds);Check(mission.GetMissionStatus(definition)==ShipMissionStatus.Completed,"Check did not complete");Check(mission.TryInteract()&&card.activeSelf,"Completed check cannot be reviewed");Click("CLOSE");
return "PASS: 10-second intro, pause freezes countdown, identical controls always in pause, expired intro stays hidden; Bridge CLOSE/reopen repeated, available/running/completed/expired review and Escape close preserve input";
