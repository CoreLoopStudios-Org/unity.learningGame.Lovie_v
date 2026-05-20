using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace CoreLoop.SentenceBuilder
{
    public class SentenceBuilderManager : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private SentenceBuilderLevelSO levelData;

        [Header("UI References")]
        [SerializeField] private Image scenarioImage;
        [SerializeField] private Transform slotContainer;
        [SerializeField] private Transform wordPoolContainer;
        [SerializeField] private CanvasGroup gameAreaCanvasGroup;

        // Created automatically at runtime — not an Inspector field.
        private Transform placedWordsContainer;

        [Header("Prefabs")]
        [SerializeField] private SentenceSlot slotPrefab;
        [SerializeField] private SentenceWordItem wordItemPrefab;

        [Header("Buttons & Text")]
        [SerializeField] private Button checkAnswerButton;
        [SerializeField] private Button shuffleButton;
        [SerializeField] private Button hintButton;
        [SerializeField] private TextMeshProUGUI hintText;

        [Header("Settings")]
        [SerializeField] private int initialHints = 3;
        [SerializeField] private float animationDuration = 0.3f;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip successSound;
        [SerializeField] private AudioClip wordClickSound;
        [SerializeField] private AudioClip errorSound;

        private int currentSentenceIndex = 0;
        private int remainingHints;

        private List<SentenceSlot> activeSlots = new List<SentenceSlot>();
        private List<SentenceWordItem> activeWords = new List<SentenceWordItem>();

        private bool isAnimating = false;

        // Cached from the word prefab so we don't do repeated GetComponent calls.
        private TextMeshProUGUI _prefabTmpRef;
        private float _wordHPad;
        private float _wordVPad;
        // Uniform slot size computed once per sentence and reused for reset.
        private float _defaultSlotW;
        private float _defaultSlotH;

        private void Start()
        {
            placedWordsContainer = CreatePlacedWordsOverlay();
            CacheWordPrefabMetrics();

            remainingHints = initialHints;
            UpdateHintUI();

            if (checkAnswerButton != null) checkAnswerButton.onClick.AddListener(CheckAnswer);
            if (shuffleButton != null) shuffleButton.onClick.AddListener(ShuffleWords);
            if (hintButton != null) hintButton.onClick.AddListener(UseHint);

            if (levelData != null && levelData.sentences.Count > 0)
                StartCoroutine(LoadSentenceRoutine(0));
            else
                Debug.LogWarning("Sentence Builder: No Level Data assigned or it is empty!");
        }

        private Transform CreatePlacedWordsOverlay()
        {
            var go = new GameObject("Placed Words Overlay");
            go.transform.SetParent(slotContainer.parent, false);

            var rt  = go.AddComponent<RectTransform>();
            var src = slotContainer as RectTransform;
            rt.anchorMin        = src.anchorMin;
            rt.anchorMax        = src.anchorMax;
            rt.anchoredPosition = src.anchoredPosition;
            rt.sizeDelta        = src.sizeDelta;
            rt.pivot            = src.pivot;

            go.transform.SetAsLastSibling();
            return rt;
        }

        private void UpdateHintUI()
        {
            if (hintText != null)  hintText.text = $"Hint ({remainingHints})";
            if (hintButton != null) hintButton.interactable = remainingHints > 0;
        }

        private IEnumerator LoadSentenceRoutine(int index)
        {
            isAnimating = true;

            if (gameAreaCanvasGroup != null && gameAreaCanvasGroup.alpha > 0)
                yield return FadeCanvasGroup(gameAreaCanvasGroup, 1f, 0f, 0.5f);

            foreach (Transform child in slotContainer) Destroy(child.gameObject);
            foreach (Transform child in wordPoolContainer) Destroy(child.gameObject);
            foreach (Transform child in placedWordsContainer) Destroy(child.gameObject);
            activeSlots.Clear();
            activeWords.Clear();

            currentSentenceIndex = index;
            var sentenceData = levelData.sentences[index];

            if (scenarioImage != null && sentenceData.image != null)
            {
                scenarioImage.sprite = sentenceData.image;
                scenarioImage.preserveAspect = true;
            }

            string[] parsedWords = sentenceData.GetParsedWords();
            foreach (var _ in parsedWords)
                activeSlots.Add(Instantiate(slotPrefab, slotContainer));

            SizeSlotsUniform();

            List<string> wordsToSpawn = new List<string>(parsedWords);
            if (sentenceData.decoyWords != null)
                wordsToSpawn.AddRange(sentenceData.decoyWords);
            wordsToSpawn = wordsToSpawn.OrderBy(w => System.Guid.NewGuid()).ToList();

            foreach (var wordStr in wordsToSpawn)
            {
                var wordItem = Instantiate(wordItemPrefab, wordPoolContainer);
                // ContentSizeFitter on a child of a LayoutGroup conflicts — remove it;
                // the parent FlowLayoutGroup sizes children via GetPreferredWidth instead.
                var csf = wordItem.GetComponent<ContentSizeFitter>();
                if (csf != null) Destroy(csf);
                wordItem.Setup(wordStr, OnWordClicked);
                activeWords.Add(wordItem);
            }

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(slotContainer as RectTransform);
            LayoutRebuilder.ForceRebuildLayoutImmediate(wordPoolContainer as RectTransform);

            if (gameAreaCanvasGroup != null)
                yield return FadeCanvasGroup(gameAreaCanvasGroup, 0f, 1f, 0.5f);

            isAnimating = false;
        }

        private void CacheWordPrefabMetrics()
        {
            if (wordItemPrefab == null) return;
            _prefabTmpRef = wordItemPrefab.GetComponentInChildren<TextMeshProUGUI>();
            var vlg = wordItemPrefab.GetComponent<VerticalLayoutGroup>();
            _wordHPad = vlg != null ? vlg.padding.left + vlg.padding.right : 50f;
            _wordVPad = vlg != null ? vlg.padding.top  + vlg.padding.bottom : 50f;

            // Read default slot size directly from the prefab's LayoutElement.
            var slotLE = slotPrefab != null ? slotPrefab.GetComponent<LayoutElement>() : null;
            _defaultSlotW = slotLE != null && slotLE.preferredWidth  > 0 ? slotLE.preferredWidth  : 114f;
            _defaultSlotH = slotLE != null && slotLE.preferredHeight > 0 ? slotLE.preferredHeight : 102f;
        }

        // All slots start at the prefab's default size, equally spaced inside the container.
        private void SizeSlotsUniform()
        {
            if (activeSlots.Count == 0) return;

            // Guarantee the slot container grows vertically when slots wrap.
            var csf = slotContainer.GetComponent<ContentSizeFitter>();
            if (csf == null) csf = slotContainer.gameObject.AddComponent<ContentSizeFitter>();
            csf.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            foreach (var slot in activeSlots)
                ApplySlotSize(slot, _defaultSlotW, _defaultSlotH);

            LayoutRebuilder.ForceRebuildLayoutImmediate(slotContainer as RectTransform);
        }

        // Resizes a slot to fit the placed word exactly, then reflowing the container.
        // Called before the fly-in animation so the word animates to the correct final position.
        private void ResizeSlotToWord(SentenceSlot slot, SentenceWordItem wordItem)
        {
            if (_prefabTmpRef == null) return;
            Vector2 p = _prefabTmpRef.GetPreferredValues(wordItem.Word);
            float w = Mathf.Max(p.x + _wordHPad, 40f);
            float h = Mathf.Max(p.y + _wordVPad, 40f);
            ApplySlotSize(slot, w, h);
            LayoutRebuilder.ForceRebuildLayoutImmediate(slotContainer as RectTransform);
        }

        // Returns a slot to default size and makes its background visible again.
        private void ResetSlotSize(SentenceSlot slot)
        {
            slot.SetFilled(false);
            ApplySlotSize(slot, _defaultSlotW, _defaultSlotH);
            LayoutRebuilder.ForceRebuildLayoutImmediate(slotContainer as RectTransform);
        }

        private void ApplySlotSize(SentenceSlot slot, float w, float h)
        {
            var le = slot.GetComponent<LayoutElement>();
            if (le == null) le = slot.gameObject.AddComponent<LayoutElement>();
            le.minWidth       = w;  le.preferredWidth  = w;  le.flexibleWidth  = 0;
            le.minHeight      = h;  le.preferredHeight = h;  le.flexibleHeight = 0;
        }

        private void OnWordClicked(SentenceWordItem wordItem)
        {
            if (isAnimating) return;
            if (audioSource != null && wordClickSound != null) audioSource.PlayOneShot(wordClickSound);

            if (wordItem.IsInSlot)
            {
                var slot = wordItem.CurrentSlot;
                slot.ClearWord();
                wordItem.CurrentSlot = null;
                // Slot resets to default size immediately as the word flies away.
                ResetSlotSize(slot);
                Canvas.ForceUpdateCanvases();
                StartCoroutine(MoveWordToPool(wordItem));
            }
            else
            {
                var emptySlot = activeSlots.FirstOrDefault(s => s.IsEmpty);
                if (emptySlot != null)
                {
                    // Resize slot before animating so the word flies to the correct final position.
                    ResizeSlotToWord(emptySlot, wordItem);
                    Canvas.ForceUpdateCanvases();
                    emptySlot.SetWord(wordItem);
                    wordItem.CurrentSlot = emptySlot;
                    StartCoroutine(MoveWordToSlot(wordItem, emptySlot));
                }
            }
        }

        // Moves the word into the placed-words overlay, centered on the slot's world position.
        private IEnumerator MoveWordToSlot(SentenceWordItem item, SentenceSlot targetSlot)
        {
            isAnimating = true;
            item.SetInteractable(false);

            Vector3 startPos = item.RectTransform.position;

            item.transform.SetParent(placedWordsContainer, false);
            item.RectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            item.RectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            item.RectTransform.pivot     = new Vector2(0.5f, 0.5f);

            Canvas.ForceUpdateCanvases();

            item.RectTransform.position = startPos;

            float elapsed = 0;
            while (elapsed < animationDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / animationDuration;
                t = t * t * (3f - 2f * t);
                item.RectTransform.position = Vector3.Lerp(startPos, targetSlot.RectTransform.position, t);
                yield return null;
            }

            item.RectTransform.position = targetSlot.RectTransform.position;
            targetSlot.SetFilled(true); // hide slot background once word has landed
            item.SetInteractable(true);
            isAnimating = false;
        }

        // Returns the word to the pool and lets the layout group position it.
        private IEnumerator MoveWordToPool(SentenceWordItem item)
        {
            isAnimating = true;
            item.SetInteractable(false);

            Vector3 startPos = item.RectTransform.position;

            item.transform.SetParent(wordPoolContainer, false);
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(wordPoolContainer as RectTransform);

            Vector3 endPos = item.RectTransform.position;

            LayoutGroup poolLayout = wordPoolContainer.GetComponent<LayoutGroup>();
            if (poolLayout != null) poolLayout.enabled = false;

            item.RectTransform.position = startPos;

            float elapsed = 0;
            while (elapsed < animationDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / animationDuration;
                t = t * t * (3f - 2f * t);
                item.RectTransform.position = Vector3.Lerp(startPos, endPos, t);
                yield return null;
            }

            item.RectTransform.position = endPos;
            if (poolLayout != null) poolLayout.enabled = true;

            item.SetInteractable(true);
            isAnimating = false;
        }

        private void ShuffleWords()
        {
            if (isAnimating) return;
            var poolWords = activeWords.Where(w => !w.IsInSlot).ToList();
            if (poolWords.Count <= 1) return;
            foreach (var word in poolWords.OrderBy(w => System.Guid.NewGuid()))
                word.transform.SetAsLastSibling();
        }

        private void UseHint()
        {
            if (isAnimating || remainingHints <= 0) return;
            var targetSentence = levelData.sentences[currentSentenceIndex].GetParsedWords();

            for (int i = 0; i < activeSlots.Count; i++)
            {
                var slot = activeSlots[i];
                string expectedWord = targetSentence[i];

                if (slot.IsEmpty || slot.CurrentWord.Word != expectedWord)
                {
                    var correctWordItem = activeWords.FirstOrDefault(w => w.Word == expectedWord && w.CurrentSlot != slot);
                    if (correctWordItem != null)
                    {
                        remainingHints--;
                        UpdateHintUI();
                        StartCoroutine(ApplyHintRoutine(slot, correctWordItem));
                        return;
                    }
                }
            }
        }

        private IEnumerator ApplyHintRoutine(SentenceSlot targetSlot, SentenceWordItem correctWordItem)
        {
            isAnimating = true;

            if (!targetSlot.IsEmpty)
            {
                var wrongWord = targetSlot.CurrentWord;
                wrongWord.CurrentSlot = null;
                targetSlot.ClearWord();
                ResetSlotSize(targetSlot);
                isAnimating = false;
                yield return StartCoroutine(MoveWordToPool(wrongWord));
                isAnimating = true;
            }

            if (correctWordItem.IsInSlot)
            {
                var oldSlot = correctWordItem.CurrentSlot;
                oldSlot.ClearWord();
                correctWordItem.CurrentSlot = null;
                ResetSlotSize(oldSlot);
            }

            ResizeSlotToWord(targetSlot, correctWordItem);
            Canvas.ForceUpdateCanvases();
            targetSlot.SetWord(correctWordItem);
            correctWordItem.CurrentSlot = targetSlot;

            isAnimating = false;
            yield return StartCoroutine(MoveWordToSlot(correctWordItem, targetSlot));
        }

        private void CheckAnswer()
        {
            if (isAnimating) return;
            if (activeSlots.Any(s => s.IsEmpty))
            {
                Debug.Log("Sentence is not complete yet!");
                return;
            }

            var expectedWords = levelData.sentences[currentSentenceIndex].GetParsedWords();
            bool isCorrect = true;
            for (int i = 0; i < expectedWords.Length; i++)
            {
                if (activeSlots[i].CurrentWord.Word != expectedWords[i])
                {
                    isCorrect = false;
                    break;
                }
            }

            if (isCorrect)
            {
                Debug.Log("Correct Sentence!");
                if (audioSource != null && successSound != null) audioSource.PlayOneShot(successSound);
                int nextIndex = currentSentenceIndex + 1;
                if (nextIndex < levelData.sentences.Count)
                    StartCoroutine(LoadSentenceRoutine(nextIndex));
                else
                    Debug.Log("Sentence Builder Mini-Game Complete!");
            }
            else
            {
                Debug.Log("Incorrect Sentence!");
                if (audioSource != null && errorSound != null) audioSource.PlayOneShot(errorSound);
            }
        }

        private IEnumerator FadeCanvasGroup(CanvasGroup cg, float start, float end, float duration)
        {
            float elapsed = 0;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                cg.alpha = Mathf.Lerp(start, end, elapsed / duration);
                yield return null;
            }
            cg.alpha = end;
        }
    }
}
