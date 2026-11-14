using UnityEngine;

public enum CrewRole { Commander, Medic, Pilot, Engineer }

/// <summary>Visual character only. Movement and role abilities are intentionally separate.</summary>
[RequireComponent(typeof(Animator), typeof(SpriteRenderer))]
public sealed class CrewCharacter : MonoBehaviour
{
    [SerializeField] private CrewRole role = CrewRole.Commander;
    private static readonly int Walking = Animator.StringToHash("Walking");

    public CrewRole Role => role;

    public void SetWalking(bool walking)
    {
        GetComponent<Animator>().SetBool(Walking, walking);
    }

    public void SetFacingLeft(bool facingLeft)
    {
        GetComponent<SpriteRenderer>().flipX = facingLeft;
    }
}
