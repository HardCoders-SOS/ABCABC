using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Progress")]
    [SerializeField, Min(1)] private int currentDay = 1;
    [SerializeField, Min(1)] private int maxDay = 7;

    [Header("Systems")]
    [SerializeField] private BeachEventManager beachEventManager;
    [SerializeField] private RunningNPCSpawner runningNPCSpawner;
    [SerializeField] private BeachSafetyManager beachSafetyManager;
    [SerializeField] private GameHUD gameHUD;

    private float currentTime;
    private bool isTimerRunning;
    private bool dayAdvancePending;
    private int rescueFailuresThisDay;

    public int CurrentDay => currentDay;
    public float CurrentTime => currentTime;
    public DayPhase CurrentPhase { get; private set; } = DayPhase.Day;
    public DayInfo CurrentDayInfo { get; private set; }
    public int RescueFailuresThisDay => rescueFailuresThisDay;
    public GameState State { get; private set; } =
        GameState.DayTransition;

    public event Action<GameState> StateChanged;
    public event Action<int, DayInfo> DayChanged;
    public event Action<DayPhase> PhaseChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (beachSafetyManager != null)
            beachSafetyManager.RescueCompleted += HandleRescueCompleted;
    }

    private void Start()
    {
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.TargetReached += HandleTargetScoreReached;

        if (!StartDay(currentDay))
            enabled = false;
    }

    private void OnDestroy()
    {
        if (beachSafetyManager != null)
            beachSafetyManager.RescueCompleted -= HandleRescueCompleted;

        if (ScoreManager.Instance != null)
            ScoreManager.Instance.TargetReached -= HandleTargetScoreReached;

        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (!isTimerRunning)
            return;

        // Rescue UI or another overlay may change Time.timeScale.
        // The day/night clock must continue during every rescue event.
        currentTime = Mathf.Max(
            0f,
            currentTime - Time.unscaledDeltaTime
        );
        gameHUD?.SetTime(currentTime);

        if (currentTime <= 0f)
        {
            if (State == GameState.Rescue)
            {
                isTimerRunning = false;
                dayAdvancePending = true;
            }
            else
            {
                AdvancePhase();
            }
        }
    }

    public void PauseTimer()
    {
        isTimerRunning = false;
    }

    public void ResumeTimer()
    {
        if (State == GameState.Playing ||
            State == GameState.Rescue)
        {
            isTimerRunning = true;
        }
    }

    public void SetState(GameState nextState)
    {
        if (State == GameState.GameClear ||
            State == GameState.GameOver)
        {
            return;
        }

        if (nextState == GameState.Playing &&
            dayAdvancePending)
        {
            dayAdvancePending = false;
            AdvancePhase();
            return;
        }

        if (State == nextState)
            return;

        State = nextState;
        isTimerRunning =
            nextState == GameState.Playing ||
            nextState == GameState.Rescue;
        StateChanged?.Invoke(State);
    }

    public bool LoadDayInfo(int day)
    {
        if (DayInfoLoader.TryLoad(
                day,
                out DayInfo info,
                out string error))
        {
            CurrentDayInfo = info;
            return true;
        }

        Debug.LogError($"[GameManager] {error}", this);
        return false;
    }

    private bool StartDay(int day)
    {
        SetState(GameState.DayTransition);

        if (!LoadDayInfo(day))
            return false;

        currentDay = day;
        CurrentPhase = DayPhase.Day;
        currentTime = CurrentDayInfo.dayTime;
        dayAdvancePending = false;
        rescueFailuresThisDay = 0;

        ApplyDayInfo(CurrentDayInfo);
        gameHUD?.SetDay(currentDay);
        gameHUD?.SetPhase(CurrentPhase);
        gameHUD?.SetTime(currentTime);
        gameHUD?.SetRescueFailures(
            rescueFailuresThisDay,
            CurrentDayInfo.rescueFailureLimit
        );
        DayChanged?.Invoke(currentDay, CurrentDayInfo);
        PhaseChanged?.Invoke(CurrentPhase);

        SetState(GameState.Playing);
        return true;
    }

    private void AdvancePhase()
    {
        isTimerRunning = false;

        if (CurrentPhase == DayPhase.Day)
        {
            CurrentPhase = DayPhase.Night;
            currentTime = CurrentDayInfo.nightTime;
            gameHUD?.SetPhase(CurrentPhase);
            gameHUD?.SetTime(currentTime);
            PhaseChanged?.Invoke(CurrentPhase);
            isTimerRunning = true;
            return;
        }

        AdvanceDay();
    }

    private void AdvanceDay()
    {
        isTimerRunning = false;
        int nextDay = currentDay + 1;

        if (nextDay > maxDay)
        {
            CompleteGame();
            return;
        }

        if (!StartDay(nextDay))
            SetState(GameState.DayTransition);
    }

    private void CompleteGame()
    {
        SetState(GameState.GameClear);
        Debug.Log("Game Clear!", this);
    }

    private void HandleTargetScoreReached()
    {
        Debug.Log(
            $"[GameManager] 목표 점수 " +
            $"{ScoreManager.Instance.TargetScore}점 달성!",
            this
        );
        CompleteGame();
    }

    private void HandleRescueCompleted(bool success)
    {
        if (success ||
            CurrentDayInfo == null ||
            State == GameState.GameClear ||
            State == GameState.GameOver)
        {
            return;
        }

        rescueFailuresThisDay++;
        gameHUD?.SetRescueFailures(
            rescueFailuresThisDay,
            CurrentDayInfo.rescueFailureLimit
        );

        Debug.Log(
            $"[GameManager] Day {currentDay} 구조 실패 누적: " +
            $"{rescueFailuresThisDay}/" +
            $"{CurrentDayInfo.rescueFailureLimit}",
            this
        );

        if (rescueFailuresThisDay >=
            CurrentDayInfo.rescueFailureLimit)
        {
            SetState(GameState.GameOver);
            Debug.Log(
                $"Game Over! Rescue failures: " +
                $"{rescueFailuresThisDay}/" +
                $"{CurrentDayInfo.rescueFailureLimit}",
                this
            );
        }
    }

    private void ApplyDayInfo(DayInfo info)
    {
        if (beachEventManager != null)
        {
            beachEventManager.SetRescueSpawnTime(
                info.rescueMin,
                info.rescueMax
            );
            beachEventManager.SetTrashSpawnTime(
                info.trashMin,
                info.trashMax
            );
        }

        runningNPCSpawner?.SetSpawnInterval(
            info.runningMin,
            info.runningMax
        );

        beachSafetyManager?.SetRescueTimeLimit(
            info.rescueTimeLimit
        );
    }
}
