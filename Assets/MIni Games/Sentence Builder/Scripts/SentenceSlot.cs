using UnityEngine;

namespace CoreLoop.SentenceBuilder
{
    public class SentenceSlot : MonoBehaviour
    {
        public RectTransform RectTransform => transform as RectTransform;
        private SentenceWordItem currentWord;
        
        public bool IsEmpty => currentWord == null;
        public SentenceWordItem CurrentWord => currentWord;

        public void SetWord(SentenceWordItem word)
        {
            currentWord = word;
        }

        public void ClearWord()
        {
            currentWord = null;
        }
    }
}
