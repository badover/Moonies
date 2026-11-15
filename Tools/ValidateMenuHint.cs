void Check(bool ok,string message){if(!ok)throw new System.Exception(message);}
var menu=UnityEngine.Object.FindAnyObjectByType<CrewMenu>();
var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
var camera=UnityEngine.Camera.main;var target=new UnityEngine.RenderTexture(1600,900,24);camera.targetTexture=target;
var canvases=UnityEngine.Object.FindObjectsByType<UnityEngine.Canvas>(UnityEngine.FindObjectsInactive.Include);
foreach(var canvas in canvases){canvas.renderMode=UnityEngine.RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=canvas.sortingOrder<0?3:1;}
void Render(){UnityEngine.Canvas.ForceUpdateCanvases();camera.Render();}
void Capture(string name){Render();UnityEngine.RenderTexture.active=target;var image=new UnityEngine.Texture2D(1600,900,UnityEngine.TextureFormat.RGB24,false);image.ReadPixels(new UnityEngine.Rect(0,0,1600,900),0,0);image.Apply();System.IO.File.WriteAllBytes(System.IO.Path.Combine(System.IO.Path.GetTempPath(),name),image.EncodeToPNG());UnityEngine.Object.Destroy(image);}
void Click(string name,bool invisible=false){
 Render();var button=UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>().Single(x=>x.name==name);var graphic=button.GetComponent<UnityEngine.UI.Image>();
 if(invisible)Check(button.transition==UnityEngine.UI.Selectable.Transition.None&&graphic.color.a==0,"Visible tint: "+name);
 var rect=button.GetComponent<UnityEngine.RectTransform>();var data=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){position=UnityEngine.RectTransformUtility.WorldToScreenPoint(camera,rect.TransformPoint(rect.rect.center)),button=UnityEngine.EventSystems.PointerEventData.InputButton.Left};
 var hits=new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();UnityEngine.EventSystems.EventSystem.current.RaycastAll(data,hits);Check(hits.Count>0&&hits[0].gameObject==button.gameObject,"Incorrect click target: "+name);
 UnityEngine.EventSystems.ExecuteEvents.Execute(button.gameObject,data,UnityEngine.EventSystems.ExecuteEvents.pointerEnterHandler);
 UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(button.gameObject);
 UnityEngine.EventSystems.ExecuteEvents.Execute(button.gameObject,data,UnityEngine.EventSystems.ExecuteEvents.pointerDownHandler);
 if(invisible)Check(graphic.color.a==0,"Hover/focus/press adds tint: "+name);
 UnityEngine.EventSystems.ExecuteEvents.Execute(button.gameObject,data,UnityEngine.EventSystems.ExecuteEvents.pointerUpHandler);
 UnityEngine.EventSystems.ExecuteEvents.Execute(button.gameObject,data,UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler);
 if(invisible)Check(graphic.color.a==0,"Selected state adds tint: "+name);
}
try{
 Capture("start-no-tint.png");Click("START",true);
 foreach(string role in new[]{"Pilot","Engineer","Commander","Medic"})Click("Choose "+role,true);
 Capture("roles-no-tint.png");Click("BOARD SHIP");
 var mission=UnityEngine.Object.FindAnyObjectByType<ShipMission>();Check(mission!=null,"Boarding failed");
 var suggestion=(UnityEngine.GameObject)typeof(CrewMenu).GetField("bridgeSuggestion",flags).GetValue(menu);
 Check(suggestion.activeInHierarchy,"Bridge suggestion absent while check is available");
 Check(suggestion.GetComponent<UnityEngine.UI.Image>().color.a==1,"Suggestion border is not opaque");
 var backing=suggestion.transform.Find("Opaque Hint Backing").GetComponent<UnityEngine.UI.Image>();Check(backing.color.a==1,"Hint backing is not opaque");
 var text=suggestion.GetComponentInChildren<UnityEngine.UI.Text>();Check(text.text=="Maybe it's a good idea to check the Bridge."&&text.color.r>text.color.b&&text.fontSize<=16,"Hint wording/color/size incorrect");
 Capture("bridge-suggestion.png");
 var definition=UnityEngine.Resources.Load<ShipMissionDefinition>("Missions/G01_LEO_Check");
 Check(!definition.briefing.Contains("fault")&&!definition.briefing.Contains("trouble"),"Briefing still threatens consequences");
 var player=UnityEngine.Object.FindAnyObjectByType<CrewCharacter>();player.transform.position=new UnityEngine.Vector3(definition.WorldPosition.x,definition.WorldPosition.y,-1);typeof(ShipMission).GetField("deck",flags).SetValue(mission,definition.deck);mission.SendMessage("UpdateNearbyMission");
 menu.SendMessage("OpenMissionCard",definition);Check(!suggestion.activeSelf,"Suggestion overlaps mission card");
 menu.SendMessage("SkipSelectedMission");
 var status=(UnityEngine.UI.Text)typeof(CrewMenu).GetField("missionCardStatus",flags).GetValue(menu);Check(status.text=="CHECK SKIPPED","Skip status still warns about consequences");
 menu.SendMessage("CloseMissionCard");menu.SendMessage("Update");Check(!suggestion.activeSelf,"Suggestion remains after skipped check");
 menu.SendMessage("ReturnToMenu");await System.Threading.Tasks.Task.Delay(100);
 menu.SendMessage("Confirm");mission=UnityEngine.Object.FindAnyObjectByType<ShipMission>();player=UnityEngine.Object.FindAnyObjectByType<CrewCharacter>();
 player.transform.position=new UnityEngine.Vector3(definition.WorldPosition.x,definition.WorldPosition.y,-1);typeof(ShipMission).GetField("deck",flags).SetValue(mission,definition.deck);mission.SendMessage("UpdateNearbyMission");
 menu.SendMessage("OpenMissionCard",definition);menu.SendMessage("StartSelectedMission");mission.SendMessage("UpdateActiveMission",definition.durationSeconds);menu.SendMessage("CloseMissionCard");menu.SendMessage("Update");
 Check(mission.GetMissionStatus(definition)==ShipMissionStatus.Completed&&!suggestion.activeSelf,"Suggestion remains after completion");
 return "PASS: START and four roles remain clickable with no hover/focus/pressed/selected blue tint; opaque yellow Bridge hint; neutral briefing/skip; hint hides for card, skip and completion";
}finally{
 camera.targetTexture=null;UnityEngine.RenderTexture.active=null;foreach(var canvas in canvases){if(canvas!=null){canvas.renderMode=UnityEngine.RenderMode.ScreenSpaceOverlay;canvas.worldCamera=null;}}target.Release();UnityEngine.Object.Destroy(target);
}
