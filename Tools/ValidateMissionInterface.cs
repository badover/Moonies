void Check(bool ok,string message){if(!ok)throw new System.Exception(message);}
var menu=UnityEngine.Object.FindAnyObjectByType<CrewMenu>();var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
if(UnityEngine.Object.FindAnyObjectByType<ShipMission>()!=null){menu.SendMessage("ReturnToMenu");await System.Threading.Tasks.Task.Delay(80);}
menu.SendMessage("Confirm");await System.Threading.Tasks.Task.Delay(100);
var mission=UnityEngine.Object.FindAnyObjectByType<ShipMission>();mission.enabled=false;
var catalog=UnityEngine.Resources.LoadAll<ShipMissionDefinition>("Missions").Where(x=>!x.startsQuestionCampaign).ToArray();
MissionRunPlan.Entry[] plan=null;
for(int seed=0;seed<2000;seed++){var draw=MissionRunPlan.Create(catalog,seed);if(draw[0].definition.missionId=="G-01"){plan=draw;break;}}
Check(plan!=null,"No draw contains initial checkout");
typeof(ShipMission).GetField("runPlan",flags).SetValue(mission,plan);typeof(ShipMission).GetField("missionDefinitions",flags).SetValue(mission,plan.Select(x=>x.definition).ToArray());typeof(ShipMission).GetField("missionClock",flags).SetValue(mission,plan[0].trigger+.01f);
menu.SendMessage("UpdateControlsIntro",20f);menu.SendMessage("Update");
var camera=UnityEngine.Camera.main;var target=new UnityEngine.RenderTexture(1600,900,24);camera.targetTexture=target;
var canvases=UnityEngine.Object.FindObjectsByType<UnityEngine.Canvas>(UnityEngine.FindObjectsInactive.Include);
var originalOrders=canvases.Select(x=>x.sortingOrder).ToArray();
foreach(var canvas in canvases){if(canvas.sortingOrder>=0)canvas.sortingOrder+=100;canvas.renderMode=UnityEngine.RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=canvas.sortingOrder<0?3:1;}
void Render(){UnityEngine.Canvas.ForceUpdateCanvases();camera.Render();}
void Capture(string name){Render();UnityEngine.RenderTexture.active=target;var texture=new UnityEngine.Texture2D(1600,900,UnityEngine.TextureFormat.RGB24,false);texture.ReadPixels(new UnityEngine.Rect(0,0,1600,900),0,0);texture.Apply();System.IO.File.WriteAllBytes("/home/tim/Earth_game/Artifacts/"+name+".png",texture.EncodeToPNG());UnityEngine.Object.Destroy(texture);}
void Click(string name){
 Render();var b=UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>().Single(x=>x.name==name);Check(b.IsInteractable(),"Button disabled: "+name);
 var r=b.GetComponent<UnityEngine.RectTransform>();var data=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){position=UnityEngine.RectTransformUtility.WorldToScreenPoint(camera,r.TransformPoint(r.rect.center)),button=UnityEngine.EventSystems.PointerEventData.InputButton.Left};
 var hits=new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();UnityEngine.EventSystems.EventSystem.current.RaycastAll(data,hits);Check(hits.Count>0&&hits[0].gameObject==b.gameObject,"Button obstructed: "+name+" by "+(hits.Count>0?hits[0].gameObject.name:"nothing"));
 UnityEngine.EventSystems.ExecuteEvents.Execute(b.gameObject,data,UnityEngine.EventSystems.ExecuteEvents.pointerDownHandler);UnityEngine.EventSystems.ExecuteEvents.Execute(b.gameObject,data,UnityEngine.EventSystems.ExecuteEvents.pointerUpHandler);UnityEngine.EventSystems.ExecuteEvents.Execute(b.gameObject,data,UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler);
}
try{
 Click("MISSION TASKS");Check(mission.IsInterfaceOpen&&!mission.IsPaused,"Board stops simulation");
 var rows=(UnityEngine.UI.Text[])typeof(CrewMenu).GetField("taskRows",flags).GetValue(menu);
 Check(rows.Count(x=>x.transform.parent.gameObject.activeSelf)==plan.Length,"Board omits run tasks");
 for(int i=1;i<plan.Length;i++)Check(rows[i].text=="Awaiting mission phase"&&!rows[i].transform.parent.GetComponent<UnityEngine.UI.Button>().interactable,"Future task leaked before its phase");
 Capture("mission-task-board");Click("MISSION ROW 0");
 Check(((UnityEngine.UI.Text)typeof(CrewMenu).GetField("missionCardTitle",flags).GetValue(menu)).text=="BRIDGE CONTROL","Technical mission header returned");
 Click("START CHECK");Check(mission.ActiveMission==plan[0].definition&&!mission.IsPaused,"Start Scan click fails");mission.SendMessage("UpdateActiveMission",2f);Click("CLOSE");
 menu.SendMessage("UpdateMissionHint");var backdrop=(UnityEngine.GameObject)typeof(CrewMenu).GetField("missionHintBackdrop",flags).GetValue(menu);
 Check(backdrop.activeInHierarchy&&backdrop.GetComponent<UnityEngine.UI.Image>().color.a==1,"Closed scan has no readable progress HUD");
 Check(((UnityEngine.UI.Text)typeof(CrewMenu).GetField("missionHint",flags).GetValue(menu)).text.Contains("SCANNING"),"Scan state missing from HUD");Capture("scan-after-close");
 mission.SendMessage("UpdateActiveMission",6f);Click("MISSION TASKS");Click("MISSION ROW 0");
 Check(mission.ProcedureStepIndex==1,"Review resets scan progress");Capture("mission-procedure");
 int correct=mission.CurrentStep.correctChoice;Click("PROCEDURE OPTION "+((correct+1)%3));Check(mission.ProcedureStepIndex==1,"Unsafe button advances mission");Click("PROCEDURE OPTION "+correct);Check(mission.ProcedureStepIndex==2,"Correct button not connected");
 Click("PROCEDURE OPTION "+mission.CurrentStep.correctChoice);Click("CLOSE");mission.SendMessage("UpdateActiveMission",10f);menu.SendMessage("UpdateMissionHint");
 Check(((UnityEngine.UI.Text)typeof(CrewMenu).GetField("missionHint",flags).GetValue(menu)).text.Contains("ACTION REQUIRED"),"Finished scan fails to ask for confirmation");
 Click("MISSION TASKS");Click("MISSION ROW 0");Click("PROCEDURE OPTION "+mission.CurrentStep.correctChoice);Check(mission.GetMissionStatus(plan[0].definition)==ShipMissionStatus.Completed,"Final confirmation click fails");Click("CLOSE");
 return "PASS: rendered raycasts for Mission Tasks, task row, Start Scan, Close and all answer buttons; future tasks hidden; live closed-scan HUD; reviewing retains progress; unsafe commands rejected; departure scan and final confirmation; three 1600x900 captures";
}finally{
 camera.targetTexture=null;UnityEngine.RenderTexture.active=null;for(int i=0;i<canvases.Length;i++){var canvas=canvases[i];if(canvas!=null){canvas.renderMode=UnityEngine.RenderMode.ScreenSpaceOverlay;canvas.worldCamera=null;canvas.sortingOrder=originalOrders[i];}}target.Release();UnityEngine.Object.Destroy(target);menu.SendMessage("ReturnToMenu");
}
