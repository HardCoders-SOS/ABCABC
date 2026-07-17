using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MapNavigationController : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Transform leftViewPoint;
    [SerializeField] private Transform centerViewPoint;
    [SerializeField] private Transform rightViewPoint;
    [SerializeField, Min(0.01f)] private float moveDuration = 0.3f;

    [Header("Arrow Buttons")]
    [SerializeField] private Button leftArrowButton;
    [SerializeField] private Button rightArrowButton;

    [Header("Incident Indicators")]
    [Tooltip("왼쪽 화면에서 중앙 또는 오른쪽 사건을 알리는 화살표 1개")]
    [SerializeField] private GameObject[] leftIncidentMarks;
    [Tooltip("중앙 화면의 왼쪽/오른쪽 맵 사건 Warning 2개")]
    [SerializeField] private GameObject[] centerIncidentMarks;
    [Tooltip("오른쪽 화면에서 왼쪽 또는 중앙 사건을 알리는 화살표 1개")]
    [SerializeField] private GameObject[] rightIncidentMarks;

    [SerializeField] private MapArea startingArea = MapArea.Center;

    private readonly int[] incidentCountsByArea = new int[3];
    private Coroutine moveCoroutine;

    public MapArea CurrentArea { get; private set; }

    private void Awake()
    {
        if (leftArrowButton != null)
            leftArrowButton.onClick.AddListener(MoveLeft);

        if (rightArrowButton != null)
            rightArrowButton.onClick.AddListener(MoveRight);

        CurrentArea = startingArea;
        MoveImmediately(CurrentArea);
        RefreshArrowButtons();
        RefreshIncidentMarks();
    }

    private void OnDestroy()
    {
        if (leftArrowButton != null)
            leftArrowButton.onClick.RemoveListener(MoveLeft);

        if (rightArrowButton != null)
            rightArrowButton.onClick.RemoveListener(MoveRight);
    }

    public void MoveLeft()
    {
        int nextIndex = Mathf.Max(
            0,
            (int)CurrentArea - 1
        );
        MoveTo((MapArea)nextIndex);
    }

    public void MoveRight()
    {
        int nextIndex = Mathf.Min(
            2,
            (int)CurrentArea + 1
        );
        MoveTo((MapArea)nextIndex);
    }

    public void MoveTo(MapArea area)
    {
        if (area == CurrentArea)
            return;

        Transform target = GetViewPoint(area);

        if (cameraTransform == null || target == null)
            return;

        CurrentArea = area;
        RefreshArrowButtons();
        RefreshIncidentMarks();

        if (moveCoroutine != null)
            StopCoroutine(moveCoroutine);

        Vector3 targetPosition = target.position;
        targetPosition.z = cameraTransform.position.z;

        moveCoroutine = StartCoroutine(
            MoveRoutine(targetPosition)
        );
    }

    public void RaiseIncident(MapArea area)
    {
        incidentCountsByArea[(int)area]++;
        RefreshIncidentMarks();
    }

    public void ResolveIncident(MapArea area)
    {
        int index = (int)area;
        incidentCountsByArea[index] =
            Mathf.Max(0, incidentCountsByArea[index] - 1);
        RefreshIncidentMarks();
    }

    public MapArea GetAreaAtPosition(Vector3 worldPosition)
    {
        float leftDistance = GetHorizontalDistance(
            leftViewPoint,
            worldPosition
        );
        float centerDistance = GetHorizontalDistance(
            centerViewPoint,
            worldPosition
        );
        float rightDistance = GetHorizontalDistance(
            rightViewPoint,
            worldPosition
        );

        if (leftDistance <= centerDistance &&
            leftDistance <= rightDistance)
        {
            return MapArea.Left;
        }

        if (rightDistance <= centerDistance)
            return MapArea.Right;

        return MapArea.Center;
    }

    private IEnumerator MoveRoutine(Vector3 targetPosition)
    {
        Vector3 startPosition = cameraTransform.position;
        float elapsed = 0f;

        while (elapsed < moveDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(
                elapsed / moveDuration
            );
            float eased =
                progress * progress * (3f - 2f * progress);

            cameraTransform.position = Vector3.Lerp(
                startPosition,
                targetPosition,
                eased
            );
            yield return null;
        }

        cameraTransform.position = targetPosition;
        moveCoroutine = null;
    }

    private void MoveImmediately(MapArea area)
    {
        Transform target = GetViewPoint(area);

        if (cameraTransform != null && target != null)
        {
            Vector3 targetPosition = target.position;
            targetPosition.z = cameraTransform.position.z;
            cameraTransform.position = targetPosition;
        }
    }

    private Transform GetViewPoint(MapArea area)
    {
        switch (area)
        {
            case MapArea.Left:
                return leftViewPoint;

            case MapArea.Right:
                return rightViewPoint;

            default:
                return centerViewPoint;
        }
    }

    private void RefreshArrowButtons()
    {
        if (leftArrowButton != null)
        {
            leftArrowButton.interactable =
                CurrentArea != MapArea.Left;
        }

        if (rightArrowButton != null)
        {
            rightArrowButton.interactable =
                CurrentArea != MapArea.Right;
        }
    }

    private void RefreshIncidentMarks()
    {
        SetAllInactive(leftIncidentMarks);
        SetAllInactive(centerIncidentMarks);
        SetAllInactive(rightIncidentMarks);

        switch (CurrentArea)
        {
            case MapArea.Left:
                SetMarkActive(
                    leftIncidentMarks,
                    0,
                    HasIncident(MapArea.Center) ||
                    HasIncident(MapArea.Right)
                );
                break;

            case MapArea.Right:
                SetMarkActive(
                    rightIncidentMarks,
                    0,
                    HasIncident(MapArea.Left) ||
                    HasIncident(MapArea.Center)
                );
                break;

            case MapArea.Center:
                SetMarkActive(
                    centerIncidentMarks,
                    0,
                    HasIncident(MapArea.Left)
                );
                SetMarkActive(
                    centerIncidentMarks,
                    1,
                    HasIncident(MapArea.Right)
                );
                break;
        }
    }

    private bool HasIncident(MapArea area)
    {
        return incidentCountsByArea[(int)area] > 0;
    }

    private static void SetAllInactive(GameObject[] marks)
    {
        if (marks == null)
            return;

        foreach (GameObject mark in marks)
        {
            if (mark != null)
                mark.SetActive(false);
        }
    }

    private static void SetMarkActive(
        GameObject[] marks,
        int index,
        bool active)
    {
        if (marks == null ||
            index < 0 ||
            index >= marks.Length ||
            marks[index] == null)
        {
            return;
        }

        marks[index].SetActive(active);
    }

    private static float GetHorizontalDistance(
        Transform viewPoint,
        Vector3 worldPosition)
    {
        return viewPoint == null
            ? float.PositiveInfinity
            : Mathf.Abs(viewPoint.position.x - worldPosition.x);
    }
}
