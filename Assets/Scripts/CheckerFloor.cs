using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class CheckerFloor : MonoBehaviour
{
    [SerializeField] private Color colorA = Color.white;
    [SerializeField] private Color colorB = new Color(0.25f, 0.25f, 0.25f);
    [SerializeField] private float tiling = 20f;
    [SerializeField] private Material baseMaterial;

    void Start()
    {
        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGB24, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.SetPixel(0, 0, colorA);
        tex.SetPixel(1, 0, colorB);
        tex.SetPixel(0, 1, colorB);
        tex.SetPixel(1, 1, colorA);
        tex.Apply();

        Material mat = baseMaterial != null ? new Material(baseMaterial) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.SetTexture("_BaseMap", tex);
        mat.SetTextureScale("_BaseMap", new Vector2(tiling, tiling));

        GetComponent<MeshRenderer>().material = mat;
    }
}
