using UnityEngine;

public enum CrewRole { Commander, Medic, Pilot, Engineer }

[RequireComponent(typeof(Animator), typeof(SpriteRenderer))]
public sealed class CrewCharacter : MonoBehaviour
{
    [SerializeField] private CrewRole role=CrewRole.Commander;
    [System.Serializable] private class Layout { public float ppu; public int[] bottoms; }
    private static readonly string[] Names={"Commander","Medic","Pilot","Engineer"};
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Sprite[] frames;
    private float clock,climbDirection;
    private bool walking;
    public bool IsClimbing {get;private set;}
    public CrewRole Role=>role;
    private void Awake(){animator=GetComponent<Animator>();spriteRenderer=GetComponent<SpriteRenderer>();}
    public void UseInteriorAnimations()
    {
        var sheet=Resources.Load<Texture2D>("InteriorCrew/"+Names[(int)role]);
        var data=Resources.Load<TextAsset>("InteriorCrew/"+Names[(int)role]);
        if(sheet==null||data==null) throw new System.InvalidOperationException("Interior crew animation is missing: "+role);
        var layout=JsonUtility.FromJson<Layout>(data.text);
        frames=new Sprite[12];
        for(int i=0;i<12;i++)
        {
            int row=i/4,col=i%4;
            int x0=Mathf.RoundToInt(col*sheet.width/4f),x1=Mathf.RoundToInt((col+1)*sheet.width/4f);
            int y0=Mathf.RoundToInt((2-row)*sheet.height/3f),y1=Mathf.RoundToInt((3-row)*sheet.height/3f);
            frames[i]=Sprite.Create(sheet,new Rect(x0,y0,x1-x0,y1-y0),new Vector2(.5f,(float)layout.bottoms[i]/(y1-y0)),layout.ppu,0,SpriteMeshType.FullRect);
            frames[i].name=Names[(int)role]+"_"+(row==0?"Idle":row==1?"Walk":"Climb")+"_"+col;
        }
        animator.enabled=false;
        ApplyFrame();
    }
    public void SetClimbing(bool climbing,float direction)
    {
        if(IsClimbing!=climbing) clock=0;
        IsClimbing=climbing;climbDirection=direction;
        if(climbing){walking=false;spriteRenderer.flipX=false;}
        ApplyFrame();
    }
    public void SetWalking(bool value)
    {
        if(walking!=value && !IsClimbing) clock=0;
        walking=value;
        if(frames==null) animator.SetBool("Walking",value);
        else ApplyFrame();
    }
    public void SetFacingLeft(bool value)=>spriteRenderer.flipX=value;
    private void LateUpdate()
    {
        if(frames==null || Time.timeScale==0) return;
        if(!IsClimbing && !walking){clock=0;ApplyFrame();return;}
        float rate=IsClimbing?6*climbDirection:6;
        clock=Mathf.Repeat(clock+Time.deltaTime*rate,4);
        ApplyFrame();
    }
    private void ApplyFrame()
    {
        if(frames!=null) spriteRenderer.sprite=frames[(IsClimbing?8:walking?4:0)+Mathf.FloorToInt(clock)%4];
    }
    private void OnDestroy(){if(frames!=null)foreach(var frame in frames)Destroy(frame);}
}
