using UnityEngine;

public enum FloatingObjectType
{
    Star,
    Cloud,
    Bubble
}

/// <summary>
/// Per-type visual/audio config. Create one asset per type (Star, Cloud, Bubble).
/// Drag into SpawnManager's type config list.
/// </summary>
[CreateAssetMenu(fileName = "FloatingObjectConfig", menuName = "SightWordPop/Floating Object Config")]
public class FloatingObjectConfigSO : ScriptableObject
{
    [Header("Identity")]
    public FloatingObjectType objectType;
    public GameObject prefab;              // The full prefab for this type
    public int poolSize = 10;

    [Header("Shake / Wiggle (while audio plays)")]
    public ShakeStyle shakeStyle;
    public float shakeIntensity = 8f;      // Degrees or pixels depending on style
    public float shakeSpeed = 6f;          // Oscillations per second

    [Header("Pop VFX")]
    public GameObject popParticlePrefab;   // Instantiated at position on correct tap
    public Color popColor = Color.white;

    [Header("Pop SFX")]
    public AudioClip correctPopSFX;        // e.g. star twinkle burst
    public AudioClip wrongTapSFX;          // e.g. dull thud

    [Header("Spawn Weight")]
    [Range(1, 10)]
    public int spawnWeight = 3;            // Higher = spawns more often
}

public enum ShakeStyle
{
    SpinWobble,      // Star: rotate oscillation + scale pulse
    SideSway,        // Cloud: gentle left-right translation
    SquishStretch    // Bubble: Y scale squish + X scale stretch
}