using TMPro;
using UnityEngine;

public class ScoreHUD : MonoBehaviour
{
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private TMP_Text scoreText;

    private void OnEnable()
    {
        if (scoreManager == null)
        {
            Debug.LogError(
                "[ScoreHUD] ScoreManager is not assigned.",
                this
            );
            return;
        }

        scoreManager.ScoreChanged += UpdateScore;
        UpdateScore(scoreManager.Score);
    }

    private void OnDisable()
    {
        if (scoreManager != null)
            scoreManager.ScoreChanged -= UpdateScore;
    }

    private void UpdateScore(int score)
    {
        if (scoreText != null)
            scoreText.text = $"Score : {score}";
    }
}
