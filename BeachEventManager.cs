using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Compatibility facade for existing scene references.
/// New code should reference each spawner directly.
/// </summary>
public class BeachEventManager : MonoBehaviour
{
    [SerializeField] private NPCSpawner[] npcSpawners;
    [SerializeField] private TrashSpawner trashSpawner;

    public IReadOnlyList<NPCSpawner> NPCSpawners => npcSpawners;
    public TrashSpawner TrashSpawner => trashSpawner;

    public void SetRescueSpawnTime(float min, float max)
    {
        if (npcSpawners == null)
            return;

        foreach (NPCSpawner npcSpawner in npcSpawners)
            npcSpawner?.SetSpawnInterval(min, max);
    }

    public void SetTrashSpawnTime(float min, float max)
    {
        trashSpawner?.SetSpawnInterval(min, max);
    }

    public void SetSpawningEnabled(bool enabled)
    {
        if (npcSpawners != null)
        {
            foreach (NPCSpawner npcSpawner in npcSpawners)
                npcSpawner?.SetSpawningEnabled(enabled);
        }

        trashSpawner?.SetSpawningEnabled(enabled);
    }
}
