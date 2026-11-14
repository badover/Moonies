using UnityEditor;
using UnityEngine;
using System.IO;

/// <summary>Record each frame's foot baseline during import, rather than guessing it at runtime.</summary>
public sealed class InteriorCrewImporter : AssetPostprocessor
{
    [System.Serializable] public class Layout { public float ppu; public int[] bottoms; }
    private bool IsCrew => assetPath.StartsWith("Assets/Resources/InteriorCrew/") && assetPath.EndsWith(".png");
    private void OnPreprocessTexture()
    {
        if(!IsCrew) return;
        var importer=(TextureImporter)assetImporter;
        importer.textureType=TextureImporterType.Default;
        importer.textureShape=TextureImporterShape.Texture2D;
        importer.spriteImportMode=SpriteImportMode.None;
        importer.npotScale=TextureImporterNPOTScale.None;
        importer.maxTextureSize=4096;
        importer.textureCompression=TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled=true;
        importer.filterMode=FilterMode.Trilinear;
        importer.wrapMode=TextureWrapMode.Clamp;
        importer.alphaIsTransparency=true;
    }
    private void OnPostprocessTexture(Texture2D texture)
    {
        if(!IsCrew) return;
        var pixels=texture.GetPixels32();
        var layout=new Layout {bottoms=new int[12]};
        float heightSum=0;
        for(int frame=0;frame<12;frame++)
        {
            int row=frame/4,col=frame%4;
            int x0=Mathf.RoundToInt(col*texture.width/4f),x1=Mathf.RoundToInt((col+1)*texture.width/4f);
            int y0=Mathf.RoundToInt((2-row)*texture.height/3f),y1=Mathf.RoundToInt((3-row)*texture.height/3f);
            int bottom=y1,top=y0;
            for(int y=y0;y<y1;y++) for(int x=x0;x<x1;x++)
                if(pixels[y*texture.width+x].a>80){bottom=Mathf.Min(bottom,y);top=Mathf.Max(top,y);}
            if(bottom>top) throw new System.InvalidOperationException("Empty crew animation cell: "+assetPath+" "+frame);
            layout.bottoms[frame]=bottom-y0;
            if(frame<4) heightSum+=top-bottom+1;
        }
        layout.ppu=(heightSum/4)/2.2f;
        string path=Path.ChangeExtension(assetPath,".json");
        string json=JsonUtility.ToJson(layout);
        if(!File.Exists(path)||File.ReadAllText(path)!=json) File.WriteAllText(path,json);
    }
}
