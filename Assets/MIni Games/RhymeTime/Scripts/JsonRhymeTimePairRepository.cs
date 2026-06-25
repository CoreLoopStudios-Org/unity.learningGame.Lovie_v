using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Loads Rhyme Time word pairs from a local JSON file in Resources. Stand-in for the current
/// milestone — the architecture (interface-based access) allows this to be replaced by a
/// Firestore-backed implementation later with no changes to RhymeTimeManager.
/// </summary>
public class JsonRhymeTimePairRepository : IRhymeTimePairRepository
{
    #region Fields

    private const string ResourcePath = "RhymeTime/Pairs";

    private List<RhymeTimePairData> _allPairs;
    private int _nextPairIndex;

    #endregion

    #region Properties

    /// <inheritdoc/>
    public bool HasMorePairs => _allPairs != null && _nextPairIndex < _allPairs.Count;
    public int TotalPairCount => _allPairs?.Count ?? 0;

    #endregion

    #region Public Methods

    /// <inheritdoc/>
    public void Initialize()
    {
        TextAsset json = Resources.Load<TextAsset>(ResourcePath);
        if (json == null)
        {
            Debug.LogError($"[JsonRhymeTimePairRepository] Could not find JSON at Resources/{ResourcePath}");
            _allPairs = new List<RhymeTimePairData>();
            return;
        }

        RhymeTimePairData[] parsed = JsonHelper.FromJsonArray<RhymeTimePairData>(json.text);
        _allPairs = new List<RhymeTimePairData>(parsed);
        ShufflePairs();
        _nextPairIndex = 0;
    }

    /// <inheritdoc/>
    public List<RhymeTimeEntry> GetNextBatch(int pairCount)
    {
        var result = new List<RhymeTimeEntry>();

        if (_allPairs == null)
        {
            Debug.LogError("[JsonRhymeTimePairRepository] GetNextBatch called before Initialize().");
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

    #endregion

    #region Private Methods

    private void ShufflePairs()
    {
        for (int i = _allPairs.Count - 1; i > 0; i--)
        {
            int swapIndex = Random.Range(0, i + 1);
            (_allPairs[i], _allPairs[swapIndex]) = (_allPairs[swapIndex], _allPairs[i]);
        }
    }

    #endregion

    #region Events / Callbacks

    // None.

    #endregion
}

/// <summary>Raw JSON-deserializable shape for one authored rhyme pair.</summary>
[System.Serializable]
public class RhymeTimePairData
{
    public string pairId;
    public string wordA;
    public string wordB;
}