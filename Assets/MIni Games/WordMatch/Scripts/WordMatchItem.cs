using System.Collections;
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

        [Header("Glow Settings")]
        [SerializeField] private GameObject matchedHighlight;
        [SerializeField] private float pulseSpeed = 2f;
        [SerializeField] private float minGlowAlpha = 0.4f;
        [SerializeField] private float maxGlowAlpha = 1.0f;

        private WordMatchEntry entry;
        private AudioSource audioSource;
        private Image highlightImage;
        private Coroutine pulseCoroutine;

        public WordMatchEntry Entry => entry;
        public CardType Type => cardType;
        public MatchPoint MatchPoint => matchPoint;

        public void Setup(WordMatchEntry entry, AudioSource audioSource)
        {
            this.entry = entry;
            this.audioSource = audioSource;

            if (matchedHighlight != null)
            {
                highlightImage = matchedHighlight.GetComponent<Image>();
                matchedHighlight.SetActive(false);
            }

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

        public void SetMatched(bool isMatched)
        {
            if (matchedHighlight != null)
            {
                matchedHighlight.SetActive(isMatched);
                
                if (isMatched)
                {
                    if (pulseCoroutine != null) StopCoroutine(pulseCoroutine);
                    pulseCoroutine = StartCoroutine(PulseGlowRoutine());
                }
                else
                {
                    if (pulseCoroutine != null)
                    {
                        StopCoroutine(pulseCoroutine);
                        pulseCoroutine = null;
                    }
                }
            }
        }

        private IEnumerator PulseGlowRoutine()
        {
            if (highlightImage == null) yield break;

            float elapsed = 0f;
            while (true)
            {
                elapsed += Time.deltaTime * pulseSpeed;
                // Use Sin wave to oscillate between 0 and 1, then remap to min/max alpha
                float lerpVal = (Mathf.Sin(elapsed) + 1f) / 2f;
                float alpha = Mathf.Lerp(minGlowAlpha, maxGlowAlpha, lerpVal);
                
                Color c = highlightImage.color;
                c.a = alpha;
                highlightImage.color = c;
                
                yield return null;
            }
        }

        private void PlayAudio()
        {
            if (audioSource != null && entry.audioClip != null)
                audioSource.PlayOneShot(entry.audioClip);
        }
    }
}
