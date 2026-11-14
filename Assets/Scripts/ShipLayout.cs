using UnityEngine;

/// <summary>Interior traced in source-image pixels (1600 x 533). The tank/service section is outside the playable hull.</summary>
public static class ShipLayout
{
    public const float PixelsPerUnit = 40;
    public const float BodyHalfWidth = .43f;
    public const float RightWallPixel = 1050;
    public const float CrewScale = .64f;
    private static readonly float[] FloorPixels = { 400, 302, 213 };
    private static readonly float[] LeftPixels = { 270, 95, 275 };
    public struct Ladder
    {
        public float x;
        public int bottom, top;
        public Ladder(float pixelX, int bottom, int top) { x = ToWorldX(pixelX); this.bottom = bottom; this.top = top; }
    }
    public static readonly Ladder[] Ladders = {
        new Ladder(350, 1, 2),
        new Ladder(435, 0, 1), new Ladder(1018, 0, 1)
    };
    public static float ToWorldX(float x) => (x-800)/PixelsPerUnit;
    public static float FloorY(int deck, float worldX)
    {
        float pixelX = worldX*PixelsPerUnit+800;
        // The cockpit floor rises towards the nose; feet follow the inside of the hull.
        float pixelY = deck == 1 && pixelX < 235 ? Mathf.Lerp(272,302,Mathf.InverseLerp(95,235,pixelX)) : FloorPixels[deck];
        return (266.5f-pixelY)/PixelsPerUnit;
    }
    public static float ClampX(int deck, float x) => Mathf.Clamp(x, ToWorldX(LeftPixels[deck])+BodyHalfWidth, ToWorldX(RightWallPixel)-BodyHalfWidth);
    public static bool FindLadder(int deck, float x, float direction, out int index)
    {
        index = -1; float closest = .5f;
        for (int i=0; i<Ladders.Length; i++)
        {
            var ladder = Ladders[i];
            bool connects = direction > 0 ? deck == ladder.bottom : deck == ladder.top;
            float distance = Mathf.Abs(x-ladder.x);
            if (connects && distance < closest) { index=i; closest=distance; }
        }
        return index >= 0;
    }
    public static float ZoomAfterScroll(float size, float scroll)
    {
        // Input System backends report either notches (~1) or wheel units (~120).
        // One full notch changes magnification by 22%; fractional trackpad input stays smooth.
        float notches = Mathf.Abs(scroll) > 10 ? scroll / 120 : scroll;
        return Mathf.Clamp(size*Mathf.Pow(.78f,notches), 2.5f, 12);
    }
}
