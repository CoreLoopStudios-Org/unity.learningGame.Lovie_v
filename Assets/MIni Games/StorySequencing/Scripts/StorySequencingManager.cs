using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Modules.GameFramework.Content;

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
        [SerializeField] private string _resultMessageFormat = "{0} out of {1} in the correct order!";

        private IStorySequencingContentRepository _contentRepository;
        private StorySequencingEntry _entry;
        private readonly List<DraggableEventCard> _spawnedCards = new List<DraggableEventCard>();
        private Canvas _listCanvas;

        private int _correctPositionCount;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _contentRepository = new JsonStorySequencingContentRepository();

            if (_cardListContainer != null)
            {
                _listCanvas = _cardListContainer.GetComponentInParent<Canvas>();
            }
        }

        private void Start()
        {
            LoadAndDisplayStory();
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
        /// Bound to the Check Answer button's OnClick(). Compares every
        /// card's current sibling index within _cardListContainer against
        /// its correct position, updates <see cref="CorrectPositionCount"/>,
        /// and displays the result. Awards partial credit — this is not
        /// pass/fail.
        /// </summary>
        public void CheckAnswer()
        {
            _correctPositionCount = 0;

            foreach (DraggableEventCard card in _spawnedCards)
            {
                if (card == null)
                {
                    continue;
                }

                StorySequencingEvent matchingEvent = FindEventById(card.CurrentEventId);

                if (matchingEvent != null && card.transform.GetSiblingIndex() == matchingEvent.CorrectPosition)
                {
                    _correctPositionCount++;
                }
            }

            ShowResultFeedback();
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
                card.Initialize(sequencingEvent.Id, sequencingEvent.Text);
                card.OnCardDropped += HandleCardDropped;

                _spawnedCards.Add(card);
            }
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

        private void ShowResultFeedback()
        {
            if (_feedbackText == null)
            {
                return;
            }

            _feedbackText.text = string.Format(_resultMessageFormat, CorrectPositionCount, TotalEventCount);
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
        }

        #endregion
    }
}
