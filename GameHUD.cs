using TMPro;
using UnityEngine;

public class GameHUD : MonoBehaviour
{
    [SerializeField] private TMP_Text dayText;
    [SerializeField] private TMP_Text timerText;

    public void SetDay(int day)
    {
        if (dayText != null)
            dayText.text = $"Day {day}";
    }

    public void SetTime(float secondsRemaining)
    {
        int totalSeconds =
            Mathf.Max(0, Mathf.CeilToInt(secondsRemaining));

        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;

        if (timerText != null)
            timerText.text = $"{minutes:00}:{seconds:00}";
    }
}
