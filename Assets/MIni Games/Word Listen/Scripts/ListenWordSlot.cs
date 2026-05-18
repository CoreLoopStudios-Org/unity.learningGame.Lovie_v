using UnityEngine;

namespace CoreLoop.ListenWord
{
    public class ListenWordSlot : MonoBehaviour
    {
        public RectTransform RectTransform => transform as RectTransform;
        private ListenWordLetterItem currentLetter;
        
        public bool IsEmpty => currentLetter == null;
        public ListenWordLetterItem CurrentLetter => currentLetter;

        public void SetLetter(ListenWordLetterItem letter)
        {
            currentLetter = letter;
        }

        public void ClearLetter()
        {
            currentLetter = null;
        }
    }
}
