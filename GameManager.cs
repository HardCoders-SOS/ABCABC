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

    public int CurrentDay => currentDay;
    public float CurrentTime => currentTime;
    public DayInfo CurrentDayInfo { get; private set; }
    public GameState State { get; private set; } =
        GameState.DayTransition;

    public event Action<GameState> StateChanged;
    public event Action<int, DayInfo> DayChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        if (!StartDay(currentDay))
            enabled = false;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (!isTimerRunning)
            return;

        currentTime = Mathf.Max(0f, currentTime - Time.deltaTime);
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
                AdvanceDay();
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
        if (nextState == GameState.Playing &&
            dayAdvancePending)
        {
            dayAdvancePending = false;
            AdvanceDay();
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
        currentTime = CurrentDayInfo.dayTime;
        dayAdvancePending = false;

        ApplyDayInfo(CurrentDayInfo);
        gameHUD?.SetDay(currentDay);
        gameHUD?.SetTime(currentTime);
        DayChanged?.Invoke(currentDay, CurrentDayInfo);

        SetState(GameState.Playing);
        return true;
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
