using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Video;
using System;
using System.Collections.Generic;
using System.Linq;

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
    private Sprite targetMarkerSprite;
    private GameObject targetMarker;
    private bool ready;
    private bool ladderNeedsRelease;
    private float runningTimeScale=1;
    private float missionClock;
    private float activeMissionElapsed;
    private ShipMissionDefinition[] missionDefinitions;
    private MissionRunPlan.Entry[] runPlan;
    private int procedureStepIndex;
    private float procedureStepElapsed;
    private string procedureFeedback;
    public int RunSeed { get; private set; }
    public bool RunFinished { get; private set; }
    public bool IsInterfaceOpen { get; set; }
    public float MissionSeconds => missionClock;
    public System.Collections.Generic.IReadOnlyList<MissionRunPlan.Entry> RunPlan => runPlan;
    public string PhaseLabel => MissionRunPlan.PhaseNames[MissionRunPlan.PhaseAt(missionClock)];
    public event Action RunEnded;
    public int ProcedureStepIndex => procedureStepIndex;
    public string ProcedureFeedback => procedureFeedback;
    public MissionProcedureStep CurrentStep => activeMission != null && activeMission.steps != null && procedureStepIndex < activeMission.steps.Length ? activeMission.steps[procedureStepIndex] : null;
    public float ProcedureStepProgress => CurrentStep == null ? 0 : CurrentStep.durationSeconds <= 0 ? 1 : Mathf.Clamp01(procedureStepElapsed / CurrentStep.durationSeconds);
    private ShipMissionDefinition nearbyMission;
    private ShipMissionDefinition activeMission;
    private readonly Dictionary<ShipMissionDefinition, ShipMissionStatus> missionStatuses = new Dictionary<ShipMissionDefinition, ShipMissionStatus>();
    public float RadiationLevel { get; private set; }
    public bool IsPaused {get;private set;}
    public CrewHealth Health {get;private set;}
    public event Action<ShipMissionDefinition> MissionInteractionRequested;
    public event Action<ShipMissionDefinition, bool, bool> MissionResolved;
    public ShipMissionDefinition NearbyMission => nearbyMission;
    public ShipMissionDefinition ActiveMission => activeMission;
    public float ActiveMissionProgress => activeMission == null ? 0 : activeMission.objectiveType == ShipMissionObjectiveType.Procedure
        ? (procedureStepIndex + (CurrentStep != null && CurrentStep.automatic ? ProcedureStepProgress : 0)) / Mathf.Max(1, activeMission.steps.Length)
        : Mathf.Clamp01(activeMissionElapsed / activeMission.durationSeconds);
    public float ActiveMissionSecondsRemaining => activeMission == null ? 0 : Mathf.Max(0, activeMission.durationSeconds - activeMissionElapsed);
    public bool IsNearActiveTarget => activeMission != null && (activeMission.objectiveType == ShipMissionObjectiveType.ReachTarget || CurrentStep != null && CurrentStep.requiresShelter) && IsAtTarget(activeMission);

    public void Begin(CrewCharacter prefab, System.Action pauseRequested, VideoPlayer videoBackground)
    {
        onPauseRequested = pauseRequested;
        missionDefinitions = Resources.LoadAll<ShipMissionDefinition>("Missions")
            .Where(definition => !definition.startsQuestionCampaign).OrderBy(definition => definition.availableUntilSeconds).ToArray();
        RunSeed = UnityEngine.Random.Range(0, int.MaxValue);
        runPlan = MissionRunPlan.Create(missionDefinitions, RunSeed);
        missionDefinitions = runPlan.Select(x => x.definition).ToArray();
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
        Health=player.GetComponent<CrewHealth>();
        if(Health==null) Health=player.gameObject.AddComponent<CrewHealth>();
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
        if (IsPaused || RunFinished) return;
        float delta = Mathf.Min(Time.deltaTime, MissionRunPlan.RunSeconds - missionClock);
        missionClock += delta;
        if(IsPaused) return;
        float x = keys == null ? 0 : (keys.dKey.isPressed || keys.rightArrowKey.isPressed ? 1 : 0) - (keys.aKey.isPressed || keys.leftArrowKey.isPressed ? 1 : 0);
        float y = keys == null ? 0 : (keys.wKey.isPressed || keys.upArrowKey.isPressed ? 1 : 0) - (keys.sKey.isPressed || keys.downArrowKey.isPressed ? 1 : 0);
        if (!IsInterfaceOpen) MoveCrew(x, y, delta);
        UpdateActiveMission(delta);
        if (IsPaused) return;
        UpdateNearbyMission();
        if (missionClock >= MissionRunPlan.RunSeconds) { FinishRun(); return; }
        if (!IsInterfaceOpen && keys != null && keys.eKey.wasPressedThisFrame) TryInteract();
        if (!IsInterfaceOpen && Mouse.current != null) zoom = ShipLayout.ZoomAfterScroll(zoom, Mouse.current.scroll.ReadValue().y);
    }
    public bool TryInteract()
    {
        if(!ready || IsPaused) return false;
        if (IsNearActiveTarget)
        {
            if (activeMission.objectiveType == ShipMissionObjectiveType.Procedure) AdvanceProcedure();
            else ResolveActiveMission(true);
            return true;
        }
        UpdateNearbyMission();
        if(nearbyMission==null) return false;
        MissionInteractionRequested?.Invoke(nearbyMission);
        return true;
    }
    public ShipMissionStatus GetMissionStatus(ShipMissionDefinition definition)
    {
        if (definition == null || runPlan == null) return ShipMissionStatus.Unavailable;
        MissionRunPlan.Entry entry=null;
        for(int i=0;i<runPlan.Length;i++) if(runPlan[i].definition==definition){entry=runPlan[i];break;}
        if (entry == null) return ShipMissionStatus.Unavailable;
        if (missionStatuses.TryGetValue(definition, out var status)) return status;
        if (RunFinished) return ShipMissionStatus.Expired;
        return missionClock < entry.trigger ? ShipMissionStatus.Unavailable : ShipMissionStatus.Available;
    }
    public bool StartMission(ShipMissionDefinition definition)
    {
        if (!ready || definition == null || activeMission != null || RunFinished || GetMissionStatus(definition) != ShipMissionStatus.Available)
            return false;
        if (definition.missionId=="G-02") IncreaseRadiation(35);
        activeMission = definition;
        activeMissionElapsed = 0;
        procedureStepIndex = 0; procedureStepElapsed = 0; procedureFeedback = "";
        missionStatuses[definition] = ShipMissionStatus.InProgress;
        if (definition.objectiveType == ShipMissionObjectiveType.ReachTarget || definition.steps.Any(x => x.requiresShelter))
        {
            targetMarker = new GameObject("Mission target — " + definition.missionId, typeof(SpriteRenderer));
            targetMarker.transform.SetParent(transform, false);
            targetMarker.transform.position = (Vector3)definition.TargetWorldPosition + Vector3.up * 1.9f;
            targetMarker.transform.localScale = Vector3.one * .18f;
            targetMarker.transform.rotation = Quaternion.Euler(0, 0, 45);
            var markerRenderer = targetMarker.GetComponent<SpriteRenderer>();
            if (targetMarkerSprite == null)
                targetMarkerSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), new Vector2(.5f, .5f), 1);
            markerRenderer.sprite = targetMarkerSprite;
            markerRenderer.sharedMaterial = Resources.Load<Material>("ShipSprite");
            markerRenderer.color = new Color(1f, .78f, .2f, .9f);
            markerRenderer.sortingOrder = 20;
        }
        return true;
    }
    public bool SkipMission(ShipMissionDefinition definition)
    {
        if (!ready || definition == null || activeMission != null || RunFinished || GetMissionStatus(definition) != ShipMissionStatus.Available)
            return false;
        missionStatuses[definition] = ShipMissionStatus.Skipped;
        IncreaseRadiation(definition.radiationIncreaseOnSkip + (definition.missionId=="G-02" ? 35 : 0));
        ResolveFailure(definition);
        UpdateNearbyMission();
        return true;
    }
    private void UpdateActiveMission(float delta)
    {
        if (activeMission == null || IsPaused || delta <= 0) return;
        activeMissionElapsed += delta;
        if (activeMission.objectiveType == ShipMissionObjectiveType.Procedure)
        {
            // Consume only the portion of this frame before the active-time deadline.
            var step=CurrentStep;
            if (step != null && step.automatic)
            {
                procedureStepElapsed += Mathf.Min(delta, Mathf.Max(0, activeMission.durationSeconds - (activeMissionElapsed-delta)));
                if (procedureStepElapsed >= step.durationSeconds) AdvanceProcedure();
            }
            if (activeMission != null && activeMissionElapsed >= activeMission.durationSeconds) ResolveActiveMission(false);
            return;
        }
        if (activeMission.objectiveType == ShipMissionObjectiveType.ReachTarget)
        {
            if (activeMissionElapsed >= activeMission.durationSeconds) ResolveActiveMission(false);
            return;
        }
        // A launched scan keeps running after CLOSE and while walking away from its station.
        if (activeMissionElapsed >= activeMission.durationSeconds) ResolveActiveMission(true);
    }
    public bool ChooseProcedureOption(int index)
    {
        if (IsPaused || RunFinished || activeMission == null || CurrentStep == null || CurrentStep.automatic || CurrentStep.requiresShelter) return false;
        var step=CurrentStep;
        if (index < 0 || index >= step.options.Length) return false;
        if (index != step.correctChoice)
        {
            procedureFeedback="UNSAFE COMMAND REJECTED — review the indicators and try again.";
            return false;
        }
        AdvanceProcedure(); return true;
    }
    private void AdvanceProcedure()
    {
        procedureFeedback=CurrentStep == null ? "" : CurrentStep.result;
        procedureStepIndex++; procedureStepElapsed=0;
        if (targetMarker != null && (CurrentStep == null || !CurrentStep.requiresShelter)) targetMarker.SetActive(false);
        if (activeMission != null && procedureStepIndex >= activeMission.steps.Length) ResolveActiveMission(true);
    }
    private void FinishRun()
    {
        if (RunFinished) return;
        RunFinished=true;
        if (activeMission != null) ResolveActiveMission(false);
        foreach (var definition in missionDefinitions)
            if (!missionStatuses.ContainsKey(definition)) missionStatuses[definition]=ShipMissionStatus.Expired;
        SetPaused(true); RunEnded?.Invoke();
    }
    private void ResolveFailure(ShipMissionDefinition definition)
    {
        bool lethalExposure=IsFatalFailure(definition);
        Health.TakeDamage(lethalExposure ? Health.Current : definition.damageOnFailure);
        MissionResolved?.Invoke(definition, false, Health.Current<=0);
    }
    private void IncreaseRadiation(float amount)
    {
        RadiationLevel = Mathf.Clamp(RadiationLevel + amount, 0, 100);
    }
    private bool IsFatalFailure(ShipMissionDefinition definition)
    {
        return definition.failureIsFatalAtHighRadiation && RadiationLevel >= definition.fatalRadiationThreshold;
    }
    private bool IsAtTarget(ShipMissionDefinition definition)
    {
        return player != null && deck == definition.targetDeck &&
            Vector2.Distance(player.transform.position, definition.TargetWorldPosition) <= definition.interactionRadius;
    }
    private void ResolveActiveMission(bool success)
    {
        ShipMissionDefinition resolved = activeMission;
        if (resolved == null) return;
        if (success)
        {
            activeMissionElapsed = resolved.durationSeconds;
            missionStatuses[resolved] = ShipMissionStatus.Completed;
        }
        else
        {
            missionStatuses[resolved] = ShipMissionStatus.Failed;
        }
        activeMission = null;
        if (targetMarker != null) Destroy(targetMarker);
        targetMarker = null;
        if (targetMarkerSprite != null) { Destroy(targetMarkerSprite); targetMarkerSprite = null; }
        if (resolved.missionId == "G-02") IncreaseRadiation(success ? -RadiationLevel : 25);
        if(success) MissionResolved?.Invoke(resolved, true, false);
        else ResolveFailure(resolved);
        UpdateNearbyMission();
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
        int bestPriority = -1;
        foreach (var definition in missionDefinitions)
        {
            if (definition == null || !definition.AllowsRole(player.Role)) continue;
            var status = GetMissionStatus(definition);
            // Viewing the card is independent from whether its check can still be started.
            if (status == ShipMissionStatus.Unavailable) continue;
            if (!IsNearMission(definition)) continue;
            float distance = Mathf.Abs(player.transform.position.x - definition.WorldPosition.x);
            int priority = status == ShipMissionStatus.Available ? 1 : 0;
            if (priority > bestPriority || priority == bestPriority && distance < closest)
            { bestPriority=priority; closest=distance; nearbyMission=definition; }
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
        if (targetMarkerSprite != null) Destroy(targetMarkerSprite);

    }
}
