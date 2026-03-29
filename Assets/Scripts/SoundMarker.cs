using UnityEngine;

/// <summary>
/// Spawns a small sphere at a world position that renders through walls and fades out.
/// </summary>
public class SoundMarker : MonoBehaviour
{
    private Material _material;
    private float _elapsed;
    private float _duration;

    public static void Spawn(Vector3 position, float duration, Material material)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.transform.position = position;
        go.transform.localScale = Vector3.one * 0.2f;

        // Remove collider — purely visual
        Destroy(go.GetComponent<Collider>());

        SoundMarker marker = go.AddComponent<SoundMarker>();
        marker._duration = duration;
        marker._material = new Material(material);
        go.GetComponent<MeshRenderer>().material = marker._material;
    }

    private void Update()
    {
        _elapsed += Time.deltaTime;
        float alpha = Mathf.Clamp01(1f - (_elapsed / _duration));
        _material.SetFloat("_Alpha", alpha);

        if (_elapsed >= _duration)
            Destroy(gameObject);
    }
}
