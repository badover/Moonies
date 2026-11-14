using UnityEngine;

public enum CrewRole { Commander, Medic, Pilot, Engineer }

/// <summary>Visual character only. Movement and role abilities are intentionally separate.</summary>
[RequireComponent(typeof(Animator), typeof(SpriteRenderer))]
public sealed class CrewCharacter : MonoBehaviour
{
    [SerializeField] private CrewRole role = CrewRole.Commander;
    private static readonly int Walking = Animator.StringToHash("Walking");

    private Animator animator;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private Sprite[] climbFrames;
    private float climbClock, climbDirection;
    public bool IsClimbing { get; private set; }
    public CrewRole Role => role;

    public void SetClimbing(bool climbing, float direction)
    {
        if (climbing && climbFrames == null)
        {
            var sheet = Resources.Load<Texture2D>("CrewClimb");
            if (sheet == null) throw new System.InvalidOperationException("CrewClimb sheet is missing.");
            float width = sheet.width/4f, height = sheet.height/4f;
            climbFrames = new Sprite[4];
            for (int i=0; i<4; i++)
                climbFrames[i] = Sprite.Create(sheet, new Rect(i*width,(3-(int)role)*height,width,height), new Vector2(.5f,.06f),height/2.55f);
        }
        if (climbing != IsClimbing)
        {
            IsClimbing = climbing;
            animator.enabled = !climbing;
            if (!climbing) { animator.SetBool(Walking,false); animator.Play("Idle",0,0); animator.Update(0); }
            else spriteRenderer.flipX = false;
        }
        climbDirection = direction;
        if (climbing) UpdateClimbSprite();
    }

    private void LateUpdate()
    {
        if (!IsClimbing) return;
        climbClock = Mathf.Repeat(climbClock + Time.deltaTime*6*climbDirection,4);
        UpdateClimbSprite();
    }
    private void UpdateClimbSprite() => spriteRenderer.sprite = climbFrames[Mathf.FloorToInt(climbClock)%4];
    private void OnDestroy()
    {
        if (climbFrames != null) foreach (var frame in climbFrames) Destroy(frame);
    }

    public void SetWalking(bool walking)
    {
        if (!IsClimbing) animator.SetBool(Walking, walking);
    }

    public void SetFacingLeft(bool facingLeft)
    {
        spriteRenderer.flipX = facingLeft;
    }
}
