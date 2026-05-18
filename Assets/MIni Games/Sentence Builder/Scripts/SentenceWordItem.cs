using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace CoreLoop.SentenceBuilder
{
    public class SentenceWordItem : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI wordText;
        [SerializeField] private Button button;
        
        private string word;
        private Action<SentenceWordItem> onClickCallback;
        public RectTransform RectTransform => transform as RectTransform;
        
        public string Word => word;
        public SentenceSlot CurrentSlot { get; set; }
        public bool IsInSlot => CurrentSlot != null;
        
        public void Setup(string wordTextValue, Action<SentenceWordItem> onClick)
        {
            word = wordTextValue;
            wordText.text = word;
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
