using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Side-view walking constrained to the hull, deck floors and connecting ladders.</summary>
public sealed class ShipMission : MonoBehaviour
{
    private CrewCharacter player;
    private Camera view;
    private System.Action onExit;
    private SpriteRenderer background;
    private Sprite[] frames;
    private float[] durations;
    private int frame;
    private float clock;
    private int deck = 1;
    private int activeLadder = -1;
    private Vector3 velocity;
    private float zoom = 4.5f;
    private float zoomVelocity;
    private Sprite shipSprite;
    private bool ready;

    public void Begin(CrewCharacter prefab, System.Action exit)
    {
        onExit = exit;
        if (prefab == null) throw new System.InvalidOperationException("Selected crew prefab is missing.");
        var texture = Resources.Load<Texture2D>("Menu/ModernSpacecraft");
        var atlas = Resources.Load<Texture2D>("Menu/Starfield");
        var material = Resources.Load<Material>("ShipSprite");
        if (texture == null || atlas == null || material == null)
            throw new System.InvalidOperationException("Ship assets are missing. Wait for Unity to finish importing.");
        view = Camera.main;
        if (view == null) throw new System.InvalidOperationException("Main Camera is missing.");
        view.orthographic = true;
        view.enabled = true;
        view.cullingMask = -1;
        view.rect = new Rect(0,0,1,1);
        view.transform.rotation = Quaternion.identity;
        view.orthographicSize = zoom;
        view.backgroundColor = Color.black;
        QualitySettings.vSyncCount = 1;
        Application.targetFrameRate = 60;
        shipSprite = Sprite.Create(texture, new Rect(0,0,texture.width,texture.height), new Vector2(.5f,.5f), ShipLayout.PixelsPerUnit, 0, SpriteMeshType.FullRect);
        var ship = new GameObject("Modern spacecraft — three decks", typeof(SpriteRenderer));
        ship.transform.SetParent(transform, false);
        var renderer = ship.GetComponent<SpriteRenderer>();
        renderer.sprite = shipSprite;
        renderer.sharedMaterial = material;
        player = Instantiate(prefab, new Vector3(-9, ShipLayout.FloorY(deck, -9), -1), Quaternion.identity, transform);
        player.transform.localScale = Vector3.one * ShipLayout.CrewScale;
        player.UseInteriorAnimations();
        var playerRenderer = player.GetComponent<SpriteRenderer>();
        playerRenderer.sharedMaterial = Resources.Load<Material>("CrewInterior");
        playerRenderer.sortingOrder = 10;
        playerRenderer.color = Color.white;
        player.GetComponent<Animator>().cullingMode = AnimatorCullingMode.AlwaysAnimate;

        if (playerRenderer.sprite == null)
            throw new System.InvalidOperationException("Crew animation has missing sprite references. Rebuild Animated Characters from Tools/Crew.");
        var sky = new GameObject("Starfield", typeof(SpriteRenderer));
        sky.transform.SetParent(transform, false);
        background = sky.GetComponent<SpriteRenderer>();
        background.sharedMaterial = renderer.sharedMaterial;
        background.sortingOrder = -20;
        durations = JsonUtility.FromJson<FrameTiming>(Resources.Load<TextAsset>("Menu/StarfieldTiming").text).durations;
        frames = new Sprite[durations.Length];
        int w = atlas.width / 10, h = atlas.height / 8;
        for (int i = 0; i < frames.Length; i++)
            frames[i] = Sprite.Create(atlas, new Rect(i % 10 * w, (7-i/10)*h, w,h), new Vector2(.5f,.5f), 100);
        background.sprite = frames[0];
        view.transform.position = new Vector3(player.transform.position.x, 0, -10);
        ready = true;
    }
    [System.Serializable] private class FrameTiming { public float[] durations; }
    private void Update()
    {
        if (!ready || player == null) return;
        var keys = Keyboard.current;
        if (keys != null && keys.escapeKey.wasPressedThisFrame) { onExit(); return; }
        float x = keys == null ? 0 : (keys.dKey.isPressed || keys.rightArrowKey.isPressed ? 1 : 0) - (keys.aKey.isPressed || keys.leftArrowKey.isPressed ? 1 : 0);
        float y = keys == null ? 0 : (keys.wKey.isPressed || keys.upArrowKey.isPressed ? 1 : 0) - (keys.sKey.isPressed || keys.downArrowKey.isPressed ? 1 : 0);
        MoveCrew(x, y, Time.deltaTime);
        if (Mouse.current != null) zoom = ShipLayout.ZoomAfterScroll(zoom, Mouse.current.scroll.ReadValue().y);
        clock += Time.deltaTime;
        int previous = frame;
        while (clock >= durations[frame]) { clock -= durations[frame]; frame = (frame+1)%frames.Length; }
        if (previous != frame) background.sprite = frames[frame];
    }
    /// <summary>Move on deck floors or between the two endpoints of one ladder; never enter propulsion machinery or leave the hull.</summary>
    public void MoveCrew(float x, float y, float delta)
    {
        if (!ready) return;
        Vector3 p = player.transform.position;
        if (activeLadder < 0 && y != 0 && ShipLayout.FindLadder(deck, p.x, y, out int found))
        {
            activeLadder = found;
            p.x = ShipLayout.Ladders[found].x;
        }
        if (activeLadder >= 0)
        {
            var ladder = ShipLayout.Ladders[activeLadder];
            float bottom = ShipLayout.FloorY(ladder.bottom, p.x);
            float top = ShipLayout.FloorY(ladder.top, p.x);
            p.y = Mathf.Clamp(p.y + y * 1.65f * delta, bottom, top);
            player.SetClimbing(true, y);
            if (y > 0 && p.y >= top || y < 0 && p.y <= bottom)
            {
                deck = y > 0 ? ladder.top : ladder.bottom;
                activeLadder = -1;
                player.SetClimbing(false, 0);
            }
        }
        else
        {
            float next = ShipLayout.MoveOnDeck(deck, p.x, p.x + x * 2.8f * delta);
            player.SetWalking(Mathf.Abs(next-p.x) > .0001f);
            if (x != 0) player.SetFacingLeft(x < 0);
            p.x = next;
            p.y = ShipLayout.FloorY(deck, p.x);
        }
        player.transform.position = p;
    }
    private void LateUpdate()
    {
        if (!ready || player == null) return;
        view.orthographicSize = Mathf.SmoothDamp(view.orthographicSize, zoom, ref zoomVelocity, .09f, Mathf.Infinity, Time.unscaledDeltaTime);
        float halfWidth = view.orthographicSize * view.aspect;
        float limit = Mathf.Max(0, 20-halfWidth);
        Vector3 target = new Vector3(Mathf.Clamp(player.transform.position.x, -limit,limit), player.transform.position.y*.3f, -10);
        view.transform.position = Vector3.SmoothDamp(view.transform.position, target, ref velocity, .09f, Mathf.Infinity, Time.unscaledDeltaTime);
        background.transform.position = new Vector3(view.transform.position.x, view.transform.position.y, 10);
        background.transform.localScale = new Vector3(2*view.orthographicSize*view.aspect/background.sprite.bounds.size.x, 2*view.orthographicSize/background.sprite.bounds.size.y, 1);
    }
    private void OnDestroy()
    {
        if (frames != null) foreach (var sprite in frames) Destroy(sprite);
        if (shipSprite != null) Destroy(shipSprite);

    }
}
