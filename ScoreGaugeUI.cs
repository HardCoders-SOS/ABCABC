using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ScoreGaugeUI : MonoBehaviour
{
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private Image gaugeFillImage;
    [SerializeField] private TMP_Text scoreText;

    private void Start()
    {
        if (scoreManager == null)
            scoreManager = ScoreManager.Instance;

        if (scoreManager == null)
        {
            Debug.LogError(
                "[ScoreGaugeUI] ScoreManager가 없습니다.",
                this
            );
            enabled = false;
            return;
        }

        scoreManager.ScoreChanged += Refresh;
        Refresh(scoreManager.Score);
    }

    private void OnDestroy()
    {
        if (scoreManager != null)
            scoreManager.ScoreChanged -= Refresh;
    }

    private void Refresh(int score)
    {
        float progress = Mathf.Clamp01(
            (float)score / scoreManager.TargetScore
        );

        if (gaugeFillImage != null)
            gaugeFillImage.fillAmount = progress;

        if (scoreText != null)
        {
            scoreText.text =
                $"{Mathf.Min(score, scoreManager.TargetScore)} / " +
                $"{scoreManager.TargetScore}";
        }
    }
}
