using UnityEngine;
using UnityEngine.UI;
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
        [SerializeField] private WordMatchItem itemPrefab;
        [SerializeField] private UILineConnector linePrefab;

        [Header("Layout")]
        [SerializeField] private Transform leftColumn;
        [SerializeField] private Transform rightColumn;
        [SerializeField] private Transform lineContainer;

        [Header("Settings")]
        [SerializeField] private Color lineColor = Color.white;
        [SerializeField] private Color correctColor = Color.green;
        [SerializeField] private Color incorrectColor = Color.red;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip successSound;
        [SerializeField] private AudioClip errorSound;

        private List<WordMatchItem> leftItems = new List<WordMatchItem>();
        private List<WordMatchItem> rightItems = new List<WordMatchItem>();
        private List<UILineConnector> activeLines = new List<UILineConnector>();
        
        private UILineConnector currentDrawingLine;
        private MatchPoint currentStartPoint;

        private Dictionary<MatchPoint, MatchPoint> matches = new Dictionary<MatchPoint, MatchPoint>();

        private void Start()
        {
            InitializeLevel();
        }

        private void InitializeLevel()
        {
            if (currentLevel == null) return;

            // Clear existing
            foreach (Transform child in leftColumn) Destroy(child.gameObject);
            foreach (Transform child in rightColumn) Destroy(child.gameObject);
            foreach (Transform child in lineContainer) Destroy(child.gameObject);
            leftItems.Clear();
            rightItems.Clear();
            activeLines.Clear();

            // Spawn left column (Images)
            foreach (var entry in currentLevel.entries)
            {
                var item = Instantiate(itemPrefab, leftColumn);
                item.Setup(entry, WordMatchItem.ItemType.Image, audioSource);
                leftItems.Add(item);
            }

            // Spawn right column (Words) - Shuffled
            var shuffledEntries = currentLevel.entries.OrderBy(a => System.Guid.NewGuid()).ToList();
            foreach (var entry in shuffledEntries)
            {
                var item = Instantiate(itemPrefab, rightColumn);
                item.Setup(entry, WordMatchItem.ItemType.Word, audioSource);
                rightItems.Add(item);
            }
        }

        public void OnMatchPointDown(MatchPoint point, Vector2 position)
        {
            // If point already has a match, remove it
            if (matches.ContainsKey(point))
            {
                RemoveMatch(point);
            }

            currentStartPoint = point;
            currentDrawingLine = Instantiate(linePrefab, lineContainer);
            currentDrawingLine.SetColor(lineColor);
            UpdateDrawingLine(position);
        }

        public void OnMatchPointDrag(Vector2 position)
        {
            if (currentDrawingLine != null)
            {
                UpdateDrawingLine(position);
            }
        }

        public void OnMatchPointUp(MatchPoint endPoint, PointerEventData eventData)
        {
            if (currentDrawingLine == null) return;

            // Find if we released over a valid match point
            MatchPoint hitPoint = GetMatchPointUnderPointer(eventData);

            if (hitPoint != null && hitPoint != currentStartPoint && hitPoint.OwnerItem.Type != currentStartPoint.OwnerItem.Type)
            {
                // Remove existing match from hitPoint if any
                if (matches.ContainsKey(hitPoint))
                {
                    RemoveMatch(hitPoint);
                }

                // Check if match is correct
                bool isCorrect = currentStartPoint.OwnerItem.Entry == hitPoint.OwnerItem.Entry;
                Color feedbackColor = isCorrect ? correctColor : incorrectColor;
                
                // Finalize line with color feedback
                currentDrawingLine.UpdateLine(currentStartPoint.RectTransform.position, hitPoint.RectTransform.position);
                currentDrawingLine.SetColor(feedbackColor);
                activeLines.Add(currentDrawingLine);
                
                // Play feedback sound
                if (audioSource != null)
                {
                    audioSource.PlayOneShot(isCorrect ? successSound : errorSound);
                }
                
                // Store match
                matches[currentStartPoint] = hitPoint;
                matches[hitPoint] = currentStartPoint;
                
                currentStartPoint.SetConnected(true);
                hitPoint.SetConnected(true);
            }
            else
            {
                // Invalid match, destroy line
                Destroy(currentDrawingLine.gameObject);
            }

            currentDrawingLine = null;
            currentStartPoint = null;
        }

        private void UpdateDrawingLine(Vector2 endPos)
        {
            currentDrawingLine.UpdateLine(currentStartPoint.RectTransform.position, endPos);
        }

        private MatchPoint GetMatchPointUnderPointer(PointerEventData eventData)
        {
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);
            
            Debug.Log($"Raycast hit {results.Count} objects");

            foreach (var result in results)
            {
                // Ignore the line we are currently drawing so it doesn't block the raycast
                if (currentDrawingLine != null && (result.gameObject == currentDrawingLine.gameObject || result.gameObject.transform.IsChildOf(currentDrawingLine.transform)))
                {
                    continue;
                }

                Debug.Log($"Hit object: {result.gameObject.name} at {result.gameObject.transform.position}");

                MatchPoint point = result.gameObject.GetComponent<MatchPoint>();
                if (point == null) point = result.gameObject.GetComponentInParent<MatchPoint>();

                if (point != null)
                {
                    Debug.Log($"Found MatchPoint: {point.gameObject.name} (Owner: {point.OwnerItem.Entry.word})");
                    return point;
                }
            }
            
            Debug.LogWarning("No MatchPoint found under pointer.");
            return null;
        }

        private void RemoveMatch(MatchPoint point)
        {
            if (matches.TryGetValue(point, out MatchPoint otherPoint))
            {
                // Find and destroy the line between these two points
                UILineConnector lineToRemove = activeLines.FirstOrDefault(l => 
                    (Vector2.Distance(l.GetComponent<Image>().rectTransform.position, point.RectTransform.position) < 1f ||
                     Vector2.Distance(l.GetComponent<Image>().rectTransform.position, otherPoint.RectTransform.position) < 1f));
                
                if (lineToRemove != null)
                {
                    activeLines.Remove(lineToRemove);
                    Destroy(lineToRemove.gameObject);
                }

                matches.Remove(point);
                matches.Remove(otherPoint);
                point.SetConnected(false);
                otherPoint.SetConnected(false);
            }
        }

        public void Submit()
        {
            int correctCount = 0;
            HashSet<WordMatchEntry> processed = new HashSet<WordMatchEntry>();

            foreach (var pair in matches)
            {
                if (processed.Contains(pair.Key.OwnerItem.Entry)) continue;

                if (pair.Key.OwnerItem.Entry == pair.Value.OwnerItem.Entry)
                {
                    correctCount++;
                }
                processed.Add(pair.Key.OwnerItem.Entry);
                processed.Add(pair.Value.OwnerItem.Entry);
            }

            Debug.Log($"Results: {correctCount} / {currentLevel.entries.Count} correct!");
        }
    }
}
