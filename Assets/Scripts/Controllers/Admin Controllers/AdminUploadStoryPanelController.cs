using System;
using System.IO;
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
        [SerializeField] private TMP_InputField imagePathInput;
        [SerializeField] private TMP_InputField contentInput;

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
        }

        private void OnDestroy()
        {
            if (uploadButton != null)
                uploadButton.onClick.RemoveListener(OnUploadClicked);
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
            string imagePath = GetInputText(imagePathInput);
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
            if (!string.IsNullOrEmpty(imagePath) && !IsUrl(imagePath) && !File.Exists(imagePath))
            {
                ShowStatus($"Image file not found: {imagePath}");
                return;
            }

            // The story API has no category column, so it is embedded in
            // contentPayload. "content" matches the child-side StoryQuestLevel
            // field so the uploaded text stays playable.
            var payload = new StoryContentPayload { category = category, content = content };

            isUploading = true;
            if (uploadButton != null) uploadButton.interactable = false;
            ShowStatus("Uploading story...");

            try
            {
                string coverImageUrl = await ResolveCoverImageUrlAsync(imagePath);
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
                isUploading = false;
                if (uploadButton != null) uploadButton.interactable = true;
            }
        }

        // Accepts a direct image URL, or a local file path which is uploaded
        // via /api/admin/media/upload to get a hosted URL.
        private async Awaitable<string> ResolveCoverImageUrlAsync(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return null;

            if (IsUrl(raw)) return raw;

            byte[] fileData = File.ReadAllBytes(raw);
            string url = await adminApi.UploadMediaAsync(fileData, Path.GetFileName(raw));
            if (string.IsNullOrEmpty(url))
                throw new Exception("Image upload did not return a URL.");
            return url;
        }

        private static bool IsUrl(string value)
        {
            return value.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || value.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
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
            if (imagePathInput != null) imagePathInput.text = string.Empty;
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
