using UnityEngine;
using TMPro;

public class OTPTimer : MonoBehaviour
{
    public TMP_Text timerText;

    private float remainingTime = 120f; // 2 minutes

    private void Update()
    {
        if (remainingTime > 0)
        {
            remainingTime -= Time.deltaTime;

            if (remainingTime <= 0)
            {
                remainingTime = 0;
            }

            int minutes = Mathf.FloorToInt(remainingTime / 60);
            int seconds = Mathf.FloorToInt(remainingTime % 60);

            timerText.text = $"Your code will expire in {minutes}:{seconds:00}";

            // Turn red when 15 seconds or less remain
            if (remainingTime <= 15)
            {
                timerText.color = Color.red;
            }
        }
    }
}