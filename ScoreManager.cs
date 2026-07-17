using System;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [SerializeField] private int startingScore;

    public int Score { get; private set; }

    public event Action<int> ScoreChanged;

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
        if (Score == score)
            return;

        Score = score;
        ScoreChanged?.Invoke(Score);
    }
}
