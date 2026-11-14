using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Video;
using System;
using System.Collections.Generic;

/// <summary>Side-view walking constrained to the hull, deck floors and connecting ladders.</summary>
public sealed class ShipMission : MonoBehaviour
{
    private CrewCharacter player;
    private Camera view;
    private System.Action onPauseRequested;
    private int deck = 1;
    private int activeLadder = -1;
    private Vector3 velocity;
    private float zoom = 4.5f;
    private float zoomVelocity;
    private Sprite shipSprite;
    private bool ready;
    private bool ladderNeedsRelease;
    private float runningTimeScale=1;
    private float missionClock;
    private float activeMissionElapsed;
    private ShipMissionDefinition[] missionDefinitions;
    private ShipMissionDefinition nearbyMission;
    private ShipMissionDefinition activeMission;
    private readonly Dictionary<ShipMissionDefinition, ShipMissionStatus> missionStatuses = new Dictionary<ShipMissionDefinition, ShipMissionStatus>();
    public bool IsPaused {get;private set;}
    public event Action<ShipMissionDefinition> MissionInteractionRequested;
    public ShipMissionDefinition NearbyMission => nearbyMission;
    public ShipMissionDefinition ActiveMission => activeMission;
    public float ActiveMissionProgress => activeMission == null ? 0 : Mathf.Clamp01(activeMissionElapsed / activeMission.durationSeconds);

