using System;
using System.Collections;
using UnityEngine;

public class DistractionEventManager : MonoBehaviour
{
    [Serializable]
    private class DayEvent
    {
        [Min(1)] public int day;
        public Sprite portrait;
        public Sprite dialogueBackground;
        [TextArea(2, 4)] public string[] dialogueLines;
    }

    [SerializeField] private DistractionUI distractionUI;
    [SerializeField] private DayEvent[] dayEvents =
    {
        new DayEvent
        {
            day = 2
        },
        new DayEvent
        {
            day = 4
        },
        new DayEvent
        {
            day = 7
        }
    };

    private GameManager gameManager;
    private DayEvent currentEvent;
    private DayInfo currentDayInfo;
    private Coroutine eventCoroutine;
    private bool dialogueDismissed;
    private int lastDialogueIndex = -1;

    private void Start()
    {
        gameManager = GameManager.Instance;

        if (gameManager == null || distractionUI == null)
        {
            Debug.LogError(
                "[DistractionEventManager] Required references are missing.",
                this
            );
            enabled = false;
            return;
        }

        gameManager.DayChanged += HandleDayChanged;
        gameManager.StateChanged += HandleStateChanged;
        distractionUI.Dismissed += HandleDismissed;

        if (gameManager.CurrentDayInfo != null)
        {
            HandleDayChanged(
                gameManager.CurrentDay,
                gameManager.CurrentDayInfo
            );
        }
    }

    private void OnDestroy()
    {
        if (gameManager != null)
        {
            gameManager.DayChanged -= HandleDayChanged;
            gameManager.StateChanged -= HandleStateChanged;
        }

        if (distractionUI != null)
            distractionUI.Dismissed -= HandleDismissed;
    }

    private void HandleDayChanged(int day, DayInfo info)
    {
        StopCurrentEvent();
        currentEvent = FindEvent(day);
        currentDayInfo = info;

        if (currentEvent != null &&
            currentDayInfo != null &&
            currentDayInfo.distractionEnabled)
        {
            Debug.Log(
                $"[Distraction] Day {day} 이벤트 시작 | " +
                $"첫 등장: {currentDayInfo.distractionFirstDelay}초",
                this
            );
            eventCoroutine = StartCoroutine(EventRoutine());
        }
    }

    private void HandleStateChanged(GameState state)
    {
        if (state == GameState.DayTransition ||
            state == GameState.GameClear ||
            state == GameState.GameOver)
        {
            distractionUI.Hide();
        }
    }

    private IEnumerator EventRoutine()
    {
        yield return WaitForPlayableTime(
            currentDayInfo.distractionFirstDelay
        );

        while (currentEvent != null &&
               !IsGameFinished())
        {
            yield return WaitUntilEventCanAppear();

            dialogueDismissed = false;
            distractionUI.Show(
                currentEvent.portrait,
                currentEvent.dialogueBackground,
                GetNextDialogue(currentEvent)
            );

            Debug.Log(
                $"[Distraction] Day {currentEvent.day} UI 표시",
                this
            );

            yield return new WaitUntil(
                () => dialogueDismissed ||
                      currentEvent == null ||
                      IsGameFinished()
            );

            if (currentEvent == null ||
                IsGameFinished())
            {
                break;
            }

            float repeatDelay = UnityEngine.Random.Range(
                currentDayInfo.distractionMinRepeat,
                currentDayInfo.distractionMaxRepeat
            );
            yield return WaitForPlayableTime(repeatDelay);
        }

        eventCoroutine = null;
        distractionUI.Hide();
    }

    private IEnumerator WaitUntilEventCanAppear()
    {
        yield return new WaitUntil(
            () => currentEvent == null ||
                  gameManager.State == GameState.Playing ||
                  gameManager.State == GameState.Rescue ||
                  IsGameFinished()
        );
    }

    private IEnumerator WaitForPlayableTime(float duration)
    {
        float elapsed = 0f;

        while (currentEvent != null &&
               !IsGameFinished() &&
               elapsed < duration)
        {
            if (gameManager.State == GameState.Playing ||
                gameManager.State == GameState.Rescue)
            {
                elapsed += Time.deltaTime;
            }

            yield return null;
        }
    }

    private void HandleDismissed()
    {
        dialogueDismissed = true;
    }

    private bool IsGameFinished()
    {
        return gameManager.State == GameState.GameClear ||
               gameManager.State == GameState.GameOver;
    }

    private void StopCurrentEvent()
    {
        currentEvent = null;
        currentDayInfo = null;
        dialogueDismissed = true;
        distractionUI?.Hide();

        if (eventCoroutine == null)
            return;

        StopCoroutine(eventCoroutine);
        eventCoroutine = null;
    }

    private DayEvent FindEvent(int day)
    {
        if (dayEvents == null)
            return null;

        foreach (DayEvent dayEvent in dayEvents)
        {
            if (dayEvent != null && dayEvent.day == day)
                return dayEvent;
        }

        return null;
    }

    private string GetNextDialogue(DayEvent dayEvent)
    {
        if (dayEvent.dialogueLines == null ||
            dayEvent.dialogueLines.Length == 0)
        {
            return "...";
        }

        if (dayEvent.dialogueLines.Length == 1)
            return dayEvent.dialogueLines[0];

        int index;

        do
        {
            index = UnityEngine.Random.Range(
                0,
                dayEvent.dialogueLines.Length
            );
        }
        while (index == lastDialogueIndex);

        lastDialogueIndex = index;
        return dayEvent.dialogueLines[index];
    }
}
