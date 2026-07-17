using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class WarningSpaceRelay : MonoBehaviour
{
    private void Awake()
    {
        Collider2D trigger = GetComponent<Collider2D>();
        trigger.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        NPCWarningController npc =
            other.GetComponent<NPCWarningController>();

        if (npc == null)
            return;

        npc.EnterWater();
    }
}
