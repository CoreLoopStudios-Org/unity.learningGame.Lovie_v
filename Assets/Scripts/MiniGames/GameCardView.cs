using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGames
{
    public class GameCardView : MonoBehaviour
    {
        [SerializeField] private MiniGameInfo info;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text subtitleText;
        [SerializeField] private TMP_Text coinsText;
        [SerializeField] private Image iconImage;
        [SerializeField] private Button playButton;

        private void Awake()
        {
            if (playButton != null)
                playButton.onClick.AddListener(HandlePlayClicked);
        }

        private void OnDestroy()
        {
            if (playButton != null)
                playButton.onClick.RemoveListener(HandlePlayClicked);
        }

        private void OnEnable()
        {
            if (info != null)
                Bind(info);
        }

        public void Bind(MiniGameInfo gameInfo)
        {
            info = gameInfo;
            if (info == null)
                return;

            if (nameText != null) nameText.text = info.DisplayName;
            if (subtitleText != null) subtitleText.text = info.Subtitle;
            if (coinsText != null) coinsText.text = info.Coins.ToString();
            if (iconImage != null) iconImage.sprite = info.Icon;
        }

        private void HandlePlayClicked()
        {
            if (info != null)
                UI.MiniGameNavigator.Load(info.Game);
        }
    }
}
