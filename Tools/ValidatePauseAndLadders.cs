void Check(bool ok,string text){if(!ok)throw new System.Exception(text);}
var menu=UnityEngine.Object.FindAnyObjectByType<CrewMenu>();
var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
var results=new System.Collections.Generic.List<string>();
if(UnityEngine.Object.FindAnyObjectByType<ShipMission>()!=null){menu.SendMessage("ReturnToMenu");await System.Threading.Tasks.Task.Delay(100);}
Check(!UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Text>().Any(x=>x.text.StartsWith("Selected:")),"Selection caption still visible");
for(int role=0;role<4;role++){
 menu.SendMessage("Select",role);menu.SendMessage("Confirm");await System.Threading.Tasks.Task.Delay(100);
 var mission=UnityEngine.Object.FindAnyObjectByType<ShipMission>();var player=UnityEngine.Object.FindAnyObjectByType<CrewCharacter>();
 Check(mission!=null&&player!=null&&(int)player.Role==role,"Boarding failed");
 float shaft=ShipLayout.ToWorldX(1030),middle=ShipLayout.FloorY(1,shaft),lower=ShipLayout.FloorY(0,shaft);
 player.transform.position=new UnityEngine.Vector3(shaft,middle,-1);
 mission.MoveCrew(0,-1,.4f);Check(player.IsClimbing,"Did not grab downward ladder");
 player.transform.position=new UnityEngine.Vector3(shaft,middle-.2f,-1);
 mission.MoveCrew(1,0,.02f);
 Check(!player.IsClimbing && player.transform.position.x>shaft+ShipLayout.BodyHalfWidth,"D did not step off near middle deck");
 Check(UnityEngine.Mathf.Abs(player.transform.position.y-middle)<.001,"Side exit did not land on floor");
 mission.MoveCrew(0,0,0);player.transform.position=new UnityEngine.Vector3(shaft,middle,-1);
 mission.MoveCrew(0,-1,10);Check(!player.IsClimbing,"Bottom landing failed");
 mission.MoveCrew(0,0,0);player.transform.position=new UnityEngine.Vector3(shaft,lower,-1);
 mission.MoveCrew(0,1,10);
 Check(!player.IsClimbing && UnityEngine.Mathf.Abs(player.transform.position.y-middle)<.001,"W did not step onto middle deck");
 var landing=player.transform.position;mission.MoveCrew(0,1,1);
 Check(!player.IsClimbing && player.transform.position==landing,"Held W re-grabbed next ladder");
 mission.MoveCrew(0,0,0);player.transform.position=new UnityEngine.Vector3(shaft,middle,-1);
 mission.MoveCrew(0,1,.5f);var inAir=player.transform.position;mission.MoveCrew(1,0,.01f);
 Check(player.IsClimbing && player.transform.position==inAir,"Can walk off into empty shaft");
 var renderer=player.GetComponent<UnityEngine.SpriteRenderer>();var sprite=renderer.sprite;var position=player.transform.position;
 var cameraPosition=UnityEngine.Camera.main.transform.position;float cameraZoom=UnityEngine.Camera.main.orthographicSize;
 float timer=(float)typeof(CrewMenu).GetField("missionElapsed",flags).GetValue(menu);
 var pauseHandler=(System.Action)typeof(ShipMission).GetField("onPauseRequested",flags).GetValue(mission);
 Check(pauseHandler.Method.Name=="TogglePause","Escape callback is still connected to crew selection");
 pauseHandler();
 Check(mission.IsPaused && UnityEngine.Time.timeScale==0,"Escape did not pause");
 Check(UnityEngine.GameObject.Find("Pause Screen")!=null && UnityEngine.GameObject.Find("Role Selection")==null,"Pause shows crew selection");
 mission.MoveCrew(1,1,1);mission.SendMessage("LateUpdate");player.SendMessage("LateUpdate");menu.SendMessage("Update");
 Check(player.transform.position==position && renderer.sprite==sprite,"Pause did not freeze player/animation");
 Check(UnityEngine.Camera.main.transform.position==cameraPosition && UnityEngine.Camera.main.orthographicSize==cameraZoom,"Camera moved while paused");
 Check((float)typeof(CrewMenu).GetField("missionElapsed",flags).GetValue(menu)==timer,"Timer advanced while paused");
 var resume=UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>().Single(x=>x.name=="RESUME");
 UnityEngine.EventSystems.ExecuteEvents.Execute(resume.gameObject,new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){button=UnityEngine.EventSystems.PointerEventData.InputButton.Left},UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler);
 Check(!mission.IsPaused && UnityEngine.Time.timeScale==1,"Resume button did not resume");
 Check(UnityEngine.Object.FindAnyObjectByType<CrewCharacter>()==player && (int)player.Role==role,"Pause replaced crew");
 mission.MoveCrew(0,1,.1f);Check(player.transform.position.y>position.y,"Climbing did not continue after resume");
 var tint=renderer.sharedMaterial.GetColor("_Color");Check(tint.r>=1 && tint.g>1 && tint.b>1,"Character brightness not raised");
 results.Add(player.Role+": W/D ladder exit, no re-grab, no air exit, pause handler, frozen timer/camera/animation, RESUME and same role PASS");
 menu.SendMessage("ReturnToMenu");await System.Threading.Tasks.Task.Delay(100);
}
return results;
