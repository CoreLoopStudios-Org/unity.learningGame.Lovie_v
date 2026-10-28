using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UI;

namespace Modules.Games.StorySequencing
{
    /// <summary>
    /// Builds and shows a simple "game not successful" overlay at runtime, so the scene
    /// needs no hand-made failure panel. Offers Try Again (reload the game) and Back to Games.
    /// </summary>
    public static class StorySequencingFailureView
    {
        #region Fields
        private const string ROOT_NAME = "Failure Overlay";
        private const string TITLE_TEXT = "Game Not Successful";
        private const string BODY_TEXT = "Don't worry, you can try again!";
        private const string RETRY_LABEL = "Try Again";
        private const string BACK_LABEL = "Back to Games";
        private const float DIM_ALPHA = 0.75f;
        private const float CARD_WIDTH = 760f;
        private const float CARD_HEIGHT = 560f;
        private const float BUTTON_WIDTH = 520f;
        private const float BUTTON_HEIGHT = 110f;
        private const float TITLE_SIZE = 64f;
        private const float BODY_SIZE = 42f;
        private const float BUTTON_TEXT_SIZE = 44f;
        #endregion

        #region Public Methods
        /// <summary>
        /// Creates the overlay under the canvas that contains <paramref name="anchor"/> and returns it.
        /// Returns null if no canvas can be found.
        /// </summary>
        /// <param name="anchor">Any object inside the game's canvas.</param>
        public static GameObject Show(Transform anchor)
        {
            Canvas canvas = anchor != null ? anchor.GetComponentInParent<Canvas>() : null;
            if (canvas == null)
            {
                return null;
            }

            Canvas rootCanvas = canvas.rootCanvas;
            GameObject root = CreateRect(ROOT_NAME, rootCanvas.transform, Vector2.zero, Vector2.one);
            root.AddComponent<Image>().color = new Color(0f, 0f, 0f, DIM_ALPHA);

            GameObject card = CreateRect("Card", root.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            RectTransform cardRect = (RectTransform)card.transform;
            cardRect.sizeDelta = new Vector2(CARD_WIDTH, CARD_HEIGHT);
            card.AddComponent<Image>().color = Color.white;

            CreateLabel("Title", card.transform, TITLE_TEXT, TITLE_SIZE, new Vector2(0f, 170f), new Vector2(CARD_WIDTH, 120f));
            CreateLabel("Body", card.transform, BODY_TEXT, BODY_SIZE, new Vector2(0f, 70f), new Vector2(CARD_WIDTH, 100f));

            CreateButton("Retry Button", card.transform, RETRY_LABEL, new Vector2(0f, -60f),
                () => MiniGameNavigator.Load(MiniGameNavigator.MiniGame.StorySequencing));
            CreateButton("Back Button", card.transform, BACK_LABEL, new Vector2(0f, -200f),
                MiniGameNavigator.BackToMenu);

            root.transform.SetAsLastSibling();
            return root;
        }
        #endregion

        #region Private Methods
        private static GameObject CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return go;
        }

        private static void CreateLabel(string name, Transform parent, string text, float size, Vector2 pos, Vector2 dims)
        {
            GameObject go = CreateRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            var rect = (RectTransform)go.transform;
            rect.anchoredPosition = pos;
            rect.sizeDelta = dims;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.15f, 0.15f, 0.2f);
            tmp.raycastTarget = false;
        }

        private static void CreateButton(string name, Transform parent, string label, Vector2 pos, UnityEngine.Events.UnityAction onClick)
        {
            GameObject go = CreateRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            var rect = (RectTransform)go.transform;
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(BUTTON_WIDTH, BUTTON_HEIGHT);
            var image = go.AddComponent<Image>();
            image.color = new Color(0.36f, 0.55f, 0.95f);
            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);
            CreateLabel("Label", go.transform, label, BUTTON_TEXT_SIZE, Vector2.zero, new Vector2(BUTTON_WIDTH, BUTTON_HEIGHT));
            ((TextMeshProUGUI)go.transform.Find("Label").GetComponent<TMP_Text>()).color = Color.white;
        }
        #endregion
    }
}
