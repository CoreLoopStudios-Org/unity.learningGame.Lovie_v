using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generic object pool. One pool per FloatingObjectType.
/// SpawnManager creates and holds these — nothing else instantiates floating objects.
/// </summary>
public class ObjectPool
{
    private readonly GameObject _prefab;
    private readonly Transform _poolRoot;
    private readonly Queue<GameObject> _available = new Queue<GameObject>();

    public ObjectPool(GameObject prefab, int initialSize, Transform poolRoot)
    {
        _prefab = prefab;
        _poolRoot = poolRoot;

        for (int i = 0; i < initialSize; i++)
            CreateAndEnqueue();
    }

    public GameObject Get(Vector3 position, Transform parent = null)
    {
        if (_available.Count == 0)
            CreateAndEnqueue();

        var obj = _available.Dequeue();
        obj.transform.SetParent(parent);
        obj.transform.position = position;
        obj.SetActive(true);
        return obj;
    }

    public void Return(GameObject obj)
    {
        obj.SetActive(false);
        obj.transform.SetParent(_poolRoot);
        _available.Enqueue(obj);
    }

    private void CreateAndEnqueue()
    {
        var obj = Object.Instantiate(_prefab, _poolRoot);
        obj.SetActive(false);
        _available.Enqueue(obj);
    }
}