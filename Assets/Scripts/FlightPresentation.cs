using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>Persistent soundtrack and the successful-flight cinematic → credits → title flow.</summary>
public sealed class FlightPresentation : MonoBehaviour
{
    public enum Phase { Game, Cinematic, Credits }
    public Phase CurrentPhase {get;private set;}
    public bool MusicPlaying => music!=null && music.isPlaying;
    public VideoPlayer CinematicPlayer => cinematic;
    private AudioSource music,cinematicAudio;
    private VideoPlayer cinematic;
    private RenderTexture movieTexture;
    private GameObject overlay,cinematicScreen,creditsScreen;
    private Text loading;
    private Font font;
    private Action returnToTitle;
    private bool initialized,waitingForPlayback;
    private float playbackDeadline;

    public void Initialize(Action onReturnToTitle)
    {
        if(initialized)return;
        initialized=true;returnToTitle=onReturnToTitle;
        font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        music=gameObject.AddComponent<AudioSource>();music.playOnAwake=false;music.loop=true;
        music.spatialBlend=0;music.volume=.35f;music.ignoreListenerPause=true;
        music.clip=Resources.Load<AudioClip>("Audio/MissionMusic");
        if(music.clip==null)Debug.LogWarning("Mission soundtrack is missing.");
        var videoObject=new GameObject("Final Cinematic Player");videoObject.transform.SetParent(transform,false);
        cinematicAudio=videoObject.AddComponent<AudioSource>();cinematicAudio.playOnAwake=false;
        cinematicAudio.spatialBlend=0;cinematicAudio.volume=.85f;cinematicAudio.ignoreListenerPause=true;
        cinematic=videoObject.AddComponent<VideoPlayer>();cinematic.playOnAwake=false;cinematic.isLooping=false;
        cinematic.source=VideoSource.Url;cinematic.url=Path.Combine(Application.streamingAssetsPath,"FinalCinematic.webm");
        cinematic.renderMode=VideoRenderMode.RenderTexture;cinematic.aspectRatio=VideoAspectRatio.FitInside;
        // The flight is paused at completion; the movie and its audio must continue at normal speed.
        cinematic.timeUpdateMode=VideoTimeUpdateMode.UnscaledGameTime;
        cinematic.waitForFirstFrame=true;cinematic.skipOnDrop=true;
        cinematic.audioOutputMode=VideoAudioOutputMode.AudioSource;cinematic.controlledAudioTrackCount=1;
        cinematic.EnableAudioTrack(0,true);cinematic.SetTargetAudioSource(0,cinematicAudio);
        cinematic.prepareCompleted+=OnPrepared;cinematic.loopPointReached+=OnEnded;
        cinematic.errorReceived+=OnError;cinematic.started+=OnStarted;

        overlay=new GameObject("Flight Ending",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
        overlay.transform.SetParent(transform,false);
        var canvas=overlay.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=1000;
        var scaler=overlay.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution=new Vector2(1600,900);scaler.matchWidthOrHeight=.5f;
        var black=Node("Ending Backing",overlay.transform,Vector2.zero,Vector2.one).gameObject.AddComponent<Image>();black.color=Color.black;
        cinematicScreen=Node("Final Cinematic",overlay.transform,Vector2.zero,Vector2.one).gameObject;
        var movie=Node("Cinematic Image",cinematicScreen.transform,Vector2.zero,Vector2.one).gameObject.AddComponent<RawImage>();movie.raycastTarget=false;
        var fit=movie.gameObject.AddComponent<AspectRatioFitter>();fit.aspectMode=AspectRatioFitter.AspectMode.FitInParent;fit.aspectRatio=16f/9;
        loading=Label(cinematicScreen.transform,"PREPARING FLIGHT RECORD…",new Vector2(.2f,.45f),new Vector2(.8f,.55f),22);
        Button(cinematicScreen.transform,"SKIP CINEMATIC",new Vector2(.80f,.025f),new Vector2(.98f,.08f),ShowCredits);
        creditsScreen=Node("Credits Screen",overlay.transform,Vector2.zero,Vector2.one).gameObject;
        var artBounds=Node("Credits Artwork Bounds",creditsScreen.transform,new Vector2(.02f,.12f),new Vector2(.98f,.98f));
        var credits=Node("Credits Artwork",artBounds,Vector2.zero,Vector2.one).gameObject.AddComponent<RawImage>();
        credits.texture=Resources.Load<Texture2D>("Menu/Credits");credits.raycastTarget=false;
        var creditsFit=credits.gameObject.AddComponent<AspectRatioFitter>();creditsFit.aspectMode=AspectRatioFitter.AspectMode.FitInParent;
        creditsFit.aspectRatio=credits.texture!=null ? (float)credits.texture.width/credits.texture.height : 16f/9;
        Button(creditsScreen.transform,"RETURN TO MAIN MENU",new Vector2(.32f,.025f),new Vector2(.68f,.09f),ReturnToTitle);
        cinematicScreen.SetActive(false);creditsScreen.SetActive(false);overlay.SetActive(false);
        StartMusic();
    }
    public void PlayEnding()
    {
        if(!initialized || CurrentPhase!=Phase.Game)return;
        CurrentPhase=Phase.Cinematic;music.Stop();cinematicAudio.Stop();
        if(movieTexture==null){movieTexture=new RenderTexture(1920,1080,0,RenderTextureFormat.ARGB32);movieTexture.name="Final Cinematic Output";movieTexture.Create();}
        cinematic.targetTexture=movieTexture;cinematicScreen.GetComponentInChildren<RawImage>().texture=movieTexture;
        overlay.SetActive(true);creditsScreen.SetActive(false);cinematicScreen.SetActive(true);loading.gameObject.SetActive(true);
        waitingForPlayback=true;playbackDeadline=Time.realtimeSinceStartup+20;
        cinematic.Prepare();
    }
    private void Update()
    {
        if(CurrentPhase==Phase.Cinematic && waitingForPlayback && Time.realtimeSinceStartup>playbackDeadline)
        {Debug.LogWarning("Final cinematic did not start; showing credits.");ShowCredits();}
    }
    private void OnPrepared(VideoPlayer player){if(CurrentPhase==Phase.Cinematic)player.Play();}
    private void OnStarted(VideoPlayer player){waitingForPlayback=false;loading.gameObject.SetActive(false);}
    private void OnEnded(VideoPlayer player){if(CurrentPhase==Phase.Cinematic)ShowCredits();}
    private void OnError(VideoPlayer player,string error)
    {Debug.LogWarning("Final cinematic playback: "+error);if(CurrentPhase==Phase.Cinematic)ShowCredits();}
    public void ShowCredits()
    {
        if(CurrentPhase!=Phase.Cinematic)return;
        waitingForPlayback=false;cinematic.Stop();cinematicAudio.Stop();CurrentPhase=Phase.Credits;
        cinematicScreen.SetActive(false);creditsScreen.SetActive(true);StartMusic();
    }
    private void ReturnToTitle()
    {
        if(CurrentPhase!=Phase.Credits)return;
        ResetToGame();returnToTitle?.Invoke();
    }
    public void ResetToGame()
    {
        if(!initialized)return;
        waitingForPlayback=false;cinematic.Stop();cinematicAudio.Stop();CurrentPhase=Phase.Game;
        overlay.SetActive(false);cinematicScreen.SetActive(false);creditsScreen.SetActive(false);StartMusic();
    }
    private void StartMusic(){if(music!=null && music.clip!=null && !music.isPlaying)music.Play();}
    private void OnDestroy()
    {
        if(music!=null)music.Stop();
        if(cinematic!=null){cinematic.prepareCompleted-=OnPrepared;cinematic.loopPointReached-=OnEnded;cinematic.errorReceived-=OnError;cinematic.started-=OnStarted;cinematic.Stop();cinematic.targetTexture=null;}
        if(movieTexture!=null){movieTexture.Release();Destroy(movieTexture);}
    }
    private RectTransform Node(string name,Transform parent,Vector2 min,Vector2 max)
    {
        var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(parent,false);
        rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=rect.offsetMax=Vector2.zero;return rect;
    }
    private Text Label(Transform parent,string value,Vector2 min,Vector2 max,int size)
    {
        var text=Node(value,parent,min,max).gameObject.AddComponent<Text>();text.font=font;text.text=value;text.color=Color.white;
        text.fontSize=size;text.alignment=TextAnchor.MiddleCenter;text.raycastTarget=false;
        text.resizeTextForBestFit=true;text.resizeTextMinSize=12;text.resizeTextMaxSize=size;return text;
    }
    private void Button(Transform parent,string value,Vector2 min,Vector2 max,UnityEngine.Events.UnityAction callback)
    {
        var image=Node(value,parent,min,max).gameObject.AddComponent<Image>();image.color=new Color(.025f,.16f,.21f,.97f);
        var button=image.gameObject.AddComponent<Button>();button.transition=Selectable.Transition.None;button.onClick.AddListener(callback);
        Label(image.transform,value,new Vector2(.025f,.05f),new Vector2(.975f,.95f),20);
    }
}
