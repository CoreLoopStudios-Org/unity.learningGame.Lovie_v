using System.Collections;
using UnityEngine;
using TMPro;
using Api;

namespace UI
{
    // Standalone coin display: attach to any object with TMP_Text and it keeps
    // itself in sync with CoinWallet for the child's whole session. No
    // page-controller wiring needed.
    public class CoinBalanceText : MonoBehaviour
    {
        [SerializeField] private TMP_Text coinText;

        [SerializeField] private string prefix = "";

        private bool bound;
        private Coroutine bindRoutine;

        private void Awake()
        {
            if (coinText == null)
                coinText = GetComponent<TMP_Text>();
        }

        private void OnEnable()
        {
            bindRoutine = StartCoroutine(BindWhenWalletReady());
        }

        private void OnDisable()
        {
            if (bindRoutine != null)
            {
                StopCoroutine(bindRoutine);
                bindRoutine = null;
            }

            Unbind();
        }

        // CoinWallet is created lazily at child login, so it may not exist yet
        // when this panel enables.
        private IEnumerator BindWhenWalletReady()
        {
            while (CoinWallet.Instance == null)
                yield return null;

            CoinWallet.Instance.OnBalanceChanged += UpdateText;
            bound = true;
            UpdateText(CoinWallet.Instance.Balance);
        }

        private void Unbind()
        {
            if (bound && CoinWallet.Instance != null)
                CoinWallet.Instance.OnBalanceChanged -= UpdateText;
            bound = false;
        }

        private void UpdateText(int balance)
        {
            if (coinText != null)
                coinText.text = $"{prefix}{balance}";
        }
    }
}
