using System;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [SerializeField] private int startingScore;
    [SerializeField, Min(1)] private int targetScore = 3000;

    public int Score { get; private set; }
    public int TargetScore => targetScore;

    public event Action<int> ScoreChanged;
    public event Action TargetReached;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Score = startingScore;
    }

    private void Start()
    {
        ScoreChanged?.Invoke(Score);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void AddScore(int amount)
    {
        SetScore(Score + amount);
    }

    public void ResetScore()
    {
        SetScore(startingScore);
    }

    public void SetScore(int score)
    {
        int previousScore = Score;
        int nextScore = Mathf.Max(0, score);

        if (previousScore == nextScore)
            return;

        Score = nextScore;
        ScoreChanged?.Invoke(Score);

        if (previousScore < targetScore &&
            Score >= targetScore)
        {
            TargetReached?.Invoke();
        }
    }
}
