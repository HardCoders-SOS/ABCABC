using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RescueUI : MonoBehaviour
{
    [SerializeField] private GameObject canvas;
    [SerializeField] private Button[] itemButtons;
    [SerializeField] private TMP_Text statisticsText;
    [SerializeField] private TMP_Text timerText;

    public event Action<int> ItemSelected;

    public bool IsConfigured =>
        canvas != null &&
        itemButtons != null &&
        itemButtons.Length == 3;

    private void Awake()
    {
        SetVisible(false);
        RegisterButtonListeners();
    }

    public void Show(
        IReadOnlyList<BeachSafetyManager.RescueItem> items)
    {
        if (!IsConfigured || items == null)
            return;

        for (int i = 0; i < itemButtons.Length; i++)
        {
            Button button = itemButtons[i];

            if (button == null)
                continue;

            bool hasItem = i < items.Count;
            button.gameObject.SetActive(hasItem);
            button.interactable = hasItem;

            if (!hasItem)
                continue;

            Image image = button.GetComponent<Image>();

            if (image != null)
            {
                image.sprite = items[i].sprite;
                image.preserveAspect = true;
            }
        }

        SetVisible(true);
    }

    public void Hide()
    {
        SetVisible(false);
        SetButtonsInteractable(false);
    }

    public void SetStatistics(int successCount, int failureCount)
    {
        if (statisticsText == null)
            return;

        statisticsText.text =
            $"구조 성공 : {successCount}\n" +
            $"구조 실패 : {failureCount}";
    }

    public void SetTime(float secondsRemaining)
    {
        if (timerText == null)
            return;

        int seconds = Mathf.Max(
            0,
            Mathf.CeilToInt(secondsRemaining)
        );
        timerText.text = $"구조 제한시간 : {seconds}";
    }

    private void SetVisible(bool visible)
    {
        canvas?.SetActive(visible);
    }

    private void RegisterButtonListeners()
    {
        if (itemButtons == null)
            return;

        for (int i = 0; i < itemButtons.Length; i++)
        {
            Button button = itemButtons[i];

            if (button == null)
                continue;

            int index = i;
            button.onClick.AddListener(
                () => ItemSelected?.Invoke(index)
            );
        }
    }

    private void SetButtonsInteractable(bool interactable)
    {
        if (itemButtons == null)
            return;

        foreach (Button button in itemButtons)
        {
            if (button != null)
                button.interactable = interactable;
        }
    }
}
