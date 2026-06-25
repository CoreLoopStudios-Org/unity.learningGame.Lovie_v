using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CoreLoop.WordMatch
{
    public class RhymeTimeItem : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI contentText;
        [SerializeField] private RhymeTimeMatchPoint matchPoint;

        [Header("Glow Settings")]
        [SerializeField] private GameObject matchedHighlight;
        [SerializeField] private float pulseSpeed = 2f;
        [SerializeField] private float minGlowAlpha = 0.4f;
        [SerializeField] private float maxGlowAlpha = 1.0f;

        private RhymeTimeEntry entry;
        private Image highlightImage;
        private Coroutine pulseCoroutine;

        public RhymeTimeEntry Entry => entry;
        public RhymeTimeMatchPoint MatchPoint => matchPoint;

        public void Setup(RhymeTimeEntry entry, AudioSource audioSource)
        {
            this.entry = entry;

            if (matchedHighlight != null)
            {
                highlightImage = matchedHighlight.GetComponent<Image>();
                matchedHighlight.SetActive(false);
            }

            contentText.text = entry.Word;
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
                float lerpVal = (Mathf.Sin(elapsed) + 1f) / 2f;
                float alpha = Mathf.Lerp(minGlowAlpha, maxGlowAlpha, lerpVal);

                Color c = highlightImage.color;
                c.a = alpha;
                highlightImage.color = c;

                yield return null;
            }
        }
    }
}