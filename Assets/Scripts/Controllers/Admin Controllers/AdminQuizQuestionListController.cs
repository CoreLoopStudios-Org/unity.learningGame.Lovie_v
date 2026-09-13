using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Api;
using Api.Endpoints;
using Api.Models;

namespace UI
{
    public class AdminQuizQuestionListController : MonoBehaviour
    {
        private const string ListTitleFormat = "Previous Created Question ({0:00})";

        [Header("List")]
        [SerializeField] private Transform questionsContainer;
        [SerializeField] private AdminQuizQuestionCard questionCardPrefab;

        [Header("Feedback")]
        [SerializeField] private TextMeshProUGUI listTitleText;

        private ApiClient apiClient;
        private AdminApi adminApi;
        private int requestId;

        private void Awake()
        {
            apiClient = ApiClient.Instance;
            apiClient.Initialize(ApiConfig.Instance);
            adminApi = new AdminApi(apiClient);
        }

        private void OnEnable()
        {
            _ = RefreshAsync();
        }

        public async Awaitable RefreshAsync()
        {
            string storyId = AdminQuizContext.SelectedStoryId;
            if (string.IsNullOrEmpty(storyId))
            {
                Debug.LogWarning("[AdminQuizQuestionListController] No story selected in AdminQuizContext.");
                Populate(Array.Empty<Quiz>());
                return;
            }

            if (!SessionManager.Instance.IsValidToken())
            {
                Debug.LogWarning("[AdminQuizQuestionListController] No valid session.");
                return;
            }

            int id = ++requestId;

            try
            {
                Quiz[] quizzes = await adminApi.GetQuizzesAsync();

                if (id != requestId || !isActiveAndEnabled) return;

                Populate(FilterByStory(quizzes, storyId));
            }
            catch (Exception ex)
            {
                if (id == requestId)
                    Debug.LogWarning($"[AdminQuizQuestionListController] Failed to load quizzes: {ex.Message}");
            }
        }

        private static Quiz[] FilterByStory(Quiz[] quizzes, string storyId)
        {
            if (quizzes == null) return Array.Empty<Quiz>();

            int count = 0;
            foreach (Quiz quiz in quizzes)
            {
                if (quiz != null && quiz.storyId == storyId) count++;
            }

            Quiz[] result = new Quiz[count];
            int index = 0;
            foreach (Quiz quiz in quizzes)
            {
                if (quiz != null && quiz.storyId == storyId) result[index++] = quiz;
            }
            return result;
        }

        private void Populate(Quiz[] quizzes)
        {
            ClearSpawnedCards();

            if (listTitleText != null)
                listTitleText.text = string.Format(ListTitleFormat, quizzes.Length);

            if (quizzes.Length == 0) return;

            if (questionsContainer == null || questionCardPrefab == null)
            {
                Debug.LogWarning("[AdminQuizQuestionListController] Questions container or card prefab not assigned.");
                return;
            }

            foreach (Quiz quiz in quizzes)
            {
                AdminQuizQuestionCard card = Instantiate(questionCardPrefab, questionsContainer);

                string question = quiz.title;
                string[] options = null;
                int correctIndex = -1;

                QuizPayload payload = ParsePayload(quiz.questionsPayload);
                if (payload != null && payload.questions != null && payload.questions.Length > 0)
                {
                    question = payload.questions[0].question;
                    options = payload.questions[0].options;
                    correctIndex = payload.questions[0].correctIndex;
                }

                card.Setup(question, options, correctIndex);
            }
        }

        private static QuizPayload ParsePayload(string questionsPayload)
        {
            if (string.IsNullOrWhiteSpace(questionsPayload)) return null;
            try
            {
                return JsonUtility.FromJson<QuizPayload>(questionsPayload);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private void ClearSpawnedCards()
        {
            if (questionsContainer == null) return;

            foreach (Transform child in questionsContainer)
            {
                Destroy(child.gameObject);
            }
        }
    }
}
