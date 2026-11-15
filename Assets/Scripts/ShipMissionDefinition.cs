using UnityEngine;

public enum ShipMissionStatus
{
    Unavailable,
    Available,
    InProgress,
    Completed,
    Skipped,
    Expired,
    Failed
}

public enum ShipMissionObjectiveType
{
    TimedCheck,
    ReachTarget
}

/// <summary>Authorable mission content and its ship location. Add assets under Resources/Missions to add missions.</summary>
[CreateAssetMenu(menuName = "Earth Game/Ship Mission", fileName = "NewShipMission")]
public sealed class ShipMissionDefinition : ScriptableObject
{
    [Header("Mission")]
    public string missionId;
    public string title;
    public string timeWindowLabel;
    [Min(0)] public float availableFromSeconds;
    [Min(0)] public float availableUntilSeconds = 80;
    [Min(1)] public float durationSeconds = 30;
    public string difficulty = "Easy";
    [TextArea(3, 6)] public string briefing;

    [Header("Ship interaction")]
    public ShipMissionObjectiveType objectiveType = ShipMissionObjectiveType.TimedCheck;
    public string locationName = "Bridge";
    [Range(0, 2)] public int deck = 2;
    [Min(0)] public float shipPixelX = 380;
    [Min(.1f)] public float interactionRadius = 1.5f;
    [Header("Reach-target objective")]
    [Range(0, 2)] public int targetDeck;
    [Min(0)] public float targetShipPixelX;
    [Header("Radiation consequences")]
    [Range(0, 100)] public float radiationIncreaseOnSkip;
    [Range(0, 100)] public float radiationIncreaseOnExpiry;
    public bool failureIsFatalAtHighRadiation;
    [Range(0, 100)] public float fatalRadiationThreshold = 60;
    [Tooltip("Leave empty to allow every crew role.")]
    public CrewRole[] allowedRoles = new CrewRole[0];

    public bool AllowsRole(CrewRole role)
    {
        if (allowedRoles == null || allowedRoles.Length == 0) return true;
        for (int i = 0; i < allowedRoles.Length; i++)
            if (allowedRoles[i] == role) return true;
        return false;
    }

    public Vector2 WorldPosition
    {
        get
        {
            float x = ShipLayout.ToWorldX(shipPixelX);
            return new Vector2(x, ShipLayout.FloorY(deck, x));
        }
    }

    public Vector2 TargetWorldPosition
    {
        get
        {
            float x = ShipLayout.ToWorldX(targetShipPixelX);
            return new Vector2(x, ShipLayout.FloorY(targetDeck, x));
        }
    }
}
