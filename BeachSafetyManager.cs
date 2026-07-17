using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BeachSafetyManager : MonoBehaviour
{
    [Serializable]
    public class RescueItem
    {
        public string itemName;
        public Sprite sprite;
        public bool isCorrect;
    }

    [Header("Rescue UI")]
    [SerializeField] private RescueUI rescueUI;

    [Header("Rescue Items")]
    [SerializeField] private RescueItem[] rescueItems;

    [Header("Rescue Timer")]
    [SerializeField, Min(1f)] private float rescueTimeLimit = 10f;

    [Header("Statistics")]
    [SerializeField] private int rescuedNPCCount;
    [SerializeField] private int failedNPCCount;

    private readonly List<RescueItem> displayedItems = new();
    private GameObject currentRescueNPC;
    private bool rescueEventActive;
    private bool resultProcessed;
    private bool currentRescueSucceeded;
    private Coroutine rescueTimerCoroutine;

    public int RescuedNPCCount => rescuedNPCCount;
    public int FailedNPCCount => failedNPCCount;
    public bool IsRescueActive => rescueEventActive;

    public float RescueSuccessRate
    {
        get
        {
            int total = rescuedNPCCount + failedNPCCount;
            return total == 0 ? 0f : (float)rescuedNPCCount / total;
        }
    }

    public void SetRescueTimeLimit(float seconds)
    {
        rescueTimeLimit = Mathf.Max(1f, seconds);
    }

    private void Awake()
    {
        if (rescueUI != null)
            rescueUI.ItemSelected += SelectRescueItem;
    }

    private void OnDestroy()
    {
        if (rescueUI != null)
            rescueUI.ItemSelected -= SelectRescueItem;
    }

    private void Start()
    {
        ValidateSettings();
        UpdateCountText();
    }

    public bool StartRescue(GameObject targetNPC)
    {
        return StartRescueEvent(targetNPC);
    }

    public bool OnNPCEnteredWarningSpace(GameObject targetNPC)
    {
        return StartRescueEvent(targetNPC);
    }

    public bool StartRescueEvent(GameObject targetNPC)
    {
        if (rescueEventActive || targetNPC == null)
            return false;

        if (!CanCreateRescueSelection())
            return false;

        currentRescueNPC = targetNPC;
        rescueEventActive = true;
        resultProcessed = false;
        currentRescueSucceeded = false;

        GameManager.Instance?.SetState(GameState.Rescue);

        CreateRandomItemSelection();
        rescueUI.Show(displayedItems);
        StartRescueTimer();
        return true;
    }

    public void ResetStatistics()
    {
        rescuedNPCCount = 0;
        failedNPCCount = 0;
        UpdateCountText();
    }

    private void SelectRescueItem(int buttonIndex)
    {
        if (!rescueEventActive || resultProcessed)
            return;

        if (buttonIndex < 0 || buttonIndex >= displayedItems.Count)
            return;

        CompleteRescue(displayedItems[buttonIndex].isCorrect);
    }

    private void CompleteRescue(bool success)
    {
        if (!rescueEventActive || resultProcessed)
            return;

        resultProcessed = true;
        currentRescueSucceeded = success;

        if (success)
        {
            rescuedNPCCount++;
            ScoreManager.Instance?.AddScore(10);
        }
        else
        {
            failedNPCCount++;
            ScoreManager.Instance?.AddScore(-10);
        }

        UpdateCountText();
        EndRescueEvent();
    }

    private void EndRescueEvent()
    {
        StopRescueTimer();
        rescueUI?.Hide();
        GameManager.Instance?.SetState(GameState.Playing);

        GameObject completedNPC = currentRescueNPC;
        currentRescueNPC = null;
        rescueEventActive = false;
        displayedItems.Clear();

        if (completedNPC == null)
            return;

        NPCWarningController npc =
            completedNPC.GetComponent<NPCWarningController>();

        if (npc != null)
            npc.ResolveRescue(currentRescueSucceeded);
        else
            Destroy(completedNPC);
    }

    private void StartRescueTimer()
    {
        StopRescueTimer();
        rescueTimerCoroutine =
            StartCoroutine(RescueTimerRoutine());
    }

    private void StopRescueTimer()
    {
        if (rescueTimerCoroutine == null)
            return;

        StopCoroutine(rescueTimerCoroutine);
        rescueTimerCoroutine = null;
    }

    private IEnumerator RescueTimerRoutine()
    {
        float remainingTime = rescueTimeLimit;
        rescueUI?.SetTime(remainingTime);

        while (remainingTime > 0f &&
               rescueEventActive &&
               !resultProcessed)
        {
            remainingTime -= Time.deltaTime;
            rescueUI?.SetTime(remainingTime);
            yield return null;
        }

        rescueTimerCoroutine = null;

        if (rescueEventActive && !resultProcessed)
            CompleteRescue(false);
    }

    private void CreateRandomItemSelection()
    {
        displayedItems.Clear();

        List<RescueItem> correctItems = new();
        List<RescueItem> incorrectItems = new();

        foreach (RescueItem item in rescueItems)
        {
            if (item == null)
                continue;

            if (item.isCorrect)
                correctItems.Add(item);
            else
                incorrectItems.Add(item);
        }

        displayedItems.Add(
            correctItems[UnityEngine.Random.Range(0, correctItems.Count)]
        );

        ShuffleList(incorrectItems);
        displayedItems.Add(incorrectItems[0]);
        displayedItems.Add(incorrectItems[1]);
        ShuffleList(displayedItems);

    }

    private void UpdateCountText()
    {
        rescueUI?.SetStatistics(
            rescuedNPCCount,
            failedNPCCount
        );
    }

    private bool CanCreateRescueSelection()
    {
        if (rescueUI == null ||
            !rescueUI.IsConfigured ||
            rescueItems == null ||
            rescueItems.Length < 3)
        {
            Debug.LogError("[BeachSafetyManager] Rescue UI settings are incomplete.", this);
            return false;
        }

        int correctCount = 0;
        int incorrectCount = 0;

        foreach (RescueItem item in rescueItems)
        {
            if (item == null)
                continue;

            if (item.isCorrect)
                correctCount++;
            else
                incorrectCount++;
        }

        if (correctCount >= 1 && incorrectCount >= 2)
            return true;

        Debug.LogError(
            "[BeachSafetyManager] At least one correct and two incorrect items are required.",
            this
        );
        return false;
    }

    private void ValidateSettings()
    {
        CanCreateRescueSelection();
    }

    private static void ShuffleList<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int randomIndex = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[randomIndex]) =
                (list[randomIndex], list[i]);
        }
    }
}
