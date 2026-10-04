using System;
using System.Collections.Generic;
using UnityEngine;
using Api;
using Api.Endpoints;
using Api.Models;

namespace UI
{
    public class QuizQuestion
    {
        public string text;
        public string[] options;
        public int correctIndex;
        public bool firstTryCorrect = true;
        public QuizGroup Group;
    }

    /// <summary>
    /// One quiz from the backend and its parsed questions. lastQuestionIndex is the
    /// question's position in the flattened page sequence — used to detect quiz completion.
    /// </summary>
    public class QuizGroup
    {
        public string quizId;
        public List<QuizQuestion> questions = new List<QuizQuestion>();
        public int lastQuestionIndex;
    }

    /// <summary>
    /// Finds the most recently finished story that still has untaken quizzes and
    /// parses its questionsPayload into quiz groups.
    /// </summary>
    public static class QuizContentService
    {
        /// <returns>
        /// Quiz groups for the next story to play; an empty list when there is nothing to
        /// quiz; null when the load was cancelled (isCancelled returned true).
        /// </returns>
        public static async Awaitable<List<QuizGroup>> LoadNextStoryQuizzesAsync(int optionSlotCount, Func<bool> isCancelled = null)
        {
            var apiClient = ApiClient.Instance;
            apiClient.Initialize(ApiConfig.Instance);
            var childApi = new ChildApi(apiClient);

            string childId = SessionManager.Instance != null ? SessionManager.Instance.ChildId : null;
            List<StoryReadingRecord> completedStories = StoryProgressStore.GetCompletedStories(childId);
            var takenQuizIds = new HashSet<string>(StoryProgressStore.GetTakenQuizIds(childId));

            foreach (StoryReadingRecord record in completedStories)
            {
                Quiz[] quizzes = await childApi.GetQuizzesAsync(record.storyId);
                if (isCancelled != null && isCancelled()) return null;

                List<QuizGroup> groups = ParseQuizzes(quizzes, takenQuizIds, optionSlotCount);
                if (groups.Count > 0) return groups;
            }

            return new List<QuizGroup>();
        }

        private static List<QuizGroup> ParseQuizzes(Quiz[] quizzes, HashSet<string> takenQuizIds, int optionSlotCount)
        {
            var groups = new List<QuizGroup>();
            if (quizzes == null) return groups;

            foreach (Quiz quiz in quizzes)
            {
                if (quiz == null || string.IsNullOrEmpty(quiz.questionsPayload)) continue;
                if (takenQuizIds.Contains(quiz.id)) continue;

                QuizPayload payload;
                try
                {
                    payload = JsonUtility.FromJson<QuizPayload>(quiz.questionsPayload);
                }
                catch
                {
                    payload = null;
                }

                if (payload?.questions == null) continue;

                var group = new QuizGroup { quizId = quiz.id };

                foreach (QuizPayloadQuestion payloadQuestion in payload.questions)
                {
                    if (payloadQuestion?.options == null || payloadQuestion.options.Length == 0) continue;
                    if (payloadQuestion.correctIndex < 0 || payloadQuestion.correctIndex >= payloadQuestion.options.Length) continue;
                    if (payloadQuestion.options.Length > optionSlotCount)
                    {
                        Debug.LogWarning($"[QuizContentService] Skipping question with {payloadQuestion.options.Length} options — UI has {optionSlotCount} slots.");
                        continue;
                    }

                    group.questions.Add(new QuizQuestion
                    {
                        text = payloadQuestion.question,
                        options = payloadQuestion.options,
                        correctIndex = payloadQuestion.correctIndex
                    });
                }

                if (group.questions.Count > 0)
                {
                    groups.Add(group);
                }
            }

            return groups;
        }
    }
}
