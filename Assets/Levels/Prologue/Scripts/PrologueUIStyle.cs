using UnityEngine;
using UnityEngine.UI;

public sealed class PrologueUIStyle : MonoBehaviour
{
    [SerializeField] private Image[] surfaces;
    private Sprite rounded;
    private Texture2D ownedTexture;
    void Awake() { foreach(var image in surfaces) if(image!=null) Round(image); }
    void OnDestroy() { if(rounded!=null) Destroy(rounded); if(ownedTexture!=null) Destroy(ownedTexture); }
    private void Round(Image image)
    {
        if(rounded==null)
        {
            const int size=64; const float radius=14;
            var texture=new Texture2D(size,size,TextureFormat.RGBA32,false);
            ownedTexture=texture;
            texture.name="PrologueRoundedCardRuntime"; texture.wrapMode=TextureWrapMode.Clamp;
            texture.filterMode=FilterMode.Bilinear;
            var pixels=new Color32[size*size];
            for(int y=0;y<size;y++) for(int x=0;x<size;x++)
            {
                float dx=Mathf.Max(Mathf.Abs(x+.5f-size*.5f)-(size*.5f-radius),0);
                float dy=Mathf.Max(Mathf.Abs(y+.5f-size*.5f)-(size*.5f-radius),0);
                float alpha=Mathf.Clamp01(radius-Mathf.Sqrt(dx*dx+dy*dy)+.5f);
                pixels[y*size+x]=new Color32(255,255,255,(byte)Mathf.RoundToInt(alpha*255));
            }
            texture.SetPixels32(pixels); texture.Apply(false,true);
            rounded=Sprite.Create(texture,new Rect(0,0,size,size),new Vector2(.5f,.5f),100,0,
                SpriteMeshType.FullRect,new Vector4(16,16,16,16));
            rounded.name="PrologueRoundedCardRuntime";
        }
        image.sprite=rounded; image.type=Image.Type.Sliced;
    }
}
