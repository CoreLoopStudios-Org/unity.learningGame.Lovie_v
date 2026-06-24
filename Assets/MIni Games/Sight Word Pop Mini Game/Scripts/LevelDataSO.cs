using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject word bank for a single level.
/// 
/// FUTURE BACKEND SWAP:
/// Replace this with a LevelDataProvider : IWordProvider that
/// fetches JSON from your API and deserializes into List<WordEntry>.
/// GameManager only talks to IWordProvider — zero other changes needed.
/// </summary>
[CreateAssetMenu(fileName = "LevelData", menuName = "SightWordPop/Level Data")]
public class LevelDataSO : ScriptableObject
{
    [Header("Level Config")]
    public string levelName;
    public int wordsPerRound = 8;          // How many word objects spawn per round
    public int targetWordCount = 4;        // How many are the "correct" target words
    public float spawnIntervalMin = 1.5f;
    public float spawnIntervalMax = 2.5f;
    public float floatSpeedMin = 60f;      // Units per second (pixels)
    public float floatSpeedMax = 100f;
    public float audioPlayInterval = 3f;   // Seconds between each spoken word

    [Header("Word Bank")]
    public List<WordEntry> allWords;       // All available words for this level

    /// <summary>Returns a shuffled copy — never mutates the original asset.</summary>
    public List<WordEntry> GetShuffledWords(int count)
    {
        var copy = new List<WordEntry>(allWords);
        for (int i = copy.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (copy[i], copy[j]) = (copy[j], copy[i]);
        }
        return copy.GetRange(0, Mathf.Min(count, copy.Count));
    }
}