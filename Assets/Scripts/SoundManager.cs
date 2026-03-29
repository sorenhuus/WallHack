using UnityEngine;

/// <summary>
/// Singleton that plays sounds at a world position.
/// Attach to a persistent GameObject in the scene (e.g. NetworkManager).
/// </summary>
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [SerializeField] private AudioClip[] footstepClips;

    [Header("Footstep Rolloff")]
    [SerializeField] private float minDistance = 5f;   // full volume within this range
    [SerializeField] private float maxDistance = 30f;  // inaudible beyond this range
    [SerializeField] private AudioRolloffMode rolloffMode = AudioRolloffMode.Linear;

    private int _lastFootstepIndex = -1;

    private void Awake()
    {
        Instance = this;
    }

    public void PlayFootstepAt(Vector3 position)
    {
        if (footstepClips == null || footstepClips.Length == 0) return;

        int index;
        do { index = Random.Range(0, footstepClips.Length); }
        while (index == _lastFootstepIndex && footstepClips.Length > 1);
        _lastFootstepIndex = index;

        AudioClip clip = footstepClips[index];

        GameObject go = new GameObject("Footstep");
        go.transform.position = position;

        AudioSource source = go.AddComponent<AudioSource>();
        source.clip = clip;
        source.spatialBlend = 1f;
        source.rolloffMode = rolloffMode;
        source.minDistance = minDistance;
        source.maxDistance = maxDistance;
        source.Play();

        Destroy(go, clip.length);
    }
}