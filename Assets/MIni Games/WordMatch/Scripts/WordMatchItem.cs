using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CoreLoop.WordMatch
{
    public class WordMatchItem : MonoBehaviour
    {
        public enum ItemType { Image, Word }
        
        [Header("UI References")]
        [SerializeField] private Image contentImage;
        [SerializeField] private TextMeshProUGUI contentText;
        [SerializeField] private Button audioButton;
        [SerializeField] private MatchPoint matchPoint;

        [SerializeField] private float dotOffset = 20f;

        private WordMatchEntry entry;
        private ItemType type;
        private AudioSource audioSource;

        public WordMatchEntry Entry => entry;
        public ItemType Type => type;
        public MatchPoint MatchPoint => matchPoint;

        public void Setup(WordMatchEntry entry, ItemType type, AudioSource audioSource)
        {
            this.entry = entry;
            this.type = type;
            this.audioSource = audioSource;

            RectTransform dotRect = matchPoint.RectTransform;

            if (type == ItemType.Image)
            {
                contentImage.gameObject.SetActive(true);
                contentText.gameObject.SetActive(false);
                audioButton.gameObject.SetActive(false);
                contentImage.sprite = entry.image;

                // Anchor to Middle Right
                dotRect.anchorMin = new Vector2(1, 0.5f);
                dotRect.anchorMax = new Vector2(1, 0.5f);
                dotRect.pivot = new Vector2(0.5f, 0.5f);
                dotRect.anchoredPosition = new Vector2(dotOffset, 0);
            }
            else
            {
                contentImage.gameObject.SetActive(false);
                contentText.gameObject.SetActive(true);
                audioButton.gameObject.SetActive(true);
                contentText.text = entry.word;
                
                audioButton.onClick.RemoveAllListeners();
                audioButton.onClick.AddListener(PlayAudio);

                // Anchor to Middle Left
                dotRect.anchorMin = new Vector2(0, 0.5f);
                dotRect.anchorMax = new Vector2(0, 0.5f);
                dotRect.pivot = new Vector2(0.5f, 0.5f);
                dotRect.anchoredPosition = new Vector2(-dotOffset, 0);
            }

            matchPoint.Setup(this);
        }

        private void PlayAudio()
        {
            if (audioSource != null && entry.audioClip != null)
            {
                audioSource.PlayOneShot(entry.audioClip);
            }
        }
    }
}
