using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace UI
{
    /// <summary>
    /// One day row of the Daily Streaks section: the green check showing the day is
    /// claimed and the coin amount label.
    /// </summary>
    public class DailyStreakDayView : MonoBehaviour
    {
        [SerializeField] private Image tickImage;
        [SerializeField] private TMP_Text coinsText;

        public void SetState(bool claimed, int coins)
        {
            if (tickImage != null)
            {
                tickImage.gameObject.SetActive(claimed);
            }

            if (coinsText != null)
            {
                coinsText.text = $"{coins} Coins";
            }
        }
    }
}
