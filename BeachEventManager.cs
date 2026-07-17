using UnityEngine;

/// <summary>
/// Compatibility facade for existing scene references.
/// New code should reference each spawner directly.
/// </summary>
public class BeachEventManager : MonoBehaviour
{
    [SerializeField] private NPCSpawner npcSpawner;
    [SerializeField] private TrashSpawner trashSpawner;

    public NPCSpawner NPCSpawner => npcSpawner;
    public TrashSpawner TrashSpawner => trashSpawner;

    public void SetRescueSpawnTime(float min, float max)
    {
        npcSpawner?.SetSpawnInterval(min, max);
    }

    public void SetTrashSpawnTime(float min, float max)
    {
        trashSpawner?.SetSpawnInterval(min, max);
    }

    public void SetSpawningEnabled(bool enabled)
    {
        npcSpawner?.SetSpawningEnabled(enabled);
        trashSpawner?.SetSpawningEnabled(enabled);
    }
}
