using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Api;
using Api.Endpoints;
using Api.Models;

namespace UI
{
    public class ParentDashboardController : MonoBehaviour
    {
        public static ParentDashboardController Instance { get; private set; }

        // The child currently shown on the home dashboard; other panels (e.g. Progress) read this.
        public ChildListItem SelectedChild { get; private set; }

        private const int MaxRecentActivities = 4;

        [Header("Child Profile")]
        [SerializeField] private TextMeshProUGUI childNameText;
        [SerializeField] private Image childProfilePicture;

        [Header("Stats")]
        [SerializeField] private TextMeshProUGUI readingStreakText;
        [SerializeField] private TextMeshProUGUI readingTimeText;
        [SerializeField] private TextMeshProUGUI storiesCompletedText;
        [SerializeField] private TextMeshProUGUI quizAverageText;
        [SerializeField] private TextMeshProUGUI newWordsText;
        [SerializeField] private TextMeshProUGUI coinsText;

        [Header("Child Credentials")]
        [SerializeField] private TextMeshProUGUI childUsernameText;
        [SerializeField] private TextMeshProUGUI childPasswordText;

        [Header("Recent Activities (fixed slots, newest first)")]
        [SerializeField] private ParentActivityCard[] activityCards;

        [Header("Common UI")]
        [SerializeField] private Button logoutButton;
        [SerializeField] private string loginScene = "Main Game/Parent/Parent Login";

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

        private void Awake()
        {
            Instance = this;

            ApiClient apiClient = ApiClient.Instance;
            apiClient.Initialize(ApiConfig.Instance);
            parentApi = new ParentApi(apiClient);

            if (logoutButton != null) logoutButton.onClick.AddListener(OnLogoutClicked);
        }

        private void OnEnable()
        {
            if (!SessionManager.Instance.IsValidToken())
            {
                OnLogoutClicked();
                return;
            }

            ShowDefaults();
            _ = LoadFirstChildAsync();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (logoutButton != null) logoutButton.onClick.RemoveListener(OnLogoutClicked);
        }

        // Called by ChildListPanelController when a child card is picked.
        public void SelectChild(ChildListItem child)
        {
            if (child == null || string.IsNullOrEmpty(child.id)) return;

            SelectedChild = child;
            _ = LoadChildDataAsync(child.id);
        }

        private async Awaitable LoadFirstChildAsync()
        {
            if (!SessionManager.Instance.IsValidToken()) return;

            int id = ++requestId;

            try
            {
                ChildListItem[] children = await parentApi.GetChildrenAsync();

                if (id != requestId || !isActiveAndEnabled) return;

                if (children == null || children.Length == 0)
                {
                    SelectedChild = null;
                    ShowDefaults(); // no children: keep default values on every field
                    return;
                }

                SelectChild(children[0]);
            }
            catch (Exception ex)
            {
                if (id == requestId)
                    Debug.LogWarning($"[ParentDashboard] Failed to load children: {ex.Message}");
            }
        }

        private async Awaitable LoadChildDataAsync(string childId)
        {
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
                    Debug.LogWarning($"[ParentDashboard] Failed to load child data: {ex.Message}");
            }
        }

        private void RenderChild(ChildDetail detail, ChildActivity[] activities)
        {
            if (childNameText != null)
            {
                string displayName = string.IsNullOrEmpty(detail?.fullName) ? detail?.username : detail.fullName;
                childNameText.text = string.IsNullOrEmpty(displayName) ? "-" : displayName;
            }

            if (coinsText != null)
                coinsText.text = (detail?.coins ?? 0).ToString();

            if (readingStreakText != null)
                readingStreakText.text = (detail?.loginStreak ?? 0).ToString();

            if (childUsernameText != null)
                childUsernameText.text = string.IsNullOrEmpty(detail?.username) ? "-" : detail.username;

            if (childPasswordText != null)
                childPasswordText.text = string.IsNullOrEmpty(detail?.password) ? "-" : detail.password;

            int storiesRead = 0;
            float quizScoreSum = 0f;
            int quizCount = 0;
            int todayReadingSeconds = 0;
            DateTime todayUtc = DateTime.UtcNow.Date;

            if (activities != null)
            {
                foreach (ChildActivity activity in activities)
                {
                    if (activity == null) continue;

                    var type = (ActivityType)activity.activityType;
                    ActivityPayloadData payload = ParsePayload(activity.payload);

                    switch (type)
                    {
                        case ActivityType.StoryRead:
                            storiesRead++;
                            if (payload != null && ParseTime(activity.createdAt).ToUniversalTime().Date == todayUtc)
                                todayReadingSeconds += Mathf.Max(0, payload.durationSeconds);
                            break;

                        case ActivityType.QuizAttempt:
                            if (payload != null)
                            {
                                quizScoreSum += payload.score;
                                quizCount++;
                            }
                            break;
                    }
                }
            }

            if (readingTimeText != null)
                readingTimeText.text = $"{todayReadingSeconds / 60:00}m";

            if (storiesCompletedText != null)
                storiesCompletedText.text = storiesRead.ToString();

            if (quizAverageText != null)
                quizAverageText.text = quizCount > 0 ? $"{quizScoreSum / quizCount:0}%" : "0%";

            if (newWordsText != null)
                newWordsText.text = "0"; // no API source yet

            PopulateActivities(activities);
        }

