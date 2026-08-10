using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Loads Word Wizard entries from a local JSON file in Resources.
/// Stand-in for the current milestone — the architecture (interface-based
/// access) allows this to be replaced by a Firestore-backed implementation
/// later with no changes to WordWizardManager.
/// </summary>
public class JsonWordWizardContentRepository : IWordWizardContentRepository
{
    #region Fields

    private const string ResourcePath = "WordWizard/Entries";

    private List<WordWizardData> _allEntries;
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
            Debug.LogError($"[JsonWordWizardContentRepository] Could not find JSON at Resources/{ResourcePath}");
            _allEntries = new List<WordWizardData>();
            return;
        }

        WordWizardData[] parsed = JsonHelper.FromJsonArray<WordWizardData>(json.text);
        _allEntries = new List<WordWizardData>(parsed);
        ShuffleEntries();
        _nextEntryIndex = 0;
    }

    /// <inheritdoc/>
    public List<WordWizardEntry> GetNextBatch(int count)
    {
        var result = new List<WordWizardEntry>();

        if (_allEntries == null)
        {
            Debug.LogError("[JsonWordWizardContentRepository] GetNextBatch called before Initialize().");
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

    #endregion

    #region Events / Callbacks

    // None.

    #endregion
}

/// <summary>Raw JSON-deserializable shape for one authored Word Wizard entry.</summary>
[Serializable]
public class WordWizardData
{
    public string id;
    public string targetWord;
    public string decoyLetters;
}
