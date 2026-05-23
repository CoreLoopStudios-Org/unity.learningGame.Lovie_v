using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace CoreLoop.ListenWord
{
    public class ListenWordLetterItem : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI letterText;
        [SerializeField] private Button button;
        
        private char letter;
        private Action<ListenWordLetterItem> onClickCallback;
        public RectTransform RectTransform => transform as RectTransform;
        
        public char Letter => letter;
        public ListenWordSlot CurrentSlot { get; set; }
        public bool IsInSlot => CurrentSlot != null;
        
        public void Setup(char letterValue, Action<ListenWordLetterItem> onClick)
        {
            letter = Char.ToUpper(letterValue);
            letterText.text = letter.ToString();
            onClickCallback = onClick;
            
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClickCallback?.Invoke(this));
        }

        public void SetInteractable(bool interactable)
        {
            button.interactable = interactable;
        }
    }
}
