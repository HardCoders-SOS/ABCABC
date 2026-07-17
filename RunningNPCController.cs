using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class RunningNPCController : MonoBehaviour
{
    [Header("Move")]
    [SerializeField] private float moveSpeed = 4f;

    [SerializeField] private float arriveDistance = 0.15f;

    private RunningNPCSpawner spawner;

    private Transform[] movePoints;
    private Transform currentTarget;

    private SpriteRenderer spriteRenderer;
    private Collider2D clickCollider;

    private bool isPaused;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        clickCollider = GetComponent<Collider2D>();
    }

    public void SetPaused(bool pause)
    {
        isPaused = pause;
    }

    public void SetInteractionEnabled(bool enabled)
    {
        SetPaused(!enabled);

        if (clickCollider != null)
            clickCollider.enabled = enabled;
    }

    public void Initialize(
        RunningNPCSpawner owner,
        Transform[] points,
        Sprite sprite)
    {
        spawner = owner;
        movePoints = points;
        spriteRenderer.sprite = sprite;

        ChooseNextTarget();
    }

    private void Update()
    {
        if (isPaused)
            return;

        if (currentTarget == null)
            return;

        Vector3 direction = currentTarget.position - transform.position;

        if (direction.x > 0.01f)
            spriteRenderer.flipX = false;
        else if (direction.x < -0.01f)
            spriteRenderer.flipX = true;

        transform.position = Vector3.MoveTowards(
            transform.position,
            currentTarget.position,
            moveSpeed * Time.deltaTime);

        if (Vector3.Distance(
            transform.position,
            currentTarget.position) <= arriveDistance)
        {
            ChooseNextTarget();
        }
    }

    private void ChooseNextTarget()
    {
        if (movePoints == null || movePoints.Length == 0)
            return;

        currentTarget =
            movePoints[
                Random.Range(0, movePoints.Length)];
    }

    private void OnMouseDown()
    {
        Debug.Log("Running NPC Click");

        ScoreManager.Instance?.AddScore(5);

        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        spawner?.NotifyRemoved(this);
    }
}
