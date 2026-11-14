using UnityEngine;

/// <summary>Modern ship collision layout traced on the 2172 x 724 cutaway; floor coordinates refer to the walking surface.</summary>
public static class ShipLayout
{
    public const float PixelsPerUnit=54.3f, BodyHalfWidth=.55f, RightWallPixel=2015, CrewScale=.84f;
    // Doorways between living/work rooms are passages. The propulsion machinery is sealed off.
    private static readonly Vector2[][] Rooms={
        new[]{new Vector2(320,1955)},
        new[]{new Vector2(220,2015)},
        new[]{new Vector2(175,1665)}
    };
    private static readonly Vector2[][] Floors={
        new[]{new Vector2(320,633),new Vector2(640,636),new Vector2(1955,636)},
        new[]{new Vector2(220,475),new Vector2(2015,475)},
        new[]{new Vector2(175,300),new Vector2(1665,300)}
    };
    public struct Ladder
    {
        public float x; public int bottom,top;
        public Ladder(float pixelX,int bottom,int top){x=ToWorldX(pixelX);this.bottom=bottom;this.top=top;}
    }
    public static readonly Ladder[] Ladders={new Ladder(1030,0,1),new Ladder(1030,1,2)};
    public static float ToWorldX(float x) => (x-1086)/PixelsPerUnit;
    public static float FloorY(int deck,float worldX)
    {
        float pixelX=worldX*PixelsPerUnit+1086;
        var points=Floors[deck];float y=points[0].y;
        for(int i=1;i<points.Length;i++)
        {
            y=Mathf.Lerp(points[i-1].y,points[i].y,Mathf.InverseLerp(points[i-1].x,points[i].x,pixelX));
            if(pixelX<=points[i].x) break;
        }
        return (362-y)/PixelsPerUnit;
    }
    public static float MoveOnDeck(int deck,float from,float requested)
    {
        Vector2 room=Rooms[deck][0];float nearest=float.MaxValue;
        foreach(var candidate in Rooms[deck])
        {
            float distance=Mathf.Abs(from-Mathf.Clamp(from,ToWorldX(candidate.x),ToWorldX(candidate.y)));
            if(distance<nearest){nearest=distance;room=candidate;}
        }
        return Mathf.Clamp(requested,ToWorldX(room.x)+BodyHalfWidth,ToWorldX(room.y)-BodyHalfWidth);
    }
    public static float ClampX(int deck,float x) => MoveOnDeck(deck,x,x);
    public static bool FindLadder(int deck,float x,float direction,out int index)
    {
        index=-1;float closest=.5f;
        for(int i=0;i<Ladders.Length;i++)
        {
            var ladder=Ladders[i];bool connects=direction>0?deck==ladder.bottom:deck==ladder.top;
            float distance=Mathf.Abs(x-ladder.x);
            if(connects && distance<closest){index=i;closest=distance;}
        }
        return index>=0;
    }
    public static int Continuation(int index,int direction)
    {
        var current=Ladders[index];
        for(int i=0;i<Ladders.Length;i++)
        {
            if(i==index || Mathf.Abs(Ladders[i].x-current.x)>.01f) continue;
            if(direction>0 && current.top==Ladders[i].bottom || direction<0 && current.bottom==Ladders[i].top) return i;
        }
        return -1;
    }
    public static float ZoomAfterScroll(float size,float scroll)
    {
        float notches=Mathf.Abs(scroll)>10?scroll/120:scroll;
        return Mathf.Clamp(size*Mathf.Pow(.82f,notches),2.5f,12);
    }
}
