using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages object pooling and batch spawning for Sight Word Pop.
/// Spawns floating word objects in staggered batches from below the
/// screen. Enforces a minimum 2D distance between every spawned object
/// at spawn time so objects never visually overlap as they float up.
/// </summary>
public class SpawnManager : MonoBehaviour
{
    #region Fields

    public static SpawnManager Instance { get; private set; }

    [Header("Config")]
    [SerializeField] private List<FloatingObjectConfigSO> _typeConfigs;
    [SerializeField] private RectTransform _spawnArea;
    [SerializeField] private Transform _poolRoot;

    [Header("Batch Spawn Settings")]
    [SerializeField] private int _batchSize = 3;
    [SerializeField] private float _delayBetweenBatches = 4f;
    [SerializeField] private float _delayInsideBatch = 2f;

    [Header("Spawn Spacing")]
    [SerializeField] private float _minSpawnDistance = 280f;
    [SerializeField] private int _maxSpawnPushAttempts = 20;

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

    /// <summary>
    /// Y position just below the visible screen boundary in local space.
    /// Read at spawn time — never cached — so layout is always settled first.
    /// </summary>
    private float SpawnBelowScreenY => -(_spawnArea.rect.height * 0.5f) - 200f;

    /// <summary>
    /// Y position just above the visible screen boundary in local space.
    /// Read at spawn time — never cached — so layout is always settled first.
    /// </summary>
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
    /// Begins the batch spawn loop for the given word list.
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

        _spawnRoutine = StartCoroutine(BatchSpawnRoutine());
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
    /// Called when word audio starts playing.
    /// </summary>
    public void NotifyWordAudioStarted(string word)
    {
        foreach (var obj in _activeObjects)
        {
            if (string.Equals(obj.Word, word, System.StringComparison.OrdinalIgnoreCase))
            {
                obj.StartShake();
            }
        }
    }

    /// <summary>
    /// Stops the shake animation on any active object matching the word.
    /// Called when word audio finishes.
    /// </summary>
    public void NotifyWordAudioStopped(string word)
    {
        foreach (var obj in _activeObjects)
        {
            if (string.Equals(obj.Word, word, System.StringComparison.OrdinalIgnoreCase))
            {
                obj.StopShake();
            }
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

    private IEnumerator BatchSpawnRoutine()
    {
        while (_spawnArea.rect.height <= 0f) yield return null;
        yield return new WaitForSeconds(0.5f);

        while (_wordQueueIndex < _wordQueue.Count)
        {
            int spawned = 0;
            // One speed for the whole batch — objects in the same batch
            // must travel at identical speed or they converge and overlap.
            float batchSpeed = Random.Range(_levelData.floatSpeedMin, _levelData.floatSpeedMax);

            while (spawned < _batchSize && _wordQueueIndex < _wordQueue.Count)
            {
                SpawnOne(batchSpeed);
                spawned++;

                if (spawned < _batchSize && _wordQueueIndex < _wordQueue.Count)
                    yield return new WaitForSeconds(_delayInsideBatch);
            }

            if (_wordQueueIndex < _wordQueue.Count)
                yield return new WaitForSeconds(_delayBetweenBatches);
        }
    }

    private void SpawnOne(float speed)
    {
        if (_wordQueueIndex >= _wordQueue.Count) return;

        string word = _wordQueue[_wordQueueIndex++];

        var type = PickWeightedType();
        var cfg = _configMap[type];
        var pool = _pools[type];

        Vector2 spawnLocalPos = FindClearSpawnPosition();

        GameObject go = pool.Get(_spawnArea.TransformPoint(spawnLocalPos), _spawnArea);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = spawnLocalPos;

        FloatingObject floatingObj = go.GetComponent<FloatingObject>();
        floatingObj.Init(word, cfg, speed, TopBoundaryY, _tapCallback);
        _activeObjects.Add(floatingObj);
    }

    private void SpawnOne()
    {
        if (_wordQueueIndex >= _wordQueue.Count)
            return;

        string word = _wordQueue[_wordQueueIndex++];

        var type = PickWeightedType();
        var cfg = _configMap[type];
        var pool = _pools[type];

        Vector2 spawnLocalPos = FindClearSpawnPosition();

        GameObject go = pool.Get(_spawnArea.TransformPoint(spawnLocalPos), _spawnArea);

        // anchoredPosition must be set explicitly AFTER reparenting.
        // ObjectPool.Get() sets transform.position (world space), but
        // FloatingObject.Update() moves and reads anchoredPosition (local space).
        // If anchors are not centered, these two coordinate systems diverge —
        // the object appears correct for one frame then jumps to the wrong spot.
        // Setting anchoredPosition directly here guarantees both systems agree.
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = spawnLocalPos;

        FloatingObject floatingObj = go.GetComponent<FloatingObject>();

        float speed = Random.Range(_levelData.floatSpeedMin, _levelData.floatSpeedMax);

        floatingObj.Init(word, cfg, speed, TopBoundaryY, _tapCallback);
        _activeObjects.Add(floatingObj);
    }

    /// <summary>
    /// Finds a spawn position in local space below the screen that is at
    /// least _minSpawnDistance away from every currently active object.
    /// Pushes the candidate further down on each failed attempt.
    /// Falls back to the last candidate if max attempts are exceeded
    /// to guarantee the loop always terminates.
    /// </summary>
    private Vector2 FindClearSpawnPosition()
    {
        float spawnX = GetRandomLaneX();
        float spawnY = SpawnBelowScreenY - Random.Range(0f, _minSpawnDistance);

        for (int attempt = 0; attempt < _maxSpawnPushAttempts; attempt++)
        {
            Vector2 candidate = new Vector2(spawnX, spawnY);

            if (IsClearOfAllActiveObjects(candidate))
            {
                return candidate;
            }

            spawnY -= _minSpawnDistance;
            spawnX = GetRandomLaneX();
        }

        Debug.LogWarning("[SpawnManager] Could not find clear spawn position " +
                         "after max attempts. Spawning at fallback position.");

        return new Vector2(spawnX, spawnY);
    }

    /// <summary>
    /// Returns true if the candidate local-space position is at least
    /// _minSpawnDistance away from every active object's current
    /// anchoredPosition.
    /// </summary>
    private bool IsClearOfAllActiveObjects(Vector2 candidateLocalPos)
    {
        foreach (FloatingObject obj in _activeObjects)
        {
            if (obj == null) continue;

            RectTransform rt = obj.GetComponent<RectTransform>();
            if (rt == null) continue;

            float distance = Vector2.Distance(candidateLocalPos, rt.anchoredPosition);

            if (distance < _minSpawnDistance)
            {
                return false;
            }
        }

        return true;
    }

    private float GetRandomLaneX()
    {
        float width = _spawnArea.rect.width;

        float[] lanes = new float[]
        {
            -width * 0.35f,
            0f,
            width * 0.35f
        };

        return lanes[Random.Range(0, lanes.Length)];
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