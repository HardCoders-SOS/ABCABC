using UnityEngine;

public class NPCSpawnTracker : MonoBehaviour
{
    private NPCSpawner spawner;
    private bool removalNotified;

    public void Initialize(
        NPCSpawner owner
    )
    {
        spawner = owner;
        removalNotified = false;
    }

    private void OnDestroy()
    {
        if (removalNotified)
            return;

        removalNotified = true;

        spawner?.NotifyNPCRemoved(gameObject);
    }
}
