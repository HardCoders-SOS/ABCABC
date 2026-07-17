using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DistractionUI : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private Image portraitImage;
    [SerializeField] private Image dialogueImage;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private Button portraitButton;

    [Header("Animation")]
    [SerializeField] private RectTransform animatedRoot;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Vector2 hiddenOffset =
        new Vector2(700f, 0f);
    [SerializeField, Min(0.01f)] private float showDuration = 0.35f;
    [SerializeField, Min(0.01f)] private float hideDuration = 0.2f;

    public event Action Dismissed;

    public bool IsVisible =>
        root != null && root.activeSelf;

    private Vector2 visiblePosition;
    private Coroutine animationCoroutine;
    private bool canDismiss;

    private void Awake()
    {
        if (animatedRoot != null)
            visiblePosition = animatedRoot.anchoredPosition;

        if (portraitButton != null)
            portraitButton.onClick.AddListener(Dismiss);

        HideImmediate();
    }

    private void OnDestroy()
    {
        if (portraitButton != null)
            portraitButton.onClick.RemoveListener(Dismiss);
    }

    public void Show(
        Sprite portrait,
        Sprite dialogueBackground,
        string dialogue)
    {
        if (portraitImage != null)
            portraitImage.sprite = portrait;

        if (dialogueImage != null &&
            dialogueBackground != null)
        {
            dialogueImage.sprite = dialogueBackground;
        }

        if (dialogueText != null)
            dialogueText.text = dialogue;

        root?.SetActive(true);
        StartAnimation(ShowRoutine());
    }

    public void Hide()
    {
        StopAnimation();
        HideImmediate();
    }

    public void Dismiss()
    {
        if (!IsVisible || !canDismiss)
            return;

        canDismiss = false;
        StartAnimation(HideRoutine());
    }

    private IEnumerator ShowRoutine()
    {
        canDismiss = false;

        if (animatedRoot != null)
            animatedRoot.anchoredPosition =
                visiblePosition + hiddenOffset;

        SetCanvasState(0f, false);

        float elapsed = 0f;

        while (elapsed < showDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / showDuration);
            float eased = 1f - Mathf.Pow(1f - progress, 3f);

            if (animatedRoot != null)
            {
                animatedRoot.anchoredPosition = Vector2.Lerp(
                    visiblePosition + hiddenOffset,
                    visiblePosition,
                    eased
                );
            }

            SetCanvasAlpha(eased);
            yield return null;
        }

        if (animatedRoot != null)
            animatedRoot.anchoredPosition = visiblePosition;

        SetCanvasState(1f, true);
        canDismiss = true;
        animationCoroutine = null;
    }

    private IEnumerator HideRoutine()
    {
        Vector2 startPosition =
            animatedRoot != null
                ? animatedRoot.anchoredPosition
                : visiblePosition;

        float startAlpha =
            canvasGroup != null ? canvasGroup.alpha : 1f;
        float elapsed = 0f;

        SetCanvasState(startAlpha, false);

        while (elapsed < hideDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / hideDuration);
            float eased = progress * progress;

            if (animatedRoot != null)
            {
                animatedRoot.anchoredPosition = Vector2.Lerp(
                    startPosition,
                    visiblePosition + hiddenOffset,
                    eased
                );
            }

            SetCanvasAlpha(Mathf.Lerp(startAlpha, 0f, progress));
            yield return null;
        }

        HideImmediate();
        animationCoroutine = null;
        Dismissed?.Invoke();
    }

    private void StartAnimation(IEnumerator routine)
    {
        StopAnimation();
        animationCoroutine = StartCoroutine(routine);
    }

    private void StopAnimation()
    {
        if (animationCoroutine == null)
            return;

        StopCoroutine(animationCoroutine);
        animationCoroutine = null;
    }

    private void HideImmediate()
    {
        canDismiss = false;
        SetCanvasState(0f, false);

        if (animatedRoot != null)
        {
            animatedRoot.anchoredPosition =
                visiblePosition + hiddenOffset;
        }

        root?.SetActive(false);
    }

    private void SetCanvasState(float alpha, bool interactable)
    {
        if (canvasGroup == null)
            return;

        canvasGroup.alpha = alpha;
        canvasGroup.interactable = interactable;
        canvasGroup.blocksRaycasts = interactable;
    }

    private void SetCanvasAlpha(float alpha)
    {
        if (canvasGroup != null)
            canvasGroup.alpha = alpha;
    }
}
