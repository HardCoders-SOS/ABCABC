using System;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class NPCWarningController : MonoBehaviour
{
    private enum NPCState
    {
        Moving,
        WarningAvailable,
        Warned,
        Rescue,
        Resolved
    }

    [Serializable]
    private class NPCSpriteSet
    {
        public Sprite normalSprite;
        public Sprite backSprite;
        public Sprite warningSprite;
        public Sprite rescueSprite;

        public bool IsValid =>
            normalSprite != null &&
            warningSprite != null &&
            rescueSprite != null;
    }

    [Header("Sprites")]
    [SerializeField] private NPCSpriteSet[] npcSpriteSets =
        new NPCSpriteSet[4];

    [Header("Movement")]
    [SerializeField, Min(0f)] private float moveSpeed = 2f;
    [SerializeField] private Vector2 fallbackMoveDirection = Vector2.up;

    [Header("Perspective")]
    [SerializeField, Range(0.1f, 1f)]
    private float scaleAtWater = 0.55f;

    [SerializeField, Min(0f)]
    private float turnDistanceFromWater = 2f;

    [Header("Warning Detection")]
    [SerializeField, Min(0f)] private float rayDistance = 2f;
    [SerializeField] private LayerMask warningLineLayer;

    [Header("Debug")]
    [SerializeField] private bool showDebugRay = true;

    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private GameObject warningEmoji;
    private NPCSpriteSet selectedSpriteSet;
    private BeachSafetyManager rescueManager;
    private Transform waterTarget;
    private Vector3 initialScale;
    private float initialDistanceToWater;
    private bool isFacingAway;
    private NPCState currentState = NPCState.Moving;

    public bool IsWarned => currentState == NPCState.Warned;
    public bool IsRescueActive => currentState == NPCState.Rescue;

    public void Initialize(
        BeachSafetyManager manager,
        Transform target)
    {
        rescueManager = manager;
        waterTarget = target;
        InitializePerspective();
    }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        initialScale = transform.localScale;

        ConfigureRigidbody();
        FindWarningEmoji();
        SelectRandomNPC();
        ChangeState(NPCState.Moving);
    }

    private void Update()
    {
        MoveNPC();
        UpdatePerspective();
        DetectWarningLine();
    }

    public void TryWarning()
    {
        if (currentState != NPCState.WarningAvailable)
            return;

        ChangeState(NPCState.Warned);
        ScoreManager.Instance?.AddScore(10);
        Destroy(gameObject, 0.3f);
    }

    public void StartRescueEvent()
    {
        if (currentState == NPCState.Warned ||
            currentState == NPCState.Rescue ||
            currentState == NPCState.Resolved)
        {
            return;
        }

        if (rescueManager == null)
        {
            Debug.LogError(
                $"[{name}] BeachSafetyManager is not assigned.",
                this
            );
            return;
        }

        if (rescueManager.StartRescue(gameObject))
            ChangeState(NPCState.Rescue);
    }

    public void ResolveRescue(bool success)
    {
        if (currentState != NPCState.Rescue)
            return;

        ChangeState(NPCState.Resolved);
        Debug.Log(success ? $"[{name}] Rescue success" : $"[{name}] Rescue failed", this);
        Destroy(gameObject);
    }

    private void ConfigureRigidbody()
    {
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.simulated = true;
    }

    private void MoveNPC()
    {
        if (currentState != NPCState.Moving &&
            currentState != NPCState.WarningAvailable)
        {
            return;
        }

        transform.position +=
            (Vector3)(GetMoveDirection() * moveSpeed * Time.deltaTime);
    }

    private void InitializePerspective()
    {
        if (waterTarget == null)
        {
            initialDistanceToWater = 0f;
            return;
        }

        initialDistanceToWater = Vector2.Distance(
            transform.position,
            waterTarget.position
        );
        UpdatePerspective();
    }

    private void UpdatePerspective()
    {
        if (waterTarget == null ||
            initialDistanceToWater <= Mathf.Epsilon)
        {
            return;
        }

        float distanceToWater = Vector2.Distance(
            transform.position,
            waterTarget.position
        );

        float progress = Mathf.Clamp01(
            1f - distanceToWater / initialDistanceToWater
        );

        transform.localScale = Vector3.Lerp(
            initialScale,
            initialScale * scaleAtWater,
            progress
        );

        bool shouldFaceAway =
            distanceToWater <= turnDistanceFromWater;

        if (shouldFaceAway != isFacingAway)
        {
            isFacingAway = shouldFaceAway;
            ApplyStateSprite();
        }
    }

    private void DetectWarningLine()
    {
        if (currentState != NPCState.Moving)
            return;

        RaycastHit2D hit = Physics2D.Raycast(
            transform.position,
            GetMoveDirection(),
            rayDistance,
            warningLineLayer
        );

        if (hit.collider != null &&
            hit.collider.CompareTag("WarningLine"))
        {
            ChangeState(NPCState.WarningAvailable);
        }
    }

    private void FindWarningEmoji()
    {
        Transform emojiTransform = transform.Find("WarningEmoji");

        if (emojiTransform == null)
        {
            Debug.LogError($"[{name}] WarningEmoji child is missing.", this);
            return;
        }

        warningEmoji = emojiTransform.gameObject;
        warningEmoji.SetActive(false);
    }

    private void SelectRandomNPC()
    {
        if (npcSpriteSets == null || npcSpriteSets.Length == 0)
            return;

        int startIndex = UnityEngine.Random.Range(0, npcSpriteSets.Length);

        for (int offset = 0; offset < npcSpriteSets.Length; offset++)
        {
            int index = (startIndex + offset) % npcSpriteSets.Length;
            NPCSpriteSet candidate = npcSpriteSets[index];

            if (candidate != null && candidate.IsValid)
            {
                selectedSpriteSet = candidate;
                return;
            }
        }

        Debug.LogError($"[{name}] No valid NPC sprite set exists.", this);
    }

    private void ChangeState(NPCState nextState)
    {
        currentState = nextState;
        ApplyStateSprite();

        if (warningEmoji != null)
        {
            warningEmoji.SetActive(
                currentState == NPCState.WarningAvailable
            );
        }
    }

    private void ApplyStateSprite()
    {
        if (selectedSpriteSet == null)
            return;

        switch (currentState)
        {
            case NPCState.Moving:
                spriteRenderer.sprite =
                    GetWalkingSprite();
                break;

            case NPCState.WarningAvailable:
                spriteRenderer.sprite =
                    isFacingAway
                        ? GetWalkingSprite()
                        : selectedSpriteSet.warningSprite;
                break;

            case NPCState.Rescue:
                spriteRenderer.sprite = selectedSpriteSet.rescueSprite;
                break;
        }
    }

    private Vector2 GetMoveDirection()
    {
        if (waterTarget != null)
        {
            Vector2 direction =
                waterTarget.position - transform.position;

            if (direction.sqrMagnitude > Mathf.Epsilon)
                return direction.normalized;
        }

        return fallbackMoveDirection.sqrMagnitude > 0f
            ? fallbackMoveDirection.normalized
            : Vector2.up;
    }

    private Sprite GetWalkingSprite()
    {
        if (isFacingAway &&
            selectedSpriteSet.backSprite != null)
        {
            return selectedSpriteSet.backSprite;
        }

        return selectedSpriteSet.normalSprite;
    }

    private void OnDrawGizmosSelected()
    {
        if (!showDebugRay)
            return;

        Vector2 direction =
            GetMoveDirection();

        Vector3 start = transform.position;
        Vector3 end = start + (Vector3)(direction * rayDistance);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(start, end);
        Gizmos.DrawWireSphere(end, 0.07f);
    }
}