    public void Begin(CrewCharacter prefab, System.Action pauseRequested, VideoPlayer videoBackground)
    {
        onPauseRequested = pauseRequested;
        missionDefinitions = Resources.LoadAll<ShipMissionDefinition>("Missions");
        if (prefab == null) throw new System.InvalidOperationException("Selected crew prefab is missing.");
        var texture = Resources.Load<Texture2D>("Menu/ModernSpacecraft");
        var material = Resources.Load<Material>("ShipSprite");
        if (texture == null || material == null || videoBackground == null)
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
        view.transform.position = new Vector3(player.transform.position.x, 0, -10);
        ready = true;
    }
    private void Update()
    {
        if (!ready || player == null) return;
        var keys = Keyboard.current;
        if (keys != null && keys.escapeKey.wasPressedThisFrame) { onPauseRequested(); return; }
        if (IsPaused) return;
        float delta = Time.deltaTime;
        missionClock += delta;
        float x = keys == null ? 0 : (keys.dKey.isPressed || keys.rightArrowKey.isPressed ? 1 : 0) - (keys.aKey.isPressed || keys.leftArrowKey.isPressed ? 1 : 0);
        float y = keys == null ? 0 : (keys.wKey.isPressed || keys.upArrowKey.isPressed ? 1 : 0) - (keys.sKey.isPressed || keys.downArrowKey.isPressed ? 1 : 0);
        MoveCrew(x, y, delta);
        UpdateActiveMission(delta);
        UpdateNearbyMission();
        if (keys != null && keys.eKey.wasPressedThisFrame && nearbyMission != null)
            MissionInteractionRequested?.Invoke(nearbyMission);
        if (Mouse.current != null) zoom = ShipLayout.ZoomAfterScroll(zoom, Mouse.current.scroll.ReadValue().y);
    }
    public ShipMissionStatus GetMissionStatus(ShipMissionDefinition definition)
    {
        if (definition == null) return ShipMissionStatus.Unavailable;
        if (missionStatuses.TryGetValue(definition, out var status)) return status;
        if (missionClock < definition.availableFromSeconds) return ShipMissionStatus.Unavailable;
        return missionClock <= definition.availableUntilSeconds ? ShipMissionStatus.Available : ShipMissionStatus.Expired;
    }
    public bool StartMission(ShipMissionDefinition definition)
    {
        if (definition == null || activeMission != null || nearbyMission != definition || GetMissionStatus(definition) != ShipMissionStatus.Available)
            return false;
        activeMission = definition;
        activeMissionElapsed = 0;
        missionStatuses[definition] = ShipMissionStatus.InProgress;
        return true;
    }
    public bool SkipMission(ShipMissionDefinition definition)
    {
        if (definition == null || nearbyMission != definition || GetMissionStatus(definition) != ShipMissionStatus.Available)
            return false;
        missionStatuses[definition] = ShipMissionStatus.Skipped;
        UpdateNearbyMission();
        return true;
    }
    private void UpdateActiveMission(float delta)
    {
        if (activeMission == null || !IsNearMission(activeMission)) return;
        activeMissionElapsed += delta;
        if (activeMissionElapsed >= activeMission.durationSeconds)
        {
            activeMissionElapsed = activeMission.durationSeconds;
            missionStatuses[activeMission] = ShipMissionStatus.Completed;
            activeMission = null;
        }
    }
    private void UpdateNearbyMission()
    {
        nearbyMission = null;
        if (player == null || missionDefinitions == null) return;
        if (activeMission != null)
        {
            if (IsNearMission(activeMission)) nearbyMission = activeMission;
            return;
        }
        float closest = float.PositiveInfinity;
        foreach (var definition in missionDefinitions)
        {
            if (definition == null || !definition.AllowsRole(player.Role)) continue;
            var status = GetMissionStatus(definition);
            if (status != ShipMissionStatus.Available && status != ShipMissionStatus.InProgress) continue;
            if (!IsNearMission(definition)) continue;
            float distance = Mathf.Abs(player.transform.position.x - definition.WorldPosition.x);
            if (distance < closest) { closest = distance; nearbyMission = definition; }
        }
    }
    private bool IsNearMission(ShipMissionDefinition definition)
    {
        if (definition == null || player == null || deck != definition.deck) return false;
        return Vector2.Distance(player.transform.position, definition.WorldPosition) <= definition.interactionRadius;
    }
    /// <summary>Move on deck floors or between the two endpoints of one ladder; never enter propulsion machinery or leave the hull.</summary>
    public void MoveCrew(float x,float y,float delta)
    {
        if(!ready || IsPaused) return;
        Vector3 p=player.transform.position;
        if(y==0) ladderNeedsRelease=false;
        if(activeLadder<0 && !ladderNeedsRelease && y!=0 && ShipLayout.FindLadder(deck,p.x,y,out int found))
            activeLadder=found;
        if(activeLadder>=0)
        {
            var ladder=ShipLayout.Ladders[activeLadder];
            float bottom=ShipLayout.FloorY(ladder.bottom,ladder.x),top=ShipLayout.FloorY(ladder.top,ladder.x);
            int landing=-1;
            if(x!=0)
            {
                if(Mathf.Abs(p.y-bottom)<=.35f) landing=ladder.bottom;
                else if(Mathf.Abs(p.y-top)<=.35f) landing=ladder.top;
            }
            if(landing>=0)
            {
                deck=landing;activeLadder=-1;ladderNeedsRelease=y!=0;
                player.SetClimbing(false,0);
                // Step off at walking speed: never teleport across the shaft.
                p.x=ShipLayout.MoveOnDeck(deck,p.x,p.x+x*2.8f*delta);
                p.y=ShipLayout.FloorY(deck,p.x);
                player.SetWalking(true);player.SetFacingLeft(x<0);
            }
            else
            {
                float beforeY=p.y;
                if(y!=0) p.x=Mathf.MoveTowards(p.x,ladder.x,3.5f*delta);
                float requested=p.y;
                if(Mathf.Abs(p.x-ladder.x)<.02f) requested+=y*1.65f*delta;
                // Continue into the next connected segment while W/S remains held.
                // At the middle deck we stay on the ladder until A/D requests an exit.
                for(int step=0;step<ShipLayout.Ladders.Length;step++)
                {
                    ladder=ShipLayout.Ladders[activeLadder];
                    bottom=ShipLayout.FloorY(ladder.bottom,ladder.x);top=ShipLayout.FloorY(ladder.top,ladder.x);
                    int direction=requested>top?1:requested<bottom?-1:0;
                    int next=direction==0?-1:ShipLayout.Continuation(activeLadder,direction);
                    if(next<0) break;
                    deck=direction>0?ladder.top:ladder.bottom;
                    activeLadder=next;
                }
                ladder=ShipLayout.Ladders[activeLadder];
                bottom=ShipLayout.FloorY(ladder.bottom,ladder.x);top=ShipLayout.FloorY(ladder.top,ladder.x);
                p.y=Mathf.Clamp(requested,bottom,top);
                player.SetClimbing(true,Mathf.Abs(p.y-beforeY)>.00001f?Mathf.Sign(p.y-beforeY):0);
            }
        }
        else
        {
            float next=ShipLayout.MoveOnDeck(deck,p.x,p.x+x*2.8f*delta);
            player.SetWalking(Mathf.Abs(next-p.x)>.0001f);
            if(x!=0) player.SetFacingLeft(x<0);
            p.x=next;p.y=ShipLayout.FloorY(deck,p.x);
        }
        player.transform.position=p;
    }
    public void SetPaused(bool value)
    {
        if(value==IsPaused) return;
        if(value){runningTimeScale=Time.timeScale;Time.timeScale=0;}
        else Time.timeScale=runningTimeScale;
        IsPaused=value;
    }
    private void LateUpdate()
    {
        if (!ready || player == null || IsPaused) return;
        view.orthographicSize = Mathf.SmoothDamp(view.orthographicSize, zoom, ref zoomVelocity, .09f, Mathf.Infinity, Time.unscaledDeltaTime);
        float halfWidth = view.orthographicSize * view.aspect;
        float limit = Mathf.Max(0, 20-halfWidth);
        Vector3 target = new Vector3(Mathf.Clamp(player.transform.position.x, -limit,limit), player.transform.position.y*.3f, -10);
        view.transform.position = Vector3.SmoothDamp(view.transform.position, target, ref velocity, .09f, Mathf.Infinity, Time.unscaledDeltaTime);
    }
    private void OnDestroy()
    {
        if(IsPaused) Time.timeScale=runningTimeScale;
        if (shipSprite != null) Destroy(shipSprite);

    }
}
