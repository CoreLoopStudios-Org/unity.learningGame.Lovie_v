using UnityEngine;
using System.Collections.Generic;

namespace CoreLoop.ListenWord
{
    [System.Serializable]
    public class ListenWordData
    {
        [Tooltip("The image showing the scenario for this word.")]
        public Sprite image;
        
        [Tooltip("The audio that says the word.")]
        public AudioClip audioClip;
        
        [Tooltip("The word the player needs to spell. It will be broken down into letters automatically.")]
        public string targetWord; 
        
        [Tooltip("Optional: Extra letters to show in the pool to increase difficulty (e.g. 'xyz').")]
        public string decoyLetters; 

        public char[] GetParsedLetters()
        {
            if (string.IsNullOrWhiteSpace(targetWord)) return new char[0];
            return targetWord.ToCharArray();
        }
        
        public char[] GetDecoyLetters()
        {
            if (string.IsNullOrWhiteSpace(decoyLetters)) return new char[0];
            return decoyLetters.ToCharArray();
        }
    }

    [CreateAssetMenu(fileName = "NewListenWordLevel", menuName = "MiniGames/ListenWord/Level")]
    public class ListenWordLevelSO : ScriptableObject
    {
        public List<ListenWordData> wordsToSpell;
    }
}