        private void PopulateActivities(ChildActivity[] activities)
        {
            if (activityCards == null || activityCards.Length == 0) return;

            List<ChildActivity> recent = activities != null ? GetRecentActivities(activities) : new List<ChildActivity>();

            for (int i = 0; i < activityCards.Length; i++)
            {
                ParentActivityCard card = activityCards[i];
                if (card == null) continue;

                if (i < recent.Count)
                    card.Setup(GetActivityName(recent[i]), GetActivityTypeLabel(recent[i]), FormatRelativeTime(recent[i].createdAt));
                else
                    card.Clear();
            }
        }

        private static List<ChildActivity> GetRecentActivities(ChildActivity[] activities)
        {
            var result = new List<ChildActivity>();

            foreach (ChildActivity activity in activities)
            {
                if (activity == null) continue;
                if ((ActivityType)activity.activityType == ActivityType.DailyLogin) continue;
                result.Add(activity);
            }

            result.Sort((x, y) => ParseTime(y.createdAt).CompareTo(ParseTime(x.createdAt)));

            return result.Count > MaxRecentActivities ? result.GetRange(0, MaxRecentActivities) : result;
        }

        private static string GetActivityName(ChildActivity activity)
        {
            ActivityPayloadData payload = ParsePayload(activity.payload);

            if (!string.IsNullOrEmpty(payload?.title)) return payload.title;
            if (!string.IsNullOrEmpty(payload?.name)) return payload.name;

            return Capitalize(GetActivityTypeLabel(activity));
        }

        private static string GetActivityTypeLabel(ChildActivity activity)
        {
            switch ((ActivityType)activity.activityType)
            {
                case ActivityType.StoryRead: return "story";
                case ActivityType.QuizAttempt: return "quiz";
                case ActivityType.GamePlayed: return "game";
                default: return "activity";
            }
        }

        private void ShowDefaults()
        {
            if (childNameText != null) childNameText.text = "-";
            if (childUsernameText != null) childUsernameText.text = "-";
            if (childPasswordText != null) childPasswordText.text = "-";
            if (readingStreakText != null) readingStreakText.text = "0";
            if (readingTimeText != null) readingTimeText.text = "00m";
            if (storiesCompletedText != null) storiesCompletedText.text = "0";
            if (quizAverageText != null) quizAverageText.text = "0%";
            if (newWordsText != null) newWordsText.text = "0";
            if (coinsText != null) coinsText.text = "0";

            ClearActivityCards();
        }

        private void ClearActivityCards()
        {
            if (activityCards == null) return;

            foreach (ParentActivityCard card in activityCards)
            {
                if (card != null) card.Clear();
            }
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

        private static DateTime ParseTime(string value)
        {
            return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime parsed)
                ? parsed
                : DateTime.MinValue;
        }

        private static string FormatRelativeTime(string createdAt)
        {
            DateTime time = ParseTime(createdAt).ToUniversalTime();
            TimeSpan span = DateTime.UtcNow - time;

            if (span.TotalMinutes < 1) return "Just now";
            if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes}m ago";
            if (span.TotalHours < 24) return $"{(int)span.TotalHours}h ago";
            return $"{(int)span.TotalDays}d ago";
        }

        private static string Capitalize(string value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return char.ToUpperInvariant(value[0]) + value.Substring(1);
        }

        private void OnLogoutClicked()
        {
            SessionManager.Instance.ClearSession();
            UnityEngine.SceneManagement.SceneManager.LoadScene(loginScene);
        }
    }
}
