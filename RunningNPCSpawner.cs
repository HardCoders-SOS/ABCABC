using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RunningNPCSpawner : MonoBehaviour
{
    [Header("Running NPC")]
    [SerializeField] private GameObject npcPrefab;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private Transform[] movePoints;
    [SerializeField] private Sprite[] sprites;

    [Header("Spawn Interval")]
    [SerializeField, Min(0f)] private float minSpawnTime = 10f;
    [SerializeField, Min(0f)] private float maxSpawnTime = 20f;

    private readonly HashSet<RunningNPCController> activeNPCs = new();
    private Coroutine spawnCoroutine;
    private bool spawningEnabled = true;
    private bool interactionEnabled = true;

    private void Start()
    {
        ValidateSettings();
        StartSpawning();
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

        if (enabled)
            StartSpawning();
        else
            StopSpawning();
    }

    public void SetInteractionEnabled(bool enabled)
    {
        interactionEnabled = enabled;
        activeNPCs.RemoveWhere(npc => npc == null);

        foreach (RunningNPCController npc in activeNPCs)
            npc.SetInteractionEnabled(enabled);
    }

    public void NotifyRemoved(RunningNPCController npc)
    {
        if (npc != null)
            activeNPCs.Remove(npc);
        else
            activeNPCs.RemoveWhere(item => item == null);
    }

    private void StartSpawning()
    {
        if (spawningEnabled && spawnCoroutine == null)
            spawnCoroutine = StartCoroutine(SpawnRoutine());
    }

    private void StopSpawning()
    {
        if (spawnCoroutine == null)
            return;

        StopCoroutine(spawnCoroutine);
        spawnCoroutine = null;
    }

    private IEnumerator SpawnRoutine()
    {
        while (spawningEnabled)
        {
            yield return new WaitForSeconds(
                Random.Range(minSpawnTime, maxSpawnTime)
            );

            activeNPCs.RemoveWhere(npc => npc == null);

            if (activeNPCs.Count == 0)
                SpawnNPC();
        }

        spawnCoroutine = null;
    }

    private void SpawnNPC()
    {
        if (!CanSpawn())
            return;

        Transform spawn =
            spawnPoints[Random.Range(0, spawnPoints.Length)];

        GameObject instance = Instantiate(
            npcPrefab,
            spawn.position,
            Quaternion.identity
        );

        RunningNPCController controller =
            instance.GetComponent<RunningNPCController>();

        if (controller == null)
        {
            Debug.LogError(
                "[RunningNPCSpawner] RunningNPCController is missing.",
                instance
            );
            Destroy(instance);
            return;
        }

        controller.Initialize(
            this,
            movePoints,
            sprites[Random.Range(0, sprites.Length)]
        );
        controller.SetInteractionEnabled(interactionEnabled);
        activeNPCs.Add(controller);
    }

    private bool CanSpawn()
    {
        return npcPrefab != null &&
               spawnPoints != null && spawnPoints.Length > 0 &&
               movePoints != null && movePoints.Length > 0 &&
               sprites != null && sprites.Length > 0;
    }

    private void ValidateSettings()
    {
        NormalizeInterval(ref minSpawnTime, ref maxSpawnTime);

        if (!CanSpawn())
            Debug.LogWarning("[RunningNPCSpawner] Spawn settings are incomplete.", this);
    }

    private static void NormalizeInterval(ref float min, ref float max)
    {
        min = Mathf.Max(0f, min);
        max = Mathf.Max(0f, max);

        if (max < min)
            (min, max) = (max, min);
    }
}
