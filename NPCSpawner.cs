using System.Collections;
using UnityEngine;

public class NPCSpawner : MonoBehaviour
{
    [Header("NPC")]
    [SerializeField] private GameObject npcPrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform waterTarget;
    [SerializeField] private BeachSafetyManager rescueManager;
    [SerializeField] private bool spawnImmediately = true;

    [Header("Spawn Interval")]
    [SerializeField, Min(0f)] private float minSpawnTime = 15f;
    [SerializeField, Min(0f)] private float maxSpawnTime = 30f;

    private GameObject currentNPC;
    private Coroutine spawnCoroutine;
    private bool spawningEnabled = true;

    public GameObject CurrentNPC => currentNPC;

    private void Start()
    {
        ValidateSettings();

        if (spawnImmediately)
            SpawnNow();
        else
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
        else if (enabled && currentNPC == null)
        {
            ScheduleSpawn();
        }
    }

    public void SpawnNow()
    {
        if (!spawningEnabled || currentNPC != null)
            return;

        if (npcPrefab == null || spawnPoint == null)
        {
            Debug.LogError("[NPCSpawner] NPC prefab or spawn point is missing.", this);
            return;
        }

        currentNPC = Instantiate(
            npcPrefab,
            spawnPoint.position,
            spawnPoint.rotation
        );

        NPCSpawnTracker tracker =
            currentNPC.GetComponent<NPCSpawnTracker>();

        if (tracker == null)
            tracker = currentNPC.AddComponent<NPCSpawnTracker>();

        tracker.Initialize(this);

        NPCWarningController controller =
            currentNPC.GetComponent<NPCWarningController>();

        if (controller != null)
        {
            controller.Initialize(
                rescueManager,
                waterTarget
            );
        }
    }

    public void NotifyNPCRemoved(GameObject removedNPC)
    {
        if (removedNPC != currentNPC)
            return;

        currentNPC = null;

        if (spawningEnabled)
            ScheduleSpawn();
    }

    private void ScheduleSpawn()
    {
        if (!spawningEnabled || currentNPC != null || spawnCoroutine != null)
            return;

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

        if (spawnPoint == null)
            Debug.LogWarning("[NPCSpawner] Spawn point is not assigned.", this);

        if (waterTarget == null)
            Debug.LogWarning("[NPCSpawner] Water target is not assigned.", this);

        if (rescueManager == null)
            Debug.LogWarning("[NPCSpawner] Rescue manager is not assigned.", this);
    }

    private static void NormalizeInterval(ref float min, ref float max)
    {
        min = Mathf.Max(0f, min);
        max = Mathf.Max(0f, max);

        if (max < min)
            (min, max) = (max, min);
    }
}
