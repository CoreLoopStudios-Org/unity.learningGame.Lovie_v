using UnityEngine;
using UnityEngine.UI;
using Api;
using TMPro;

namespace UI
{
    public class MainMenuHUD : MonoBehaviour
    {
        [Header("Coin Display")]
        [SerializeField] private TextMeshProUGUI coinText;
        [SerializeField] private string coinPrefix = "Coins: ";
        [SerializeField] private bool autoRefreshOnStart = true;

        [Header("Streak Display")]
        [SerializeField] private TextMeshProUGUI streakText;
        [SerializeField] private string streakPrefix = "Streak: ";

        private void OnEnable()
        {
            if (CoinWallet.Instance != null)
            {
                CoinWallet.Instance.OnBalanceChanged += HandleBalanceChanged;
                CoinWallet.Instance.OnStreakChanged += HandleStreakChanged;

                if (autoRefreshOnStart)
                {
                    Refresh();
                }
                else
                {
                    UpdateDisplay(CoinWallet.Instance.Balance, CoinWallet.Instance.Streak);
                }
            }
        }

        private void OnDisable()
        {
            if (CoinWallet.Instance != null)
            {
                CoinWallet.Instance.OnBalanceChanged -= HandleBalanceChanged;
                CoinWallet.Instance.OnStreakChanged -= HandleStreakChanged;
            }
        }

        private void Start()
        {
            if (CoinWallet.Instance != null && autoRefreshOnStart)
            {
                Refresh();
            }
        }

        public async void Refresh()
        {
            if (CoinWallet.Instance != null)
            {
                await CoinWallet.Instance.RefreshAsync();
                UpdateDisplay(CoinWallet.Instance.Balance, CoinWallet.Instance.Streak);
            }
        }

        private void HandleBalanceChanged(int newBalance)
        {
            UpdateCoinDisplay(newBalance);
        }

        private void HandleStreakChanged(int newStreak)
        {
            UpdateStreakDisplay(newStreak);
        }

        private void UpdateDisplay(int coins, int streak)
        {
            UpdateCoinDisplay(coins);
            UpdateStreakDisplay(streak);
        }

        private void UpdateCoinDisplay(int coins)
        {
            if (coinText != null)
            {
                coinText.text = $"{coinPrefix}{coins}";
            }
        }

        private void UpdateStreakDisplay(int streak)
        {
            if (streakText != null)
            {
                streakText.text = $"{streakPrefix}{streak}";
            }
        }

        public void SetCoinText(TextMeshProUGUI text)
        {
            coinText = text;
        }

        public void SetStreakText(TextMeshProUGUI text)
        {
            streakText = text;
        }
    }
}
