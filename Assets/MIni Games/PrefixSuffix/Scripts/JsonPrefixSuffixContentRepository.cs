using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Loads Prefix &amp; Suffix entries from a local JSON file in Resources.
/// Stand-in for the current milestone — the architecture (interface-based
/// access) allows this to be replaced by a Firestore-backed implementation
/// later with no changes to PrefixSuffixManager.
/// </summary>
public class JsonPrefixSuffixContentRepository : IPrefixSuffixContentRepository
{
    #region Fields

    private const string ResourcePath = "PrefixSuffix/Entries";

    private List<PrefixSuffixData> _allEntries;
    private int _nextEntryIndex;

    #endregion

    #region Properties

    /// <inheritdoc/>
    public bool HasMoreEntries => _allEntries != null && _nextEntryIndex < _allEntries.Count;

    /// <inheritdoc/>
    public int TotalEntryCount => _allEntries?.Count ?? 0;

    #endregion

    #region Public Methods

    /// <inheritdoc/>
    public void Initialize()
    {
        TextAsset json = Resources.Load<TextAsset>(ResourcePath);
        if (json == null)
        {
            Debug.LogError($"[JsonPrefixSuffixContentRepository] Could not find JSON at Resources/{ResourcePath}");
            _allEntries = new List<PrefixSuffixData>();
            return;
        }

        PrefixSuffixData[] parsed = JsonHelper.FromJsonArray<PrefixSuffixData>(json.text);
        _allEntries = new List<PrefixSuffixData>(parsed);
        ShuffleEntries();
        _nextEntryIndex = 0;
    }

    /// <inheritdoc/>
    public List<PrefixSuffixEntry> GetNextBatch(int count)
    {
        var result = new List<PrefixSuffixEntry>();

        if (_allEntries == null)
        {
            Debug.LogError("[JsonPrefixSuffixContentRepository] GetNextBatch called before Initialize().");
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

    #endregion

    #region Private Methods

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

        Debug.LogError($"[JsonPrefixSuffixContentRepository] Unrecognized mode '{rawMode}', defaulting to Prefix.");
        return PrefixSuffixMode.Prefix;
    }

    #endregion

    #region Events / Callbacks

    // None.

    #endregion
}

/// <summary>Raw JSON-deserializable shape for one authored prefix/suffix entry.</summary>
[Serializable]
public class PrefixSuffixData
{
    public string id;
    public string rootWord;
    public string mode;
    public string[] options;
    public int correctOptionIndex;
}
