using System.Collections.Generic;
using UnityEngine;
using Api;

/// <summary>
/// Remote API repository for Rhyme Time word pairs.
/// Fetches from GET /api/child/minigames/content/RhymeTime,
/// caches to disk, and falls back to bundled Resources if offline.
/// </summary>
public class ApiRhymeTimePairRepository : IRhymeTimePairRepository
{
    private const string ResourcePath = "RhymeTime/Pairs";
    private const string CacheKey = "RhymeTime";

    private List<RhymeTimePairData> _allPairs;
    private int _nextPairIndex;

    public bool HasMorePairs => _allPairs != null && _nextPairIndex < _allPairs.Count;
    public int TotalPairCount => _allPairs?.Count ?? 0;

    public void Initialize()
    {
        string json = null;

        // 1. Try disk cache
        if (!ContentCacheService.TryGetFromCache(CacheKey, out json))
        {
            // 2. Fall back to bundled Resources
            json = ContentCacheService.GetResourcesFallback(ResourcePath);
        }

        // 3. Trigger background sync to refresh cache
        _ = ContentCacheService.SyncRemoteContentAsync("RhymeTime");

        if (string.IsNullOrEmpty(json))
        {
            Debug.LogError("[ApiRhymeTimePairRepository] Could not load Rhyme Time content.");
            _allPairs = new List<RhymeTimePairData>();
            return;
        }

        RhymeTimePairData[] parsed = JsonHelper.FromJsonArray<RhymeTimePairData>(json);
        _allPairs = parsed != null ? new List<RhymeTimePairData>(parsed) : new List<RhymeTimePairData>();
        ShufflePairs();
        _nextPairIndex = 0;
    }

    public List<RhymeTimeEntry> GetNextBatch(int pairCount)
    {
        var result = new List<RhymeTimeEntry>();

        if (_allPairs == null)
        {
            Debug.LogError("[ApiRhymeTimePairRepository] GetNextBatch called before Initialize().");
            return result;
        }

        for (int i = 0; i < pairCount && HasMorePairs; i++)
        {
            RhymeTimePairData pair = _allPairs[_nextPairIndex];
            _nextPairIndex++;

            result.Add(new RhymeTimeEntry(pair.pairId + "_a", pair.wordA, pair.pairId));
            result.Add(new RhymeTimeEntry(pair.pairId + "_b", pair.wordB, pair.pairId));
        }

        return result;
    }

    private void ShufflePairs()
    {
        for (int i = _allPairs.Count - 1; i > 0; i--)
        {
            int swapIndex = Random.Range(0, i + 1);
            (_allPairs[i], _allPairs[swapIndex]) = (_allPairs[swapIndex], _allPairs[i]);
        }
    }
}
