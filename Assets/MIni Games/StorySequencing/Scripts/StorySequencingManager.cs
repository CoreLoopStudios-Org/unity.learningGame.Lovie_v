using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Modules.GameFramework.Content;
using ImagineMe.API;

namespace Modules.Games.StorySequencing
{
    /// <summary>
    /// Top-level controller for the Story Sequencing mini-game. Loads the
    /// story content, populates the reading panel, spawns one draggable
    /// event card per event in shuffled order, resolves drag-and-drop
    /// reordering, and scores how many cards land in their correct final
    /// position when Check Answer is pressed.
    /// </summary>
    public class StorySequencingManager : MonoBehaviour
    {
        #region Fields

        [Header("Content")]
        [SerializeField] private string _storyId = "seq_story_001";

        [Header("Reading Panel")]
        [SerializeField] private TMP_Text _storyText;
        [SerializeField] private GameObject _readingPanel;

        [Header("Sequencing")]
        [SerializeField] private GameObject _sequencingPanel;
        [SerializeField] private DraggableEventCard _cardPrefab;
        [SerializeField] private Transform _cardListContainer;
        [SerializeField] private Button _checkAnswerButton;
        [SerializeField] private TextMeshProUGUI _feedbackText;
        [SerializeField] private GameCompletionReporter completionReporter;

        [Header("Attempts")]
        [Tooltip("How many wrong Check Answer presses are allowed before the correct order is shown.")]
        [SerializeField] private int _maxAttempts = 3;
        [SerializeField] private string _perfectMessage = "Perfect! Every sentence is in the right place.";
        [Tooltip("{0} = cards in the right place, {1} = total cards, {2} = tries left.")]
        [SerializeField] private string _tryAgainFormat = "{0} of {1} in the right place. Move the red cards and try again. {2} tries left.";
        [SerializeField] private string _lastTryMessage = "Out of tries. The red cards were in the wrong place.";
        [SerializeField] private string _revealMessage = "Here is the correct order.";
        [Tooltip("Seconds the wrong answers stay visible before the cards sort themselves.")]
        [SerializeField] private float _wrongAnswersHoldSeconds = 2f;
        [Tooltip("Seconds the sorted order stays visible before the failure panel opens.")]
        [SerializeField] private float _failurePanelDelaySeconds = 1.5f;

        [Header("Failure")]
        [Tooltip("Optional. Leave empty to use the built-in runtime failure overlay.")]
        [SerializeField] private GameObject _failurePanel;

        private IStorySequencingContentRepository _contentRepository;
        private StorySequencingEntry _entry;
        private readonly List<DraggableEventCard> _spawnedCards = new List<DraggableEventCard>();
        private Canvas _listCanvas;

        private const float REVEAL_STAGGER_SECONDS = 0.12f;

        private int _correctPositionCount;
        private int _attemptsUsed;
        private bool _hasReportedCompletion;
        private bool _isSolved;
        private bool _isFailureSequenceRunning;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _contentRepository = Api.ApiConfig.Instance.UseRemoteContent
                ? (IStorySequencingContentRepository)new ApiStorySequencingContentRepository()
                : new JsonStorySequencingContentRepository();

            if (_cardListContainer != null)
            {
                _listCanvas = _cardListContainer.GetComponentInParent<Canvas>();
            }
        }

        private void Start()
        {
            if (completionReporter == null)
            {
                completionReporter = GetComponent<GameCompletionReporter>();
                if (completionReporter == null)
                {
                    completionReporter = gameObject.AddComponent<GameCompletionReporter>();
                }
            }
            completionReporter.SetGameId("story_sequencing");

            LoadAndDisplayStory();

            // The scene's CompleteBtn OnClick target is dangling, so wire the handler here.
            if (_checkAnswerButton != null)
            {
                _checkAnswerButton.onClick.AddListener(CheckAnswer);
            }

        }

