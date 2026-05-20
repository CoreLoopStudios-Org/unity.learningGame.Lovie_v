using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.EventSystems;

namespace CoreLoop.WordMatch
{
    public class WordMatchManager : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private WordMatchLevelSO currentLevel;

        [Header("Prefabs")]
        [SerializeField] private WordMatchItem imageCardPrefab;
        [SerializeField] private WordMatchItem textCardPrefab;
        [SerializeField] private UILineConnector linePrefab;

        [Header("Layout")]
        [SerializeField] private Transform leftColumn;
        [SerializeField] private Transform rightColumn;
        [SerializeField] private Transform lineContainer;
        [SerializeField] private CanvasGroup columnsCanvasGroup;

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
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip successSound;
        [SerializeField] private AudioClip errorSound;

        private List<WordMatchItem> leftItems = new List<WordMatchItem>();
        private List<WordMatchItem> rightItems = new List<WordMatchItem>();

        private UILineConnector currentDrawingLine;
        private MatchPoint currentStartPoint;

        private Dictionary<MatchPoint, MatchPoint> matches = new Dictionary<MatchPoint, MatchPoint>();
        private Dictionary<MatchPoint, UILineConnector> pointLines = new Dictionary<MatchPoint, UILineConnector>();

        private int  currentRoundIndex = 0;
        private bool isTransitioning   = false;

        private float _timeRemaining;
        private bool  _timerRunning;
        private TextMeshProUGUI _timeUpText;
        private float _timeUpTextOriginalSize;

        private void Start()
        {
            if (columnsCanvasGroup != null) columnsCanvasGroup.alpha = 0f;

            _timeRemaining = maxTime;
            UpdateTimerDisplay();
            if (timeUpPopup != null)
            {
                _timeUpText = timeUpPopup.GetComponentInChildren<TextMeshProUGUI>();
                if (_timeUpText != null) _timeUpTextOriginalSize = _timeUpText.fontSize;
                timeUpPopup.alpha = 0f;
                timeUpPopup.gameObject.SetActive(false);
            }
            if (timeUpRestartButton != null)
                timeUpRestartButton.onClick.AddListener(OnTimeUpRestart);

            StartCoroutine(LoadRoundRoutine(0));
        }

        private void Update()
        {
            if (!_timerRunning) return;
            _timeRemaining -= Time.deltaTime;
            UpdateTimerDisplay();
            if (_timeRemaining <= 0f)
            {
                _timeRemaining = 0f;
                _timerRunning  = false;
                UpdateTimerDisplay();
                StartCoroutine(TimeUpRoutine());
            }
        }

        private void UpdateTimerDisplay()
        {
            if (timerText == null) return;
            int t = Mathf.Max(0, Mathf.FloorToInt(_timeRemaining));
            timerText.text = $"{t / 60:00}:{t % 60:00}";
        }

        private IEnumerator TimeUpRoutine()
        {
            isTransitioning = true;

            if (timeUpPopup != null)
            {
                timeUpPopup.gameObject.SetActive(true);
                timeUpPopup.alpha = 0f;
                if (_timeUpText != null) _timeUpText.fontSize = 0f;
                float e = 0f;
                while (e < 0.4f)
                {
                    e += Time.deltaTime;
                    float progress = Mathf.Clamp01(e / 0.4f);
                    timeUpPopup.alpha = progress;
                    if (_timeUpText != null)
                        _timeUpText.fontSize = _timeUpTextOriginalSize * progress;
                    yield return null;
                }
                timeUpPopup.alpha = 1f;
                if (_timeUpText != null) _timeUpText.fontSize = _timeUpTextOriginalSize;
            }

            float remaining = timeUpDisplayDuration;
            while (remaining > 0f)
            {
                if (timeUpCountdownText != null)
                    timeUpCountdownText.text = Mathf.CeilToInt(remaining).ToString();
                remaining -= Time.deltaTime;
                yield return null;
            }
            if (timeUpCountdownText != null) timeUpCountdownText.text = string.Empty;

            if (timeUpPopup != null)
            {
                timeUpPopup.alpha = 0f;
                timeUpPopup.gameObject.SetActive(false);
                if (_timeUpText != null) _timeUpText.fontSize = _timeUpTextOriginalSize;
            }

            isTransitioning = false;
            yield return StartCoroutine(LoadRoundRoutine(0));
        }

        private void OnTimeUpRestart()
        {
            StopAllCoroutines();
            if (timeUpPopup != null)
            {
                timeUpPopup.alpha = 0f;
                timeUpPopup.gameObject.SetActive(false);
            }
            if (_timeUpText != null) _timeUpText.fontSize = _timeUpTextOriginalSize;
            if (timeUpCountdownText != null) timeUpCountdownText.text = string.Empty;
            isTransitioning = false;
            StartCoroutine(LoadRoundRoutine(0));
        }

        private IEnumerator LoadRoundRoutine(int roundIndex)
        {
            if (currentLevel == null || roundIndex >= currentLevel.rounds.Count) yield break;

            currentRoundIndex = roundIndex;
            var round = currentLevel.rounds[roundIndex];

            foreach (Transform child in leftColumn) Destroy(child.gameObject);
            foreach (Transform child in rightColumn) Destroy(child.gameObject);
            foreach (Transform child in lineContainer) Destroy(child.gameObject);
            leftItems.Clear();
            rightItems.Clear();
            matches.Clear();
            pointLines.Clear();
            currentDrawingLine = null;
            currentStartPoint = null;

            foreach (var entry in round.entries)
            {
                var item = Instantiate(imageCardPrefab, leftColumn);
                item.Setup(entry, audioSource);
                leftItems.Add(item);
            }

            var shuffledEntries = round.entries.OrderBy(_ => System.Guid.NewGuid()).ToList();
            foreach (var entry in shuffledEntries)
            {
                var item = Instantiate(textCardPrefab, rightColumn);
                item.Setup(entry, audioSource);
                rightItems.Add(item);
            }

            UpdateRoundText();
            yield return StartCoroutine(FadeColumns(0f, 1f));

            if (roundIndex == 0)
            {
                _timeRemaining = maxTime;
                UpdateTimerDisplay();
                _timerRunning = true;
            }
        }

