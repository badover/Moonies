void Check(bool ok,string message){if(!ok)throw new System.Exception(message);}
void Capture(string name,bool full=false){
 var cam=UnityEngine.Camera.main;
 if(full){cam.orthographicSize=11.5f;cam.transform.position=new UnityEngine.Vector3(0,0,-10);}
 var rt=new UnityEngine.RenderTexture(1600,900,24);cam.targetTexture=rt;
 var sky=UnityEngine.GameObject.Find("Starfield").GetComponent<UnityEngine.SpriteRenderer>();
 sky.transform.position=new UnityEngine.Vector3(cam.transform.position.x,cam.transform.position.y,10);
 sky.transform.localScale=new UnityEngine.Vector3(2*cam.orthographicSize*cam.aspect/sky.sprite.bounds.size.x,2*cam.orthographicSize/sky.sprite.bounds.size.y,1);
 cam.Render();UnityEngine.RenderTexture.active=rt;
 var image=new UnityEngine.Texture2D(1600,900,UnityEngine.TextureFormat.RGB24,false);image.ReadPixels(new UnityEngine.Rect(0,0,1600,900),0,0);image.Apply();
 System.IO.File.WriteAllBytes(System.IO.Path.Combine(System.IO.Path.GetTempPath(),name),image.EncodeToPNG());cam.targetTexture=null;UnityEngine.RenderTexture.active=null;rt.Release();UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(image);
}
var menu=UnityEngine.Object.FindAnyObjectByType<CrewMenu>();
Check(menu!=null,"Menu missing");
var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
var results=new System.Collections.Generic.List<string>();
for(int role=0;role<4;role++){
 menu.SendMessage("Select",role);menu.SendMessage("Confirm");await System.Threading.Tasks.Task.Delay(150);
 var p=UnityEngine.Object.FindAnyObjectByType<CrewCharacter>();var mission=UnityEngine.Object.FindAnyObjectByType<ShipMission>();
 Check(p!=null && mission!=null && (int)p.Role==role,"Boarding failed "+role);
 var render=p.GetComponent<UnityEngine.SpriteRenderer>();var texture=render.sprite.texture;
 Check(render.sharedMaterial.shader.isSupported,"Shader unsupported");
 Check(texture.mipmapCount>1,"Character mipmaps absent");
 Check(UnityEngine.Mathf.Abs(p.transform.localScale.y-ShipLayout.CrewScale)<.001f,"Scale mismatch");
 Check(render.sprite.name.Contains("Idle"),"Idle frame missing");
 mission.MoveCrew(1,0,.1f);p.SendMessage("LateUpdate");Check(render.sprite.texture==texture && render.sprite.name.Contains("Walk"),"Walking changed model atlas");
 mission.MoveCrew(-1,0,10000);Check(p.transform.position.x>=ShipLayout.ToWorldX(220)+ShipLayout.BodyHalfWidth-.001,"Middle bow hull failed");
 mission.MoveCrew(1,0,10000);Check(p.transform.position.x<=ShipLayout.ToWorldX(2015)-ShipLayout.BodyHalfWidth+.001,"Middle stern hull failed");
 float ladder=ShipLayout.ToWorldX(1030);
 p.transform.position=new UnityEngine.Vector3(ladder,ShipLayout.FloorY(1,ladder),-1);
 mission.MoveCrew(0,1,.4f);Check(p.IsClimbing && render.sprite.texture==texture && render.sprite.name.Contains("Climb"),"Climb changed outfit");
 float y=p.transform.position.y;mission.MoveCrew(0,0,.5f);Check(UnityEngine.Mathf.Abs(p.transform.position.y-y)<.001,"Ladder pause failed");
 var held=render.sprite;p.SendMessage("LateUpdate");Check(render.sprite==held,"Paused climb animation changed");
 mission.MoveCrew(0,-1,.1f);Check(p.transform.position.y<y,"Ladder reversal failed");
 if(role==3){UnityEngine.Camera.main.orthographicSize=4.5f;UnityEngine.Camera.main.transform.position=new UnityEngine.Vector3(ladder,0,-10);Capture("modern-ladder-proof.png");}
 mission.MoveCrew(0,1,10);Check(!p.IsClimbing && render.sprite.texture==texture,"Upper landing changed model");
 Check(UnityEngine.Mathf.Abs(p.transform.position.y-ShipLayout.FloorY(2,p.transform.position.x))<.001,"Upper floor failed");
 mission.MoveCrew(1,0,10000);Check(p.transform.position.x<=ShipLayout.ToWorldX(1665)-ShipLayout.BodyHalfWidth+.001,"Propulsion barrier failed");
 p.transform.position=new UnityEngine.Vector3(ladder,ShipLayout.FloorY(2,ladder),-1);mission.MoveCrew(0,-1,10);mission.MoveCrew(0,0,0);p.transform.position=new UnityEngine.Vector3(ladder,ShipLayout.FloorY(1,ladder),-1);mission.MoveCrew(0,-1,10);
 Check(UnityEngine.Mathf.Abs(p.transform.position.y-ShipLayout.FloorY(0,p.transform.position.x))<.001,"Lower landing failed");
 mission.MoveCrew(-1,0,10000);Check(p.transform.position.x>=ShipLayout.ToWorldX(320)+ShipLayout.BodyHalfWidth-.001,"Lower bow failed");
 Check(UnityEngine.Mathf.Abs(p.transform.position.y-ShipLayout.FloorY(0,p.transform.position.x))<.001,"Feet left lower floor");
 mission.MoveCrew(1,0,10000);Check(p.transform.position.x<=ShipLayout.ToWorldX(1955)-ShipLayout.BodyHalfWidth+.001,"Lower stern failed");
 // The remote progress feature must actually fill a sprite mesh, reach its goal, and reset.
 var progress=(UnityEngine.UI.Image)typeof(CrewMenu).GetField("missionProgress",flags).GetValue(menu);
 Check(progress!=null && progress.sprite!=null && progress.gameObject.activeInHierarchy,"Progress not renderable");
 typeof(CrewMenu).GetField("missionElapsed",flags).SetValue(menu,300f);menu.SendMessage("Update");
 Check(UnityEngine.Mathf.Abs(progress.fillAmount-.5f)<.005,"50 percent progress failed");
 UnityEngine.Canvas.ForceUpdateCanvases();var mesh=progress.canvasRenderer.GetMesh();
 Check(mesh.vertexCount>0 && UnityEngine.Mathf.Abs(mesh.bounds.size.x/progress.rectTransform.rect.width-.5f)<.01f,"Progress renders full width instead of half");
 typeof(CrewMenu).GetField("missionElapsed",flags).SetValue(menu,600f);menu.SendMessage("Update");Check(progress.fillAmount==1,"Progress goal failed");
 var timer=(UnityEngine.UI.Text)typeof(CrewMenu).GetField("missionTime",flags).GetValue(menu);Check(timer.text.Contains("GOAL REACHED"),"Goal label absent");
 // Smooth zoom should converge without overshooting or allocating new sprites.
 var camera=UnityEngine.Camera.main;camera.orthographicSize=4.5f;typeof(ShipMission).GetField("zoom",flags).SetValue(mission,3f);
 float previous=camera.orthographicSize;
 for(int n=0;n<100;n++){mission.SendMessage("LateUpdate");Check(camera.orthographicSize<=previous+.001f && camera.orthographicSize>=2.999f,"Zoom overshoot");previous=camera.orthographicSize;}
 Check(UnityEngine.Mathf.Abs(camera.orthographicSize-3)<.03f,"Zoom convergence failed");
 if(role==3){Capture("modern-ship-proof.png",true);}
 results.Add(p.Role+": same atlas in Idle/Walk/Climb; central ladder; hull bounds; floor contact; propulsion barrier; 50/100% progress mesh; smooth zoom PASS");
 menu.SendMessage("ReturnToMenu");await System.Threading.Tasks.Task.Delay(150);
}
Check(UnityEngine.Mathf.Abs(ShipLayout.ZoomAfterScroll(5,1)-ShipLayout.ZoomAfterScroll(5,120))<.001,"Scroll backends differ");
return results;
