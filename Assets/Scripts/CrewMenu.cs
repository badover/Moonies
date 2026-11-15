using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Video;
using System.IO;
using System.Linq;

/// <summary>Two-screen entry flow with persistent crew selection and ship boarding.</summary>
public sealed class CrewMenu : MonoBehaviour
{
    public static CrewRole SelectedRole => (CrewRole)Mathf.Clamp(PlayerPrefs.GetInt("CrewRole", 0), 0, 3);
    private readonly string[] names = { "Commander", "Medic", "Pilot", "Engineer" };
    private readonly string[] descriptions = { "Coordinate your crew\nand lead the mission.", "Monitor crew health\nand provide medical care.", "Control the flight\nand navigate to the Moon.", "Maintain ship systems\nand repair equipment." };
    private readonly string[] assets = { "Commander", "Medic", "Pilot", "Engineer" };
    private readonly Color cyan = new Color(.25f, .86f, .94f);
    private GameObject home, roles, missionHud, pauseScreen, controlsIntro, stormSuggestion, gameOverScreen;
    private GameObject taskBoard, procedurePanel, equipmentPanel, shipNotice, cabinDamageFrame;
    private ShipEquipment.Element selectedEquipment;
    private Text equipmentTitle,equipmentReadout,nearbyEquipmentButton,shipNoticeText;
    private readonly Text[] equipmentActions=new Text[2];
    private float shipNoticeRemaining,lastRadiationRate=-1;
    private bool lastRadiationProtection;
    private int lastEquipmentSecond=-1;
    private Text phaseText, procedurePrompt, procedureFeedbackText, runResultTitle, runResultBody;
    private readonly Text[] procedureOptions=new Text[3];
    private readonly Text[] taskRows=new Text[8];
    private readonly ShipMissionDefinition[] taskRowDefinitions=new ShipMissionDefinition[8];
    private float controlsIntroRemaining;
    private const float ControlsIntroSeconds=10;
    private const string ControlsText="A / D or Left / Right  —  Move\nW / S or Up / Down  —  Climb\nA / D near a floor  —  Leave ladder\nE / Click  —  Interact with equipment\nMouse wheel  —  Zoom\nM / Mission Tasks  —  Open mission interface\nEsc  —  Pause / resume";
    private ShipMission mission;
    private Canvas backgroundCanvas;
    private Image shade;
    private RawImage stars;
    private VideoPlayer backgroundVideo;
    private RenderTexture backgroundVideoTexture;
    private Image[] cards = new Image[4];
    private Image missionProgress,healthFill;
    private Text healthValue;
    private CrewHealth playerHealth;
    private Text status, confirmText, missionTime, radiationStatus;
    private int lastRadiationLevel=-1;
    private float radiationNoticeRemaining;
    private Text stormAlertTitle, stormAlertInstruction;
    private int lastStormAlertSecond=-1;
    private string lastStormAlertTitle;
    private Text missionHint, missionCardTitle, missionCardBriefing, missionCardStatus;
    private Text missionStartButton, missionSkipButton, missionCloseButton;
    private GameObject missionCardShade, missionCard, missionTaskProgressTrack;
    private Image missionTaskProgress, activeTaskProgress;
    private GameObject missionHintBackdrop;
    private ShipMissionDefinition lastCardDefinition, lastBoardActive;
    private ShipMissionStatus lastCardState;
    private int lastCardStep=-1,lastCardSecond=-1,lastBoardSecond=-1;
    private bool lastCardCanStart;
    private string lastCardFeedback;
    private ShipMissionDefinition selectedMission;
    private ShipMissionDefinition bridgeCheck;
    private ShipMissionDefinition solarStorm;
    private GameObject bridgeSuggestion;
    private string missionFeedback;
    private float missionFeedbackRemaining;
    private bool gameOver;
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
        startButton.transition=Selectable.Transition.None;
        startHit.color=Color.clear;
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
        controlsIntro=Panel(missionHud.transform,"Controls Intro",new Vector2(.045f,.61f),new Vector2(.43f,.85f),new Color(.015f,.04f,.06f,.96f)).gameObject;
        controlsIntro.GetComponent<Image>().raycastTarget=false;
        Label(controlsIntro.transform,"CONTROLS",.07f,.78f,.93f,.96f,22,cyan);
        var introText=Label(controlsIntro.transform,ControlsText,.07f,.06f,.93f,.77f,17,Color.white);
        introText.alignment=TextAnchor.MiddleLeft;
        controlsIntro.SetActive(false);
        Button(missionHud.transform,"MISSION TASKS",.025f,.90f,.205f,.97f,ToggleTaskBoard);
        phaseText=Label(missionHud.transform,"LEO CHECKOUT · 185 KM PARKING ORBIT",.24f,.965f,.71f,.997f,14,cyan);
        missionTime = Label(missionHud.transform, "MISSION TIME  00:00 / 10:00", .35f,.91f,.65f,.96f, 18, cyan);
        var radiationFrame=Panel(missionHud.transform,"Radiation Status Frame",new Vector2(.73f,.90f),new Vector2(.975f,.97f),new Color(.19f,.49f,.54f,1));
        radiationFrame.raycastTarget=false;
        var radiationBacking=Panel(radiationFrame.transform,"Radiation Opaque Backing",Vector2.zero,Vector2.one,new Color(.015f,.035f,.05f,1));
        radiationBacking.raycastTarget=false;
        radiationBacking.rectTransform.offsetMin=new Vector2(2,2);radiationBacking.rectTransform.offsetMax=new Vector2(-2,-2);
        radiationStatus=Label(radiationFrame.transform,"RADIATION  0%  ·  LOW",.025f,.10f,.975f,.90f,18,cyan);
        Panel(missionHud.transform, "Mission Progress Track", new Vector2(.35f,.875f), new Vector2(.65f,.895f), new Color(.02f,.08f,.12f,.9f));
        missionProgress = Panel(missionHud.transform, "Mission Progress", new Vector2(.35f,.875f), new Vector2(.65f,.895f), cyan);
        progressSprite=Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,Texture2D.whiteTexture.width,Texture2D.whiteTexture.height),new Vector2(.5f,.5f));
        missionProgress.sprite=progressSprite;
        missionProgress.raycastTarget=false;
        missionProgress.type = Image.Type.Filled;
        missionProgress.fillMethod = Image.FillMethod.Horizontal;
        missionProgress.fillOrigin = (int)Image.OriginHorizontal.Left;
        missionProgress.fillAmount = 0;
        CreateHealthHud(missionHud.transform);
        missionHintBackdrop=Panel(missionHud.transform,"Active Task Status",new Vector2(.20f,.10f),new Vector2(.80f,.165f),new Color(.01f,.035f,.05f,1)).gameObject;
        missionHintBackdrop.GetComponent<Image>().raycastTarget=false;
        missionHint=Label(missionHintBackdrop.transform,"",.025f,.20f,.975f,.98f,17,cyan);
        activeTaskProgress=Panel(missionHintBackdrop.transform,"Active Task Progress",new Vector2(.025f,.08f),new Vector2(.975f,.14f),cyan);
        activeTaskProgress.raycastTarget=false;activeTaskProgress.sprite=progressSprite;activeTaskProgress.type=Image.Type.Filled;
        activeTaskProgress.fillMethod=Image.FillMethod.Horizontal;activeTaskProgress.fillOrigin=0;
        missionHint.gameObject.SetActive(false);missionHintBackdrop.SetActive(false);
        var hintGold=new Color(1,.82f,.24f,1);
        bridgeSuggestion=Panel(missionHud.transform,"Bridge Suggestion",new Vector2(.27f,.18f),new Vector2(.73f,.233f),hintGold).gameObject;
        bridgeSuggestion.GetComponent<Image>().raycastTarget=false;
        var hintBacking=Panel(bridgeSuggestion.transform,"Opaque Hint Backing",Vector2.zero,Vector2.one,new Color(.045f,.035f,.015f,1));
        hintBacking.raycastTarget=false;
        hintBacking.rectTransform.offsetMin=new Vector2(2,2);hintBacking.rectTransform.offsetMax=new Vector2(-2,-2);
        Label(bridgeSuggestion.transform,"Maybe it's a good idea to check the Bridge.",.025f,.05f,.975f,.95f,16,hintGold);
        bridgeSuggestion.SetActive(false);
        bridgeCheck=Resources.Load<ShipMissionDefinition>("Missions/G01_LEO_Check");
        solarStorm=Resources.Load<ShipMissionDefinition>("Missions/G02_Solar_Storm");
        stormSuggestion=Panel(missionHud.transform,"Solar Storm Alert",new Vector2(.18f,.72f),new Vector2(.82f,.84f),new Color(1f,.65f,.13f,1)).gameObject;
        stormSuggestion.GetComponent<Image>().raycastTarget=false;
        var stormBacking=Panel(stormSuggestion.transform,"Alert Opaque Backing",Vector2.zero,Vector2.one,new Color(.13f,.025f,.012f,1));
        stormBacking.raycastTarget=false;
        stormBacking.rectTransform.offsetMin=new Vector2(3,3);stormBacking.rectTransform.offsetMax=new Vector2(-3,-3);
        stormAlertTitle=Label(stormSuggestion.transform,"",.025f,.48f,.975f,.92f,24,new Color(1f,.77f,.25f));
        stormAlertInstruction=Label(stormSuggestion.transform,"",.025f,.08f,.975f,.47f,18,Color.white);
        stormSuggestion.SetActive(false);
        missionCardShade = Panel(missionHud.transform,"Mission Card Shade",Vector2.zero,Vector2.one,new Color(.005f,.015f,.03f,.82f)).gameObject;
        missionCard = Panel(missionHud.transform,"Mission Briefing",new Vector2(.18f,.18f),new Vector2(.82f,.82f),new Color(.015f,.055f,.08f,.99f)).gameObject;
        missionCardTitle = Label(missionCard.transform,"",.05f,.77f,.95f,.93f,34,cyan);
        missionCardBriefing = Label(missionCard.transform,"",.08f,.42f,.92f,.75f,22,Color.white);
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
        procedurePanel=Node("Mission Procedure",missionCard.transform,new Vector2(.05f,.40f),new Vector2(.95f,.75f)).gameObject;
        procedurePrompt=Label(procedurePanel.transform,"",.02f,.63f,.98f,.99f,21,Color.white);
        for(int i=0;i<procedureOptions.Length;i++)
        {
            int option=i;float y=.43f-i*.19f;
            procedureOptions[i]=Button(procedurePanel.transform,"PROCEDURE OPTION "+i,.02f,y,.98f,y+.17f,()=>ChooseProcedureOption(option));
            procedureOptions[i].resizeTextMaxSize=18;
        }
        procedureFeedbackText=Label(missionCard.transform,"",.05f,.215f,.95f,.27f,14,new Color(1,.8f,.3f));
        procedurePanel.SetActive(false);
        taskBoard=Panel(missionHud.transform,"Mission Task Board",new Vector2(.10f,.12f),new Vector2(.90f,.88f),new Color(.008f,.025f,.04f,1)).gameObject;
        Label(taskBoard.transform,"MISSION CONTROL",.04f,.88f,.96f,.98f,30,cyan);
        Label(taskBoard.transform,"Shared crew procedures · One active task · Pending tasks stay available",.04f,.81f,.96f,.88f,17,Color.white);
        for(int i=0;i<taskRows.Length;i++)
        {
            int row=i;float y=.72f-i*.079f;
            taskRows[i]=Button(taskBoard.transform,"MISSION ROW "+i,.04f,y,.96f,y+.068f,()=>OpenTaskRow(row));
            taskRows[i].alignment=TextAnchor.MiddleLeft;taskRows[i].resizeTextMaxSize=18;
        }
        Button(taskBoard.transform,"CLOSE TASKS",.35f,.035f,.65f,.11f,ToggleTaskBoard);
        taskBoard.SetActive(false);
        equipmentPanel=Panel(missionHud.transform,"Equipment Panel",new Vector2(.17f,.18f),new Vector2(.83f,.82f),new Color(.008f,.03f,.045f,1)).gameObject;
        equipmentTitle=Label(equipmentPanel.transform,"",.04f,.78f,.96f,.96f,28,cyan);
        equipmentReadout=Label(equipmentPanel.transform,"",.06f,.34f,.94f,.77f,20,Color.white);
        for(int i=0;i<2;i++){int action=i;float y=.23f-i*.10f;equipmentActions[i]=Button(equipmentPanel.transform,"EQUIPMENT ACTION "+i,.05f,y,.95f,y+.08f,()=>OperateSelectedEquipment(action));equipmentActions[i].resizeTextMaxSize=18;}
        Button(equipmentPanel.transform,"OPEN STATION TASK",.05f,.03f,.68f,.11f,OpenEquipmentTask);
        Button(equipmentPanel.transform,"CLOSE EQUIPMENT",.70f,.03f,.95f,.11f,CloseEquipment);
        equipmentPanel.SetActive(false);
        nearbyEquipmentButton=Button(missionHud.transform,"E · EQUIPMENT",.025f,.84f,.29f,.89f,OpenNearbyEquipment);nearbyEquipmentButton.resizeTextMaxSize=15;
        nearbyEquipmentButton.transform.parent.gameObject.SetActive(false);
        shipNotice=Panel(missionHud.transform,"Flight Event Announcement",new Vector2(.25f,.72f),new Vector2(.75f,.84f),new Color(.01f,.06f,.09f,1)).gameObject;
        shipNotice.GetComponent<Image>().raycastTarget=false;
        shipNoticeText=Label(shipNotice.transform,"",.04f,.08f,.96f,.92f,21,new Color(1,.82f,.25f));shipNotice.SetActive(false);
        cabinDamageFrame=Node("Cabin Radiation Warning Frame",missionHud.transform,Vector2.zero,Vector2.one).gameObject;
        foreach(var bounds in new[]{new Vector4(0,0,.006f,1),new Vector4(.994f,0,1,1),new Vector4(0,0,1,.008f),new Vector4(0,.992f,1,1)})
        {var edge=Panel(cabinDamageFrame.transform,"Radiation warning edge",new Vector2(bounds.x,bounds.y),new Vector2(bounds.z,bounds.w),new Color(1,.22f,.05f,.65f));edge.raycastTarget=false;}
        cabinDamageFrame.SetActive(false);
        missionCardShade.SetActive(false);
        missionCard.SetActive(false);
        missionHud.SetActive(false);
        pauseScreen=Node("Pause Screen",root.transform,Vector2.zero,Vector2.one).gameObject;
        Panel(pauseScreen.transform,"Pause Shade",Vector2.zero,Vector2.one,new Color(.01f,.025f,.05f,.88f));
        Label(pauseScreen.transform,"PAUSED",.2f,.75f,.8f,.88f,48,cyan);
        Label(pauseScreen.transform,"CONTROLS",.25f,.65f,.75f,.72f,22,cyan);
        var pauseControls=Label(pauseScreen.transform,ControlsText,.27f,.32f,.73f,.65f,22,Color.white);
        pauseControls.alignment=TextAnchor.MiddleLeft;
        Button(pauseScreen.transform,"RESUME",.35f,.16f,.65f,.27f,TogglePause);
        pauseScreen.SetActive(false);
        roles.SetActive(false);
        gameOverScreen=Panel(missionHud.transform,"Radiation Game Over",new Vector2(.15f,.22f),new Vector2(.85f,.78f),new Color(.035f,.008f,.012f,.99f)).gameObject;
        runResultTitle=Label(gameOverScreen.transform,"MISSION FAILED",.05f,.66f,.95f,.91f,46,new Color(1f,.22f,.18f));
        runResultBody=Label(gameOverScreen.transform,"Crew vitals are critical. Return to the menu to begin a new flight.",.08f,.34f,.92f,.66f,24,Color.white);
        Button(gameOverScreen.transform,"RETURN TO MENU",.30f,.10f,.70f,.27f,ReturnToMenu);
        gameOverScreen.SetActive(false);
        Select(selected);
    }

    private void Update()
    {
        if (mission != null)
        {
            UpdateRadiationStatus();
            cabinDamageFrame.SetActive(!gameOver && mission.RadiationDamagePerSecond>0);
            if(mission.IsPaused) return;
            UpdateEquipmentInterface();
            if(UnityEngine.InputSystem.Keyboard.current!=null && UnityEngine.InputSystem.Keyboard.current.mKey.wasPressedThisFrame) ToggleTaskBoard();
            if(taskBoard.activeSelf) UpdateTaskBoard();
            UpdateControlsIntro(Time.deltaTime);
            UpdateMissionHint();
            UpdateBridgeSuggestion();
            UpdateStormSuggestion();
            UpdateFlightNotice();
            if(stormSuggestion.activeSelf && radiationNoticeRemaining>0) radiationNoticeRemaining=Mathf.Max(0,radiationNoticeRemaining-Time.deltaTime);
            if(missionFeedbackRemaining>0) missionFeedbackRemaining=Mathf.Max(0,missionFeedbackRemaining-Time.deltaTime);
            if (missionCard.activeSelf) UpdateMissionCard();
            missionElapsed = mission.MissionSeconds;
            int seconds = Mathf.FloorToInt(missionElapsed);
            // Update text only when it changes: avoid formatting strings and rebuilding UI every frame.
            if(seconds==lastMissionSecond) return;
            lastMissionSecond=seconds;
            phaseText.text=mission.PhaseLabel;
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
    private void CreateHealthHud(Transform parent)
    {
        var frame=Panel(parent,"Health HUD",Vector2.zero,Vector2.zero,new Color(.16f,.42f,.48f,1));
        frame.raycastTarget=false;
        var rect=frame.rectTransform;rect.pivot=Vector2.zero;rect.anchoredPosition=new Vector2(28,26);rect.sizeDelta=new Vector2(210,44);
        var backing=Panel(frame.transform,"Health Backing",Vector2.zero,Vector2.one,new Color(.012f,.035f,.05f,.98f));
        backing.raycastTarget=false;backing.rectTransform.offsetMin=Vector2.one;backing.rectTransform.offsetMax=-Vector2.one;
        var title=Label(frame.transform,"VITALS",.07f,.48f,.65f,.91f,12,new Color(.55f,.76f,.8f));title.alignment=TextAnchor.MiddleLeft;
        healthValue=Label(frame.transform,"100%",.66f,.48f,.93f,.91f,13,Color.white);healthValue.alignment=TextAnchor.MiddleRight;
        var track=Panel(frame.transform,"Health Track",new Vector2(.07f,.18f),new Vector2(.93f,.36f),new Color(.025f,.11f,.14f,1));track.raycastTarget=false;
        healthFill=Panel(track.transform,"Health Fill",Vector2.zero,Vector2.one,new Color(.27f,.82f,.74f,1));
        healthFill.raycastTarget=false;healthFill.sprite=progressSprite;healthFill.type=Image.Type.Filled;
        healthFill.fillMethod=Image.FillMethod.Horizontal;healthFill.fillOrigin=(int)Image.OriginHorizontal.Left;
    }
    private void BindHealth(CrewHealth health)
    {
        if(playerHealth!=null) playerHealth.Changed-=UpdateHealthHud;
        playerHealth=health;
        if(playerHealth!=null){playerHealth.Changed+=UpdateHealthHud;UpdateHealthHud(playerHealth);}
    }
    private void UpdateHealthHud(CrewHealth health)
    {
        healthFill.fillAmount=health.Fraction;
        healthFill.color=health.Fraction<=.25f?new Color(.92f,.32f,.24f):health.Fraction<=.55f?new Color(.95f,.68f,.28f):new Color(.27f,.82f,.74f);
        healthValue.text=Mathf.CeilToInt(health.Fraction*100)+"%";
        if(health.Current<=0 && mission!=null) ShowGameOver();
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
        for (int i = 0; i < cards.Length; i++) cards[i].color = Color.clear;
        status.text = "";
        status.gameObject.SetActive(false);
        confirmText.text = "BOARD AS " + names[index].ToUpperInvariant();
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
            candidate.MissionResolved += OnMissionResolved;
            candidate.RunEnded += ShowRunResults;
            candidate.EquipmentInteractionRequested += OpenEquipment;
            candidate.MissionAvailable += OnMissionAvailable;
            candidate.FlightAnnouncement += ShowFlightNotice;
            candidate.Begin(prefab, TogglePause, backgroundVideo);
            ShowVideoBehindShip(Camera.main);
            mission = candidate;
            BindHealth(mission.Health);
            missionElapsed = 0;
            lastMissionSecond=-1;
            missionProgress.fillAmount = 0;
            missionProgress.color = cyan;
            missionTime.text = "MISSION TIME  00:00 / 10:00";
            gameOver=false;
            gameOverScreen.SetActive(false);
            missionCard.SetActive(false);missionCardShade.SetActive(false);taskBoard.SetActive(false);
            mission.IsInterfaceOpen=false;equipmentPanel.SetActive(false);selectedEquipment=null;
            shipNoticeRemaining=0;shipNotice.SetActive(false);cabinDamageFrame.SetActive(false);lastEquipmentSecond=-1;
            selectedMission=null;missionFeedback=null;missionFeedbackRemaining=0;
            radiationNoticeRemaining=0;lastStormAlertSecond=-1;lastStormAlertTitle=null;
            stormSuggestion.SetActive(false);
            lastRadiationLevel=-1;lastRadiationRate=-1;UpdateRadiationStatus();
            home.SetActive(false); roles.SetActive(false);
            shade.gameObject.SetActive(false); backgroundCanvas.gameObject.SetActive(false);
            missionHud.SetActive(true);
            controlsIntroRemaining=ControlsIntroSeconds;
            UpdateControlsIntro(0);
            UpdateBridgeSuggestion();
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
        button.transition=Selectable.Transition.None;
        button.onClick.AddListener(()=>Select(role));
    }
    private void TogglePause()
    {
        if(mission==null || gameOver) return;
        if(equipmentPanel.activeSelf){CloseEquipment();return;}
        if(missionCard.activeSelf){CloseMissionCard();return;}
        if(taskBoard.activeSelf){ToggleTaskBoard();return;}
        bool paused=!mission.IsPaused;
        if(paused) stormSuggestion.SetActive(false);
        mission.SetPaused(paused);pauseScreen.SetActive(paused);
        UpdateControlsIntro(0);
        if(EventSystem.current!=null)
            EventSystem.current.SetSelectedGameObject(paused?pauseScreen.GetComponentInChildren<Button>().gameObject:null);
    }
    private void UpdateControlsIntro(float delta)
    {
        if(mission==null) return;
        bool covered=mission.IsPaused || missionCard.activeSelf || taskBoard.activeSelf || equipmentPanel.activeSelf;
        if(!covered) controlsIntroRemaining=Mathf.Max(0,controlsIntroRemaining-Mathf.Max(0,delta));
        bool visible=controlsIntroRemaining>0 && !covered;
        if(controlsIntro.activeSelf!=visible) controlsIntro.SetActive(visible);
    }
    private void UpdateMissionHint()
    {
        string hint = null;
        if (missionFeedbackRemaining > 0 && !string.IsNullOrEmpty(missionFeedback))
            hint = missionFeedback;
        else if (mission.ActiveMission != null)
        {
            var step=mission.CurrentStep;
            hint=step!=null && step.requiresShelter
                ? (mission.IsNearActiveTarget ? "PRESS E AT THE SHELTER MARKER" : "REACH THE CENTRAL SHELTER · LOWER DECK")
                : step!=null && step.automatic
                    ? "SCANNING " + Mathf.RoundToInt(mission.ProcedureStepProgress*100) + "% · " + mission.ActiveMission.title.ToUpperInvariant() + " · M TO VIEW"
                    : "ACTION REQUIRED · " + mission.ActiveMission.title.ToUpperInvariant() + " · PRESS M";
        }
        else if (mission.NearbyMission != null)
            hint="PRESS E · " + mission.NearbyMission.locationName.ToUpperInvariant() + " CONTROL";
        else if(mission.RunPlan.Any(x=>mission.GetMissionStatus(x.definition)==ShipMissionStatus.Available))
            hint="NEW MISSION TASK AVAILABLE · PRESS M OR OPEN MISSION TASKS";
        bool visible = !string.IsNullOrEmpty(hint) && !missionCard.activeSelf && !taskBoard.activeSelf && !equipmentPanel.activeSelf;
        if(missionHintBackdrop.activeSelf!=visible) missionHintBackdrop.SetActive(visible);
        activeTaskProgress.gameObject.SetActive(mission.ActiveMission!=null);
        activeTaskProgress.fillAmount=mission.ActiveMissionProgress;
        if (missionHint.gameObject.activeSelf != visible) missionHint.gameObject.SetActive(visible);
        if (visible && missionHint.text != hint) missionHint.text = hint;
    }
    private void UpdateBridgeSuggestion()
    {
        bool visible=mission!=null && bridgeCheck!=null && bridgeCheck.AllowsRole(SelectedRole)
            && mission.GetMissionStatus(bridgeCheck)==ShipMissionStatus.Available && !missionCard.activeSelf && !taskBoard.activeSelf && !equipmentPanel.activeSelf;
        if(bridgeSuggestion.activeSelf!=visible) bridgeSuggestion.SetActive(visible);
    }
    private void UpdateStormSuggestion()
    {
        if(stormSuggestion==null || solarStorm==null || mission==null) return;
        var state=mission.GetMissionStatus(solarStorm);
        bool active=mission.ActiveMission==solarStorm;
        bool available=solarStorm.AllowsRole(SelectedRole) && state==ShipMissionStatus.Available;
        bool exposed=mission.RadiationLevel>0;
        bool visible=(active || available || exposed || radiationNoticeRemaining>0) && !missionCard.activeSelf && !taskBoard.activeSelf && !equipmentPanel.activeSelf && !mission.IsPaused && !gameOver;
        if(stormSuggestion.activeSelf!=visible) stormSuggestion.SetActive(visible);
        if(!visible) return;
        string title=active ? "CABIN RADIATION ALERT — CONFIGURE SHELTER" : exposed ? (mission.IsRadiationProtected ? "CABIN RADIATION — SHELTERED" : "CABIN RADIATION — HP LOSS") : available ? "SOLAR STORM DETECTED" : "RADIATION LEVEL INCREASED";
        int remaining=active ? Mathf.CeilToInt(mission.ActiveMissionSecondsRemaining) : exposed ? Mathf.CeilToInt(mission.CabinEventSecondsRemaining) : -1;
        if(title==lastStormAlertTitle && remaining==lastStormAlertSecond) return;
        lastStormAlertTitle=title;lastStormAlertSecond=remaining;
        stormAlertTitle.text=title;
        stormAlertInstruction.text=active
            ? "CENTRAL SHELTER · LOWER DECK · M TO VIEW PROCEDURE · " + remaining + "s"
            : exposed ? "CENTRAL SHELTER · LOWER DECK · INSTALL SHIELDING AND CLOSE THE SEAL · " + remaining + "s"
            : available ? "OPEN MISSION TASKS · BEGIN THE SHELTER PROCEDURE"
            : "Radiation detected aboard the ship. Monitor the radiation indicator.";
    }

    private void UpdateRadiationStatus()
    {
        if(radiationStatus==null || mission==null) return;
        int level=Mathf.RoundToInt(mission.RadiationLevel);
        float rate=Mathf.Round(mission.RadiationDamagePerSecond*100)/100;
        bool protectedCrew=mission.IsRadiationProtected;
        if(level==lastRadiationLevel && rate==lastRadiationRate && protectedCrew==lastRadiationProtection) return;
        if(lastRadiationLevel>=0 && level>lastRadiationLevel) radiationNoticeRemaining=10;
        lastRadiationLevel=level;lastRadiationRate=rate;lastRadiationProtection=protectedCrew;
        float threshold=solarStorm!=null ? solarStorm.fatalRadiationThreshold : solarStormThreshold;
        string state=level==0 ? "SAFE" : protectedCrew ? "SHIELDED" : level>=threshold ? "CRITICAL" : "EXPOSED";
        radiationStatus.text="CABIN RADIATION  " + level + "% · " + state + (rate>0 ? "\n−"+rate.ToString("0.00",System.Globalization.CultureInfo.InvariantCulture)+" HP / s · TAKE SHELTER" : protectedCrew ? "\nNO HP LOSS INSIDE THE SHELTER" : "");
        radiationStatus.color=rate>0 ? new Color(1f,.54f,.17f) : cyan;
    }

    private const float solarStormThreshold=60;
    private void OnMissionResolved(ShipMissionDefinition definition, bool success, bool fatal)
    {
        if(definition==null) return;
        if(fatal){ShowGameOver();return;}
        missionFeedback=(success ? "COMPLETE: " : "FAILED: ") + definition.title.ToUpperInvariant();
        missionFeedbackRemaining=6;
    }
    private void ShowRunResults()
    {
        if(gameOver) return;
        ShowGameOver();
        missionElapsed=MissionGoalSeconds;missionProgress.fillAmount=1;missionProgress.color=MissionProgressGold;
        missionTime.text="MISSION TIME  10:00 / 10:00 · FLIGHT COMPLETE";
        int complete=mission.RunPlan.Count(x=>mission.GetMissionStatus(x.definition)==ShipMissionStatus.Completed);
        runResultTitle.text="FLIGHT COMPLETE";
        runResultTitle.color=cyan;
        runResultBody.text="Earth recovery reached.\nProcedures completed: " + complete + " / " + mission.RunPlan.Count +
            "\nCrew vitals: " + Mathf.CeilToInt(mission.Health.Fraction*100) + "%";
    }
    private void ShowGameOver()
    {
        gameOver=true;missionFeedbackRemaining=0;
        runResultTitle.text="MISSION FAILED";runResultTitle.color=new Color(1f,.22f,.18f);
        runResultBody.text="Crew vitals are critical. Return to the menu to begin a new flight.";
        taskBoard.SetActive(false);equipmentPanel.SetActive(false);shipNotice.SetActive(false);mission.IsInterfaceOpen=true;
        missionCard.SetActive(false);missionCardShade.SetActive(false);
        controlsIntro.SetActive(false);pauseScreen.SetActive(false);
        bridgeSuggestion.SetActive(false);stormSuggestion.SetActive(false);
        missionHint.gameObject.SetActive(false);missionHintBackdrop.SetActive(false);
        gameOverScreen.SetActive(true);mission.SetPaused(true);
        if(EventSystem.current!=null) EventSystem.current.SetSelectedGameObject(null);
    }
    private void OpenMissionCard(ShipMissionDefinition definition)
    {
        if(mission==null || definition==null || gameOver) return;
        selectedMission = definition;
        lastCardDefinition=null;missionHintBackdrop.SetActive(false);
        taskBoard.SetActive(false);equipmentPanel.SetActive(false);shipNotice.SetActive(false);mission.IsInterfaceOpen=true;
        bridgeSuggestion.SetActive(false);
        if(stormSuggestion!=null) stormSuggestion.SetActive(false);
        controlsIntro.SetActive(false);
        mission.SetPaused(false);
        missionCardShade.SetActive(true);
        missionCard.SetActive(true);
        UpdateMissionCard();
    }
    private void UpdateMissionCard()
    {
        if (selectedMission == null || mission == null) return;
        var state=mission.GetMissionStatus(selectedMission);
        bool active=state==ShipMissionStatus.InProgress;
        bool available=state==ShipMissionStatus.Available;
        bool canStart=available && mission.ActiveMission==null;
        float progress=state==ShipMissionStatus.Completed ? 1 : active ? mission.ActiveMissionProgress : 0;
        var rect=missionTaskProgress.rectTransform;rect.anchorMax=new Vector2(progress,1);rect.offsetMin=rect.offsetMax=Vector2.zero;
        int remaining=Mathf.CeilToInt(mission.ActiveMissionSecondsRemaining);
        if(lastCardDefinition==selectedMission && lastCardState==state && lastCardStep==mission.ProcedureStepIndex &&
            lastCardSecond==remaining && lastCardFeedback==mission.ProcedureFeedback && lastCardCanStart==canStart) return;
        lastCardDefinition=selectedMission;lastCardState=state;lastCardStep=mission.ProcedureStepIndex;
        lastCardSecond=remaining;lastCardFeedback=mission.ProcedureFeedback;lastCardCanStart=canStart;
        missionCardTitle.text=selectedMission.locationName.ToUpperInvariant()+" CONTROL";
        missionStartButton.transform.parent.gameObject.SetActive(canStart);
        missionSkipButton.transform.parent.gameObject.SetActive(canStart && selectedMission.missionId!=MissionRunPlan.CheckoutId);
        missionStartButton.text=!mission.CanStartAtStation(selectedMission) ? "GO TO STATION" : selectedMission.missionId=="G-01" ? "START SCAN" : "START PROCEDURE";
        missionSkipButton.text="SKIP TASK";
        missionCardBriefing.gameObject.SetActive(!active);
        missionCardBriefing.text=selectedMission.title+"\n\n"+selectedMission.briefing+
            (selectedMission.missionId==MissionRunPlan.CheckoutId ? "\n\nComplete this scan to start the flight event chain. A failed scan can be retried." : "\n\nOperate this equipment in "+selectedMission.locationName+". Timeout or skip: −"+selectedMission.damageOnFailure+" HP.");
        bool procedure=active && selectedMission.objectiveType==ShipMissionObjectiveType.Procedure;
        procedurePanel.SetActive(procedure);
        procedureFeedbackText.text=active ? mission.ProcedureFeedback : "";
        if(procedure)
        {
            var step=mission.CurrentStep;
            procedurePrompt.text=step==null ? "Verification complete." : step.prompt;
            for(int i=0;i<procedureOptions.Length;i++)
            {
                bool show=step!=null && !step.automatic && !step.requiresShelter && i<step.options.Length;
                procedureOptions[i].transform.parent.gameObject.SetActive(show);
                if(show) procedureOptions[i].text=step.options[i];
            }
        }
        missionTaskProgressTrack.SetActive(active || state==ShipMissionStatus.Completed);
        missionCardStatus.text=active
            ? "STEP " + (mission.ProcedureStepIndex+1) + " / " + selectedMission.steps.Length + " · " + Mathf.CeilToInt(mission.ActiveMissionSecondsRemaining) + "s REMAINING" +
                (mission.CurrentStep!=null && mission.CurrentStep.automatic ? " · SCANNING" : mission.CurrentStep!=null && mission.CurrentStep.requiresShelter ? " · CLOSE TO WALK" : "")
            : state==ShipMissionStatus.Completed ? "PROCEDURE COMPLETE"
            : state==ShipMissionStatus.Skipped ? "TASK SKIPPED"
            : state==ShipMissionStatus.Failed ? "PROCEDURE TIMED OUT"
            : state==ShipMissionStatus.Expired ? "FLIGHT ENDED"
            : available && !canStart ? "QUEUED · FINISH THE ACTIVE PROCEDURE FIRST"
            : state==ShipMissionStatus.Unavailable ? "AWAITING MISSION PHASE"
            : "READY TO START";
    }
    private void ChooseProcedureOption(int option)
    {
        if(mission==null || mission.ActiveMission!=selectedMission) return;
        mission.ChooseProcedureOption(option);UpdateMissionCard();
    }
    private void ToggleTaskBoard()
    {
        if(mission==null || gameOver || mission.IsPaused) return;
        bool show=!taskBoard.activeSelf;
        equipmentPanel.SetActive(false);missionCard.SetActive(false);selectedMission=null;
        taskBoard.SetActive(show);missionCardShade.SetActive(show);
        // The board is above the shade; station cards are reopened above both.
        if(show){taskBoard.transform.SetAsLastSibling();lastBoardSecond=-1;missionHintBackdrop.SetActive(false);}
        mission.IsInterfaceOpen=show;
        if(show){controlsIntro.SetActive(false);bridgeSuggestion.SetActive(false);stormSuggestion.SetActive(false);UpdateTaskBoard();}
        else UpdateControlsIntro(0);
    }
    private void UpdateTaskBoard()
    {
        int second=Mathf.FloorToInt(mission.MissionSeconds);
        if(second==lastBoardSecond && lastBoardActive==mission.ActiveMission) return;
        lastBoardSecond=second;lastBoardActive=mission.ActiveMission;
        for(int i=0;i<taskRows.Length;i++)
        {
            bool visible=i<mission.RunPlan.Count;
            taskRows[i].transform.parent.gameObject.SetActive(visible);
            if(!visible) continue;
            var definition=mission.RunPlan[i].definition;taskRowDefinitions[i]=definition;
            var state=mission.GetMissionStatus(definition);
            bool released=state!=ShipMissionStatus.Unavailable;
            taskRows[i].text=released ? definition.title + " · " + (state==ShipMissionStatus.Available && mission.ActiveMission!=null ? "Queued" : state.ToString())
                : "Awaiting mission phase";
            taskRows[i].transform.parent.GetComponent<Button>().interactable=released;
        }
    }
    private void OpenTaskRow(int row)
    {
        var definition=taskRowDefinitions[row];
        if(definition==null || mission.GetMissionStatus(definition)==ShipMissionStatus.Unavailable) return;
        taskBoard.SetActive(false);missionCard.transform.SetAsLastSibling();
        OpenMissionCard(definition);
    }
    private void ShowFlightNotice(string text)
    {
        if(mission==null || gameOver)return;
        shipNoticeText.text=text;shipNoticeRemaining=9;
    }
    private void OnMissionAvailable(ShipMissionDefinition definition)
    {
        if(mission==null)return;
        ShowFlightNotice(definition.missionId==MissionRunPlan.CheckoutId ? "FIRST OBJECTIVE: BRIDGE SYSTEMS CHECK\nPress M or use the Bridge console to start scanning." :
            "NEW EVENT: "+definition.title+"\nGo to "+definition.locationName+" · Press M to track the task.");
        lastBoardSecond=-1;
    }
    private void UpdateFlightNotice()
    {
        bool visible=shipNoticeRemaining>0 && mission.RadiationLevel<=0 && !mission.IsPaused && !gameOver && !missionCard.activeSelf && !equipmentPanel.activeSelf && !taskBoard.activeSelf && !stormSuggestion.activeSelf && !controlsIntro.activeSelf;
        if(shipNotice.activeSelf!=visible)shipNotice.SetActive(visible);
        if(visible)shipNoticeRemaining=Mathf.Max(0,shipNoticeRemaining-Time.deltaTime);
    }
    private void UpdateEquipmentInterface()
    {
        var element=mission.NearbyEquipment;
        bool visible=element!=null && !mission.IsPaused && !gameOver && !equipmentPanel.activeSelf && !missionCard.activeSelf && !taskBoard.activeSelf && !controlsIntro.activeSelf;
        nearbyEquipmentButton.transform.parent.gameObject.SetActive(visible);
        if(visible)nearbyEquipmentButton.text="E · "+element.title;
        int second=Mathf.FloorToInt(mission.MissionSeconds+Time.unscaledTime);
        if(equipmentPanel.activeSelf && second!=lastEquipmentSecond){lastEquipmentSecond=second;RefreshEquipment();}
    }
    private void OpenNearbyEquipment(){if(mission!=null && mission.NearbyEquipment!=null)mission.InteractEquipment(mission.NearbyEquipment.id);}
    private void OpenEquipment(ShipEquipment.Element element)
    {
        if(gameOver || mission==null)return;
        selectedEquipment=element;selectedMission=null;
        missionCard.SetActive(false);taskBoard.SetActive(false);controlsIntro.SetActive(false);stormSuggestion.SetActive(false);bridgeSuggestion.SetActive(false);shipNotice.SetActive(false);missionHintBackdrop.SetActive(false);
        equipmentPanel.SetActive(true);equipmentPanel.transform.SetAsLastSibling();missionCardShade.SetActive(true);mission.IsInterfaceOpen=true;
        RefreshEquipment();
    }
    private void RefreshEquipment()
    {
        if(selectedEquipment==null)return;
        equipmentTitle.text=selectedEquipment.title.ToUpperInvariant();equipmentReadout.text=mission.Equipment.Status(selectedEquipment);
        var actions=mission.Equipment.Actions(selectedEquipment);for(int i=0;i<2;i++)equipmentActions[i].text=actions[i];
    }
    private void OperateSelectedEquipment(int action)
    {
        if(selectedEquipment==null)return;
        bool ladder=selectedEquipment.kind=="ladder" && action==0;
        mission.OperateEquipment(selectedEquipment.id,action);
        if(ladder){CloseEquipment();return;}RefreshEquipment();
    }
    private void OpenEquipmentTask()
    {
        if(selectedEquipment==null)return;
        var definition=mission.ActiveMission!=null && mission.ActiveMission.stationId==selectedEquipment.station ? mission.ActiveMission :
            mission.RunPlan.Select(x=>x.definition).FirstOrDefault(x=>x.stationId==selectedEquipment.station && mission.GetMissionStatus(x)==ShipMissionStatus.Available);
        if(definition!=null){equipmentPanel.SetActive(false);missionCard.transform.SetAsLastSibling();OpenMissionCard(definition);}
        else{CloseEquipment();ToggleTaskBoard();}
    }
    private void CloseEquipment()
    {
        equipmentPanel.SetActive(false);missionCardShade.SetActive(false);selectedEquipment=null;
        if(mission!=null){mission.IsInterfaceOpen=false;UpdateControlsIntro(0);}
    }
    private void StartSelectedMission()
    {
        if(mission!=null && !mission.CanStartAtStation(selectedMission))
        {mission.TrackStation(selectedMission);CloseMissionCard();return;}
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
            if(!gameOver) mission.SetPaused(false);
            if(!gameOver) UpdateMissionCard();
        }
    }
    private void CloseMissionCard()
    {
        missionCard.SetActive(false);
        missionCardShade.SetActive(false);
        selectedMission=null;mission.IsInterfaceOpen=false;
        pauseScreen.SetActive(false);
        if(EventSystem.current!=null) EventSystem.current.SetSelectedGameObject(null);
        if(mission!=null){mission.SetPaused(false);UpdateControlsIntro(0);UpdateStormSuggestion();}
    }
    private void ReturnToMenu()
    {
        mission.SetPaused(false);
        pauseScreen.SetActive(false);
        BindHealth(null);
        Destroy(mission.gameObject); mission = null;
        missionHud.SetActive(false);
        controlsIntroRemaining=0;controlsIntro.SetActive(false);
        ShowVideoInMenu();
        shade.gameObject.SetActive(true); backgroundCanvas.gameObject.SetActive(true);
        ShowRoles(true);
    }
    private void OnDestroy()
    {
        BindHealth(null);
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
        var button = image.gameObject.AddComponent<Button>(); button.transition=Selectable.Transition.None; button.onClick.AddListener(action);
        var colors = button.colors; colors.highlightedColor = new Color(.6f,1,1); colors.selectedColor = colors.highlightedColor; button.colors = colors;
        return Label(image.transform, value, .03f,.05f,.97f,.95f, 23, Color.white);
    }
}
