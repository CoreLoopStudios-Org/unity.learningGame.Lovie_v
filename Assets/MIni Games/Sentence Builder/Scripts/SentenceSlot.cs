using UnityEngine;
using UnityEngine.UI;

namespace CoreLoop.SentenceBuilder
{
    public class SentenceSlot : MonoBehaviour
    {
        [SerializeField] private Graphic background;

        public RectTransform RectTransform => transform as RectTransform;
        private SentenceWordItem currentWord;

        public bool IsEmpty             => currentWord == null;
        public SentenceWordItem CurrentWord => currentWord;

        private void Awake()
        {
            // Auto-find the background if not assigned in the Inspector.
            if (background == null) background = GetComponent<Graphic>();
        }

        public void SetWord(SentenceWordItem word) => currentWord = word;
        public void ClearWord()                    => currentWord = null;

        /// <summary>
        /// filled=true  — word has landed here; hide the empty-slot background.
        /// filled=false — slot is empty; show the background.
        /// </summary>
        public void SetFilled(bool filled)
        {
            if (background != null) background.enabled = !filled;
        }
    }
}
