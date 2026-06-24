using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Owns all object pools and handles spawning floating word objects.
/// 
/// Spawn rules:
///   - Picks a random FloatingObjectType (weighted by spawnWeight in config)
///   - Gets a free object from that type's pool
///   - Assigns it a word from the current round's word list
///   - Places it at a random X along the bottom of the spawn area
/// 
/// GameManager tells SpawnManager what words to use — SpawnManager
/// doesn't know or care about game rules.
/// </summary>
public class SpawnManager : MonoBehaviour
{
    public static SpawnManager Instance { get; private set; }

    [Header("Config")]
    [SerializeField] private List<FloatingObjectConfigSO> _typeConfigs;
    [SerializeField] private RectTransform _spawnArea;       // The canvas area objects float in
    [SerializeField] private Transform _poolRoot;            // Inactive pool objects live here

    // ─────────────────────────────────────────────
    // Runtime
    // ─────────────────────────────────────────────
    private readonly Dictionary<FloatingObjectType, ObjectPool> _pools
        = new Dictionary<FloatingObjectType, ObjectPool>();

    private readonly Dictionary<FloatingObjectType, FloatingObjectConfigSO> _configMap
        = new Dictionary<FloatingObjectType, FloatingObjectConfigSO>();

    // All currently active floating objects (for shake control)
    private readonly List<FloatingObject> _activeObjects = new List<FloatingObject>();

    private List<string> _wordQueue = new List<string>();
    private int _wordQueueIndex;
    private Coroutine _spawnRoutine;

    // Callback assigned by GameManager
    private System.Action<FloatingObject, bool> _tapCallback;

    // ─────────────────────────────────────────────
    // Boundary helpers
    // ─────────────────────────────────────────────
    private float SpawnY => -_spawnArea.rect.height * 0.5f - 50f; // Just below bottom
    private float TopBoundaryY => _spawnArea.rect.height * 0.5f + 100f; // Just above top
    private float SpawnXMin => -_spawnArea.rect.width * 0.5f + 80f;
    private float SpawnXMax => _spawnArea.rect.width * 0.5f - 80f;

