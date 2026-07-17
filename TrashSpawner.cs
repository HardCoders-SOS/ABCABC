using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrashSpawner : MonoBehaviour
{
    [Header("Trash")]
    [SerializeField] private GameObject trashPrefab;
    [SerializeField] private Sprite[] trashSprites;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField, Min(0)] private int maxTrashCount = 5;

    [Header("Spawn Interval")]
    [SerializeField, Min(0f)] private float minSpawnTime = 5f;
    [SerializeField, Min(0f)] private float maxSpawnTime = 10f;

    private readonly HashSet<TrashController> activeTrash = new();
    private Coroutine spawnCoroutine;
    private bool spawningEnabled = true;
    private bool interactionEnabled = true;

    public int ActiveTrashCount => activeTrash.Count;

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

        activeTrash.RemoveWhere(trash => trash == null);

        foreach (TrashController trash in activeTrash)
            trash.SetInteractionEnabled(enabled);
    }

    public void NotifyTrashRemoved(TrashController trash)
    {
        if (trash != null)
            activeTrash.Remove(trash);
        else
            activeTrash.RemoveWhere(item => item == null);
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

            activeTrash.RemoveWhere(trash => trash == null);

            if (activeTrash.Count < maxTrashCount)
                SpawnTrash();
        }

        spawnCoroutine = null;
    }

    private void SpawnTrash()
    {
        if (!CanSpawn())
            return;

        Transform spawnPoint =
            spawnPoints[Random.Range(0, spawnPoints.Length)];

        Sprite sprite =
            trashSprites[Random.Range(0, trashSprites.Length)];

        GameObject instance = Instantiate(
            trashPrefab,
            spawnPoint.position,
            Quaternion.identity
        );

        TrashController controller =
            instance.GetComponent<TrashController>();

        if (controller == null)
        {
            Debug.LogError("[TrashSpawner] TrashController is missing.", instance);
            Destroy(instance);
            return;
        }

        controller.Initialize(this, sprite);
        controller.SetInteractionEnabled(interactionEnabled);
        activeTrash.Add(controller);
    }

    private bool CanSpawn()
    {
        if (trashPrefab == null)
            return false;

        if (trashSprites == null || trashSprites.Length == 0)
            return false;

        return spawnPoints != null && spawnPoints.Length > 0;
    }

    private void ValidateSettings()
    {
        NormalizeInterval(ref minSpawnTime, ref maxSpawnTime);

        if (!CanSpawn())
            Debug.LogWarning("[TrashSpawner] Spawn settings are incomplete.", this);
    }

    private static void NormalizeInterval(ref float min, ref float max)
    {
        min = Mathf.Max(0f, min);
        max = Mathf.Max(0f, max);

        if (max < min)
            (min, max) = (max, min);
    }
}
