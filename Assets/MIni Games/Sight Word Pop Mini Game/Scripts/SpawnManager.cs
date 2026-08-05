using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages object pooling and spawning for Sight Word Pop. Spawns one
/// object at a time on a short continuous tick, at a fully random
/// position along the spawn width, rejected and skipped for that tick
/// if too close to any currently active object. All objects in a round
/// share one speed — this is what keeps spacing constant after spawn;
/// without it, faster objects drift into slower ones mid-flight even
/// when spawn positions were correctly separated.
/// </summary>
public class SpawnManager : MonoBehaviour
{
    #region Fields

    public static SpawnManager Instance { get; private set; }

    [Header("Config")]
    [SerializeField] private List<FloatingObjectConfigSO> _typeConfigs;
    [SerializeField] private RectTransform _spawnArea;
    [SerializeField] private Transform _poolRoot;

    [Header("Spawn Pacing")]
    [Tooltip("How often a new spawn attempt is made.")]
    [SerializeField] private float _spawnTickInterval = 0.6f;

    [Header("Spacing")]
    [Tooltip("Minimum distance between any two objects at spawn time.")]
    [SerializeField] private float _minSpawnDistance = 350f;

    private readonly Dictionary<FloatingObjectType, ObjectPool> _pools
        = new Dictionary<FloatingObjectType, ObjectPool>();

    private readonly Dictionary<FloatingObjectType, FloatingObjectConfigSO> _configMap
        = new Dictionary<FloatingObjectType, FloatingObjectConfigSO>();

    private readonly List<FloatingObject> _activeObjects = new List<FloatingObject>();

    private List<string> _wordQueue = new List<string>();
    private int _wordQueueIndex;
    private Coroutine _spawnRoutine;
    private System.Action<FloatingObject, bool> _tapCallback;
    private LevelDataSO _levelData;

    #endregion

    #region Properties

    public IReadOnlyList<FloatingObject> ActiveObjects => _activeObjects;

    private float SpawnBelowScreenY => -(_spawnArea.rect.height * 0.5f) - 200f;
    private float TopBoundaryY => (_spawnArea.rect.height * 0.5f) + 200f;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        BuildPools();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Begins the spawn loop for the given word list.
    /// Safe to call mid-round — stops any running routine first.
    /// </summary>
    public void StartSpawning(
        List<string> words,
        LevelDataSO levelData,
        System.Action<FloatingObject, bool> tapCallback)
    {
        _wordQueue = new List<string>(words);
        _wordQueueIndex = 0;
        _tapCallback = tapCallback;
        _levelData = levelData;

        StopSpawning();
        _spawnRoutine = StartCoroutine(SpawnRoutine());
    }

    /// <summary>
    /// Stops the spawn loop immediately. Active objects remain until
    /// they float off screen or are tapped.
    /// </summary>
    public void StopSpawning()
    {
        if (_spawnRoutine != null)
        {
            StopCoroutine(_spawnRoutine);
            _spawnRoutine = null;
        }
    }

    /// <summary>
    /// Returns a floating object to its pool. Called by FloatingObject
    /// when it floats off screen or is tapped.
    /// </summary>
    public void ReturnToPool(FloatingObject obj)
    {
        _activeObjects.Remove(obj);

        if (_pools.TryGetValue(obj.ObjectType, out var pool))
        {
            pool.Return(obj.gameObject);
        }
    }

    /// <summary>
    /// Triggers the shake animation on any active object matching the word.
    /// </summary>
    public void NotifyWordAudioStarted(string word)
    {
        foreach (var obj in _activeObjects)
        {
            if (string.Equals(obj.Word, word, System.StringComparison.OrdinalIgnoreCase))
                obj.StartShake();
        }
    }

    /// <summary>
    /// Stops the shake animation on any active object matching the word.
    /// </summary>
    public void NotifyWordAudioStopped(string word)
    {
        foreach (var obj in _activeObjects)
        {
            if (string.Equals(obj.Word, word, System.StringComparison.OrdinalIgnoreCase))
                obj.StopShake();
        }
    }

    #endregion

    #region Private Methods

    private void BuildPools()
    {
        foreach (var cfg in _typeConfigs)
        {
            var pool = new ObjectPool(cfg.prefab, cfg.poolSize, _poolRoot);
            _pools[cfg.objectType] = pool;
            _configMap[cfg.objectType] = cfg;
        }
    }

    private IEnumerator SpawnRoutine()
    {
        while (_spawnArea.rect.height <= 0f)
            yield return null;

        yield return new WaitForSeconds(0.5f);

        // One speed for the entire round — not per-object. If objects
        // moved at different speeds, spacing established at spawn time
        // would drift as faster objects caught up to slower ones,
        // causing mid-flight overlap even when spawn positions were
        // correctly separated.
        float roundSpeed = Random.Range(_levelData.floatSpeedMin, _levelData.floatSpeedMax);

        while (_wordQueueIndex < _wordQueue.Count)
        {
            if (TryFindFreePosition(out Vector2 position))
            {
                SpawnOne(position, roundSpeed);
            }
            // If no free position this tick, simply skip — the next
            // tick will try again once something has risen clear.

            yield return new WaitForSeconds(_spawnTickInterval);
        }
    }

    private void SpawnOne(Vector2 localPos, float speed)
    {
        if (_wordQueueIndex >= _wordQueue.Count) return;

        string word = _wordQueue[_wordQueueIndex++];

        var type = PickWeightedType();
        var cfg = _configMap[type];
        var pool = _pools[type];

        GameObject go = pool.Get(_spawnArea.TransformPoint(
            new Vector3(localPos.x, localPos.y, 0f)), _spawnArea);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = localPos;

        FloatingObject floatingObj = go.GetComponent<FloatingObject>();
        floatingObj.Init(word, cfg, speed, TopBoundaryY, _tapCallback);
        _activeObjects.Add(floatingObj);
    }

    /// <summary>
    /// Attempts to find a single random X position along the spawn
    /// width that is at least _minSpawnDistance away from every
    /// active object currently on screen.
    /// </summary>
    private bool TryFindFreePosition(out Vector2 result)
    {
        float halfWidth = _spawnArea.rect.width * 0.5f;
        float usableHalfWidth = halfWidth * 0.8f; // keep off the hard edges

        const int maxAttempts = 8;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            float candidateX = Random.Range(-usableHalfWidth, usableHalfWidth);
            float candidateY = SpawnBelowScreenY;
            Vector2 candidate = new Vector2(candidateX, candidateY);

            if (IsFarEnoughFromActive(candidate))
            {
                result = candidate;
                return true;
            }
        }

        result = Vector2.zero;
        return false;
    }

    private bool IsFarEnoughFromActive(Vector2 candidate)
    {
        foreach (FloatingObject obj in _activeObjects)
        {
            if (obj == null) continue;

            RectTransform rt = obj.GetComponent<RectTransform>();
            if (rt == null) continue;

            if (Vector2.Distance(candidate, rt.anchoredPosition) < _minSpawnDistance)
                return false;
        }

        return true;
    }

    private FloatingObjectType PickWeightedType()
    {
        int totalWeight = 0;
        foreach (var cfg in _typeConfigs) totalWeight += cfg.spawnWeight;

        int roll = Random.Range(0, totalWeight);
        int cumulative = 0;

        foreach (var cfg in _typeConfigs)
        {
            cumulative += cfg.spawnWeight;
            if (roll < cumulative) return cfg.objectType;
        }

        return _typeConfigs[0].objectType;
    }

    #endregion
}