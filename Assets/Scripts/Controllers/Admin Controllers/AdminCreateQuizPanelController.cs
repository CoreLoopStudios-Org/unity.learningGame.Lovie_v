using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Api;
using Api.Endpoints;
using Api.Models;

namespace UI
{
    public class AdminCreateQuizPanelController : MonoBehaviour
    {
        private const int PublishedStatus = 2;

        [Header("Story")]
        [SerializeField] private TextMeshProUGUI storyNameText;

        [Header("Form")]
        [SerializeField] private TMP_InputField questionInput;
        [SerializeField] private TMP_InputField[] optionInputs = new TMP_InputField[4];
        [SerializeField] private Button[] answerMarkButtons = new Button[4];
        [SerializeField] private Sprite selectedAnswerSprite;
        [SerializeField] private Sprite defaultAnswerSprite;
        [SerializeField] private Button addQuizButton;

        [Header("Previous Questions")]
        [SerializeField] private AdminQuizQuestionListController questionList;

        [Header("Feedback")]
        [SerializeField] private TextMeshProUGUI statusFeedbackText;

        private ApiClient apiClient;
        private AdminApi adminApi;
        private int requestId;
        private int selectedIndex = -1;

        private void Awake()
        {
            apiClient = ApiClient.Instance;
            apiClient.Initialize(ApiConfig.Instance);
            adminApi = new AdminApi(apiClient);

            if (answerMarkButtons != null)
            {
                for (int i = 0; i < answerMarkButtons.Length; i++)
                {
                    if (answerMarkButtons[i] == null) continue;

                    int index = i;
                    answerMarkButtons[i].onClick.AddListener(() => OnAnswerMarkClicked(index));
                }
            }

            if (addQuizButton != null)
                addQuizButton.onClick.AddListener(OnAddQuizClicked);
        }

        private void OnDestroy()
        {
            if (answerMarkButtons != null)
            {
                foreach (Button button in answerMarkButtons)
                {
                    if (button != null) button.onClick.RemoveAllListeners();
                }
            }

            if (addQuizButton != null)
                addQuizButton.onClick.RemoveListener(OnAddQuizClicked);
        }

        private void OnEnable()
        {
            selectedIndex = -1;
            RefreshAnswerMarks();
            _ = LoadContentAsync();
        }

        private void OnAnswerMarkClicked(int index)
        {
            selectedIndex = index;
            RefreshAnswerMarks();
        }

        private void RefreshAnswerMarks()
        {
            if (answerMarkButtons == null) return;

            for (int i = 0; i < answerMarkButtons.Length; i++)
            {
                if (answerMarkButtons[i] == null) continue;

                Image markImage = answerMarkButtons[i].image;
                if (markImage == null) continue;

                Sprite sprite = i == selectedIndex ? selectedAnswerSprite : defaultAnswerSprite;
                if (sprite != null) markImage.sprite = sprite;
            }
        }

        private async Awaitable LoadContentAsync()
        {
            ClearStatus();

            string storyId = AdminQuizContext.SelectedStoryId;
            if (string.IsNullOrEmpty(storyId))
            {
                Debug.LogWarning("[AdminCreateQuizPanelController] No story selected in AdminQuizContext.");
                ShowStatus("No story selected.");
                return;
            }

            if (!SessionManager.Instance.IsValidToken())
            {
                Debug.LogWarning("[AdminCreateQuizPanelController] No valid session.");
                return;
            }

            int id = ++requestId;

            try
            {
                Story story = await adminApi.GetStoryAsync(storyId);

                if (id != requestId) return;

                if (storyNameText != null)
                    storyNameText.text = story?.title ?? string.Empty;
            }
            catch (Exception ex)
            {
                if (id == requestId)
                    ShowStatus($"Failed to load story: {ex.Message}");
            }
        }

        private void OnAddQuizClicked()
        {
            _ = AddQuizAsync();
        }

        private async Awaitable AddQuizAsync()
        {
            string question = questionInput != null ? questionInput.text.Trim() : string.Empty;
            if (string.IsNullOrEmpty(question))
            {
                ShowStatus("Please enter the question.");
                return;
            }

            string[] options = new string[optionInputs.Length];
            for (int i = 0; i < optionInputs.Length; i++)
            {
                options[i] = optionInputs[i] != null ? optionInputs[i].text.Trim() : string.Empty;
                if (string.IsNullOrEmpty(options[i]))
                {
                    ShowStatus($"Please fill option {i + 1}.");
                    return;
                }
            }

            if (selectedIndex < 0)
            {
                ShowStatus("Please mark the correct answer.");
                return;
            }

            string storyId = AdminQuizContext.SelectedStoryId;
            if (string.IsNullOrEmpty(storyId))
            {
                ShowStatus("No story selected.");
                return;
            }

            if (!SessionManager.Instance.IsValidToken())
            {
                ShowStatus("No valid session.");
                return;
            }

            QuizPayload payload = new QuizPayload
            {
                questions = new[] { new QuizPayloadQuestion { question = question, options = options, correctIndex = selectedIndex } }
            };
            string payloadJson = JsonUtility.ToJson(payload);

            if (addQuizButton != null) addQuizButton.interactable = false;

            try
            {
                await adminApi.CreateQuizAsync(storyId, question, payloadJson, PublishedStatus);

                if (this == null) return;

                ClearForm();

                if (questionList != null)
                    await questionList.RefreshAsync();

                if (this == null) return;

                ShowStatus("Quiz added.");
            }
            catch (Exception ex)
            {
                if (this != null)
                    ShowStatus($"Failed to add quiz: {ex.Message}");
            }
            finally
            {
                if (this != null && addQuizButton != null) addQuizButton.interactable = true;
            }
        }

        private void ClearForm()
        {
            if (questionInput != null) questionInput.text = string.Empty;

            if (optionInputs != null)
            {
                foreach (TMP_InputField input in optionInputs)
                {
                    if (input != null) input.text = string.Empty;
                }
            }

            selectedIndex = -1;
            RefreshAnswerMarks();
        }

        private void ShowStatus(string message)
        {
            if (statusFeedbackText != null)
            {
                statusFeedbackText.text = message;
                statusFeedbackText.gameObject.SetActive(true);
            }
        }

        private void ClearStatus()
        {
            if (statusFeedbackText != null)
            {
                statusFeedbackText.text = string.Empty;
                statusFeedbackText.gameObject.SetActive(false);
            }
        }
    }
}
