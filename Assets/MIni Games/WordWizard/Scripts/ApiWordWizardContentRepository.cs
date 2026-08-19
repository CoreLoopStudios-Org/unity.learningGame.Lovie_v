using System;
using System.Collections.Generic;
using UnityEngine;
using Api;

/// <summary>
/// Remote API repository for Word Wizard entries.
/// Fetches from GET /api/child/minigames/content/WordWizard,
/// caches to disk, and falls back to bundled Resources if offline.
/// </summary>
public class ApiWordWizardContentRepository : IWordWizardContentRepository
{
    private const string ResourcePath = "WordWizard/Entries";
    private const string CacheKey = "WordWizard";

    private List<WordWizardData> _allEntries;
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
        _ = ContentCacheService.SyncRemoteContentAsync("WordWizard");

        if (string.IsNullOrEmpty(json))
        {
            Debug.LogError("[ApiWordWizardContentRepository] Could not load Word Wizard content.");
            _allEntries = new List<WordWizardData>();
            return;
        }

        WordWizardData[] parsed = JsonHelper.FromJsonArray<WordWizardData>(json);
        _allEntries = parsed != null ? new List<WordWizardData>(parsed) : new List<WordWizardData>();
        ShuffleEntries();
        _nextEntryIndex = 0;
    }

    public List<WordWizardEntry> GetNextBatch(int count)
    {
        var result = new List<WordWizardEntry>();

        if (_allEntries == null)
        {
            Debug.LogError("[ApiWordWizardContentRepository] GetNextBatch called before Initialize().");
            return result;
        }

        for (int i = 0; i < count && HasMoreEntries; i++)
        {
            WordWizardData data = _allEntries[_nextEntryIndex];
            _nextEntryIndex++;

            result.Add(new WordWizardEntry(data.id, data.targetWord, data.decoyLetters));
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
}
