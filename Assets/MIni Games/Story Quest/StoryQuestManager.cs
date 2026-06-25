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

    [Header("Formation")]
    [SerializeField] private float _horizontalSpacing = 280f;
    [SerializeField] private float _verticalSpacing = 120f;

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

    // ─────────────────────────────────────────────
    // Boundaries
    // ─────────────────────────────────────────────
    private float SpawnBelowScreenY => -(_spawnArea.rect.height * 0.5f) - 200f;
    private float TopBoundaryY => (_spawnArea.rect.height * 0.5f) + 200f;

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
    // Public API
    // ─────────────────────────────────────────────
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
        _pools[obj.ObjectType].Return(obj.gameObject);
    }

    public void NotifyWordAudioStarted(string word)
    {
        foreach (var obj in _activeObjects)
            if (string.Equals(obj.Word, word, System.StringComparison.OrdinalIgnoreCase))
                obj.StartShake();
    }

    public void NotifyWordAudioStopped(string word)
    {
        foreach (var obj in _activeObjects)
            if (string.Equals(obj.Word, word, System.StringComparison.OrdinalIgnoreCase))
                obj.StopShake();
    }

    public IReadOnlyList<FloatingObject> ActiveObjects => _activeObjects;

    // ─────────────────────────────────────────────
    // Batch Spawn Routine
    // Spawns a full formation, waits, spawns next
    // ─────────────────────────────────────────────
    private IEnumerator BatchSpawnRoutine()
    {
        yield return new WaitForSeconds(0.5f);

        while (_wordQueueIndex < _wordQueue.Count)
        {
            SpawnBatch();

            if (_wordQueueIndex < _wordQueue.Count)
                yield return new WaitForSeconds(_delayBetweenBatches);
        }
    }

    private void SpawnBatch()
    {
        int remainingWords = _wordQueue.Count - _wordQueueIndex;
        int count = Mathf.Min(_batchSize, remainingWords);
        if (count <= 0) return;

        // All objects in a batch share the same speed
        // so formation spacing never changes as they rise
        float batchSpeed = Random.Range(_levelData.floatSpeedMin, _levelData.floatSpeedMax);

        Vector2[] formation = GetFormation(count);

        for (int i = 0; i < count; i++)
            SpawnOne(_wordQueue[_wordQueueIndex++], formation[i], batchSpeed);
    }

    private void SpawnOne(string word, Vector2 formationOffset, float speed)
    {
        var type = PickWeightedType();
        var cfg = _configMap[type];
        var pool = _pools[type];

        Vector2 spawnPosition = new Vector2(
            formationOffset.x,
            SpawnBelowScreenY + formationOffset.y);

        Vector3 worldPos = _spawnArea.TransformPoint(
            new Vector3(spawnPosition.x, spawnPosition.y, 0f));

        var go = pool.Get(worldPos, _spawnArea);
        var floatingObj = go.GetComponent<FloatingObject>();

        floatingObj.Init(word, cfg, speed, TopBoundaryY, _tapCallback);
        _activeObjects.Add(floatingObj);
    }

    // ─────────────────────────────────────────────
    // Formation layouts — guaranteed no overlap
    // ─────────────────────────────────────────────
    private Vector2[] GetFormation(int count)
    {
        switch (count)
        {
            case 1:
                return new Vector2[]
                {
                    new Vector2(0f, 0f)
                };

            case 2:
                return new Vector2[]
                {
                    new Vector2(-_horizontalSpacing * 0.5f, 0f),
                    new Vector2( _horizontalSpacing * 0.5f, 0f)
                };

            case 3:
                return new Vector2[]
                {
                    new Vector2(-_horizontalSpacing, 0f),
                    new Vector2(0f,                  _verticalSpacing),
                    new Vector2( _horizontalSpacing,  0f)
                };

            default:
                return new Vector2[]
                {
                    new Vector2(-_horizontalSpacing * 1.5f, 0f),
                    new Vector2(-_horizontalSpacing * 0.5f, _verticalSpacing),
                    new Vector2( _horizontalSpacing * 0.5f, _verticalSpacing),
                    new Vector2( _horizontalSpacing * 1.5f, 0f)
                };
        }
    }

    // ─────────────────────────────────────────────
    // Weighted random type picker
    // ─────────────────────────────────────────────
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
}