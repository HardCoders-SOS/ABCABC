using System;
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

    public event Action Dismissed;

    public bool IsVisible =>
        root != null && root.activeSelf;

    private void Awake()
    {
        if (portraitButton != null)
            portraitButton.onClick.AddListener(Dismiss);
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
    }

    public void Hide()
    {
        HideImmediate();
    }

    public void Dismiss()
    {
        if (!IsVisible)
            return;

        HideImmediate();
        Dismissed?.Invoke();
    }

    private void HideImmediate()
    {
        root?.SetActive(false);
    }
}
