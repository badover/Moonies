using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

/// <summary>Two-screen entry flow with persistent crew selection and ship boarding.</summary>
public sealed class CrewMenu : MonoBehaviour
{
    [System.Serializable] private class Timing { public float[] durations; }
    public static CrewRole SelectedRole => (CrewRole)Mathf.Clamp(PlayerPrefs.GetInt("CrewRole", 0), 0, 3);
    private readonly string[] names = { "Scientist", "Medic", "Pilot", "Engineer" };
    private readonly string[] descriptions = { "Coordinate your crew\nand lead the mission.", "Monitor crew health\nand provide medical care.", "Control the flight\nand navigate to the Moon.", "Maintain ship systems\nand repair equipment." };
    private readonly string[] assets = { "Commander", "Medic", "Pilot", "Engineer" };
    private readonly Color cyan = new Color(.25f, .86f, .94f);
    private GameObject home, roles, missionHud;
    private ShipMission mission;
    private Canvas backgroundCanvas;
    private Image shade;
    private RawImage stars;
    private Image[] cards = new Image[4];
    private Text status, confirmText;
    private Font font;
    private int selected, frame;
    private float elapsed;
    private float[] durations;

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
        stars.texture = Resources.Load<Texture2D>("Menu/Starfield");
        stars.raycastTarget = false;
        durations = JsonUtility.FromJson<Timing>(Resources.Load<TextAsset>("Menu/StarfieldTiming").text).durations;
        SetFrame();
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
        roles = Node("Role Selection", root.transform, Vector2.zero, Vector2.one).gameObject;
        var roleBounds = Node("Role Artwork Bounds", roles.transform, new Vector2(.04f,.04f), new Vector2(.96f,.96f));
        var roleArt = Node("After Start Artwork", roleBounds, Vector2.zero, Vector2.one).gameObject.AddComponent<RawImage>();
        roleArt.texture = Resources.Load<Texture2D>("Menu/AfterStart");
        roleArt.raycastTarget = false;
        var roleFit = roleArt.gameObject.AddComponent<AspectRatioFitter>();
        roleFit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        roleFit.aspectRatio = (float)roleArt.texture.width/roleArt.texture.height;
        RoleArea(roleArt.transform,2,new Vector2(.098f,.402f),new Vector2(.484f,.548f));
        RoleArea(roleArt.transform,3,new Vector2(.516f,.402f),new Vector2(.905f,.548f));
        RoleArea(roleArt.transform,0,new Vector2(.098f,.241f),new Vector2(.484f,.385f));
        RoleArea(roleArt.transform,1,new Vector2(.516f,.241f),new Vector2(.905f,.385f));
        status = Label(roleArt.transform,"",.2f,.195f,.8f,.238f,18,cyan);
        Button(roleArt.transform,"BACK",.065f,.085f,.23f,.155f,()=>ShowRoles(false));
        confirmText = Button(roleArt.transform,"BOARD SHIP",.38f,.09f,.62f,.185f,Confirm);
        missionHud = Node("Mission HUD", root.transform, Vector2.zero, Vector2.one).gameObject;
        Label(missionHud.transform, "A / D  MOVE    ·    W / S  LADDER    ·    SCROLL  ZOOM    ·    ESC  CREW MENU", .05f,.02f,.95f,.09f, 18, cyan);
        missionHud.SetActive(false);
        roles.SetActive(false);
        Select(selected);
    }

    private void Update()
    {
        if (durations == null || mission != null) return;
        int previous = frame;
        elapsed += Time.unscaledDeltaTime;
        while (elapsed >= durations[frame]) { elapsed -= durations[frame]; frame = (frame + 1) % durations.Length; }
        if (previous != frame) SetFrame();
    }
    private void SetFrame() => stars.uvRect = new Rect((frame % 10) / 10f, 1f - (frame / 10 + 1) / 8f, .1f, .125f);
    private void ShowRoles(bool show) { home.SetActive(!show); roles.SetActive(show); }
    private void Select(int index)
    {
        selected = index;
        for (int i = 0; i < cards.Length; i++) cards[i].color = i == index ? new Color(.15f,.8f,1,.22f) : Color.clear;
        status.text = "Selected: " + names[index];
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
            candidate.Begin(prefab, ReturnToMenu);
            mission = candidate;
            home.SetActive(false); roles.SetActive(false);
            shade.gameObject.SetActive(false); backgroundCanvas.gameObject.SetActive(false);
            missionHud.SetActive(true);
        }
        catch (System.Exception error)
        {
            if (candidate != null) Destroy(candidate.gameObject);
            status.text = "Unable to board: " + error.Message;
            Debug.LogException(error);
        }
    }
    private void RoleArea(Transform parent,int role,Vector2 min,Vector2 max)
    {
        var image=Panel(parent,"Choose "+names[role],min,max,Color.clear);
        cards[role]=image;
        var button=image.gameObject.AddComponent<Button>();
        button.onClick.AddListener(()=>Select(role));
    }
    private void ReturnToMenu()
    {
        Destroy(mission.gameObject); mission = null;
        missionHud.SetActive(false);
        shade.gameObject.SetActive(true); backgroundCanvas.gameObject.SetActive(true);
        ShowRoles(true);
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
