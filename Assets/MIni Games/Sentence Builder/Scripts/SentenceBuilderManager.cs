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

        private void Start()
        {
            remainingHints = initialHints;
            UpdateHintUI();
            
            if (checkAnswerButton != null) checkAnswerButton.onClick.AddListener(CheckAnswer);
            if (shuffleButton != null) shuffleButton.onClick.AddListener(ShuffleWords);
            if (hintButton != null) hintButton.onClick.AddListener(UseHint);
            
            if (levelData != null && levelData.sentences.Count > 0)
            {
                StartCoroutine(LoadSentenceRoutine(0));
            }
            else
            {
                Debug.LogWarning("Sentence Builder: No Level Data assigned or it is empty!");
            }
        }

        private void UpdateHintUI()
        {
            if (hintText != null)
            {
                hintText.text = $"Hint ({remainingHints})";
            }
            if (hintButton != null)
            {
                hintButton.interactable = remainingHints > 0;
            }
        }

        private IEnumerator LoadSentenceRoutine(int index)
        {
            isAnimating = true;
            
            // Fade out
            if (gameAreaCanvasGroup != null && gameAreaCanvasGroup.alpha > 0)
            {
                yield return FadeCanvasGroup(gameAreaCanvasGroup, 1f, 0f, 0.5f);
            }

            // Cleanup previous level
            foreach (Transform child in slotContainer) Destroy(child.gameObject);
            foreach (Transform child in wordPoolContainer) Destroy(child.gameObject);
            activeSlots.Clear();
            activeWords.Clear();

            currentSentenceIndex = index;
            var sentenceData = levelData.sentences[index];
            
            if (scenarioImage != null && sentenceData.image != null)
            {
                scenarioImage.sprite = sentenceData.image;
            }

            // Spawn Empty Slots
            string[] parsedWords = sentenceData.GetParsedWords();
            for (int i = 0; i < parsedWords.Length; i++)
            {
                var slot = Instantiate(slotPrefab, slotContainer);
                activeSlots.Add(slot);
            }

            // Prepare Words (Correct words + decoys)
            List<string> wordsToSpawn = new List<string>(parsedWords);
            if (sentenceData.decoyWords != null)
            {
                wordsToSpawn.AddRange(sentenceData.decoyWords);
            }
            
            // Shuffle initially
            wordsToSpawn = wordsToSpawn.OrderBy(w => System.Guid.NewGuid()).ToList();

            foreach (var wordStr in wordsToSpawn)
            {
                var wordItem = Instantiate(wordItemPrefab, wordPoolContainer);
                wordItem.Setup(wordStr, OnWordClicked);
                activeWords.Add(wordItem);
            }

            // Rebuild layouts so positions are calculated before any animations happen
            Canvas.ForceUpdateCanvases();

            // Fade in
            if (gameAreaCanvasGroup != null)
            {
                yield return FadeCanvasGroup(gameAreaCanvasGroup, 0f, 1f, 0.5f);
            }
            
            isAnimating = false;
        }

        private void OnWordClicked(SentenceWordItem wordItem)
        {
            if (isAnimating) return;
            if (audioSource != null && wordClickSound != null) audioSource.PlayOneShot(wordClickSound);

            if (wordItem.IsInSlot)
            {
                // Move back to the pool
                wordItem.CurrentSlot.ClearWord();
                wordItem.CurrentSlot = null;
                StartCoroutine(MoveWordItem(wordItem, wordPoolContainer));
            }
            else
            {
                // Find first empty slot (First in, first out visually based on slots)
                var emptySlot = activeSlots.FirstOrDefault(s => s.IsEmpty);
                if (emptySlot != null)
                {
                    emptySlot.SetWord(wordItem);
                    wordItem.CurrentSlot = emptySlot;
                    StartCoroutine(MoveWordItem(wordItem, emptySlot.transform));
                }
            }
        }

        private IEnumerator MoveWordItem(SentenceWordItem item, Transform targetParent)
        {
            isAnimating = true;
            item.SetInteractable(false);

            Vector3 startPos = item.RectTransform.position;

            item.transform.SetParent(targetParent, false);

            // Force anchors + pivot to center so anchoredPosition zero = perfectly centered in slot
            if (targetParent != wordPoolContainer)
            {
                item.RectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                item.RectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                item.RectTransform.pivot    = new Vector2(0.5f, 0.5f);
                item.RectTransform.anchoredPosition = Vector2.zero;
            }

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(wordPoolContainer as RectTransform);
            LayoutRebuilder.ForceRebuildLayoutImmediate(slotContainer as RectTransform);

            Vector3 endPos = item.RectTransform.position;

            // Disable layout groups so they don't fight the manual lerp
            LayoutGroup poolLayout = wordPoolContainer.GetComponent<LayoutGroup>();
            LayoutGroup slotLayout = slotContainer.GetComponent<LayoutGroup>();
            if (poolLayout != null) poolLayout.enabled = false;
            if (slotLayout != null) slotLayout.enabled = false;

            item.RectTransform.position = startPos;

            float elapsed = 0;
            while (elapsed < animationDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / animationDuration;
                t = t * t * (3f - 2f * t); // smoothstep
                item.RectTransform.position = Vector3.Lerp(startPos, endPos, t);
                yield return null;
            }

            item.RectTransform.position = endPos;
            if (targetParent != wordPoolContainer)
                item.RectTransform.anchoredPosition = Vector2.zero;

            if (poolLayout != null) poolLayout.enabled = true;
            if (slotLayout != null) slotLayout.enabled = true;

            item.SetInteractable(true);
            isAnimating = false;
        }

        private void ShuffleWords()
        {
            if (isAnimating) return;
            
            // Only shuffle words that are still in the bottom pool
            var poolWords = activeWords.Where(w => !w.IsInSlot).ToList();
            if (poolWords.Count <= 1) return;

            // Randomize sibling indices within the layout group
            foreach (var word in poolWords.OrderBy(w => System.Guid.NewGuid()))
            {
                word.transform.SetAsLastSibling();
            }
        }

        private void UseHint()
        {
            if (isAnimating || remainingHints <= 0) return;
            
            var targetSentence = levelData.sentences[currentSentenceIndex].GetParsedWords();
            
            // Find the first slot that is either empty or contains the wrong word
            for (int i = 0; i < activeSlots.Count; i++)
            {
                var slot = activeSlots[i];
                string expectedWord = targetSentence[i];
                
                if (slot.IsEmpty || slot.CurrentWord.Word != expectedWord)
                {
                    // Find the correct word in the pool (or in a wrong slot)
                    var correctWordItem = activeWords.FirstOrDefault(w => w.Word == expectedWord && w.CurrentSlot != slot);
                    
                    if (correctWordItem != null)
                    {
                        remainingHints--;
                        UpdateHintUI();

                        StartCoroutine(ApplyHintRoutine(slot, correctWordItem));
                        return; // Only use one hint per click
                    }
                }
            }
        }

        private IEnumerator ApplyHintRoutine(SentenceSlot targetSlot, SentenceWordItem correctWordItem)
        {
            isAnimating = true;

            // If the target slot already has a wrong word, float it out first
            if (!targetSlot.IsEmpty)
            {
                var wrongWord = targetSlot.CurrentWord;
                wrongWord.CurrentSlot = null;
                targetSlot.ClearWord();
                
                // Allow inner routine to execute
                isAnimating = false; 
                yield return StartCoroutine(MoveWordItem(wrongWord, wordPoolContainer));
                isAnimating = true; // take control back
            }

            // If the correct word is currently occupying another wrong slot, clear it from there
            if (correctWordItem.IsInSlot)
            {
                correctWordItem.CurrentSlot.ClearWord();
            }

            // Move the correct word to the target slot
            targetSlot.SetWord(correctWordItem);
            correctWordItem.CurrentSlot = targetSlot;
            
            // Execute float animation
            isAnimating = false; 
            yield return StartCoroutine(MoveWordItem(correctWordItem, targetSlot.transform));
        }

        private void CheckAnswer()
        {
            if (isAnimating) return;

            // Are all slots filled?
            if (activeSlots.Any(s => s.IsEmpty))
            {
                Debug.Log("Sentence is not complete yet!");
                // Could trigger a little shake or sound here
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
                if (audioSource != null && successSound != null)
                {
                    audioSource.PlayOneShot(successSound);
                }

                int nextIndex = currentSentenceIndex + 1;
                if (nextIndex < levelData.sentences.Count)
                {
                    StartCoroutine(LoadSentenceRoutine(nextIndex));
                }
                else
                {
                    Debug.Log("Sentence Builder Mini-Game Complete!");
                    // Trigger level complete UI logic here
                }
            }
            else
            {
                Debug.Log("Incorrect Sentence!");
                if (audioSource != null && errorSound != null)
                {
                    audioSource.PlayOneShot(errorSound);
                }
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