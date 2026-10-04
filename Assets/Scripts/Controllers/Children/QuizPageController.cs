using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Newtonsoft.Json;
using Api;
using Api.Endpoints;
using Api.Models;

namespace UI
{
    /// <summary>
    /// Quiz page flow: Default Panel (Next disabled) until the child finishes a story,
    /// then that story's quiz questions one by one. Correct answers enable Next, wrong
    /// ones flash red and reset; finishing shows the animation panel and logs the reward.
    /// Option visuals live on QuizOptionView, content loading on QuizContentService.
    /// </summary>
    public class QuizPageController : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject defaultPanel;
        [SerializeField] private GameObject quizSection;
        [SerializeField] private GameObject animationPanel;

        [Header("Quiz UI")]
        [SerializeField] private TextMeshProUGUI questionText;
        [SerializeField] private TextMeshProUGUI questionCountText;
        [Tooltip("Fill rect inside the Fill Bar — its width is scaled by progress.")]
        [SerializeField] private RectTransform progressFill;
        [Tooltip("Next Button object — a Button component is added if missing.")]
        [SerializeField] private GameObject nextButton;

        [Header("Options (order = Option 1..4, QuizOptionView on each)")]
        [SerializeField] private QuizOptionView[] options = new QuizOptionView[4];

        [Header("Rules & Feedback")]
        [SerializeField] private QuizOptionView.Theme theme = new QuizOptionView.Theme();
        [SerializeField, Min(0.1f)] private float wrongFlashSeconds = 2f;
        [SerializeField] private int coinsPerQuiz = 25;
        [SerializeField] private TextMeshProUGUI statusText;

        private readonly List<QuizQuestion> questions = new List<QuizQuestion>();
        private int currentIndex;
        private bool questionAnswered;
        private bool optionsLocked;
        private int requestId;
        private float quizStartTime;

        private Button nextButtonComponent;
        private float progressFillWidth;

        private void Awake()
        {
            if (nextButton != null)
            {
                nextButtonComponent = nextButton.GetComponent<Button>();
                if (nextButtonComponent == null)
                {
                    nextButtonComponent = nextButton.AddComponent<Button>();
                }

                nextButtonComponent.transition = Selectable.Transition.None;
                nextButtonComponent.targetGraphic = null;
                nextButtonComponent.onClick.AddListener(HandleNextClicked);
            }

            if (options != null)
            {
                for (int i = 0; i < options.Length; i++)
                {
                    QuizOptionView option = options[i];
                    if (option == null) continue;

                    option.Configure(theme);
                    int index = i;
                    option.Clicked += () => HandleOptionClicked(index);
                }
            }
        }

        private void OnDestroy()
        {
            if (nextButtonComponent != null)
            {
                nextButtonComponent.onClick.RemoveListener(HandleNextClicked);
            }
        }

        private void OnEnable()
        {
            int id = ++requestId;

            // Default panel doubles as the loading placeholder; Next stays disabled
            // until a question is on screen and answered.
            ShowOnly(defaultPanel);
            SetNextInteractable(false);
            ClearStatus();

            _ = LoadQuizAsync(id);
        }

        private void OnDisable()
        {
            requestId++;
        }

        private async Awaitable LoadQuizAsync(int id)
        {
            if (SessionManager.Instance == null || !SessionManager.Instance.IsChildSession)
            {
                Debug.LogWarning("[QuizPageController] No child session — cannot load quizzes.");
                ShowStatus("Please log in to see your quiz.");
                return;
            }

            try
            {
                int slotCount = options != null ? options.Length : 0;
                List<QuizGroup> groups = await QuizContentService.LoadNextStoryQuizzesAsync(
                    slotCount, () => id != requestId || !isActiveAndEnabled);

                if (groups == null) return; // cancelled — page disabled or reloaded

                if (groups.Count == 0)
                {
                    ShowOnly(defaultPanel);
                    ShowStatus("Finish reading a story to unlock its quiz!");
                    return;
                }

                BeginQuiz(groups);
            }
            catch (Exception ex)
            {
                if (id == requestId)
                {
                    ShowStatus($"Failed to load quiz: {ex.Message}");
                    Debug.LogWarning($"[QuizPageController] {ex}");
                }
            }
        }

        private void BeginQuiz(List<QuizGroup> groups)
        {
            questions.Clear();

            foreach (QuizGroup group in groups)
            {
                foreach (QuizQuestion question in group.questions)
                {
                    question.Group = group;
                    questions.Add(question);
                }

                group.lastQuestionIndex = questions.Count - 1;
            }

            currentIndex = 0;
            quizStartTime = Time.unscaledTime;

            ShowOnly(quizSection);
            ShowQuestion();
        }

        private void ShowQuestion()
        {
            questionAnswered = false;
            optionsLocked = false;
            SetNextInteractable(false);

            QuizQuestion question = questions[currentIndex];

            if (questionText != null)
            {
                questionText.text = question.text ?? string.Empty;
            }

            if (questionCountText != null)
            {
                questionCountText.text = $"{currentIndex + 1} / {questions.Count}";
            }

            for (int i = 0; i < options.Length; i++)
            {
                QuizOptionView option = options[i];
                if (option == null) continue;

                option.ResetVisual();
                option.SetAnswer(i < question.options.Length ? question.options[i] : string.Empty);
                option.SetInteractable(true);
            }

            _ = UpdateProgressBarDeferredAsync();
        }

