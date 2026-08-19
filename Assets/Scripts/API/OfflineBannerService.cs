using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Api
{
    /// <summary>
    /// Displays a non-intrusive offline status banner when the device loses network connectivity.
    /// </summary>
    public class OfflineBannerService : MonoBehaviour
    {
        private static OfflineBannerService instance;
        public static OfflineBannerService Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("OfflineBannerService");
                    instance = go.AddComponent<OfflineBannerService>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        private GameObject _bannerRoot;
        private TextMeshProUGUI _bannerText;
        private Image _bannerBg;
        private bool _wasOffline;
        private Coroutine _hideRoutine;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);

            BuildBannerUI();
        }

        private void Start()
        {
            StartCoroutine(NetworkMonitorRoutine());
        }

        private void BuildBannerUI()
        {
            var canvasGo = new GameObject("OfflineBannerCanvas");
            canvasGo.transform.SetParent(transform, false);

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;

            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();

            _bannerRoot = new GameObject("Banner");
            _bannerRoot.transform.SetParent(canvasGo.transform, false);

            var rect = _bannerRoot.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0f, 40f);

            _bannerBg = _bannerRoot.AddComponent<Image>();
            _bannerBg.color = new Color(0.85f, 0.35f, 0.15f, 0.95f);

            var textGo = new GameObject("BannerText");
            textGo.transform.SetParent(_bannerRoot.transform, false);

            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            _bannerText = textGo.AddComponent<TextMeshProUGUI>();
            _bannerText.alignment = TextAlignmentOptions.Center;
            _bannerText.fontSize = 16;
            _bannerText.color = Color.white;
            _bannerText.text = "Offline Mode — Progress saved locally";

            _bannerRoot.SetActive(false);
        }

        private IEnumerator NetworkMonitorRoutine()
        {
            var wait = new WaitForSeconds(3f);
            while (true)
            {
                bool isOffline = Application.internetReachability == NetworkReachability.NotReachable;

                if (isOffline && !_wasOffline)
                {
                    _wasOffline = true;
                    ShowOfflineBanner();
                }
                else if (!isOffline && _wasOffline)
                {
                    _wasOffline = false;
                    ShowRestoredBanner();
                }

                yield return wait;
            }
        }

        private void ShowOfflineBanner()
        {
            if (_hideRoutine != null) StopCoroutine(_hideRoutine);

            if (_bannerRoot != null)
            {
                _bannerBg.color = new Color(0.85f, 0.35f, 0.15f, 0.95f);
                _bannerText.text = "Offline Mode — Progress saved locally";
                _bannerRoot.SetActive(true);
            }
        }

        private void ShowRestoredBanner()
        {
            if (_bannerRoot != null)
            {
                _bannerBg.color = new Color(0.2f, 0.7f, 0.3f, 0.95f);
                _bannerText.text = "Online — Syncing progress...";
                _bannerRoot.SetActive(true);
                _hideRoutine = StartCoroutine(HideBannerAfterDelay(3f));
            }
        }

        private IEnumerator HideBannerAfterDelay(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            if (_bannerRoot != null)
            {
                _bannerRoot.SetActive(false);
            }
        }
    }
}
