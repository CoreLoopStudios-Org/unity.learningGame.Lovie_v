using UnityEngine;
using UnityEngine.UI;

namespace CoreLoop.ListenWord
{
    public class ListenWordSlot : MonoBehaviour
    {
        [SerializeField] private Graphic background;

        public RectTransform RectTransform => transform as RectTransform;
        private ListenWordLetterItem currentLetter;

        public bool IsEmpty                  => currentLetter == null;
        public ListenWordLetterItem CurrentLetter => currentLetter;

        private void Awake()
        {
            if (background == null) background = GetComponent<Graphic>();
        }

        public void SetLetter(ListenWordLetterItem letter) => currentLetter = letter;
        public void ClearLetter()                          => currentLetter = null;

        /// <summary>
        /// filled=true  — letter has landed here; hide the empty-slot background.
        /// filled=false — slot is empty; show the background.
        /// </summary>
        public void SetFilled(bool filled)
        {
            if (background != null) background.enabled = !filled;
        }
    }
}
