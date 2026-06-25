using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CoreLoop.WordMatch
{
    /// <summary>
    /// Central game loop controller for Rhyme Time. Pulls batches of rhyme pairs from
    /// IRhymeTimePairRepository, spawns text cards, handles drag-to-connect input, checks
    /// correctness by comparing RhymeTimeEntry.PairId (not by reference, since the two sides
    /// of a pair are different word instances), and runs the session countdown timer.
    /// </summary>
    public class RhymeTimeManager : MonoBehaviour
    {
        #region Fields

        [Header("Content")]
        [SerializeField] private int pairsPerBatch = 5;

        [Header("Prefabs")]
        [SerializeField] private RhymeTimeItem leftCardPrefab;
        [SerializeField] private RhymeTimeItem rightCardPrefab;
        [SerializeField] private UILineConnector linePrefab;

        [Header("Layout")]
        [SerializeField] private Transform leftColumn;
        [SerializeField] private Transform rightColumn;
        [SerializeField] private Transform lineContainer;
        [SerializeField] private CanvasGroup columnsCanvasGroup;
        [SerializeField] private GraphicRaycaster raycaster;

        [Header("UI")]
        [SerializeField] private TextMeshProUGUI roundText;

        [Header("Timer")]
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private float maxTime = 120f;
        [SerializeField] private CanvasGroup timeUpPopup;
        [SerializeField] private float timeUpDisplayDuration = 2f;
        [SerializeField] private Button timeUpRestartButton;
        [SerializeField] private TextMeshProUGUI timeUpCountdownText;

        [Header("Settings")]
        [SerializeField] private Color lineColor = Color.white;
        [SerializeField] private Color correctColor = Color.green;
        [SerializeField] private Color incorrectColor = Color.red;
        [SerializeField] private float fadeDuration = 0.4f;
        [SerializeField] private float lineDisplayDuration = 1.5f;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip successSound;
        [SerializeField] private AudioClip errorSound;

        private IRhymeTimePairRepository _repository;
        private readonly List<RhymeTimeItem> _leftItems = new List<RhymeTimeItem>();
        private readonly List<RhymeTimeItem> _rightItems = new List<RhymeTimeItem>();
        private readonly Dictionary<RhymeTimeMatchPoint, RhymeTimeMatchPoint> _matches = new Dictionary<RhymeTimeMatchPoint, RhymeTimeMatchPoint>();
        private readonly Dictionary<RhymeTimeMatchPoint, UILineConnector> _committedLines = new Dictionary<RhymeTimeMatchPoint, UILineConnector>();

        private RhymeTimeMatchPoint _currentStartPoint;
        private UILineConnector _currentDragLine;

        private int _currentRound;
        private int _totalRounds;
        private bool _isTransitioning;

        private float _timeRemaining;
        private bool _timerRunning;
        private Coroutine _timeUpRoutine;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _repository = new JsonRhymeTimePairRepository();
        }

        private void Start()
        {
            if (timeUpRestartButton != null)
            {
                timeUpRestartButton.onClick.AddListener(HandleRestartButtonClicked);
            }

            if (timeUpPopup != null)
            {
                timeUpPopup.alpha = 0f;
                timeUpPopup.gameObject.SetActive(false);
            }

            BeginSession();
        }

        private void Update()
        {
            if (_timerRunning)
            {
                TickTimer();
            }
        }

        private void OnDestroy()
        {
            if (timeUpRestartButton != null)
            {
                timeUpRestartButton.onClick.RemoveListener(HandleRestartButtonClicked);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>Called by the Submit button. Advances to the next batch only if every pair is correctly matched.</summary>
        public void Submit()
        {
            if (_isTransitioning) return;
            if (!AllMatchedCorrectly()) return;

            StartCoroutine(AdvanceToNextBatchRoutine());
        }

        /// <summary>Called by RhymeTimeMatchPoint when a drag starts on it.</summary>
        public void OnMatchPointDown(RhymeTimeMatchPoint point, Vector2 screenPosition)
        {
            if (_isTransitioning) return;

            _currentStartPoint = point;
            _currentDragLine = Instantiate(linePrefab, lineContainer);
            _currentDragLine.SetColor(lineColor);
            _currentDragLine.UpdateLine(point.RectTransform.position, screenPosition);
        }

        /// <summary>Called by RhymeTimeMatchPoint while dragging.</summary>
        public void OnMatchPointDrag(Vector2 screenPosition)
        {
            if (_currentDragLine == null || _currentStartPoint == null) return;

            _currentDragLine.UpdateLine(_currentStartPoint.RectTransform.position, screenPosition);
        }

        /// <summary>Called by RhymeTimeMatchPoint when a drag ends.</summary>
        public void OnMatchPointUp(RhymeTimeMatchPoint point, PointerEventData eventData)
        {
            if (_currentStartPoint == null)
            {
                CancelDragLine();
                return;
            }

            RhymeTimeMatchPoint hitPoint = FindMatchPointUnderPointer(eventData);

            if (hitPoint == null || hitPoint == _currentStartPoint)
            {
                CancelDragLine();
                return;
            }

            // The drag-preview line's job is done — destroy it before RegisterMatch creates
            // the real committed line. Leaving this alive is what caused the leaked white
            // line bug.
            DestroyDragLineOnly();

            RegisterMatch(_currentStartPoint, hitPoint);
            _currentStartPoint = null;
        }

        #endregion

        #region Private Methods

        private void BeginSession()
        {
            _repository.Initialize();
            _totalRounds = Mathf.Max(1, _repository.TotalPairCount / pairsPerBatch);
            _currentRound = 0;

            ResetTimer();
            StartTimer();

            StartCoroutine(LoadNextBatchRoutine());
        }

        private IEnumerator LoadNextBatchRoutine()
        {
            _isTransitioning = true;
            ClearBoard();

            if (!_repository.HasMorePairs)
            {
                Debug.Log("[RhymeTimeManager] Pair pool exhausted for this session.");
                _isTransitioning = false;
                yield break;
            }

            _currentRound++;
            UpdateRoundDisplay();
            SpawnBatch(_repository.GetNextBatch(pairsPerBatch));

            yield return StartCoroutine(FadeColumns(0f, 1f));
            _isTransitioning = false;
        }

        private IEnumerator AdvanceToNextBatchRoutine()
        {
            _isTransitioning = true;
            yield return StartCoroutine(FadeColumns(1f, 0f));
            yield return StartCoroutine(LoadNextBatchRoutine());
        }

        private void SpawnBatch(List<RhymeTimeEntry> entries)
        {
            var leftEntries = new List<RhymeTimeEntry>();
            var rightEntries = new List<RhymeTimeEntry>();

            for (int i = 0; i < entries.Count; i += 2)
            {
                leftEntries.Add(entries[i]);
                rightEntries.Add(entries[i + 1]);
            }

            ShuffleList(rightEntries);

            foreach (RhymeTimeEntry entry in leftEntries)
            {
                RhymeTimeItem item = Instantiate(leftCardPrefab, leftColumn);
                item.Setup(entry, audioSource);
                _leftItems.Add(item);
            }

            foreach (RhymeTimeEntry entry in rightEntries)
            {
                RhymeTimeItem item = Instantiate(rightCardPrefab, rightColumn);
                item.Setup(entry, audioSource);
                _rightItems.Add(item);
            }
        }

        private void ClearBoard()
        {
            foreach (Transform child in leftColumn) Destroy(child.gameObject);
            foreach (Transform child in rightColumn) Destroy(child.gameObject);
            foreach (Transform child in lineContainer) Destroy(child.gameObject);

            _leftItems.Clear();
            _rightItems.Clear();
            _matches.Clear();
            _committedLines.Clear();
        }

        private RhymeTimeMatchPoint FindMatchPointUnderPointer(PointerEventData eventData)
        {
            if (raycaster == null) return null;

            var results = new List<RaycastResult>();
            raycaster.Raycast(eventData, results);

            foreach (RaycastResult result in results)
            {
                RhymeTimeMatchPoint candidate = result.gameObject.GetComponentInParent<RhymeTimeMatchPoint>();
                if (candidate != null) return candidate;
            }

            return null;
        }

        private void RegisterMatch(RhymeTimeMatchPoint a, RhymeTimeMatchPoint b)
        {
            bool isCorrect = a.OwnerItem.Entry.PairId == b.OwnerItem.Entry.PairId;

            UndoExistingMatch(a);
            UndoExistingMatch(b);

            _matches[a] = b;
            _matches[b] = a;

            UILineConnector line = Instantiate(linePrefab, lineContainer);
            line.SetColor(isCorrect ? correctColor : incorrectColor);
            line.UpdateLine(a.RectTransform.position, b.RectTransform.position);

            // Stored under BOTH endpoints now — undoing from either side can find and
            // destroy this same line. Previously only "a" held the reference, which is why
            // wrong drags failed to clean up when "b" was the side re-dragged.
            _committedLines[a] = line;
            _committedLines[b] = line;

            a.SetConnected(isCorrect);
            b.SetConnected(isCorrect);
            a.OwnerItem.SetMatched(isCorrect);
            b.OwnerItem.SetMatched(isCorrect);

            PlayFeedbackSound(isCorrect);

            if (!isCorrect)
            {
                StartCoroutine(ClearIncorrectMatchRoutine(a, b));
            }
        }

        private void UndoExistingMatch(RhymeTimeMatchPoint point)
        {
            if (!_matches.TryGetValue(point, out RhymeTimeMatchPoint previousPartner)) return;

            _matches.Remove(point);
            _matches.Remove(previousPartner);

            if (_committedLines.TryGetValue(point, out UILineConnector line))
            {
                if (line != null) Destroy(line.gameObject);
                _committedLines.Remove(point);
                _committedLines.Remove(previousPartner);
            }

            point.SetConnected(false);
            previousPartner.SetConnected(false);
        }

        private IEnumerator ClearIncorrectMatchRoutine(RhymeTimeMatchPoint a, RhymeTimeMatchPoint b)
        {
            yield return new WaitForSeconds(lineDisplayDuration);

            // Only clear if a and b are STILL matched to each other. If the player re-dragged
            // either one to a new partner during the wait, that newer match already replaced
            // this one (and destroyed this line via UndoExistingMatch) — clearing here would
            // incorrectly wipe out the new match instead.
            bool stillSamePair = _matches.TryGetValue(a, out RhymeTimeMatchPoint aPartner) && aPartner == b;
            if (!stillSamePair) yield break;

            _matches.Remove(a);
            _matches.Remove(b);

            if (_committedLines.TryGetValue(a, out UILineConnector line))
            {
                if (line != null) Destroy(line.gameObject);
            }
            _committedLines.Remove(a);
            _committedLines.Remove(b);

            a.SetConnected(false);
            b.SetConnected(false);
            a.OwnerItem.SetMatched(false);
            b.OwnerItem.SetMatched(false);
        }

        private void CancelDragLine()
        {
            DestroyDragLineOnly();
            _currentStartPoint = null;
        }

        private void DestroyDragLineOnly()
        {
            if (_currentDragLine != null) Destroy(_currentDragLine.gameObject);
            _currentDragLine = null;
        }

        private bool AllMatchedCorrectly()
        {
            foreach (RhymeTimeItem item in _leftItems)
            {
                if (!_matches.TryGetValue(item.MatchPoint, out RhymeTimeMatchPoint other)) return false;
                if (item.Entry.PairId != other.OwnerItem.Entry.PairId) return false;
            }

            return true;
        }

        private void UpdateRoundDisplay()
        {
            if (roundText != null)
            {
                roundText.text = $"Round {_currentRound}/{_totalRounds}";
            }
        }

        private IEnumerator FadeColumns(float from, float to)
        {
            if (columnsCanvasGroup == null) yield break;

            columnsCanvasGroup.alpha = from;
            columnsCanvasGroup.DOFade(to, fadeDuration);
            yield return new WaitForSeconds(fadeDuration);
        }

        private void ShuffleList(List<RhymeTimeEntry> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int swapIndex = Random.Range(0, i + 1);
                (list[i], list[swapIndex]) = (list[swapIndex], list[i]);
            }
        }

        private void PlayFeedbackSound(bool isCorrect)
        {
            if (audioSource == null) return;

            AudioClip clip = isCorrect ? successSound : errorSound;
            if (clip != null) audioSource.PlayOneShot(clip);
        }

        private void StartTimer() => _timerRunning = true;

        private void ResetTimer()
        {
            _timerRunning = false;
            _timeRemaining = maxTime;
            UpdateTimerDisplay();

            if (_timeUpRoutine != null)
            {
                StopCoroutine(_timeUpRoutine);
                _timeUpRoutine = null;
            }

            HidePopupImmediate();
        }

        private void TickTimer()
        {
            _timeRemaining -= Time.deltaTime;

            if (_timeRemaining <= 0f)
            {
                _timeRemaining = 0f;
                _timerRunning = false;
                UpdateTimerDisplay();
                HandleTimeUp();
                return;
            }

            UpdateTimerDisplay();
        }

        private void UpdateTimerDisplay()
        {
            if (timerText == null) return;

            int minutes = Mathf.FloorToInt(_timeRemaining / 60f);
            int seconds = Mathf.FloorToInt(_timeRemaining % 60f);
            timerText.text = $"{minutes:00}:{seconds:00}";
        }

        private void HandleTimeUp()
        {
            StopAllCoroutines();
            _isTransitioning = true;
            ShowPopup();
            _timeUpRoutine = StartCoroutine(TimeUpCountdownRoutine());
        }

        private void ShowPopup()
        {
            if (timeUpPopup == null) return;

            timeUpPopup.gameObject.SetActive(true);
            timeUpPopup.alpha = 0f;
            timeUpPopup.DOFade(1f, fadeDuration).SetEase(Ease.InOutSine);
        }

        private void HidePopupImmediate()
        {
            if (timeUpPopup == null) return;

            DOTween.Kill(timeUpPopup);
            timeUpPopup.alpha = 0f;
            timeUpPopup.gameObject.SetActive(false);
        }

        private IEnumerator TimeUpCountdownRoutine()
        {
            float countdown = timeUpDisplayDuration;

            while (countdown > 0f)
            {
                if (timeUpCountdownText != null)
                {
                    timeUpCountdownText.text = Mathf.CeilToInt(countdown).ToString();
                }

                yield return null;
                countdown -= Time.deltaTime;
            }

            _timeUpRoutine = null;
            HidePopupImmediate();
            BeginSession();
        }

        private void HandleRestartButtonClicked()
        {
            if (_timeUpRoutine != null)
            {
                StopCoroutine(_timeUpRoutine);
                _timeUpRoutine = null;
            }

            HidePopupImmediate();
            StopAllCoroutines();
            BeginSession();
        }

        #endregion

        #region Events / Callbacks

        // None — this manager is the endpoint for MatchPoint callbacks (called directly,
        // mirroring WordMatch's pattern), not an event publisher itself.

        #endregion
    }
}