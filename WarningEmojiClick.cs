using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class WarningEmojiClick : MonoBehaviour
{
    private NPCWarningController npc;
    private Collider2D clickCollider;
    private Camera mainCamera;

    private void Awake()
    {
        npc = GetComponentInParent<NPCWarningController>();
        clickCollider = GetComponent<Collider2D>();
        mainCamera = Camera.main;

        if (npc == null)
            Debug.LogError("[WarningEmoji] 부모 NPC 컨트롤러가 없습니다.", this);

        if (mainCamera == null)
            Debug.LogError("[WarningEmoji] MainCamera를 찾지 못했습니다.", this);
    }

    private void Update()
    {
        if (!Input.GetMouseButtonDown(0))
            return;

        if (npc == null || clickCollider == null || mainCamera == null)
            return;

        Vector3 mouseWorld =
            mainCamera.ScreenToWorldPoint(Input.mousePosition);

        Vector2 mousePoint =
            new Vector2(mouseWorld.x, mouseWorld.y);

        if (!clickCollider.OverlapPoint(mousePoint))
            return;

        Debug.Log("[WarningEmoji] 느낌표 클릭 성공", this);

        npc.TryWarning();
    }
}