        private void OnDestroy()
        {
            UnsubscribeFromSpawnedCards();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Number of cards currently in their correct final position, as of
        /// the last time <see cref="CheckAnswer"/> was called. Zero until
        /// then.
        /// </summary>
        public int CorrectPositionCount => _correctPositionCount;

        /// <summary>
        /// Total number of sequencing events in the current story.
        /// </summary>
        public int TotalEventCount => _entry?.Events?.Length ?? 0;

        /// <summary>
        /// Hides the Reading panel and shows the Sequencing panel. Bound to
        /// the Next button's OnClick() on the Reading screen.
        /// </summary>
        public void ShowSequencingPanel()
        {
            if (_readingPanel != null)
            {
                _readingPanel.SetActive(false);
            }

            if (_sequencingPanel != null)
            {
                _sequencingPanel.SetActive(true);
            }
        }

        /// <summary>
        /// Bound to the Check Answer button's OnClick(). Compares every card's sibling index
        /// against its correct position and marks each card green or red. A fully correct
        /// order completes the game. A wrong order costs one try; when the tries run out the
        /// cards are sorted into the correct order and the button becomes Continue, which then
        /// opens the failure panel instead of the Game Complete panel.
        /// </summary>
        public void CheckAnswer()
        {
            if (_isSolved || _isFailureSequenceRunning)
            {
                return;
            }

            EvaluateCards();

            if (_correctPositionCount == TotalEventCount)
            {
                HandleSolved();
                return;
            }

            _attemptsUsed++;

            if (_attemptsUsed >= _maxAttempts)
            {
                StartCoroutine(FailureSequenceRoutine());
                return;
            }

            SetFeedback(string.Format(_tryAgainFormat, _correctPositionCount, TotalEventCount, _maxAttempts - _attemptsUsed));
        }

        #endregion

        #region Private Methods

        private void LoadAndDisplayStory()
        {
            _entry = _contentRepository.LoadStory(_storyId);

            if (_entry == null)
            {
                Debug.LogError("[StorySequencingManager] Failed to load story, aborting setup.");
                return;
            }

            if (_storyText != null)
            {
                _storyText.text = _entry.StoryText;
            }

            SpawnEventCards();
            RefreshAllCardPositionNumbers();
        }

        private void SpawnEventCards()
        {
            if (_cardPrefab == null || _cardListContainer == null)
            {
                Debug.LogError("[StorySequencingManager] Card prefab or list container not assigned.");
                return;
            }

            var shuffledEvents = new List<StorySequencingEvent>(_entry.Events);
            ShuffleEvents(shuffledEvents);

            foreach (StorySequencingEvent sequencingEvent in shuffledEvents)
            {
                DraggableEventCard card = Instantiate(_cardPrefab, _cardListContainer);
                card.gameObject.SetActive(true);
                card.Initialize(sequencingEvent.Id, sequencingEvent.Text);
                card.OnCardDropped += HandleCardDropped;

                _spawnedCards.Add(card);
            }

            HideSceneTemplateCard();
        }

        // The card "prefab" can be a scene object. It would stay visible as an extra row and
        // shift every sibling index by one, so move it out of the list and hide it.
        private void HideSceneTemplateCard()
        {
            if (_cardPrefab == null || !_cardPrefab.gameObject.scene.IsValid())
            {
                return;
            }

            _cardPrefab.transform.SetParent(transform, false);
            _cardPrefab.gameObject.SetActive(false);
        }

        private void ShuffleEvents(List<StorySequencingEvent> events)
        {
            for (int i = events.Count - 1; i > 0; i--)
            {
                int swapIndex = Random.Range(0, i + 1);
                (events[i], events[swapIndex]) = (events[swapIndex], events[i]);
            }
        }

        private void UnsubscribeFromSpawnedCards()
        {
            foreach (DraggableEventCard card in _spawnedCards)
            {
                if (card != null)
                {
                    card.OnCardDropped -= HandleCardDropped;
                }
            }
        }

        private int ResolveDropSiblingIndex(DraggableEventCard droppedCard, Vector2 screenPosition)
        {
            if (!TryGetWorldDropPosition(screenPosition, out Vector3 worldDropPosition))
            {
                return droppedCard.transform.GetSiblingIndex();
            }

            int siblingCount = _cardListContainer.childCount;

            // Assumes _cardListContainer holds only cards, arranged top-to-bottom
            // with the first sibling positioned highest on screen (the standard
            // Vertical Layout Group orientation) — the drop lands just above the
            // first sibling whose world Y position it is still above.
            for (int i = 0; i < siblingCount; i++)
            {
                Transform sibling = _cardListContainer.GetChild(i);

                if (sibling == droppedCard.transform)
                {
                    continue;
                }

                if (worldDropPosition.y > sibling.position.y)
                {
                    return sibling.GetSiblingIndex();
                }
            }

            return siblingCount - 1;
        }

        private bool TryGetWorldDropPosition(Vector2 screenPosition, out Vector3 worldPosition)
        {
            RectTransform containerRect = _cardListContainer as RectTransform;

            if (containerRect == null || _listCanvas == null)
            {
                worldPosition = default;
                return false;
            }

            Camera eventCamera = _listCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : _listCanvas.worldCamera;

            return RectTransformUtility.ScreenPointToWorldPointInRectangle(
                containerRect, screenPosition, eventCamera, out worldPosition);
        }

        private StorySequencingEvent FindEventById(string eventId)
        {
            if (_entry?.Events == null)
            {
                return null;
            }

            foreach (StorySequencingEvent sequencingEvent in _entry.Events)
            {
                if (sequencingEvent.Id == eventId)
                {
                    return sequencingEvent;
                }
            }

            return null;
        }

        private void SetFeedback(string message)
        {
            if (_feedbackText != null)
            {
                _feedbackText.text = message;
            }
        }

        // Counts the cards in their correct position and marks each one green or red.
        private void EvaluateCards()
        {
            _correctPositionCount = 0;

            foreach (DraggableEventCard card in _spawnedCards)
            {
                if (card == null)
                {
                    continue;
                }

                StorySequencingEvent matchingEvent = FindEventById(card.CurrentEventId);
                bool isCorrect = matchingEvent != null && card.transform.GetSiblingIndex() == matchingEvent.CorrectPosition;

                if (isCorrect)
                {
                    _correctPositionCount++;
                }

                card.ShowResult(isCorrect);
            }
        }

        // Every card is in the right place: the player wins, so report it (shows the Game Complete panel).
        private void HandleSolved()
        {
            _isSolved = true;
            SetFeedback(_perfectMessage);

            if (!_hasReportedCompletion && completionReporter != null)
            {
                _hasReportedCompletion = true;
                completionReporter.ReportCompletion(_correctPositionCount, TotalEventCount);
            }
        }

        // Out of tries: put every card in its correct place, lock dragging and turn the
        // Check Answer button into Continue. Nothing is reported, so no Game Complete panel.
        // Last try failed: leave the red/green marks on screen so the player sees what was wrong.
        // Out of tries: show the wrong cards, sort them by themselves, then open the failure panel.
        private System.Collections.IEnumerator FailureSequenceRoutine()
        {
            _isFailureSequenceRunning = true;
            SetFeedback(_lastTryMessage);
            LockCards();

            yield return new WaitForSeconds(_wrongAnswersHoldSeconds);

            SortCardsIntoCorrectOrder();
            SetFeedback(_revealMessage);

            yield return new WaitForSeconds(_spawnedCards.Count * REVEAL_STAGGER_SECONDS + _failurePanelDelaySeconds);

            ShowFailurePanel();
        }

        private void LockCards()
        {
            foreach (DraggableEventCard card in _spawnedCards)
            {
                if (card != null)
                {
                    card.SetDragEnabled(false);
                }
            }

            if (_checkAnswerButton != null)
            {
                _checkAnswerButton.interactable = false;
            }
        }

        private void SortCardsIntoCorrectOrder()
        {
            var orderedEvents = new List<StorySequencingEvent>(_entry.Events);
            orderedEvents.Sort((a, b) => a.CorrectPosition.CompareTo(b.CorrectPosition));

            for (int i = 0; i < orderedEvents.Count; i++)
            {
                DraggableEventCard card = FindCardByEventId(orderedEvents[i].Id);

                if (card == null)
                {
                    continue;
                }

                card.transform.SetSiblingIndex(i);
                card.ShowResult(true, i * REVEAL_STAGGER_SECONDS);
            }

            RefreshAllCardPositionNumbers();
        }

        private DraggableEventCard FindCardByEventId(string eventId)
        {
            foreach (DraggableEventCard card in _spawnedCards)
            {
                if (card != null && card.CurrentEventId == eventId)
                {
                    return card;
                }
            }

            return null;
        }

        private void ShowFailurePanel()
        {
            if (_sequencingPanel != null)
            {
                _sequencingPanel.SetActive(false);
            }

            if (_failurePanel != null)
            {
                _failurePanel.SetActive(true);
                return;
            }

            if (StorySequencingFailureView.Show(_sequencingPanel != null ? _sequencingPanel.transform : transform) == null)
            {
                Debug.LogWarning("[StorySequencingManager] Could not build the failure overlay: no Canvas found.");
            }
        }

        // Marks only describe the order they were checked in, so any move clears them.
        private void ClearAllResultMarks()
        {
            foreach (DraggableEventCard card in _spawnedCards)
            {
                if (card != null)
                {
                    card.ClearResult();
                }
            }
        }

        private void RefreshAllCardPositionNumbers()
        {
            if (_cardListContainer == null)
            {
                return;
            }

            for (int i = 0; i < _cardListContainer.childCount; i++)
            {
                DraggableEventCard childCard = _cardListContainer.GetChild(i).GetComponent<DraggableEventCard>();

                if (childCard != null)
                {
                    childCard.SetPositionNumber(i + 1);
                }
            }
        }

        #endregion

        #region Events / Callbacks

        private void HandleCardDropped(DraggableEventCard card, Vector2 screenPosition)
        {
            int targetIndex = ResolveDropSiblingIndex(card, screenPosition);
            card.transform.SetSiblingIndex(targetIndex);
            RefreshAllCardPositionNumbers();
            ClearAllResultMarks();
        }

        #endregion
    }
}
