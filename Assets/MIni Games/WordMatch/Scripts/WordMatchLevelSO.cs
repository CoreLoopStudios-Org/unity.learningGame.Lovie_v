using UnityEngine;
using System;
using System.Collections.Generic;

namespace CoreLoop.WordMatch
{
    [CreateAssetMenu(fileName = "NewWordMatchLevel", menuName = "MiniGames/WordMatch/Level")]
    public class WordMatchLevelSO : ScriptableObject
    {
        public List<WordMatchRound> rounds;
    }

    [Serializable]
    public class WordMatchRound
    {
        public List<WordMatchEntry> entries;
    }
}
