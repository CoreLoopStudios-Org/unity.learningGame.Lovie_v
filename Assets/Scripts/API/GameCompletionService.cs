using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Newtonsoft.Json;
using Api.Endpoints;
using Api.Models;

namespace Api
{
    public class GameCompletionService : MonoBehaviour
    {
        private static GameCompletionService instance;
        public static GameCompletionService Instance
        {
            get
            {
                if (instance == null)
                {
                    EnsureInstance();
                }
                return instance;
            }
        }

        [Header("Panel Prefab")]
        [SerializeField] private GameObject completionPanelPrefab;

        [Header("Scene Navigation")]
        [SerializeField] private string mainMenuSceneName = "Main Game/Children/Main Menu";

        [Header("UI References (auto-assigned from prefab)")]
        [SerializeField] private GameObject currentPanel;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text coinsEarnedText;
        [SerializeField] private TMP_Text totalCoinsText;
        [SerializeField] private Button continueButton;
        [SerializeField] private TMP_Text loadingText;

        private ChildApi childApi;
        private bool isShowingCompletion;

        public event Action<GameResult, ActivityLogged> OnCompletionDisplayed;
        public event Action OnContinuePressed;

        public static GameCompletionService EnsureInstance()
        {
            if (instance != null) return instance;

            var existing = FindFirstObjectByType<GameCompletionService>();
            if (existing != null)
            {
                instance = existing;
                return instance;
            }

            var go = new GameObject("GameCompletionService");
            instance = go.AddComponent<GameCompletionService>();
            return instance;
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);

            EnsureCanvasSetup();
        }

        private void Start()
        {
            InitializeDependencies();
            EnsurePanelInstantiated();
        }

        private void InitializeDependencies()
        {
            if (childApi == null)
            {
                var apiClient = ApiClient.Instance;
                if (apiClient != null)
                {
                    childApi = new ChildApi(apiClient);
                }
            }
        }

        private void EnsureCanvasSetup()
        {
            var canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 999;
            }

