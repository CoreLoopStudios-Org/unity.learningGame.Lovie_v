using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Api;
using Api.Models;

namespace UI
{
    public class AdminTopContentCard : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI categoryText;
        [SerializeField] private Image thumbnailImage;

        public void Setup(TopContent content)
        {
            if (nameText != null)
            {
                nameText.text = content?.name ?? string.Empty;
            }

            if (categoryText != null)
            {
                categoryText.text = content?.category ?? string.Empty;
            }

            if (thumbnailImage == null) return;

            if (content == null || string.IsNullOrEmpty(content.thumbnailUrl))
            {
                thumbnailImage.sprite = null; // renders nothing, keeps layout
                return;
            }

            LoadThumbnailAsync(content.thumbnailUrl);
        }

        private async void LoadThumbnailAsync(string url)
        {
            var sprite = await RemoteAssetCache.Instance.GetSpriteAsync(url);
            if (sprite != null && thumbnailImage != null)
            {
                thumbnailImage.sprite = sprite;
                thumbnailImage.enabled = true;
            }
        }
    }
}
