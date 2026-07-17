using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class TrashController : MonoBehaviour
{
    private TrashSpawner spawner;
    private SpriteRenderer spriteRenderer;
    private Collider2D clickCollider;
    private bool removalNotified;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        clickCollider = GetComponent<Collider2D>();
        clickCollider.isTrigger = false;
    }

    public void Initialize(
        TrashSpawner owner,
        Sprite sprite)
    {
        spawner = owner;
        spriteRenderer.sprite = sprite;
        removalNotified = false;
    }

    public void SetInteractionEnabled(bool enabled)
    {
        if (clickCollider != null)
            clickCollider.enabled = enabled;
    }

    private void OnMouseDown()
    {
        ScoreManager.Instance?.AddScore(1);

        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (removalNotified)
            return;

        removalNotified = true;
        spawner?.NotifyTrashRemoved(this);
    }
}
