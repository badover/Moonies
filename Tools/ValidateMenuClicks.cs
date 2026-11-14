void Check(bool ok,string text){if(!ok)throw new System.Exception(text);}
UnityEngine.Application.runInBackground=true;
var camera=UnityEngine.Camera.main;
var target=new UnityEngine.RenderTexture(1600,900,24);camera.targetTexture=target;
var canvases=UnityEngine.Object.FindObjectsByType<UnityEngine.Canvas>(UnityEngine.FindObjectsInactive.Include);
foreach(var canvas in canvases){canvas.renderMode=UnityEngine.RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=canvas.sortingOrder<0?3:1;}
void Render(){UnityEngine.Canvas.ForceUpdateCanvases();camera.Render();}
void Click(string name){
 Render();var button=UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>().FirstOrDefault(x=>x.name==name);
 Check(button!=null&&button.IsInteractable(),"Missing/disabled button "+name);var go=button.gameObject;var rect=go.GetComponent<UnityEngine.RectTransform>();
 var data=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){position=UnityEngine.RectTransformUtility.WorldToScreenPoint(camera,rect.TransformPoint(rect.rect.center)),button=UnityEngine.EventSystems.PointerEventData.InputButton.Left};
 var hits=new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();UnityEngine.EventSystems.EventSystem.current.RaycastAll(data,hits);
 Check(hits.Count>0 && hits[0].gameObject==go,"Wrong raycast hit "+name+": "+(hits.Count>0?hits[0].gameObject.name:"nothing"));
 UnityEngine.EventSystems.ExecuteEvents.Execute(go,data,UnityEngine.EventSystems.ExecuteEvents.pointerDownHandler);
 UnityEngine.EventSystems.ExecuteEvents.Execute(go,data,UnityEngine.EventSystems.ExecuteEvents.pointerUpHandler);
 UnityEngine.EventSystems.ExecuteEvents.Execute(go,data,UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler);
}
try{
 if(!UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>().Any(x=>x.name=="START"))Click("BACK");
 Click("START");Check(UnityEngine.GameObject.Find("After Start Artwork")!=null,"Role screen absent");
 foreach(string role in new[]{"Pilot","Engineer","Commander","Medic"})Click("Choose "+role);
 Render();UnityEngine.RenderTexture.active=target;var picture=new UnityEngine.Texture2D(1600,900,UnityEngine.TextureFormat.RGB24,false);picture.ReadPixels(new UnityEngine.Rect(0,0,1600,900),0,0);picture.Apply();System.IO.File.WriteAllBytes(System.IO.Path.Combine(System.IO.Path.GetTempPath(),"after-start-proof.png"),picture.EncodeToPNG());UnityEngine.Object.Destroy(picture);
 Click("BACK");Click("START");Click("BOARD SHIP");
 Check(UnityEngine.Object.FindAnyObjectByType<ShipMission>()!=null,"Board button did not start mission");
 UnityEngine.Object.FindAnyObjectByType<CrewMenu>().SendMessage("ReturnToMenu");
 await System.Threading.Tasks.Task.Delay(100);
 return "PASS: START/BACK/START BOARD SHIP and all four role buttons have correct rendered raycast targets and pointer click handlers at 1600x900";
}finally{camera.targetTexture=null;UnityEngine.RenderTexture.active=null;foreach(var canvas in canvases){if(canvas!=null){canvas.renderMode=UnityEngine.RenderMode.ScreenSpaceOverlay;canvas.worldCamera=null;}}target.Release();UnityEngine.Object.Destroy(target);}
