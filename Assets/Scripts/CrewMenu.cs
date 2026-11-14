using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Video;
using System.IO;

/// <summary>Two-screen entry flow with persistent crew selection and ship boarding.</summary>
public sealed class CrewMenu : MonoBehaviour
{
    public static CrewRole SelectedRole => (CrewRole)Mathf.Clamp(PlayerPrefs.GetInt("CrewRole", 0), 0, 3);
    private readonly string[] names = { "Commander", "Medic", "Pilot", "Engineer" };
    private readonly string[] descriptions = { "Coordinate your crew\nand lead the mission.", "Monitor crew health\nand provide medical care.", "Control the flight\nand navigate to the Moon.", "Maintain ship systems\nand repair equipment." };
    private readonly string[] assets = { "Commander", "Medic", "Pilot", "Engineer" };
    private readonly Color cyan = new Color(.25f, .86f, .94f);
    private GameObject home, roles, missionHud, pauseScreen;
    private ShipMission mission;
    private Canvas backgroundCanvas;
    private Image shade;
    private RawImage stars;
    private VideoPlayer backgroundVideo;
    private RenderTexture backgroundVideoTexture;
    private Image[] cards = new Image[4];
    private Image missionProgress;
    private Text status, confirmText, missionTime;
    private Text missionHint, missionCardTitle, missionCardMeta, missionCardLocation, missionCardBriefing, missionCardStatus;
    private Text missionStartButton, missionSkipButton, missionCloseButton;
    private GameObject missionCardShade, missionCard, missionTaskProgressTrack;
    private Image missionTaskProgress;
    private ShipMissionDefinition selectedMission;
    private Font font;
    private int selected;
    private float missionElapsed;
    private const float MissionGoalSeconds = 10 * 60f;
    private static readonly Color MissionProgressGold = new Color(1f, .72f, .18f);
    private int lastMissionSecond=-1;
    private Sprite progressSprite;

