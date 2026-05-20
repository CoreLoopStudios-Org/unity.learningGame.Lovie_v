using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace CoreLoop.ListenWord
{
    public class ListenWordManager : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private ListenWordLevelSO levelData;

        [Header("UI References")]
        [SerializeField] private Image referenceImage;
        [SerializeField] private Button playAudioButton;
        [SerializeField] private Transform slotContainer;
        [SerializeField] private Transform letterPoolContainer;
        [SerializeField] private CanvasGroup gameAreaCanvasGroup;

        [Header("Prefabs")]
        [SerializeField] private ListenWordSlot slotPrefab;
        [SerializeField] private ListenWordLetterItem letterItemPrefab;

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
        [SerializeField] private AudioClip errorSound;
        [SerializeField] private AudioClip letterClickSound;

        private int currentWordIndex = 0;
        private int remainingHints;

        private List<ListenWordSlot> activeSlots   = new List<ListenWordSlot>();
        private List<ListenWordLetterItem> activeLetters = new List<ListenWordLetterItem>();

        private bool isAnimating = false;
        private ListenWordData currentWordData;

        // Created automatically at runtime — not an Inspector field.
        private Transform placedLettersContainer;

        private void Start()
        {
            placedLettersContainer = CreatePlacedLettersOverlay();

            remainingHints = initialHints;
            UpdateHintUI();

            if (checkAnswerButton != null) checkAnswerButton.onClick.AddListener(CheckAnswer);
            if (shuffleButton    != null) shuffleButton.onClick.AddListener(ShuffleLetters);
            if (hintButton       != null) hintButton.onClick.AddListener(UseHint);
            if (playAudioButton  != null) playAudioButton.onClick.AddListener(PlayWordAudio);

            if (levelData != null && levelData.wordsToSpell.Count > 0)
                StartCoroutine(LoadWordRoutine(0));
            else
                Debug.LogWarning("Listen Word: No Level Data assigned or it is empty!");
        }

        // Sibling overlay that sits above the slot container so placed letters
        // are never clipped by any mask on the slot background.
        private Transform CreatePlacedLettersOverlay()
        {
            var go = new GameObject("Placed Letters Overlay");
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
            if (hintText   != null) hintText.text           = $"Hint ({remainingHints})";
            if (hintButton != null) hintButton.interactable = remainingHints > 0;
        }

        private void PlayWordAudio()
        {
            if (audioSource != null && currentWordData?.audioClip != null)
                audioSource.PlayOneShot(currentWordData.audioClip);
        }

        private IEnumerator LoadWordRoutine(int index)
        {
            isAnimating = true;

            if (gameAreaCanvasGroup != null && gameAreaCanvasGroup.alpha > 0)
                yield return FadeCanvasGroup(gameAreaCanvasGroup, 1f, 0f, 0.5f);

            foreach (Transform child in slotContainer)          Destroy(child.gameObject);
            foreach (Transform child in letterPoolContainer)     Destroy(child.gameObject);
            foreach (Transform child in placedLettersContainer)  Destroy(child.gameObject);
            activeSlots.Clear();
            activeLetters.Clear();

            currentWordIndex = index;
            currentWordData  = levelData.wordsToSpell[index];

            if (referenceImage != null && currentWordData.image != null)
            {
                referenceImage.sprite        = currentWordData.image;
                referenceImage.preserveAspect = true;
            }

            // Spawn one slot per letter
            char[] targetLetters = currentWordData.GetParsedLetters();
            foreach (var _ in targetLetters)
                activeSlots.Add(Instantiate(slotPrefab, slotContainer));

            // Correct letters + decoys, shuffled
            List<char> lettersToSpawn = new List<char>(targetLetters);
            char[] decoys = currentWordData.GetDecoyLetters();
            if (decoys.Length > 0) lettersToSpawn.AddRange(decoys);
            lettersToSpawn = lettersToSpawn.OrderBy(_ => System.Guid.NewGuid()).ToList();

            foreach (var letterChar in lettersToSpawn)
            {
                var item = Instantiate(letterItemPrefab, letterPoolContainer);
                item.Setup(letterChar, OnLetterClicked);
                activeLetters.Add(item);
            }

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(slotContainer      as RectTransform);
            LayoutRebuilder.ForceRebuildLayoutImmediate(letterPoolContainer as RectTransform);

            // Auto-play the word audio when the round appears
            PlayWordAudio();

            if (gameAreaCanvasGroup != null)
                yield return FadeCanvasGroup(gameAreaCanvasGroup, 0f, 1f, 0.5f);

            isAnimating = false;
        }

        private void OnLetterClicked(ListenWordLetterItem letterItem)
        {
            if (isAnimating) return;
            if (audioSource != null && letterClickSound != null)
                audioSource.PlayOneShot(letterClickSound);

            if (letterItem.IsInSlot)
            {
                var slot = letterItem.CurrentSlot;
                slot.ClearLetter();
                letterItem.CurrentSlot = null;
                slot.SetFilled(false); // restore slot background as letter leaves
                StartCoroutine(MoveLetterToPool(letterItem));
            }
            else
            {
                var emptySlot = activeSlots.FirstOrDefault(s => s.IsEmpty);
                if (emptySlot != null)
                {
                    emptySlot.SetLetter(letterItem);
                    letterItem.CurrentSlot = emptySlot;
                    StartCoroutine(MoveLetterToSlot(letterItem, emptySlot));
                }
            }
        }

        // Moves the letter into the overlay, animated to the slot's world position.
        private IEnumerator MoveLetterToSlot(ListenWordLetterItem item, ListenWordSlot targetSlot)
        {
            isAnimating = true;
            item.SetInteractable(false);

            Vector3 startPos = item.RectTransform.position;

            item.transform.SetParent(placedLettersContainer, false);
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
            targetSlot.SetFilled(true); // hide slot background once letter has landed
            item.SetInteractable(true);
            isAnimating = false;
        }

        // Returns the letter to the pool and lets the layout group re-position it.
        private IEnumerator MoveLetterToPool(ListenWordLetterItem item)
        {
            isAnimating = true;
            item.SetInteractable(false);

            Vector3 startPos = item.RectTransform.position;

            item.transform.SetParent(letterPoolContainer, false);
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(letterPoolContainer as RectTransform);

            Vector3 endPos = item.RectTransform.position;

            LayoutGroup poolLayout = letterPoolContainer.GetComponent<LayoutGroup>();
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

        private void ShuffleLetters()
        {
            if (isAnimating) return;
            var poolLetters = activeLetters.Where(l => !l.IsInSlot).ToList();
            if (poolLetters.Count <= 1) return;
            foreach (var letter in poolLetters.OrderBy(_ => System.Guid.NewGuid()))
                letter.transform.SetAsLastSibling();
        }

        private void UseHint()
        {
            if (isAnimating || remainingHints <= 0) return;
            char[] targetWord = currentWordData.GetParsedLetters();

            for (int i = 0; i < activeSlots.Count; i++)
            {
                var  slot          = activeSlots[i];
                char expectedLetter = char.ToUpper(targetWord[i]);

                if (slot.IsEmpty || slot.CurrentLetter.Letter != expectedLetter)
                {
                    var correct = activeLetters.FirstOrDefault(
                        l => l.Letter == expectedLetter && l.CurrentSlot != slot);

                    if (correct != null)
                    {
                        remainingHints--;
                        UpdateHintUI();
                        StartCoroutine(ApplyHintRoutine(slot, correct));
                        return;
                    }
                }
            }
        }

        private IEnumerator ApplyHintRoutine(ListenWordSlot targetSlot, ListenWordLetterItem correctItem)
        {
            isAnimating = true;

            if (!targetSlot.IsEmpty)
            {
                var wrongLetter = targetSlot.CurrentLetter;
                wrongLetter.CurrentSlot = null;
                targetSlot.ClearLetter();
                targetSlot.SetFilled(false);
                isAnimating = false;
                yield return StartCoroutine(MoveLetterToPool(wrongLetter));
                isAnimating = true;
            }

            if (correctItem.IsInSlot)
            {
                var oldSlot = correctItem.CurrentSlot;
                oldSlot.ClearLetter();
                correctItem.CurrentSlot = null;
                oldSlot.SetFilled(false);
            }

            targetSlot.SetLetter(correctItem);
            correctItem.CurrentSlot = targetSlot;

            isAnimating = false;
            yield return StartCoroutine(MoveLetterToSlot(correctItem, targetSlot));
        }

        private void CheckAnswer()
        {
            if (isAnimating) return;
            if (activeSlots.Any(s => s.IsEmpty))
            {
                Debug.Log("Word is not complete yet!");
                return;
            }

            char[] expected = currentWordData.GetParsedLetters();
            bool isCorrect  = true;
            for (int i = 0; i < expected.Length; i++)
            {
                if (activeSlots[i].CurrentLetter.Letter != char.ToUpper(expected[i]))
                {
                    isCorrect = false;
                    break;
                }
            }

            if (isCorrect)
            {
                Debug.Log("Correct Word!");
                if (audioSource != null && successSound != null)
                    audioSource.PlayOneShot(successSound);

                int next = currentWordIndex + 1;
                if (next < levelData.wordsToSpell.Count)
                    StartCoroutine(LoadWordRoutine(next));
                else
                    Debug.Log("Listen Word Mini-Game Complete!");
            }
            else
            {
                Debug.Log("Incorrect Word!");
                if (audioSource != null && errorSound != null)
                    audioSource.PlayOneShot(errorSound);
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
