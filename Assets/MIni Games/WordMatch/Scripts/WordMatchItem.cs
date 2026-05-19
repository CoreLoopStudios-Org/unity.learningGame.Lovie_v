using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CoreLoop.WordMatch
{
    public class WordMatchItem : MonoBehaviour
    {
        public enum CardType { ImageCard, TextCard }

        [Header("Card Type")]
        [SerializeField] private CardType cardType;

        [Header("UI References")]
        [SerializeField] private Image contentImage;
        [SerializeField] private TextMeshProUGUI contentText;
        [SerializeField] private Button audioButton;
        [SerializeField] private MatchPoint matchPoint;

        private WordMatchEntry entry;
        private AudioSource audioSource;

        public WordMatchEntry Entry => entry;
        public CardType Type => cardType;
        public MatchPoint MatchPoint => matchPoint;

        public void Setup(WordMatchEntry entry, AudioSource audioSource)
        {
            this.entry = entry;
            this.audioSource = audioSource;

            if (cardType == CardType.ImageCard)
            {
                contentImage.sprite = entry.image;
                contentImage.preserveAspect = true;
            }
            else
            {
                contentText.text = entry.word;
                audioButton.onClick.RemoveAllListeners();
                audioButton.onClick.AddListener(PlayAudio);
            }

            matchPoint.Setup(this);
        }

        private void PlayAudio()
        {
            if (audioSource != null && entry.audioClip != null)
                audioSource.PlayOneShot(entry.audioClip);
        }
    }
}