    private void Start()
    {
        QualitySettings.vSyncCount = 1;
        Application.targetFrameRate = 60;
        selected = (int)SelectedRole;
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            var events = new GameObject("Menu Event System", typeof(EventSystem));
            events.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }
        var root = new GameObject("Mission Menu", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.transform.SetParent(transform, false);
        root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1440, 900);
        scaler.matchWidthOrHeight = .5f;
        var skyCanvas = new GameObject("Background Canvas", typeof(Canvas), typeof(CanvasScaler));
        skyCanvas.transform.SetParent(transform, false);
        backgroundCanvas = skyCanvas.GetComponent<Canvas>();
        backgroundCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        backgroundCanvas.sortingOrder = -10;
        stars = Node("Animated Bg.gif", skyCanvas.transform, Vector2.zero, Vector2.one).gameObject.AddComponent<RawImage>();
        stars.raycastTarget = false;
        SetupBackgroundVideo();
        shade = Panel(root.transform, "Shade", Vector2.zero, Vector2.one, new Color(.01f,.025f,.05f,.45f));
        shade.raycastTarget = false;
        home = Node("Start Screen", root.transform, Vector2.zero, Vector2.one).gameObject;
        var artworkBounds = Node("Start Artwork Bounds", home.transform, new Vector2(.04f,.04f), new Vector2(.96f,.96f));
        var artwork = Node("Source Menu Artwork", artworkBounds, Vector2.zero, Vector2.one).gameObject.AddComponent<RawImage>();
        artwork.texture = Resources.Load<Texture2D>("Menu/StartScreen");
        artwork.raycastTarget = false;
        var artworkFit = artwork.gameObject.AddComponent<AspectRatioFitter>();
        artworkFit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        artworkFit.aspectRatio = (float)artwork.texture.width / artwork.texture.height;
        // The source artwork already contains the START label; make that exact area interactive.
        var startHit = Panel(artwork.transform, "START", new Vector2(.30f,.30f), new Vector2(.70f,.51f), new Color(1,1,1,0));
        var startButton = startHit.gameObject.AddComponent<Button>();
        startButton.onClick.AddListener(() => ShowRoles(true));
        var startColors = startButton.colors;
        startColors.normalColor = Color.clear;
        startColors.highlightedColor = new Color(.2f,.9f,1,.16f);
        startColors.selectedColor = startColors.highlightedColor;
        startButton.colors = startColors;
        startHit.color = Color.white;
        startHit.canvasRenderer.cullTransparentMesh=false;
        roles = Node("Role Selection", root.transform, Vector2.zero, Vector2.one).gameObject;
        var roleBounds = Node("Role Artwork Bounds", roles.transform, new Vector2(.04f,.04f), new Vector2(.96f,.96f));
        var roleArt = Node("After Start Artwork", roleBounds, Vector2.zero, Vector2.one).gameObject.AddComponent<RawImage>();
        roleArt.texture = Resources.Load<Texture2D>("Menu/HolographicRoles");
        roleArt.raycastTarget = false;
        var roleFit = roleArt.gameObject.AddComponent<AspectRatioFitter>();
        roleFit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        roleFit.aspectRatio = (float)roleArt.texture.width/roleArt.texture.height;
        RoleArea(roleArt.transform,2,new Vector2(.095f,.447f),new Vector2(.483f,.586f));
        RoleArea(roleArt.transform,3,new Vector2(.515f,.447f),new Vector2(.904f,.586f));
        RoleArea(roleArt.transform,0,new Vector2(.095f,.282f),new Vector2(.483f,.421f));
        RoleArea(roleArt.transform,1,new Vector2(.515f,.282f),new Vector2(.904f,.421f));
        status = Label(roleArt.transform,"",.2f,.245f,.8f,.278f,18,cyan);
        status.gameObject.SetActive(false);
        Button(roleArt.transform,"BACK",.065f,.06f,.23f,.12f,()=>ShowRoles(false));
        confirmText = Button(roleArt.transform,"BOARD SHIP",.384f,.128f,.617f,.218f,Confirm);
        confirmText.transform.parent.GetComponent<Image>().color=new Color(.015f,.06f,.08f,1);
        missionHud = Node("Mission HUD", root.transform, Vector2.zero, Vector2.one).gameObject;
        Label(missionHud.transform, "A / D  MOVE    ·    W / S  LADDER    ·    SCROLL  ZOOM    ·    ESC  PAUSE", .05f,.02f,.95f,.09f, 18, cyan);
        missionTime = Label(missionHud.transform, "MISSION TIME  00:00 / 10:00", .35f,.91f,.65f,.96f, 18, cyan);
        Panel(missionHud.transform, "Mission Progress Track", new Vector2(.35f,.875f), new Vector2(.65f,.895f), new Color(.02f,.08f,.12f,.9f));
        missionProgress = Panel(missionHud.transform, "Mission Progress", new Vector2(.35f,.875f), new Vector2(.65f,.895f), cyan);
        progressSprite=Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,Texture2D.whiteTexture.width,Texture2D.whiteTexture.height),new Vector2(.5f,.5f));
        missionProgress.sprite=progressSprite;
        missionProgress.raycastTarget=false;
        missionProgress.type = Image.Type.Filled;
        missionProgress.fillMethod = Image.FillMethod.Horizontal;
        missionProgress.fillOrigin = (int)Image.OriginHorizontal.Left;
        missionProgress.fillAmount = 0;
        missionHint = Label(missionHud.transform, "", .20f,.105f,.80f,.15f,18,cyan);
        missionHint.gameObject.SetActive(false);
        missionCardShade = Panel(missionHud.transform,"Mission Card Shade",Vector2.zero,Vector2.one,new Color(.005f,.015f,.03f,.82f)).gameObject;
        missionCard = Panel(missionHud.transform,"Mission Briefing",new Vector2(.18f,.18f),new Vector2(.82f,.82f),new Color(.015f,.055f,.08f,.99f)).gameObject;
        missionCardTitle = Label(missionCard.transform,"",.05f,.82f,.95f,.96f,34,cyan);
        missionCardMeta = Label(missionCard.transform,"",.05f,.74f,.95f,.82f,20,Color.white);
        missionCardLocation = Label(missionCard.transform,"",.05f,.68f,.95f,.74f,16,cyan);
        missionCardBriefing = Label(missionCard.transform,"",.08f,.39f,.92f,.66f,22,Color.white);
        missionCardStatus = Label(missionCard.transform,"",.05f,.32f,.95f,.39f,18,cyan);
        missionTaskProgressTrack = Panel(missionCard.transform,"Check Progress Track",new Vector2(.14f,.275f),new Vector2(.86f,.30f),new Color(.02f,.12f,.16f,1)).gameObject;
        var taskFillRect = Node("Check Progress Fill",missionTaskProgressTrack.transform,Vector2.zero,Vector2.zero);
        taskFillRect.anchorMax = new Vector2(0,1);
        taskFillRect.pivot = new Vector2(0,.5f);
        missionTaskProgress = taskFillRect.gameObject.AddComponent<Image>();
        missionTaskProgress.color = cyan;
        missionStartButton = Button(missionCard.transform,"START CHECK",.06f,.08f,.37f,.20f,StartSelectedMission);
        missionSkipButton = Button(missionCard.transform,"SKIP CHECK",.385f,.08f,.69f,.20f,SkipSelectedMission);
        missionCloseButton = Button(missionCard.transform,"CLOSE",.71f,.08f,.94f,.20f,CloseMissionCard);
        missionCardShade.SetActive(false);
        missionCard.SetActive(false);
        missionHud.SetActive(false);
        pauseScreen=Node("Pause Screen",root.transform,Vector2.zero,Vector2.one).gameObject;
        Panel(pauseScreen.transform,"Pause Shade",Vector2.zero,Vector2.one,new Color(.01f,.025f,.05f,.88f));
        Label(pauseScreen.transform,"PAUSED",.2f,.52f,.8f,.68f,48,cyan);
        Button(pauseScreen.transform,"RESUME",.35f,.34f,.65f,.46f,TogglePause);
        pauseScreen.SetActive(false);
        roles.SetActive(false);
        Select(selected);
    }

    private void Update()
    {
        if (mission != null)
        {
            if(mission.IsPaused) return;
            UpdateMissionHint();
            if (missionCard.activeSelf) UpdateMissionCard();
            missionElapsed = Mathf.Min(missionElapsed + Time.deltaTime, MissionGoalSeconds);
            int seconds = Mathf.FloorToInt(missionElapsed);
            // Update text only when it changes: avoid formatting strings and rebuilding UI every frame.
            if(seconds==lastMissionSecond) return;
            lastMissionSecond=seconds;
            float progress = missionElapsed / MissionGoalSeconds;
            missionProgress.fillAmount = progress;
            missionProgress.color = Color.Lerp(cyan, MissionProgressGold, progress);
            int minutes = seconds / 60;
            seconds %= 60;
            missionTime.text = missionElapsed >= MissionGoalSeconds
                ? "MISSION TIME  10:00 / 10:00  ·  GOAL REACHED"
                : $"MISSION TIME  {minutes:00}:{seconds:00} / 10:00";
            return;
        }
    }
    private void SetupBackgroundVideo()
    {
        backgroundVideoTexture = new RenderTexture(2560,1440,0,RenderTextureFormat.ARGB32);
        backgroundVideoTexture.name = "Starfield Video Output";
        backgroundVideoTexture.Create();
        stars.texture = backgroundVideoTexture;

        var playerObject = new GameObject("Starfield Video Player");
        playerObject.transform.SetParent(transform,false);
        backgroundVideo = playerObject.AddComponent<VideoPlayer>();
        backgroundVideo.playOnAwake = false;
        backgroundVideo.isLooping = true;
        backgroundVideo.source = VideoSource.Url;
        backgroundVideo.url = Path.Combine(Application.streamingAssetsPath,"StarfieldLoop.webm");
        backgroundVideo.renderMode = VideoRenderMode.RenderTexture;
        backgroundVideo.targetTexture = backgroundVideoTexture;
        backgroundVideo.aspectRatio = VideoAspectRatio.FitOutside;
        backgroundVideo.audioOutputMode = VideoAudioOutputMode.None;
        backgroundVideo.waitForFirstFrame = true;
        backgroundVideo.skipOnDrop = true;
        backgroundVideo.errorReceived += (player,error) => Debug.LogError("Unable to play starfield video: " + error);
        backgroundVideo.prepareCompleted += player => player.Play();
        backgroundVideo.Prepare();
    }
    private void ShowVideoBehindShip(Camera camera)
    {
        backgroundVideo.renderMode = VideoRenderMode.CameraFarPlane;
        backgroundVideo.targetTexture = null;
        backgroundVideo.targetCamera = camera;
        backgroundVideo.aspectRatio = VideoAspectRatio.FitOutside;
        backgroundVideo.Play();
    }
    private void ShowVideoInMenu()
    {
        if (backgroundVideo == null) return;
        backgroundVideo.renderMode = VideoRenderMode.RenderTexture;
        backgroundVideo.targetTexture = backgroundVideoTexture;
        backgroundVideo.targetCamera = null;
        backgroundVideo.Play();
    }
    private void ShowRoles(bool show) { home.SetActive(!show); roles.SetActive(show); }
    private void Select(int index)
    {
        selected = index;
        for (int i = 0; i < cards.Length; i++) cards[i].color = i == index ? new Color(.15f,.8f,1,.22f) : Color.clear;
        status.text = "";
        status.gameObject.SetActive(false);
        confirmText.text = "BOARD SHIP";
    }
    private void Confirm()
    {
        PlayerPrefs.SetInt("CrewRole", selected);
        PlayerPrefs.Save();
        var prefab = Resources.Load<CrewCharacter>("Crew/" + assets[selected]);
        ShipMission candidate = null;
        try
        {
            candidate = new GameObject("Ship Mission").AddComponent<ShipMission>();
            candidate.MissionInteractionRequested += OpenMissionCard;
            candidate.Begin(prefab, TogglePause, backgroundVideo);
            ShowVideoBehindShip(Camera.main);
            mission = candidate;
            missionElapsed = 0;
            lastMissionSecond=-1;
            missionProgress.fillAmount = 0;
            missionProgress.color = cyan;
            missionTime.text = "MISSION TIME  00:00 / 10:00";
            home.SetActive(false); roles.SetActive(false);
            shade.gameObject.SetActive(false); backgroundCanvas.gameObject.SetActive(false);
            missionHud.SetActive(true);
        }
        catch (System.Exception error)
        {
            if (candidate != null) Destroy(candidate.gameObject);
            status.text = "Unable to board: " + error.Message;
            status.gameObject.SetActive(true);
            Debug.LogException(error);
        }
    }
    private void RoleArea(Transform parent,int role,Vector2 min,Vector2 max)
    {
        var image=Panel(parent,"Choose "+names[role],min,max,Color.clear);
        cards[role]=image;
        image.canvasRenderer.cullTransparentMesh=false;
        var button=image.gameObject.AddComponent<Button>();
        button.onClick.AddListener(()=>Select(role));
    }
    private void TogglePause()
    {
        if(mission==null) return;
        bool paused=!mission.IsPaused;
        mission.SetPaused(paused);pauseScreen.SetActive(paused);
        if(EventSystem.current!=null)
            EventSystem.current.SetSelectedGameObject(paused?pauseScreen.GetComponentInChildren<Button>().gameObject:null);
    }
    private void UpdateMissionHint()
    {
        string hint = null;
        if (mission.NearbyMission != null)
        {
            var status = mission.GetMissionStatus(mission.NearbyMission);
            hint = status == ShipMissionStatus.InProgress
                ? "PRESS E TO VIEW CHECK  ·  " + mission.NearbyMission.title
                : "PRESS E TO INTERACT  ·  " + mission.NearbyMission.title;
        }
        else if (mission.ActiveMission != null)
            hint = "CHECK PAUSED  ·  RETURN TO " + mission.ActiveMission.locationName.ToUpperInvariant();
        bool visible = !string.IsNullOrEmpty(hint);
        if (missionHint.gameObject.activeSelf != visible) missionHint.gameObject.SetActive(visible);
        if (visible && missionHint.text != hint) missionHint.text = hint;
    }
    private void OpenMissionCard(ShipMissionDefinition definition)
    {
        selectedMission = definition;
        mission.SetPaused(true);
        missionCardShade.SetActive(true);
        missionCard.SetActive(true);
        UpdateMissionCard();
    }
    private void UpdateMissionCard()
    {
        if (selectedMission == null || mission == null) return;
        missionCardTitle.text = selectedMission.missionId + " — " + selectedMission.title;
        missionCardMeta.text = selectedMission.timeWindowLabel + " · " + Mathf.CeilToInt(selectedMission.durationSeconds) + "s · " + selectedMission.difficulty;
        missionCardLocation.text = selectedMission.locationName.ToUpperInvariant() + "  ·  DECK " + (selectedMission.deck + 1);
        missionCardBriefing.text = selectedMission.briefing;
        var state = mission.GetMissionStatus(selectedMission);
        bool available = state == ShipMissionStatus.Available;
        bool active = state == ShipMissionStatus.InProgress;
        missionStartButton.transform.parent.gameObject.SetActive(available);
        missionSkipButton.transform.parent.gameObject.SetActive(available);
        missionCloseButton.transform.parent.gameObject.SetActive(true);
        missionTaskProgressTrack.SetActive(active || state == ShipMissionStatus.Completed);
        float progress = state == ShipMissionStatus.Completed ? 1 : active ? mission.ActiveMissionProgress : 0;
        var fillRect = missionTaskProgress.rectTransform;
        fillRect.anchorMax = new Vector2(progress,1);
        fillRect.offsetMin = fillRect.offsetMax = Vector2.zero;
        if (active)
        {
            bool atStation = mission.NearbyMission == selectedMission;
            int remaining = Mathf.CeilToInt(selectedMission.durationSeconds * (1-progress));
            missionCardStatus.text = atStation
                ? "SYSTEMS CHECK IN PROGRESS  ·  " + remaining + "s REMAINING"
                : "CHECK PAUSED  ·  RETURN TO " + selectedMission.locationName.ToUpperInvariant();
        }
        else if (state == ShipMissionStatus.Completed) missionCardStatus.text = "SYSTEMS CHECK COMPLETE";
        else if (state == ShipMissionStatus.Skipped) missionCardStatus.text = "CHECK SKIPPED  ·  THE FAULT REMAINS UNCHECKED";
        else if (state == ShipMissionStatus.Expired) missionCardStatus.text = "MISSION WINDOW CLOSED";
        else missionCardStatus.text = "START THE CHECK OR SKIP THIS MISSION";
    }
    private void StartSelectedMission()
    {
        if (mission != null && mission.StartMission(selectedMission))
        {
            mission.SetPaused(false);
            UpdateMissionCard();
        }
    }
    private void SkipSelectedMission()
    {
        if (mission != null && mission.SkipMission(selectedMission))
        {
            mission.SetPaused(false);
            UpdateMissionCard();
        }
    }
    private void CloseMissionCard()
    {
        if (mission != null) mission.SetPaused(false);
        missionCard.SetActive(false);
        missionCardShade.SetActive(false);
        selectedMission = null;
    }
    private void ReturnToMenu()
    {
        mission.SetPaused(false);
        pauseScreen.SetActive(false);
        Destroy(mission.gameObject); mission = null;
        missionHud.SetActive(false);
        ShowVideoInMenu();
        shade.gameObject.SetActive(true); backgroundCanvas.gameObject.SetActive(true);
        ShowRoles(true);
    }
    private void OnDestroy()
    {
        if(backgroundVideo!=null) backgroundVideo.Stop();
        if(backgroundVideoTexture!=null){backgroundVideoTexture.Release();Destroy(backgroundVideoTexture);}
        if(progressSprite!=null) Destroy(progressSprite);
    }
    private RectTransform Node(string name, Transform parent, Vector2 min, Vector2 max)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false); rect.anchorMin = min; rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return rect;
    }
    private Image Panel(Transform parent, string name, Vector2 min, Vector2 max, Color color)
    {
        var img = Node(name, parent, min, max).gameObject.AddComponent<Image>(); img.color = color; return img;
    }
    private Text Label(Transform parent, string value, float x, float y, float xx, float yy, int size, Color color)
    {
        var text = Node(value, parent, new Vector2(x,y), new Vector2(xx,yy)).gameObject.AddComponent<Text>();
        text.font = font; text.text = value; text.fontSize = size; text.color = color;
        text.alignment = TextAnchor.MiddleCenter; text.raycastTarget = false;
        text.resizeTextForBestFit = true; text.resizeTextMinSize = 10; text.resizeTextMaxSize = size;
        return text;
    }
    private Text Button(Transform parent, string value, float x, float y, float xx, float yy, UnityEngine.Events.UnityAction action)
    {
        var image = Panel(parent, value, new Vector2(x,y), new Vector2(xx,yy), new Color(.05f,.25f,.32f,.98f));
        var button = image.gameObject.AddComponent<Button>(); button.onClick.AddListener(action);
        var colors = button.colors; colors.highlightedColor = new Color(.6f,1,1); colors.selectedColor = colors.highlightedColor; button.colors = colors;
        return Label(image.transform, value, .03f,.05f,.97f,.95f, 23, Color.white);
    }
}
