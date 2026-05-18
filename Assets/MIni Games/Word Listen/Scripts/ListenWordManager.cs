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
        
        private List<ListenWordSlot> activeSlots = new List<ListenWordSlot>();
        private List<ListenWordLetterItem> activeLetters = new List<ListenWordLetterItem>();
        
        private bool isAnimating = false;
        private ListenWordData currentWordData;

        private void Start()
        {
            remainingHints = initialHints;
            UpdateHintUI();
            
            if (checkAnswerButton != null) checkAnswerButton.onClick.AddListener(CheckAnswer);
            if (shuffleButton != null) shuffleButton.onClick.AddListener(ShuffleLetters);
            if (hintButton != null) hintButton.onClick.AddListener(UseHint);
            if (playAudioButton != null) playAudioButton.onClick.AddListener(PlayWordAudio);
            
            if (levelData != null && levelData.wordsToSpell.Count > 0)
            {
                StartCoroutine(LoadWordRoutine(0));
            }
            else
            {
                Debug.LogWarning("Listen Word: No Level Data assigned or it is empty!");
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

        private void PlayWordAudio()
        {
            if (audioSource != null && currentWordData != null && currentWordData.audioClip != null)
            {
                audioSource.PlayOneShot(currentWordData.audioClip);
            }
        }

        private IEnumerator LoadWordRoutine(int index)
        {
            isAnimating = true;
            
            // Fade out
            if (gameAreaCanvasGroup != null && gameAreaCanvasGroup.alpha > 0)
            {
                yield return FadeCanvasGroup(gameAreaCanvasGroup, 1f, 0f, 0.5f);
            }

            // Cleanup previous level
            foreach (Transform child in slotContainer) Destroy(child.gameObject);
            foreach (Transform child in letterPoolContainer) Destroy(child.gameObject);
            activeSlots.Clear();
            activeLetters.Clear();

            currentWordIndex = index;
            currentWordData = levelData.wordsToSpell[index];
            
            if (referenceImage != null && currentWordData.image != null)
            {
                referenceImage.sprite = currentWordData.image;
            }

            // Spawn Empty Slots
            char[] targetLetters = currentWordData.GetParsedLetters();
            for (int i = 0; i < targetLetters.Length; i++)
            {
                var slot = Instantiate(slotPrefab, slotContainer);
                activeSlots.Add(slot);
            }

            // Prepare Letters (Correct letters + decoys)
            List<char> lettersToSpawn = new List<char>(targetLetters);
            char[] decoyLetters = currentWordData.GetDecoyLetters();
            if (decoyLetters.Length > 0)
            {
                lettersToSpawn.AddRange(decoyLetters);
            }
            
            // Shuffle initially
            lettersToSpawn = lettersToSpawn.OrderBy(l => System.Guid.NewGuid()).ToList();

            foreach (var letterChar in lettersToSpawn)
            {
                var letterItem = Instantiate(letterItemPrefab, letterPoolContainer);
                letterItem.Setup(letterChar, OnLetterClicked);
                activeLetters.Add(letterItem);
            }

            // Rebuild layouts so positions are calculated before any animations happen
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(letterPoolContainer as RectTransform);
            LayoutRebuilder.ForceRebuildLayoutImmediate(slotContainer as RectTransform);

            // Play the word audio automatically when it appears
            PlayWordAudio();

            // Fade in
            if (gameAreaCanvasGroup != null)
            {
                yield return FadeCanvasGroup(gameAreaCanvasGroup, 0f, 1f, 0.5f);
            }
            
            isAnimating = false;
        }

        private void OnLetterClicked(ListenWordLetterItem letterItem)
        {
            if (isAnimating) return;
            if (audioSource != null && letterClickSound != null) audioSource.PlayOneShot(letterClickSound);

            if (letterItem.IsInSlot)
            {
                // Move back to the pool
                letterItem.CurrentSlot.ClearLetter();
                letterItem.CurrentSlot = null;
                StartCoroutine(MoveLetterItem(letterItem, letterPoolContainer));
            }
            else
            {
                // Find first empty slot
                var emptySlot = activeSlots.FirstOrDefault(s => s.IsEmpty);
                if (emptySlot != null)
                {
                    emptySlot.SetLetter(letterItem);
                    letterItem.CurrentSlot = emptySlot;
                    StartCoroutine(MoveLetterItem(letterItem, emptySlot.transform));
                }
            }
        }

        private IEnumerator MoveLetterItem(ListenWordLetterItem item, Transform targetParent)
        {
            isAnimating = true;
            item.SetInteractable(false);
            
            // Save the starting world position
            Vector3 startPos = item.RectTransform.position;
            
            // Change parent so layout groups calculate the new arrangement
            item.transform.SetParent(targetParent, false);
            
            // If it's going into a slot, force its anchors to perfectly center it
            if (targetParent != letterPoolContainer)
            {
                item.RectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                item.RectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                item.RectTransform.pivot = new Vector2(0.5f, 0.5f);
                item.RectTransform.anchoredPosition = Vector2.zero;
            }
            
            // Force layout update
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(letterPoolContainer as RectTransform);
            LayoutRebuilder.ForceRebuildLayoutImmediate(slotContainer as RectTransform);

            // Record the exact target world position
            Vector3 endPos = item.RectTransform.position;
            
            // Disable LayoutGroups temporarily so they don't fight our manual position changes
            LayoutGroup poolLayout = letterPoolContainer.GetComponent<LayoutGroup>();
            LayoutGroup slotLayout = slotContainer.GetComponent<LayoutGroup>();
            if (poolLayout != null) poolLayout.enabled = false;
            if (slotLayout != null) slotLayout.enabled = false;

            // Move it back to where it was visually
            item.RectTransform.position = startPos;

            // Interpolate position over time using smoothstep
            float elapsed = 0;
            while (elapsed < animationDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / animationDuration;
                t = t * t * (3f - 2f * t); // Smooth easing
                item.RectTransform.position = Vector3.Lerp(startPos, endPos, t);
                yield return null;
            }

            // Snap to final and re-center just in case
            item.RectTransform.position = endPos;
            if (targetParent != letterPoolContainer)
            {
                item.RectTransform.anchoredPosition = Vector2.zero;
            }

            // Re-enable LayoutGroups
            if (poolLayout != null) poolLayout.enabled = true;
            if (slotLayout != null) slotLayout.enabled = true;
            
            item.SetInteractable(true);
            isAnimating = false;
        }

        private void ShuffleLetters()
        {
            if (isAnimating) return;
            
            var poolLetters = activeLetters.Where(l => !l.IsInSlot).ToList();
            if (poolLetters.Count <= 1) return;

            foreach (var letter in poolLetters.OrderBy(l => System.Guid.NewGuid()))
            {
                letter.transform.SetAsLastSibling();
            }
        }

        private void UseHint()
        {
            if (isAnimating || remainingHints <= 0) return;
            
            char[] targetWord = currentWordData.GetParsedLetters();
            
            for (int i = 0; i < activeSlots.Count; i++)
            {
                var slot = activeSlots[i];
                char expectedLetter = char.ToUpper(targetWord[i]);
                
                if (slot.IsEmpty || slot.CurrentLetter.Letter != expectedLetter)
                {
                    // Find the correct letter in the pool (or in a wrong slot)
                    var correctLetterItem = activeLetters.FirstOrDefault(l => l.Letter == expectedLetter && l.CurrentSlot != slot);
                    
                    if (correctLetterItem != null)
                    {
                        remainingHints--;
                        UpdateHintUI();

                        StartCoroutine(ApplyHintRoutine(slot, correctLetterItem));
                        return; // Only use one hint per click
                    }
                }
            }
        }

        private IEnumerator ApplyHintRoutine(ListenWordSlot targetSlot, ListenWordLetterItem correctLetterItem)
        {
            isAnimating = true;

            // If the target slot already has a wrong letter, float it out first
            if (!targetSlot.IsEmpty)
            {
                var wrongLetter = targetSlot.CurrentLetter;
                wrongLetter.CurrentSlot = null;
                targetSlot.ClearLetter();
                
                isAnimating = false; 
                yield return StartCoroutine(MoveLetterItem(wrongLetter, letterPoolContainer));
                isAnimating = true; 
            }

            // If the correct letter is occupying another wrong slot, clear it
            if (correctLetterItem.IsInSlot)
            {
                correctLetterItem.CurrentSlot.ClearLetter();
            }

            targetSlot.SetLetter(correctLetterItem);
            correctLetterItem.CurrentSlot = targetSlot;
            
            isAnimating = false; 
            yield return StartCoroutine(MoveLetterItem(correctLetterItem, targetSlot.transform));
        }

        private void CheckAnswer()
        {
            if (isAnimating) return;

            if (activeSlots.Any(s => s.IsEmpty))
            {
                Debug.Log("Word is not complete yet!");
                return;
            }

            char[] expectedLetters = currentWordData.GetParsedLetters();
            bool isCorrect = true;

            for (int i = 0; i < expectedLetters.Length; i++)
            {
                if (activeSlots[i].CurrentLetter.Letter != char.ToUpper(expectedLetters[i]))
                {
                    isCorrect = false;
                    break;
                }
            }

            if (isCorrect)
            {
                Debug.Log("Correct Word!");
                if (audioSource != null && successSound != null)
                {
                    audioSource.PlayOneShot(successSound);
                }

                int nextIndex = currentWordIndex + 1;
                if (nextIndex < levelData.wordsToSpell.Count)
                {
                    StartCoroutine(LoadWordRoutine(nextIndex));
                }
                else
                {
                    Debug.Log("Listen Word Mini-Game Complete!");
                }
            }
            else
            {
                Debug.Log("Incorrect Word!");
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
