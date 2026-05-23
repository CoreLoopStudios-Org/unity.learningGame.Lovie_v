using UnityEngine;
using System.Collections.Generic;

namespace CoreLoop.SentenceBuilder
{
    [System.Serializable]
    public class SentenceData
    {
        [Tooltip("The image showing the scenario for this sentence.")]
        public Sprite image;
        
        [Tooltip("The full sentence. It will be automatically split by spaces into individual words.")]
        public string sentence; 
        
        [Tooltip("Optional: Extra words to show in the pool that are not part of the correct sentence to increase difficulty.")]
        public string[] decoyWords; 

        public string[] GetParsedWords()
        {
            if (string.IsNullOrWhiteSpace(sentence)) return new string[0];
            return sentence.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
        }
    }

    [CreateAssetMenu(fileName = "NewSentenceLevel", menuName = "MiniGames/SentenceBuilder/Level")]
    public class SentenceBuilderLevelSO : ScriptableObject
    {
        public List<SentenceData> sentences;
    }
}