        private void HandleOptionClicked(int index)
        {
            if (optionsLocked || questionAnswered) return;
            if (index < 0 || index >= options.Length) return;
            if (currentIndex < 0 || currentIndex >= questions.Count) return;

            QuizQuestion question = questions[currentIndex];

            if (index == question.correctIndex)
            {
                questionAnswered = true;
                options[index]?.ShowCorrect();
                LockOptions();
                SetNextInteractable(true);

                HandleQuizCompleted(question);
            }
            else
            {
                question.firstTryCorrect = false;
                _ = FlashWrongAnswerAsync(index, requestId);
            }
        }

        private async Awaitable FlashWrongAnswerAsync(int index, int id)
        {
            optionsLocked = true;
            options[index]?.ShowWrong();

            await Awaitable.WaitForSecondsAsync(wrongFlashSeconds);

            // Page disabled or closed while the indicator was showing.
            if (this == null || id != requestId) return;

            options[index]?.ResetVisual();
            optionsLocked = false;
        }

        private void HandleQuizCompleted(QuizQuestion question)
        {
            QuizGroup group = question.Group;
            if (group == null || currentIndex != group.lastQuestionIndex) return;

            // The last question of this quiz was just answered — record + reward it.
            string childId = SessionManager.Instance != null ? SessionManager.Instance.ChildId : null;
            StoryProgressStore.MarkQuizTaken(childId, group.quizId);
            _ = ReportQuizCompletionAsync(group);
        }

        private async Awaitable ReportQuizCompletionAsync(QuizGroup group)
        {
            int firstTry = 0;
            foreach (QuizQuestion question in group.questions)
            {
                if (question.firstTryCorrect) firstTry++;
            }

            int score = group.questions.Count == 0 ? 0 : Mathf.RoundToInt(100f * firstTry / group.questions.Count);
            float timeSpent = Time.unscaledTime - quizStartTime;

            // Backend GAP-2: coins are computed server-side; the payload requests coinsPerQuiz.
            string payload = JsonConvert.SerializeObject(new
            {
                score,
                total = group.questions.Count,
                correct = firstTry,
                isComplete = true,
                coins = coinsPerQuiz,
                timeSpent = Mathf.RoundToInt(timeSpent)
            });

            try
            {
                var apiClient = ApiClient.Instance;
                apiClient.Initialize(ApiConfig.Instance);

                ActivityLogged response = await new ChildApi(apiClient).LogQuizActivityAsync(group.quizId, payload);

                if (response != null && response.totalCoins > 0)
                {
                    CoinWallet.Instance?.UpdateBalance(response.totalCoins);
                }
                else if (CoinWallet.Instance != null)
                {
                    await CoinWallet.Instance.RefreshAsync();
                }
            }
            catch (ApiException ex)
            {
                Debug.LogWarning($"[QuizPageController] Quiz activity failed, queuing offline: {ex.Message}");
                OfflineActivityQueue.Instance?.EnqueueActivity(group.quizId, payload);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[QuizPageController] Quiz activity error, queuing offline: {ex.Message}");
                OfflineActivityQueue.Instance?.EnqueueActivity(group.quizId, payload);
            }
        }

        private void HandleNextClicked()
        {
            if (!questionAnswered) return;

            if (currentIndex < questions.Count - 1)
            {
                currentIndex++;
                ShowQuestion();
            }
            else
            {
                SetNextInteractable(false);
                ShowOnly(animationPanel);
            }
        }

        private async Awaitable UpdateProgressBarDeferredAsync()
        {
            // Wait a frame so the freshly activated quiz section has a laid-out rect.
            await Awaitable.NextFrameAsync();
            if (this == null || !isActiveAndEnabled) return;

            UpdateProgressBar();
        }

        private void UpdateProgressBar()
        {
            if (progressFill == null) return;

            if (progressFillWidth <= 0f)
            {
                progressFillWidth = progressFill.rect.width;
                if (progressFillWidth <= 0f && progressFill.parent is RectTransform parent)
                {
                    progressFillWidth = parent.rect.width;
                }
            }

            if (progressFillWidth <= 0f) return;

            float fraction = questions.Count == 0 ? 0f : (float)(currentIndex + 1) / questions.Count;
            progressFill.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, progressFillWidth * fraction);
        }

        private void LockOptions()
        {
            foreach (QuizOptionView option in options)
            {
                option?.SetInteractable(false);
            }
        }

        private void SetNextInteractable(bool interactable)
        {
            if (nextButtonComponent != null)
            {
                nextButtonComponent.interactable = interactable;
            }
        }

        private void ShowOnly(GameObject panel)
        {
            SetPanelActive(defaultPanel, panel == defaultPanel);
            SetPanelActive(quizSection, panel == quizSection);
            SetPanelActive(animationPanel, panel == animationPanel);
        }

        private static void SetPanelActive(GameObject panel, bool active)
        {
            if (panel != null && panel.activeSelf != active)
            {
                panel.SetActive(active);
            }
        }

        private void ShowStatus(string message)
        {
            if (statusText == null) return;

            statusText.text = message;
            statusText.gameObject.SetActive(true);
        }

        private void ClearStatus()
        {
            if (statusText == null) return;

            statusText.text = string.Empty;
            statusText.gameObject.SetActive(false);
        }
    }
}
