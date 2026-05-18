using UnityEngine;
using System.Collections.Generic;

namespace CoreLoop.WordMatch
{
    [CreateAssetMenu(fileName = "NewWordMatchLevel", menuName = "MiniGames/WordMatch/Level")]
    public class WordMatchLevelSO : ScriptableObject
    {
        public List<WordMatchEntry> entries;
    }
}
