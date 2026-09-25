using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Api;
using Api.Endpoints;
using Api.Models;

namespace UI
{
    public class ParentProgressDashboardController : MonoBehaviour
    {
        public static ParentProgressDashboardController Instance { get; private set; }

        [Header("Profile Section")]
        [SerializeField] private TextMeshProUGUI childNameText;
        [SerializeField] private Image childProfilePicture; // no image URL in the API yet — keep the designer placeholder
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI readingStreakText;

        [Header("Summary Section")]
        [SerializeField] private TextMeshProUGUI storiesReadText;
        [SerializeField] private TextMeshProUGUI gamesPlayedText;
        [SerializeField] private TextMeshProUGUI coinsText;

        [Header("Overview Section")]
        [SerializeField] private TextMeshProUGUI readingTimeText;
        [SerializeField] private TextMeshProUGUI storiesCompletedText;
        [SerializeField] private TextMeshProUGUI newWordsText;

        private ParentApi parentApi;
        private int requestId;

        [Serializable]
        private class ActivityPayloadData
        {
            public string title;
            public string name;
            public float score;
            public int durationSeconds;
        }

        [Serializable]
        private class ChildAdditionalData
        {
            public int level;
        }

        private void Awake()
        {
            Instance = this;

            ApiClient apiClient = ApiClient.Instance;
            apiClient.Initialize(ApiConfig.Instance);
            parentApi = new ParentApi(apiClient);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnEnable()
        {
            ShowDefaults();

            // Shows the child selected on the Parents Dashboard home page.
            ChildListItem selected = ParentDashboardController.Instance != null
                ? ParentDashboardController.Instance.SelectedChild
                : null;

            if (selected != null && !string.IsNullOrEmpty(selected.id))
                _ = LoadChildDataAsync(selected.id);
            else
                _ = LoadFirstChildAsync(); // fallback: no selection yet (e.g. opened directly)
        }

        // Kept for symmetry with the home dashboard in case a child list drives this panel directly.
        public void SelectChild(ChildListItem child)
        {
            if (child == null || string.IsNullOrEmpty(child.id)) return;
            _ = LoadChildDataAsync(child.id);
        }

        private async Awaitable LoadFirstChildAsync()
        {
            if (!SessionManager.Instance.IsValidToken())
            {
                Debug.LogWarning("[ParentProgressDashboard] No valid session, skipping progress load.");
                return;
            }

            int id = ++requestId;

            try
            {
                ChildListItem[] children = await parentApi.GetChildrenAsync();

                if (id != requestId || !isActiveAndEnabled) return;

                if (children == null || children.Length == 0)
                {
                    ShowDefaults();
                    return;
                }

                SelectChild(children[0]);
            }
            catch (Exception ex)
            {
                if (id == requestId)
                    Debug.LogWarning($"[ParentProgressDashboard] Failed to load children: {ex.Message}");
            }
        }

        private async Awaitable LoadChildDataAsync(string childId)
        {
            if (!SessionManager.Instance.IsValidToken()) return;

            int id = ++requestId;

            try
            {
                ChildDetail detail = await parentApi.GetChildAsync(childId);
                ChildActivity[] activities = await parentApi.GetChildActivitiesAsync(childId);

                if (id != requestId || !isActiveAndEnabled) return;

                RenderChild(detail, activities);
            }
            catch (Exception ex)
            {
                if (id == requestId)
                    Debug.LogWarning($"[ParentProgressDashboard] Failed to load child progress: {ex.Message}");
            }
        }

        private void RenderChild(ChildDetail detail, ChildActivity[] activities)
        {
            if (childNameText != null)
            {
                string displayName = string.IsNullOrEmpty(detail?.fullName) ? detail?.username : detail.fullName;
                childNameText.text = string.IsNullOrEmpty(displayName) ? "-" : displayName;
            }

            if (levelText != null)
                levelText.text = ParseLevel(detail?.additionalData);

            if (readingStreakText != null)
                readingStreakText.text = (detail?.loginStreak ?? 0).ToString();

            if (coinsText != null)
                coinsText.text = (detail?.coins ?? 0).ToString();

            int storiesRead = 0;
            int gamesPlayed = 0;
            int totalReadingSeconds = 0;

            if (activities != null)
            {
                foreach (ChildActivity activity in activities)
                {
                    if (activity == null) continue;

                    switch ((ActivityType)activity.activityType)
                    {
                        case ActivityType.StoryRead:
                            storiesRead++;
                            ActivityPayloadData payload = ParsePayload(activity.payload);
                            if (payload != null)
                                totalReadingSeconds += Mathf.Max(0, payload.durationSeconds);
                            break;

                        case ActivityType.GamePlayed:
                            gamesPlayed++;
                            break;
                    }
                }
            }

            if (storiesReadText != null)
                storiesReadText.text = storiesRead.ToString();

            if (gamesPlayedText != null)
                gamesPlayedText.text = gamesPlayed.ToString();

            if (readingTimeText != null)
                readingTimeText.text = FormatTotalReadingTime(totalReadingSeconds);

            // Payload has no completion flag, so completed = read until the API grows one.
            if (storiesCompletedText != null)
                storiesCompletedText.text = storiesRead.ToString();

            if (newWordsText != null)
                newWordsText.text = "0"; // no API source yet
        }

        private static string ParseLevel(string additionalData)
        {
            if (string.IsNullOrWhiteSpace(additionalData)) return "-";

            try
            {
                ChildAdditionalData data = JsonUtility.FromJson<ChildAdditionalData>(additionalData);
                return data != null && data.level > 0 ? data.level.ToString() : "-";
            }
            catch (ArgumentException)
            {
                return "-";
            }
        }

        private static string FormatTotalReadingTime(int totalSeconds)
        {
            int minutes = totalSeconds / 60;

            if (minutes < 60) return $"{minutes}m";
            return $"{minutes / 60}h {minutes % 60}m";
        }

        private static ActivityPayloadData ParsePayload(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload)) return null;

            try
            {
                return JsonUtility.FromJson<ActivityPayloadData>(payload);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        private void ShowDefaults()
        {
            if (childNameText != null) childNameText.text = "-";
            if (levelText != null) levelText.text = "-";
            if (readingStreakText != null) readingStreakText.text = "0";
            if (storiesReadText != null) storiesReadText.text = "0";
            if (gamesPlayedText != null) gamesPlayedText.text = "0";
            if (coinsText != null) coinsText.text = "0";
            if (readingTimeText != null) readingTimeText.text = "0m";
            if (storiesCompletedText != null) storiesCompletedText.text = "0";
            if (newWordsText != null) newWordsText.text = "0";
        }
    }
}
