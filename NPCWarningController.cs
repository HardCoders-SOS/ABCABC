using System;
using System.Collections;
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
        AwaitingRescueClick,
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
    [SerializeField, Min(1f)] private float verticalDirectionWeight = 1.6f;

    [Header("Perspective")]
    [SerializeField, Range(0.1f, 1f)]
    private float scaleAtWater = 0.55f;

    [SerializeField, Min(0f)]
    private float turnDistanceFromWater = 2f;

    [Header("Warning Detection")]
    [SerializeField, Min(0f)] private float rayDistance = 5f;
    [SerializeField] private LayerMask warningLineLayer;
    [SerializeField, Min(1f)] private float rescueClickGraceTime = 10f;

    [Header("Debug")]
    [SerializeField] private bool showDebugRay = true;

    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private GameObject warningEmoji;
    private NPCSpriteSet selectedSpriteSet;
    private BeachSafetyManager rescueManager;
    private MapNavigationController mapNavigation;
    private MapArea mapArea;
    private Transform waterTarget;
    private Vector3 initialScale;
    private float initialDistanceToWater;
    private bool isFacingAway;
    private bool crossedWarningLine;
    private bool incidentRaised;
    private Coroutine rescueClickTimerCoroutine;
    private NPCState currentState = NPCState.Moving;

    public bool IsRescueActive => currentState == NPCState.Rescue;

    public void Initialize(
        BeachSafetyManager manager,
        Transform target,
        MapNavigationController navigation,
        MapArea area)
    {
        rescueManager = manager;
        waterTarget = target;
        mapNavigation = navigation;
        mapArea = area;
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
        if (currentState == NPCState.WarningAvailable)
        {
            ChangeState(NPCState.Warned);
            ScoreManager.Instance?.AddScore(10);
            Destroy(gameObject, 0.3f);
            return;
        }

        if (currentState == NPCState.AwaitingRescueClick)
            StartRescueEvent();
    }

    public void EnterWater()
    {
        if (currentState != NPCState.Moving &&
            currentState != NPCState.WarningAvailable)
        {
            return;
        }

        ChangeState(NPCState.AwaitingRescueClick);
        RaiseIncident();
        StartRescueClickTimer();
    }

    private void StartRescueEvent()
    {
        if (currentState != NPCState.AwaitingRescueClick ||
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
        {
            StopRescueClickTimer();
            ChangeState(NPCState.Rescue);
        }
    }

    public void ResolveRescue(bool success)
    {
        if (currentState != NPCState.Rescue)
            return;

        ChangeState(NPCState.Resolved);
        ResolveIncident();
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
            currentState != NPCState.WarningAvailable &&
            currentState != NPCState.AwaitingRescueClick)
        {
            return;
        }

        if (currentState == NPCState.AwaitingRescueClick)
            return;

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
            crossedWarningLine ||
            distanceToWater <= turnDistanceFromWater;

        if (shouldFaceAway != isFacingAway)
        {
            isFacingAway = shouldFaceAway;
            ApplyStateSprite();
        }
    }

    private void DetectWarningLine()
    {
        if (crossedWarningLine ||
            currentState != NPCState.Moving)
        {
            return;
        }

        RaycastHit2D hit = Physics2D.Raycast(
            transform.position,
            Vector2.up,
            rayDistance,
            warningLineLayer
        );

        if (showDebugRay)
        {
            Debug.DrawRay(
                transform.position,
                Vector2.up * rayDistance,
                hit.collider != null ? Color.green : Color.yellow
            );
        }

        if (hit.collider == null ||
            !hit.collider.CompareTag("WarningLine"))
        {
            return;
        }

        crossedWarningLine = true;
        isFacingAway = true;
        ChangeState(NPCState.WarningAvailable);
    }

    private void StartRescueClickTimer()
    {
        StopRescueClickTimer();
        rescueClickTimerCoroutine =
            StartCoroutine(RescueClickTimerRoutine());
    }

    private void StopRescueClickTimer()
    {
        if (rescueClickTimerCoroutine == null)
            return;

        StopCoroutine(rescueClickTimerCoroutine);
        rescueClickTimerCoroutine = null;
    }

    private IEnumerator RescueClickTimerRoutine()
    {
        float remainingTime = rescueClickGraceTime;

        while (remainingTime > 0f &&
               currentState == NPCState.AwaitingRescueClick)
        {
            remainingTime -= Time.unscaledDeltaTime;
            yield return null;
        }

        rescueClickTimerCoroutine = null;

        if (currentState != NPCState.AwaitingRescueClick)
            yield break;

        ChangeState(NPCState.Resolved);
        ResolveIncident();

        if (rescueManager != null)
            rescueManager.RegisterMissedRescue(gameObject);
        else
            Destroy(gameObject);
    }

    private void OnDestroy()
    {
        StopRescueClickTimer();
        ResolveIncident();
    }

    private void RaiseIncident()
    {
        if (incidentRaised)
            return;

        incidentRaised = true;
        mapNavigation?.RaiseIncident(mapArea);
    }

    private void ResolveIncident()
    {
        if (!incidentRaised)
            return;

        incidentRaised = false;
        mapNavigation?.ResolveIncident(mapArea);
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
                currentState == NPCState.WarningAvailable ||
                currentState == NPCState.AwaitingRescueClick
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
                    selectedSpriteSet.warningSprite;
                break;

            case NPCState.AwaitingRescueClick:
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
            {
                direction.y *= verticalDirectionWeight;
                return direction.normalized;
            }
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

        Vector3 start = transform.position;
        Vector3 end = start + Vector3.up * rayDistance;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(start, end);
        Gizmos.DrawWireSphere(end, 0.07f);
    }
}
