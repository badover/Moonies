void Check(bool ok,string text){if(!ok)throw new System.Exception(text);}
UnityEngine.Application.runInBackground=true;
var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
var menu=UnityEngine.Object.FindAnyObjectByType<CrewMenu>();var presentation=menu.GetComponent<FlightPresentation>();
var music=(UnityEngine.AudioSource)typeof(FlightPresentation).GetField("music",flags).GetValue(presentation);
var cinematicAudio=(UnityEngine.AudioSource)typeof(FlightPresentation).GetField("cinematicAudio",flags).GetValue(presentation);
Check(music.clip!=null&&music.loop&&music.spatialBlend==0&&music.clip.loadType==UnityEngine.AudioClipLoadType.Streaming,"Music is missing, not looping, spatial or fully decoded");
Check(presentation.MusicPlaying,"Music does not play on title screen");
music.time=music.clip.length-.2f;await System.Threading.Tasks.Task.Delay(650);Check(music.isPlaying&&music.time<2,"Soundtrack does not loop at the end");
ShipMission mission=null;
async System.Threading.Tasks.Task NewFlight(){if(UnityEngine.Object.FindAnyObjectByType<ShipMission>()!=null){menu.SendMessage("ReturnToMenu");await System.Threading.Tasks.Task.Delay(100);}menu.SendMessage("Confirm");await System.Threading.Tasks.Task.Delay(100);mission=UnityEngine.Object.FindAnyObjectByType<ShipMission>();mission.enabled=false;Check(presentation.MusicPlaying&&presentation.CurrentPhase==FlightPresentation.Phase.Game,"Music/presentation not reset when boarding");}
void CompletePlan(){var statuses=(System.Collections.IDictionary)typeof(ShipMission).GetField("missionStatuses",flags).GetValue(mission);foreach(var e in mission.RunPlan)statuses[e.definition]=ShipMissionStatus.Completed;}
void Finish(){typeof(ShipMission).GetField("missionClock",flags).SetValue(mission,600f);mission.SendMessage("FinishRun");}
await NewFlight();mission.SetPaused(true);Check(presentation.MusicPlaying,"Pause stops soundtrack");mission.SetPaused(false);
CompletePlan();Check(!mission.RunSucceeded&&presentation.CurrentPhase==FlightPresentation.Phase.Game,"Ending starts before timer fills");
// Recovery must still show the ending when optional work was missed.
((System.Collections.IDictionary)typeof(ShipMission).GetField("missionStatuses",flags).GetValue(mission))[mission.RunPlan.Last().definition]=ShipMissionStatus.Skipped;
Finish();Check(mission.RunSucceeded&&presentation.CurrentPhase==FlightPresentation.Phase.Cinematic&&!presentation.MusicPlaying,"A missed side task blocks the recovery cinematic");
await NewFlight();mission.Health.TakeDamage(100);Check(presentation.CurrentPhase==FlightPresentation.Phase.Game&&presentation.MusicPlaying,"Death starts victory cinematic or disables soundtrack");
await NewFlight();CompletePlan();Finish();
Check(mission.RunSucceeded&&UnityEngine.Time.timeScale==0&&presentation.CurrentPhase==FlightPresentation.Phase.Cinematic&&!presentation.MusicPlaying,"Successful full progress does not start exclusive cinematic");
var progress=(UnityEngine.UI.Image)typeof(CrewMenu).GetField("missionProgress",flags).GetValue(menu);Check(progress.fillAmount==1,"Progress bar not filled at cinematic start");
var movie=presentation.CinematicPlayer;Check(!movie.isLooping&&movie.timeUpdateMode==UnityEngine.Video.VideoTimeUpdateMode.UnscaledGameTime&&movie.audioOutputMode==UnityEngine.Video.VideoAudioOutputMode.AudioSource,"Movie loops, freezes at timescale zero or has no routed audio");
float deadline=UnityEngine.Time.realtimeSinceStartup+30;long lastFrame=-1;bool decoded=false;
while(presentation.CurrentPhase==FlightPresentation.Phase.Cinematic && UnityEngine.Time.realtimeSinceStartup<deadline){Check(!presentation.MusicPlaying,"Music overlaps cinematic");if(movie.frame>3){decoded=true;lastFrame=movie.frame;Check(movie.GetTargetAudioSource(0)==cinematicAudio&&movie.IsAudioTrackEnabled(0),"Cinematic audio is not enabled/routed");}await System.Threading.Tasks.Task.Delay(250);}
Check(decoded&&lastFrame>30,"Actual movie frames were not decoded");
Check(presentation.CurrentPhase==FlightPresentation.Phase.Credits&&presentation.MusicPlaying&&!movie.isPlaying&&!cinematicAudio.isPlaying,"Natural movie end does not open Credits / resume music exclusively");
var camera=UnityEngine.Camera.main;var target=new UnityEngine.RenderTexture(1600,900,24);camera.targetTexture=target;var canvases=UnityEngine.Object.FindObjectsByType<UnityEngine.Canvas>(UnityEngine.FindObjectsInactive.Include);var orders=canvases.Select(x=>x.sortingOrder).ToArray();
foreach(var c in canvases){if(c.sortingOrder>=0)c.sortingOrder+=100;c.renderMode=UnityEngine.RenderMode.ScreenSpaceCamera;c.worldCamera=camera;c.planeDistance=c.sortingOrder<0?3:1;}
void Render(){UnityEngine.Canvas.ForceUpdateCanvases();camera.Render();}
void Click(string name){Render();var b=UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>().Single(x=>x.name==name);var r=b.GetComponent<UnityEngine.RectTransform>();var data=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){position=UnityEngine.RectTransformUtility.WorldToScreenPoint(camera,r.TransformPoint(r.rect.center)),button=UnityEngine.EventSystems.PointerEventData.InputButton.Left};var hits=new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();UnityEngine.EventSystems.EventSystem.current.RaycastAll(data,hits);Check(hits.Count>0&&hits[0].gameObject==b.gameObject,"Blocked ending button: "+name);UnityEngine.EventSystems.ExecuteEvents.Execute(b.gameObject,data,UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler);}
try{
 Render();UnityEngine.RenderTexture.active=target;var image=new UnityEngine.Texture2D(1600,900,UnityEngine.TextureFormat.RGB24,false);image.ReadPixels(new UnityEngine.Rect(0,0,1600,900),0,0);image.Apply();System.IO.File.WriteAllBytes("/home/tim/Earth_game/Artifacts/credits.png",image.EncodeToPNG());UnityEngine.Object.Destroy(image);
 Click("RETURN TO MAIN MENU");await System.Threading.Tasks.Task.Delay(100);Check(UnityEngine.GameObject.Find("Start Screen")!=null&&UnityEngine.GameObject.Find("Role Selection")==null&&UnityEngine.Object.FindAnyObjectByType<ShipMission>()==null&&UnityEngine.Time.timeScale==1&&presentation.MusicPlaying,"Credits do not return to actual title / restore music and time scale");
 await NewFlight();CompletePlan();Finish();Click("SKIP CINEMATIC");Check(presentation.CurrentPhase==FlightPresentation.Phase.Credits&&presentation.MusicPlaying&&!movie.isPlaying,"Skip does not cleanly transition to Credits");Click("RETURN TO MAIN MENU");await System.Threading.Tasks.Task.Delay(100);
 await NewFlight();CompletePlan();Finish();typeof(FlightPresentation).GetMethod("OnError",flags).Invoke(presentation,new object[]{movie,"Validation fallback"});Check(presentation.CurrentPhase==FlightPresentation.Phase.Credits&&presentation.MusicPlaying,"Decoder failure leaves a dead-end screen");Click("RETURN TO MAIN MENU");
 return "PASS: streamed music plays in title/game/pause and actually loops; victory requires mandatory checkout + alive + full 600s; missed side tasks still show ending, dead flights excluded; real 17s video decodes and finishes at timeScale zero with music off and its audio routed; natural Credits, music resume, rendered main-menu/skip clicks, replay and error fallback";
}finally{camera.targetTexture=null;UnityEngine.RenderTexture.active=null;for(int i=0;i<canvases.Length;i++){if(canvases[i]!=null){canvases[i].renderMode=UnityEngine.RenderMode.ScreenSpaceOverlay;canvases[i].worldCamera=null;canvases[i].sortingOrder=orders[i];}}target.Release();UnityEngine.Object.Destroy(target);}
