using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
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
    [SerializeField] private float _spawnYOffsetVariation = 250f;

    private readonly Dictionary<FloatingObjectType, ObjectPool> _pools
        = new Dictionary<FloatingObjectType, ObjectPool>();

    private readonly Dictionary<FloatingObjectType, FloatingObjectConfigSO> _configMap
        = new Dictionary<FloatingObjectType, FloatingObjectConfigSO>();

    private readonly List<FloatingObject> _activeObjects = new List<FloatingObject>();
    private readonly List<float> _currentBatchXPositions = new List<float>();

    private List<string> _wordQueue = new List<string>();
    private int _wordQueueIndex;
    private Coroutine _spawnRoutine;
    private System.Action<FloatingObject, bool> _tapCallback;
    private LevelDataSO _levelData;

    // ------------------------------------------------
    // Boundaries
    // ------------------------------------------------
    private float SpawnBelowScreenY => -(_spawnArea.rect.height * 0.5f) - 200f;
    private float TopBoundaryY => (_spawnArea.rect.height * 0.5f) + 200f;

    // ------------------------------------------------
    // Dynamic Lanes
    // ------------------------------------------------
    private float[] GetLanes()
    {
        float width = _spawnArea.rect.width;

        return new float[]
        {
            -width * 0.35f,
            0f,
            width * 0.35f
        };
    }

    // ------------------------------------------------
    // Lifecycle
    // ------------------------------------------------
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

    private void BuildPools()
    {
        foreach (var cfg in _typeConfigs)
        {
            var pool = new ObjectPool(cfg.prefab, cfg.poolSize, _poolRoot);

            _pools[cfg.objectType] = pool;
            _configMap[cfg.objectType] = cfg;
        }
    }

    // ------------------------------------------------
    // Public API
    // ------------------------------------------------
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

        if (_pools.TryGetValue(obj.ObjectType, out var pool))
        {
            pool.Return(obj.gameObject);
        }
    }

    public IReadOnlyList<FloatingObject> ActiveObjects => _activeObjects;

    public void NotifyWordAudioStarted(string word)
    {
        foreach (var obj in _activeObjects)
        {
            if (string.Equals(obj.Word, word,
                    System.StringComparison.OrdinalIgnoreCase))
            {
                obj.StartShake();
            }
        }
    }

    public void NotifyWordAudioStopped(string word)
    {
        foreach (var obj in _activeObjects)
        {
            if (string.Equals(obj.Word, word,
                    System.StringComparison.OrdinalIgnoreCase))
            {
                obj.StopShake();
            }
        }
    }

    // ------------------------------------------------
    // Spawn Routine
    // ------------------------------------------------
    private IEnumerator BatchSpawnRoutine()
    {
        yield return new WaitForSeconds(0.5f);

        while (_wordQueueIndex < _wordQueue.Count)
        {
            _currentBatchXPositions.Clear();

            int spawned = 0;

            while (spawned < _batchSize &&
                   _wordQueueIndex < _wordQueue.Count)
            {
                SpawnOne();

                spawned++;

                if (spawned < _batchSize)
                {
                    yield return new WaitForSeconds(_delayInsideBatch);
                }
            }

            if (_wordQueueIndex < _wordQueue.Count)
            {
                yield return new WaitForSeconds(_delayBetweenBatches);
            }
        }
    }

    // ------------------------------------------------
    // Spawn Single Object
    // ------------------------------------------------
    private void SpawnOne()
    {
        if (_wordQueueIndex >= _wordQueue.Count)
            return;

        string word = _wordQueue[_wordQueueIndex++];

        var type = PickWeightedType();
        var cfg = _configMap[type];
        var pool = _pools[type];

        float spawnX = FindGoodXPosition();

        _currentBatchXPositions.Add(spawnX);

        float spawnY =
            SpawnBelowScreenY -
            Random.Range(0f, _spawnYOffsetVariation);

        Vector3 worldPos = _spawnArea.TransformPoint(
            new Vector3(spawnX, spawnY, 0f));

        GameObject go = pool.Get(worldPos, _spawnArea);

        FloatingObject floatingObj =
            go.GetComponent<FloatingObject>();

        float speed = Random.Range(
            _levelData.floatSpeedMin,
            _levelData.floatSpeedMax);

        floatingObj.Init(
            word,
            cfg,
            speed,
            TopBoundaryY,
            _tapCallback);

        _activeObjects.Add(floatingObj);
    }

    // ------------------------------------------------
    // Lane Selection
    // ------------------------------------------------
    private float FindGoodXPosition()
    {
        float[] lanes = GetLanes();

        List<float> availableLanes =
            new List<float>(lanes);

        foreach (float usedLane in _currentBatchXPositions)
        {
            availableLanes.Remove(usedLane);
        }

        if (availableLanes.Count == 0)
        {
            return lanes[Random.Range(0, lanes.Length)];
        }

        return availableLanes[
            Random.Range(0, availableLanes.Count)];
    }

    // ------------------------------------------------
    // Weighted Type Selection
    // ------------------------------------------------
    private FloatingObjectType PickWeightedType()
    {
        int totalWeight = 0;

        foreach (var cfg in _typeConfigs)
        {
            totalWeight += cfg.spawnWeight;
        }

        int roll = Random.Range(0, totalWeight);

        int cumulative = 0;

        foreach (var cfg in _typeConfigs)
        {
            cumulative += cfg.spawnWeight;

            if (roll < cumulative)
            {
                return cfg.objectType;
            }
        }

        return _typeConfigs[0].objectType;
    }
}