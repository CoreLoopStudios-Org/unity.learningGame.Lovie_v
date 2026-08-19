using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Api;
using Api.Endpoints;
using Api.Models;
using System;

namespace UI
{
    public class DailyRewardPopup : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI rewardTitle;
        [SerializeField] private TextMeshProUGUI coinsText;
        [SerializeField] private TextMeshProUGUI streakText;
        [SerializeField] private Button claimButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private GameObject loadingIndicator;

        private ChildApi childApi;
        private Action onClose;

        void Start()
        {
            var config = ApiConfig.LoadConfig();
            var apiClient = new ApiClient(config);
            childApi = new ChildApi(apiClient);

            if (claimButton != null)
                claimButton.onClick.AddListener(OnClaimClicked);

            if (closeButton != null)
                closeButton.onClick.AddListener(OnCloseClicked);

            gameObject.SetActive(false);
        }

        public void Show(Action onCloseCallback = null)
        {
            onClose = onCloseCallback;
            gameObject.SetActive(true);
        }

        async void OnClaimClicked()
        {
            ShowLoading();

            try
            {
                var result = await childApi.ClaimDailyRewardAsync();

                if (result != null)
                {
                    if (result.alreadyClaimed)
                    {
                        ShowResult("Already Claimed!", "Come back tomorrow!");
                    }
                    else
                    {
                        ShowResult("Daily Reward!", $"+{result.coinsAwarded} Coins!");
                        UpdateCoinDisplay(result.totalCoins, result.loginStreak);
                    }
                }

                if (claimButton != null)
                    claimButton.interactable = false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Claim reward error: {ex.Message}");
                HideLoading();
            }
        }

        void OnCloseClicked()
        {
            gameObject.SetActive(false);
            onClose?.Invoke();
        }

        void ShowResult(string title, string message)
        {
            if (rewardTitle != null)
                rewardTitle.text = title;

            if (coinsText != null)
                coinsText.text = message;

            HideLoading();
        }

        void UpdateCoinDisplay(int totalCoins, int streak)
        {
            if (coinsText != null)
                coinsText.text = $"+{totalCoins} Total Coins";

            if (streakText != null)
                streakText.text = $"{streak} Day Streak!";
        }

        void ShowLoading()
        {
            if (loadingIndicator != null)
                loadingIndicator.SetActive(true);
        }

        void HideLoading()
        {
            if (loadingIndicator != null)
                loadingIndicator.SetActive(false);
        }

        void OnDestroy()
        {
            if (claimButton != null)
                claimButton.onClick.RemoveListener(OnClaimClicked);

            if (closeButton != null)
                closeButton.onClick.RemoveListener(OnCloseClicked);
        }
    }
}
