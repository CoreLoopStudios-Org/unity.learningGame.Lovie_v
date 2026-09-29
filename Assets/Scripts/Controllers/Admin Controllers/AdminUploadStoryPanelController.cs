using System;
using Media;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Api;
using Api.Endpoints;

namespace UI
{
    public class AdminUploadStoryPanelController : MonoBehaviour
    {
        // Backend ContentStatus: None=0, Draft=1, Published=2.
        private const int PublishedStatus = 2;

        [Header("Form Fields")]
        [SerializeField] private TMP_InputField titleInput;
        [SerializeField] private TMP_InputField categoryInput;
        [SerializeField] private TMP_InputField priceInput;
        [SerializeField] private TMP_InputField contentInput;

        [Header("Background Image")]
        [SerializeField] private StoryBackgroundImageSelector backgroundImageSelector;

        [Header("Actions")]
        [SerializeField] private Button uploadButton;

        [Header("Feedback")]
        [SerializeField] private TextMeshProUGUI statusFeedbackText;

        private ApiClient apiClient;
        private AdminApi adminApi;
        private bool isUploading;

        private void Awake()
        {
            apiClient = ApiClient.Instance;
            apiClient.Initialize(ApiConfig.Instance);
            adminApi = new AdminApi(apiClient);

            if (uploadButton != null)
                uploadButton.onClick.AddListener(OnUploadClicked);

            if (backgroundImageSelector != null)
                backgroundImageSelector.OnSelectionFailed += ShowStatus;
        }

        private void OnDestroy()
        {
            if (uploadButton != null)
                uploadButton.onClick.RemoveListener(OnUploadClicked);

            if (backgroundImageSelector != null)
                backgroundImageSelector.OnSelectionFailed -= ShowStatus;
        }

        private void OnUploadClicked()
        {
            if (isUploading) return;
            _ = UploadAsync();
        }

        private async Awaitable UploadAsync()
        {
            string title = GetInputText(titleInput);
            string category = GetInputText(categoryInput);
            string content = GetInputText(contentInput);
            string priceText = GetInputText(priceInput);

            if (string.IsNullOrEmpty(title))
            {
                ShowStatus("Story title is required.");
                return;
            }
            if (string.IsNullOrEmpty(content))
            {
                ShowStatus("Story content is required.");
                return;
            }
            int? priceInCoins = null;
            if (!string.IsNullOrEmpty(priceText))
            {
                if (!int.TryParse(priceText, out int parsedPrice) || parsedPrice < 0)
                {
                    ShowStatus("Price must be a whole number of 0 or more.");
                    return;
                }
                priceInCoins = parsedPrice;
            }

            // The story API has no category column, so it is embedded in
            // contentPayload. "content" matches the child-side StoryQuestLevel
            // field so the uploaded text stays playable.
            var payload = new StoryContentPayload { category = category, content = content };

            SetUploading(true);

            try
            {
                string coverImageUrl = await UploadBackgroundImageAsync();
                ShowStatus("Uploading story...");
                string newStoryId = await adminApi.CreateStoryAsync(
                    title, coverImageUrl, JsonUtility.ToJson(payload), PublishedStatus, priceInCoins ?? 0);

                string message = $"Story uploaded successfully{(string.IsNullOrEmpty(newStoryId) ? "" : $" (id: {newStoryId})")}.";

                if (priceInCoins.HasValue)
                {
                    message += $" Listed for {priceInCoins.Value} coins.";
                }

                ShowStatus(message);
                ClearForm();
            }
            catch (ApiException ex)
            {
                ShowStatus($"Failed to upload story: {ex.Message}");
            }
            catch (Exception ex)
            {
                ShowStatus($"Failed to upload story: {ex.Message}");
            }
            finally
            {
                SetUploading(false);
            }
        }

        private async Awaitable<string> UploadBackgroundImageAsync()
        {
            if (backgroundImageSelector == null || !backgroundImageSelector.HasImage) return null;

            ShowStatus("Uploading background image...");
            string url = await adminApi.UploadMediaAsync(
                backgroundImageSelector.ImageBytes, backgroundImageSelector.FileName, MediaConstants.JPEG_MIME_TYPE);
            if (string.IsNullOrEmpty(url))
                throw new Exception("Image upload did not return a URL.");
            return url;
        }

        private void SetUploading(bool uploading)
        {
            isUploading = uploading;
            if (uploadButton != null) uploadButton.interactable = !uploading;
            if (backgroundImageSelector != null) backgroundImageSelector.SetInteractable(!uploading);
        }

        private static string GetInputText(TMP_InputField input)
        {
            return input != null ? input.text.Trim() : string.Empty;
        }

        private void ClearForm()
        {
            if (titleInput != null) titleInput.text = string.Empty;
            if (categoryInput != null) categoryInput.text = string.Empty;
            if (priceInput != null) priceInput.text = string.Empty;
            if (backgroundImageSelector != null) backgroundImageSelector.Clear();
            if (contentInput != null) contentInput.text = string.Empty;
        }

        private void ShowStatus(string message)
        {
            if (statusFeedbackText != null)
            {
                statusFeedbackText.text = message;
                statusFeedbackText.gameObject.SetActive(true);
            }
        }

        [Serializable]
        private class StoryContentPayload
        {
            public string category;
            public string content;
        }
    }
}
