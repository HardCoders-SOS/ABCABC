using TMPro;
using UnityEngine;

public class GameHUD : MonoBehaviour
{
    [SerializeField] private TMP_Text dayText;
    [SerializeField] private TMP_Text phaseText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text rescueFailureText;

    public void SetDay(int day)
    {
        if (dayText != null)
            dayText.text = $"Day {day}";
    }

    public void SetPhase(DayPhase phase)
    {
        if (phaseText != null)
            phaseText.text = phase == DayPhase.Day ? "낮" : "밤";
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

    public void SetRescueFailures(int current, int limit)
    {
        if (rescueFailureText != null)
        {
            rescueFailureText.text =
                $"구조 실패 : {current} / {limit}";
        }
    }
}