    // ─────────────────────────────────────────────
    // Lifecycle
    // ─────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        BuildPools();
    }

    private void BuildPools()
    {
        foreach (var cfg in _typeConfigs)
        {
            var pool = new ObjectPool(cfg.prefab, cfg.poolSize, _poolRoot);
            _pools[cfg.objectType] = pool;
            _configMap[cfg.objectType] = cfg;
        }
    }

    // ─────────────────────────────────────────────
    // Public API — called by GameManager
    // ─────────────────────────────────────────────

    public void StartSpawning(
        List<string> words,
        LevelDataSO levelData,
        System.Action<FloatingObject, bool> tapCallback)
    {
        _wordQueue = new List<string>(words);
        _wordQueueIndex = 0;
        _tapCallback = tapCallback;

        StopSpawning();
        _spawnRoutine = StartCoroutine(SpawnRoutine(levelData));
    }

    public void StopSpawning()
    {
        if (_spawnRoutine != null)
        {
            StopCoroutine(_spawnRoutine);
            _spawnRoutine = null;
        }
    }

    public void ReturnToPool(FloatingObject obj)
    {
        _activeObjects.Remove(obj);
        var pool = _pools[obj.ObjectType];
        pool.Return(obj.gameObject);
    }

    /// <summary>Called by GameManager when word audio starts — shakes matching object.</summary>
    public void NotifyWordAudioStarted(string word)
    {
        foreach (var obj in _activeObjects)
        {
            if (string.Equals(obj.Word, word, System.StringComparison.OrdinalIgnoreCase))
                obj.StartShake();
        }
    }

    /// <summary>Called by GameManager when word audio ends — stops shake.</summary>
    public void NotifyWordAudioStopped(string word)
    {
        foreach (var obj in _activeObjects)
        {
            if (string.Equals(obj.Word, word, System.StringComparison.OrdinalIgnoreCase))
                obj.StopShake();
        }
    }

    /// <summary>Returns all active objects (for InputHandler raycasting).</summary>
    public IReadOnlyList<FloatingObject> ActiveObjects => _activeObjects;

    // ─────────────────────────────────────────────
    // Spawn coroutine
    // ─────────────────────────────────────────────
    private IEnumerator SpawnRoutine(LevelDataSO levelData)
    {
        // Spawn all words at once, distributed across screen height
        int count = Mathf.Min(_wordQueue.Count, levelData.wordsPerRound);
        
        for (int i = 0; i < count; i++)
        {
            SpawnOne(levelData, i, count);
            yield return new WaitForSeconds(0.3f); // tiny stagger, not 2.5s
        }
    }

    private void SpawnOne(LevelDataSO levelData, int index, int total)
    {
        if (_wordQueueIndex >= _wordQueue.Count) return;

        string word = _wordQueue[_wordQueueIndex++];
        var type = PickWeightedType();
        var cfg = _configMap[type];
        var pool = _pools[type];

        // Divide screen into vertical slots so objects spread out
        float screenHeight = _spawnArea.rect.height;
        float slotHeight = screenHeight / total;
        float slotCenterY = -screenHeight * 0.5f + (slotHeight * index) + (slotHeight * 0.5f);
        
        // Random X, but Y is slotted to prevent vertical crowding
        Vector2 spawnPos = Vector2.zero;
        bool foundPos = false;

        for (int attempt = 0; attempt < 8; attempt++)
        {
            float randomX = Random.Range(SpawnXMin, SpawnXMax);
            float randomY = slotCenterY + Random.Range(-slotHeight * 0.3f, slotHeight * 0.3f);
            spawnPos = new Vector2(randomX, randomY);

            bool tooClose = false;
            foreach (var active in _activeObjects)
            {
                var activeRect = active.GetComponent<RectTransform>();
                if (activeRect == null) continue;
                if (Vector2.Distance(activeRect.anchoredPosition, spawnPos) < 220f)
                {
                    tooClose = true;
                    break;
                }
            }

            if (!tooClose) { foundPos = true; break; }
        }

        var worldPos = _spawnArea.TransformPoint(new Vector3(spawnPos.x, spawnPos.y, 0f));
        var go = pool.Get(worldPos, _spawnArea);
        var floatingObj = go.GetComponent<FloatingObject>();

        // Slower speed so they don't rush off screen
        float speed = Random.Range(levelData.floatSpeedMin, levelData.floatSpeedMax);
        floatingObj.Init(word, cfg, speed, TopBoundaryY, _tapCallback);
        _activeObjects.Add(floatingObj);
    }

    private void SpawnOne(LevelDataSO levelData)
    {
        if (_wordQueueIndex >= _wordQueue.Count) return;

        string word = _wordQueue[_wordQueueIndex++];
        var type = PickWeightedType();
        var cfg = _configMap[type];
        var pool = _pools[type];

        // Try up to 5 positions, pick one that's not too close to existing objects
        Vector2 spawnPos = Vector2.zero;
        bool foundGoodPos = false;

        for (int attempt = 0; attempt < 5; attempt++)
        {
            float spawnX = Random.Range(SpawnXMin, SpawnXMax);
            spawnPos = new Vector2(spawnX, SpawnY);

            bool tooClose = false;
            foreach (var active in _activeObjects)
            {
                var activeRect = active.GetComponent<RectTransform>();
                if (activeRect == null) continue;
                float dist = Vector2.Distance(
                    activeRect.anchoredPosition, spawnPos);
                if (dist < 180f) // Minimum gap between objects
                {
                    tooClose = true;
                    break;
                }
            }

            if (!tooClose) { foundGoodPos = true; break; }
        }

        var worldPos = _spawnArea.TransformPoint(
            new Vector3(spawnPos.x, spawnPos.y, 0f));
        var go = pool.Get(worldPos, _spawnArea);
        var floatingObj = go.GetComponent<FloatingObject>();

        float speed = Random.Range(levelData.floatSpeedMin, levelData.floatSpeedMax);
        floatingObj.Init(word, cfg, speed, TopBoundaryY, _tapCallback);
        _activeObjects.Add(floatingObj);
    }

    private FloatingObjectType PickWeightedType()
    {
        int totalWeight = 0;
        foreach (var cfg in _typeConfigs)
            totalWeight += cfg.spawnWeight;

        int roll = Random.Range(0, totalWeight);
        int cumulative = 0;

        foreach (var cfg in _typeConfigs)
        {
            cumulative += cfg.spawnWeight;
            if (roll < cumulative)
                return cfg.objectType;
        }

        return _typeConfigs[0].objectType;
    }
}