using System;
using System.Collections.Generic;
using UnityEngine;
using Api;

/// <summary>
/// Remote API repository for Prefix & Suffix entries.
/// Fetches from GET /api/child/minigames/content/PrefixSuffix,
/// caches to disk, and falls back to bundled Resources if offline.
/// </summary>
public class ApiPrefixSuffixContentRepository : IPrefixSuffixContentRepository
{
    private const string ResourcePath = "PrefixSuffix/Entries";
    private const string CacheKey = "PrefixSuffix";

    private List<PrefixSuffixData> _allEntries;
    private int _nextEntryIndex;

    public bool HasMoreEntries => _allEntries != null && _nextEntryIndex < _allEntries.Count;
    public int TotalEntryCount => _allEntries?.Count ?? 0;

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
        _ = ContentCacheService.SyncRemoteContentAsync("PrefixSuffix");

        if (string.IsNullOrEmpty(json))
        {
            Debug.LogError("[ApiPrefixSuffixContentRepository] Could not load Prefix & Suffix content.");
            _allEntries = new List<PrefixSuffixData>();
            return;
        }

        PrefixSuffixData[] parsed = JsonHelper.FromJsonArray<PrefixSuffixData>(json);
        _allEntries = parsed != null ? new List<PrefixSuffixData>(parsed) : new List<PrefixSuffixData>();
        ShuffleEntries();
        _nextEntryIndex = 0;
    }

    public List<PrefixSuffixEntry> GetNextBatch(int count)
    {
        var result = new List<PrefixSuffixEntry>();

        if (_allEntries == null)
        {
            Debug.LogError("[ApiPrefixSuffixContentRepository] GetNextBatch called before Initialize().");
            return result;
        }

        for (int i = 0; i < count && HasMoreEntries; i++)
        {
            PrefixSuffixData data = _allEntries[_nextEntryIndex];
            _nextEntryIndex++;

            result.Add(new PrefixSuffixEntry(data.id, data.rootWord, ParseMode(data.mode), data.options, data.correctOptionIndex));
        }

        return result;
    }

    private void ShuffleEntries()
    {
        for (int i = _allEntries.Count - 1; i > 0; i--)
        {
            int swapIndex = UnityEngine.Random.Range(0, i + 1);
            (_allEntries[i], _allEntries[swapIndex]) = (_allEntries[swapIndex], _allEntries[i]);
        }
    }

    private static PrefixSuffixMode ParseMode(string rawMode)
    {
        if (Enum.TryParse(rawMode, ignoreCase: true, out PrefixSuffixMode parsed))
        {
            return parsed;
        }

        Debug.LogError($"[ApiPrefixSuffixContentRepository] Unrecognized mode '{rawMode}', defaulting to Prefix.");
        return PrefixSuffixMode.Prefix;
    }
}