        public void Submit()
        {
            if (isTransitioning) return;
            if (!AllMatchedCorrectly()) return;

            int nextRound = currentRoundIndex + 1;
            if (nextRound < currentLevel.rounds.Count)
                StartCoroutine(TransitionToRoundRoutine(nextRound));
            else
                StartCoroutine(LevelCompleteRoutine());
        }

        private bool AllMatchedCorrectly()
        {
            foreach (var item in leftItems)
            {
                if (!matches.TryGetValue(item.MatchPoint, out MatchPoint other)) return false;
                if (item.Entry != other.OwnerItem.Entry) return false;
            }
            return true;
        }

        private void UpdateRoundText()
        {
            if (roundText != null)
                roundText.text = $"Round {currentRoundIndex + 1}/{currentLevel.rounds.Count}";
        }

        private IEnumerator TransitionToRoundRoutine(int nextRoundIndex)
        {
            isTransitioning = true;
            yield return StartCoroutine(FadeColumns(1f, 0f));
            yield return StartCoroutine(LoadRoundRoutine(nextRoundIndex));
            isTransitioning = false;
        }

        private IEnumerator LevelCompleteRoutine()
        {
            isTransitioning = true;
            _timerRunning   = false;
            yield return StartCoroutine(FadeColumns(1f, 0f));
            Debug.Log("Word Match Level Complete!");
        }

        private IEnumerator FadeColumns(float from, float to)
        {
            if (columnsCanvasGroup == null) yield break;

            columnsCanvasGroup.alpha = from;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                columnsCanvasGroup.alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
                yield return null;
            }
            columnsCanvasGroup.alpha = to;
        }

        public void OnMatchPointDown(MatchPoint point, Vector2 position)
        {
            if (isTransitioning) return;

            // Destroy any line left over from an interrupted drag
            if (currentDrawingLine != null)
            {
                Destroy(currentDrawingLine.gameObject);
                currentDrawingLine = null;
            }

            if (matches.ContainsKey(point))
                RemoveMatch(point);

            currentStartPoint = point;
            currentDrawingLine = Instantiate(linePrefab, lineContainer);
            currentDrawingLine.SetColor(lineColor);
            UpdateDrawingLine(position);
        }

        public void OnMatchPointDrag(Vector2 position)
        {
            if (currentDrawingLine != null)
                UpdateDrawingLine(position);
        }

        public void OnMatchPointUp(MatchPoint endPoint, PointerEventData eventData)
        {
            if (currentDrawingLine == null) return;

            MatchPoint hitPoint = GetMatchPointUnderPointer(eventData);

            if (hitPoint != null && hitPoint != currentStartPoint && hitPoint.OwnerItem.Type != currentStartPoint.OwnerItem.Type)
            {
                if (matches.ContainsKey(hitPoint))
                    RemoveMatch(hitPoint);

                bool isCorrect = currentStartPoint.OwnerItem.Entry == hitPoint.OwnerItem.Entry;
                currentDrawingLine.UpdateLine(currentStartPoint.RectTransform.position, hitPoint.RectTransform.position);
                currentDrawingLine.SetColor(isCorrect ? correctColor : incorrectColor);

                if (audioSource != null)
                    audioSource.PlayOneShot(isCorrect ? successSound : errorSound);

                matches[currentStartPoint] = hitPoint;
                matches[hitPoint] = currentStartPoint;
                pointLines[currentStartPoint] = currentDrawingLine;
                pointLines[hitPoint] = currentDrawingLine;

                currentStartPoint.SetConnected(true);
                hitPoint.SetConnected(true);
            }
            else
            {
                Destroy(currentDrawingLine.gameObject);
            }

            currentDrawingLine = null;
            currentStartPoint = null;
        }

        private void UpdateDrawingLine(Vector2 screenPos)
        {
            Canvas canvas = lineContainer.GetComponentInParent<Canvas>();
            RectTransformUtility.ScreenPointToWorldPointInRectangle(
                lineContainer as RectTransform,
                screenPos,
                canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
                out Vector3 worldPos);
            currentDrawingLine.UpdateLine(currentStartPoint.RectTransform.position, worldPos);
        }

        private MatchPoint GetMatchPointUnderPointer(PointerEventData eventData)
        {
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            foreach (var result in results)
            {
                if (currentDrawingLine != null &&
                    (result.gameObject == currentDrawingLine.gameObject ||
                     result.gameObject.transform.IsChildOf(currentDrawingLine.transform)))
                    continue;

                MatchPoint point = result.gameObject.GetComponent<MatchPoint>();
                if (point == null) point = result.gameObject.GetComponentInParent<MatchPoint>();
                if (point != null) return point;
            }
            return null;
        }

        private void RemoveMatch(MatchPoint point)
        {
            if (matches.TryGetValue(point, out MatchPoint otherPoint))
            {
                if (pointLines.TryGetValue(point, out UILineConnector lineToRemove))
                {
                    if (lineToRemove != null) Destroy(lineToRemove.gameObject);
                    pointLines.Remove(point);
                    pointLines.Remove(otherPoint);
                }

                matches.Remove(point);
                matches.Remove(otherPoint);
                point.SetConnected(false);
                otherPoint.SetConnected(false);
            }
        }
    }
}
