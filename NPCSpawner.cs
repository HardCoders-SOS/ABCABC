using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCSpawner : MonoBehaviour
{
    [System.Serializable]
    private class SpawnEntry
    {
        public Transform spawnPoint;
        public Transform waterTarget;
    }

    [Header("NPC")]
    [SerializeField] private GameObject npcPrefab;

    [Tooltip("이 맵에서 NPC가 등장할 타일들을 등록합니다.")]
    [SerializeField] private SpawnEntry[] spawnEntries;

    [Header("Map")]
    [SerializeField] private BeachSafetyManager rescueManager;
    [SerializeField] private MapNavigationController mapNavigation;
    [SerializeField] private MapArea mapArea = MapArea.Center;

    [Header("Legacy Single Spawn Fallback")]
    [Tooltip("Spawn Entries가 비어 있을 때만 사용됩니다.")]
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform waterTarget;

    [Header("Spawn Interval")]
    [SerializeField, Min(0f)] private float minSpawnTime = 15f;
    [SerializeField, Min(0f)] private float maxSpawnTime = 30f;

    private readonly GameObject[] currentNPCsByArea =
        new GameObject[3];
    private Coroutine spawnCoroutine;
    private bool spawningEnabled = true;
    private int lastSpawnEntryIndex = -1;

    public int ActiveNPCCount
    {
        get
        {
            int count = 0;

            foreach (GameObject npc in currentNPCsByArea)
            {
                if (npc != null)
                    count++;
            }

            return count;
        }
    }

    private void Start()
    {
        ValidateSettings();
        // 여러 맵 스포너가 동시에 생성하는 첫 프레임 몰림을 막습니다.
        ScheduleSpawn();
    }

    public void SetSpawnInterval(float min, float max)
    {
        NormalizeInterval(ref min, ref max);
        minSpawnTime = min;
        maxSpawnTime = max;
    }

    public void SetSpawningEnabled(bool enabled)
    {
        spawningEnabled = enabled;

        if (!enabled && spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
        else if (enabled && HasAvailableArea())
        {
            ScheduleSpawn();
        }
    }

    public void SpawnNow()
    {
        if (!spawningEnabled || !HasAvailableArea())
            return;

        SpawnEntry entry = GetRandomSpawnEntry();
        Transform selectedSpawnPoint =
            entry != null ? entry.spawnPoint : spawnPoint;

        if (!TryGetAvailableWaterTarget(
                out Transform selectedWaterTarget,
                out MapArea selectedMapArea))
        {
            selectedWaterTarget = waterTarget;
            selectedMapArea = mapArea;
        }

        if (npcPrefab == null || selectedSpawnPoint == null)
        {
            Debug.LogError("[NPCSpawner] NPC prefab or spawn point is missing.", this);
            return;
        }

        GameObject spawnedNPC = Instantiate(
            npcPrefab,
            selectedSpawnPoint.position,
            selectedSpawnPoint.rotation
        );
        currentNPCsByArea[(int)selectedMapArea] = spawnedNPC;

        NPCSpawnTracker tracker =
            spawnedNPC.GetComponent<NPCSpawnTracker>();

        if (tracker == null)
            tracker = spawnedNPC.AddComponent<NPCSpawnTracker>();

        tracker.Initialize(this);

        NPCWarningController controller =
            spawnedNPC.GetComponent<NPCWarningController>();

        if (controller != null)
        {
            controller.Initialize(
                rescueManager,
                selectedWaterTarget,
                mapNavigation,
                selectedMapArea
            );
        }

        if (HasAvailableArea())
            ScheduleSpawn();
    }

    public void NotifyNPCRemoved(GameObject removedNPC)
    {
        for (int i = 0; i < currentNPCsByArea.Length; i++)
        {
            if (currentNPCsByArea[i] == removedNPC)
            {
                currentNPCsByArea[i] = null;
                break;
            }
        }

        if (spawningEnabled)
            ScheduleSpawn();
    }

    private void ScheduleSpawn()
    {
        if (!spawningEnabled ||
            !HasAvailableArea() ||
            spawnCoroutine != null)
        {
            return;
        }

        spawnCoroutine = StartCoroutine(SpawnRoutine());
    }

    private IEnumerator SpawnRoutine()
    {
        yield return new WaitForSeconds(
            Random.Range(minSpawnTime, maxSpawnTime)
        );

        spawnCoroutine = null;
        SpawnNow();
    }

    private void ValidateSettings()
    {
        NormalizeInterval(ref minSpawnTime, ref maxSpawnTime);

        if (npcPrefab == null)
            Debug.LogWarning("[NPCSpawner] NPC prefab is not assigned.", this);

        if (!HasValidSpawnEntry() && spawnPoint == null)
            Debug.LogWarning("[NPCSpawner] Spawn entry or fallback spawn point is missing.", this);

        if (!HasValidSpawnEntry() && waterTarget == null)
            Debug.LogWarning("[NPCSpawner] Water target is not assigned.", this);

        if (rescueManager == null)
            Debug.LogWarning("[NPCSpawner] Rescue manager is not assigned.", this);
    }

    private SpawnEntry GetRandomSpawnEntry()
    {
        if (spawnEntries == null || spawnEntries.Length == 0)
            return null;

        int validEntryCount = 0;

        for (int i = 0; i < spawnEntries.Length; i++)
        {
            if (spawnEntries[i] != null &&
                spawnEntries[i].spawnPoint != null)
            {
                validEntryCount++;
            }
        }

        if (validEntryCount == 0)
            return null;

        int candidateCount =
            validEntryCount > 1
                ? validEntryCount - 1
                : 1;
        int selectedCandidate = Random.Range(0, candidateCount);

        for (int i = 0; i < spawnEntries.Length; i++)
        {
            SpawnEntry entry = spawnEntries[i];

            if (entry == null || entry.spawnPoint == null)
                continue;

            if (validEntryCount > 1 &&
                i == lastSpawnEntryIndex)
            {
                continue;
            }

            if (selectedCandidate > 0)
            {
                selectedCandidate--;
                continue;
            }

            lastSpawnEntryIndex = i;
            return entry;
        }

        return null;
    }

    private bool HasValidSpawnEntry()
    {
        if (spawnEntries == null)
            return false;

        foreach (SpawnEntry entry in spawnEntries)
        {
            if (entry != null && entry.spawnPoint != null)
                return true;
        }

        return false;
    }

    private bool TryGetAvailableWaterTarget(
        out Transform selectedTarget,
        out MapArea selectedArea)
    {
        selectedTarget = null;
        selectedArea = mapArea;

        if (spawnEntries == null || spawnEntries.Length == 0)
            return false;

        List<int> candidates = new();

        for (int i = 0; i < spawnEntries.Length; i++)
        {
            SpawnEntry entry = spawnEntries[i];

            if (entry == null || entry.waterTarget == null)
                continue;

            MapArea area =
                mapNavigation != null
                    ? mapNavigation.GetAreaAtPosition(
                        entry.waterTarget.position
                    )
                    : mapArea;

            if (currentNPCsByArea[(int)area] != null)
                continue;

            candidates.Add(i);
        }

        if (candidates.Count == 0)
            return false;

        SpawnEntry selectedEntry =
            spawnEntries[candidates[Random.Range(0, candidates.Count)]];

        selectedTarget = selectedEntry.waterTarget;
        selectedArea =
            mapNavigation != null
                ? mapNavigation.GetAreaAtPosition(
                    selectedTarget.position
                )
                : mapArea;
        return true;
    }

    private bool HasAvailableArea()
    {
        foreach (GameObject npc in currentNPCsByArea)
        {
            if (npc == null)
                return true;
        }

        return false;
    }

    private static void NormalizeInterval(ref float min, ref float max)
    {
        min = Mathf.Max(0f, min);
        max = Mathf.Max(0f, max);

        if (max < min)
            (min, max) = (max, min);
    }
}
