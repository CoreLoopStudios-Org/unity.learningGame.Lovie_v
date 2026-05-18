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

            if (type == ItemType.Image)
            {
                contentImage.gameObject.SetActive(true);
                contentText.gameObject.SetActive(false);
                audioButton.gameObject.SetActive(false);
                contentImage.sprite = entry.image;
            }
            else
            {
                contentImage.gameObject.SetActive(false);
                contentText.gameObject.SetActive(true);
                audioButton.gameObject.SetActive(true);
                contentText.text = entry.word;
                
                audioButton.onClick.RemoveAllListeners();
                audioButton.onClick.AddListener(PlayAudio);
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