            var scaler = GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;
            }

            var raycaster = GetComponent<GraphicRaycaster>();
            if (raycaster == null)
            {
                gameObject.AddComponent<GraphicRaycaster>();
            }
        }

        private void EnsurePanelInstantiated()
        {
            if (currentPanel != null) return;

            if (completionPanelPrefab == null)
            {
                completionPanelPrefab = Resources.Load<GameObject>("Prefabs/GameCompletionPanel");
                if (completionPanelPrefab == null)
                {
                    completionPanelPrefab = Resources.Load<GameObject>("GameCompletionPanel");
                }
            }

            if (completionPanelPrefab != null)
            {
                currentPanel = Instantiate(completionPanelPrefab, transform);
                currentPanel.SetActive(false);
                BindPanelUI(currentPanel);
            }
            else
            {
                CreateFallbackPanel();
            }
        }

        private void BindPanelUI(GameObject panel)
        {
            scoreText = panel.transform.Find("ScoreText")?.GetComponent<TMP_Text>();
            coinsEarnedText = panel.transform.Find("CoinsEarnedText")?.GetComponent<TMP_Text>();
            totalCoinsText = panel.transform.Find("TotalCoinsText")?.GetComponent<TMP_Text>();
            loadingText = panel.transform.Find("LoadingText")?.GetComponent<TMP_Text>();

            var buttonObj = panel.transform.Find("ContinueButton");
            if (buttonObj != null)
            {
                continueButton = buttonObj.GetComponent<Button>();
                if (continueButton != null)
                {
                    continueButton.onClick.RemoveAllListeners();
                    continueButton.onClick.AddListener(OnContinueClicked);
                }
            }
        }

        private void CreateFallbackPanel()
        {
            // Create a simple runtime fallback panel if prefab is missing
            var panelObj = new GameObject("FallbackCompletionPanel", typeof(RectTransform), typeof(Image));
            panelObj.transform.SetParent(transform, false);
            var rt = panelObj.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            panelObj.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.15f, 0.95f);

            var titleObj = new GameObject("ScoreText", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObj.transform.SetParent(panelObj.transform, false);
            scoreText = titleObj.GetComponent<TextMeshProUGUI>();
            scoreText.alignment = TextAlignmentOptions.Center;
            scoreText.fontSize = 48;
            scoreText.color = Color.white;
            scoreText.rectTransform.anchoredPosition = new Vector2(0, 100);

            var coinsObj = new GameObject("CoinsEarnedText", typeof(RectTransform), typeof(TextMeshProUGUI));
            coinsObj.transform.SetParent(panelObj.transform, false);
            coinsEarnedText = coinsObj.GetComponent<TextMeshProUGUI>();
            coinsEarnedText.alignment = TextAlignmentOptions.Center;
            coinsEarnedText.fontSize = 40;
            coinsEarnedText.color = new Color(1f, 0.84f, 0f);
            coinsEarnedText.rectTransform.anchoredPosition = new Vector2(0, 20);

            var totalObj = new GameObject("TotalCoinsText", typeof(RectTransform), typeof(TextMeshProUGUI));
            totalObj.transform.SetParent(panelObj.transform, false);
            totalCoinsText = totalObj.GetComponent<TextMeshProUGUI>();
            totalCoinsText.alignment = TextAlignmentOptions.Center;
            totalCoinsText.fontSize = 32;
            totalCoinsText.color = Color.white;
            totalCoinsText.rectTransform.anchoredPosition = new Vector2(0, -50);

            var loadObj = new GameObject("LoadingText", typeof(RectTransform), typeof(TextMeshProUGUI));
            loadObj.transform.SetParent(panelObj.transform, false);
            loadingText = loadObj.GetComponent<TextMeshProUGUI>();
            loadingText.alignment = TextAlignmentOptions.Center;
            loadingText.fontSize = 36;
            loadingText.color = Color.white;
            loadingText.text = "Saving progress...";

            var btnObj = new GameObject("ContinueButton", typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(panelObj.transform, false);
            var btnRt = btnObj.GetComponent<RectTransform>();
            btnRt.sizeDelta = new Vector2(250, 70);
            btnRt.anchoredPosition = new Vector2(0, -150);
            btnObj.GetComponent<Image>().color = new Color(0.2f, 0.7f, 0.3f);
            continueButton = btnObj.GetComponent<Button>();
            continueButton.onClick.AddListener(OnContinueClicked);

            var btnTextObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            btnTextObj.transform.SetParent(btnObj.transform, false);
            var btnText = btnTextObj.GetComponent<TextMeshProUGUI>();
            btnText.text = "Continue";
            btnText.alignment = TextAlignmentOptions.Center;
            btnText.fontSize = 32;
            btnText.color = Color.white;
            btnText.rectTransform.anchorMin = Vector2.zero;
            btnText.rectTransform.anchorMax = Vector2.one;
            btnText.rectTransform.sizeDelta = Vector2.zero;

            currentPanel = panelObj;
            currentPanel.SetActive(false);
        }

        public async void ReportCompletion(GameResult result)
        {
            if (result == null)
            {
                Debug.LogError("[GameCompletionService] GameResult is null");
                return;
            }

            EnsureCanvasSetup();
            EnsurePanelInstantiated();
            InitializeDependencies();

            isShowingCompletion = true;
            ShowLoading();

            try
            {
                var payloadData = new
                {
                    gameId = result.GameId,
                    score = result.Score,
                    correctCount = result.CorrectCount,
                    totalCount = result.TotalCount,
                    durationSeconds = result.DurationSeconds
                };

                string payloadJson = JsonConvert.SerializeObject(payloadData);
                ActivityLogged response = null;

                if (childApi != null)
                {
                    response = await childApi.LogGameActivityAsync(payloadJson);
                }

                if (response != null && response.totalCoins > 0)
                {
                    CoinWallet.Instance?.UpdateBalance(response.totalCoins);
                }
                else if (CoinWallet.Instance != null)
                {
                    await CoinWallet.Instance.RefreshAsync();
                }

                ShowCompletionPanel(result, response);
                OnCompletionDisplayed?.Invoke(result, response);
            }
            catch (ApiException ex)
            {
                Debug.LogWarning($"[GameCompletionService] API warning: {ex.Message}");
                ShowCompletionPanel(result, null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameCompletionService] Error reporting completion: {ex.Message}");
                ShowCompletionPanel(result, null);
            }
        }

        private void ShowLoading()
        {
            if (currentPanel != null) currentPanel.SetActive(true);
            if (loadingText != null)
            {
                loadingText.gameObject.SetActive(true);
                loadingText.text = "Saving your progress...";
            }
            if (scoreText != null) scoreText.gameObject.SetActive(false);
            if (coinsEarnedText != null) coinsEarnedText.gameObject.SetActive(false);
            if (totalCoinsText != null) totalCoinsText.gameObject.SetActive(false);
            if (continueButton != null) continueButton.gameObject.SetActive(false);
        }

        private void ShowCompletionPanel(GameResult result, ActivityLogged response)
        {
            if (currentPanel != null) currentPanel.SetActive(true);
            if (loadingText != null) loadingText.gameObject.SetActive(false);

            if (scoreText != null)
            {
                scoreText.gameObject.SetActive(true);
                scoreText.text = $"Score: {result.Score}";
            }

            if (coinsEarnedText != null)
            {
                coinsEarnedText.gameObject.SetActive(true);
                int earned = response != null ? response.coinsEarned : (result.Score / 50);
                coinsEarnedText.text = $"+{earned} Coins";
            }

            if (totalCoinsText != null)
            {
                totalCoinsText.gameObject.SetActive(true);
                int total = CoinWallet.Instance != null ? CoinWallet.Instance.Balance : (response != null ? response.totalCoins : 0);
                totalCoinsText.text = $"Total: {total} Coins";
            }

            if (continueButton != null) continueButton.gameObject.SetActive(true);
        }

        public void HideCompletionPanel()
        {
            isShowingCompletion = false;
            if (currentPanel != null) currentPanel.SetActive(false);
        }

        private void OnContinueClicked()
        {
            HideCompletionPanel();
            OnContinuePressed?.Invoke();

            // Return to Main Menu
            if (!string.IsNullOrEmpty(mainMenuSceneName))
            {
                SceneManager.LoadScene(mainMenuSceneName);
            }
        }

        public bool IsShowingCompletion => isShowingCompletion;
    }
}

namespace ImagineMe.API
{
    // Alias forwarder for namespaces using ImagineMe.API
    public class GameCompletionService : Api.GameCompletionService
    {
    }
}
