using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Api.Models;
using Avatar;

namespace UI
{
    // Component for the "Leader Board User Card" prefab — rank, name, score and a
    // Body/Hair/Dress avatar rendered from the entry's avatarState JSON.
    public class LeaderboardUserCard : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI rankText;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI scoreText;

        [Header("Avatar")]
        [SerializeField] private Image bodyImage;
        [SerializeField] private Image hairImage;
        [SerializeField] private Image dressImage;
        [SerializeField] private AvatarPartDatabase avatarDatabase;

        public void Setup(LeaderboardEntry entry)
        {
            if (entry == null)
                return;

            if (rankText != null)
                rankText.text = $"#{entry.rank}";

            if (nameText != null)
                nameText.text = string.IsNullOrEmpty(entry.username) ? entry.fullName : entry.username;

            if (scoreText != null)
                scoreText.text = entry.score.ToString();

            AvatarSlotRenderer.Apply(
                entry.avatarState,
                AvatarSlotRenderer.ResolveDatabase(avatarDatabase),
                bodyImage, hairImage, dressImage);
        }
    }
}
