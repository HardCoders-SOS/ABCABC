using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EndingUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject root;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private BeachSafetyManager beachSafetyManager;

    [Header("Text")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text finalScoreText;
    [SerializeField] private TMP_Text rescueResultText;
    [SerializeField] private TMP_Text messageText;

    [Header("Buttons")]
    [SerializeField] private Button restartButton;
    [SerializeField] private Button quitButton;

    [Header("Animation")]
    [SerializeField, Min(0.01f)] private float fadeDuration = 0.6f;

    private GameManager gameManager;
    private Coroutine fadeCoroutine;

    private void Awake()
    {
        HideImmediate();

        if (restartButton != null)
            restartButton.onClick.AddListener(RestartGame);

        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);
    }

    private void Start()
    {
        gameManager = GameManager.Instance;

        if (gameManager == null)
        {
            Debug.LogError("[EndingUI] GameManager is missing.", this);
            return;
        }

        gameManager.StateChanged += HandleStateChanged;

        if (gameManager.State == GameState.GameClear)
            Show();
    }

    private void OnDestroy()
    {
        if (gameManager != null)
            gameManager.StateChanged -= HandleStateChanged;

        if (restartButton != null)
            restartButton.onClick.RemoveListener(RestartGame);

        if (quitButton != null)
            quitButton.onClick.RemoveListener(QuitGame);
    }

    private void HandleStateChanged(GameState state)
    {
        if (state == GameState.GameClear)
            Show();
    }

    public void Show()
    {
        UpdateResult();
        root?.SetActive(true);

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadeInRoutine());
    }

    public void RestartGame()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.buildIndex);
    }

    public void QuitGame()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private void UpdateResult()
    {
        int score =
            scoreManager != null ? scoreManager.Score : 0;

        int success =
            beachSafetyManager != null
                ? beachSafetyManager.RescuedNPCCount
                : 0;

        int failure =
            beachSafetyManager != null
                ? beachSafetyManager.FailedNPCCount
                : 0;

        float successRate =
            beachSafetyManager != null
                ? beachSafetyManager.RescueSuccessRate * 100f
                : 0f;

        if (titleText != null)
            titleText.text = "해변 안전 임무 완료!";

        if (finalScoreText != null)
            finalScoreText.text = $"최종 점수 : {score}";

        if (rescueResultText != null)
        {
            rescueResultText.text =
                $"구조 성공 : {success}\n" +
                $"구조 실패 : {failure}\n" +
                $"구조 성공률 : {successRate:0}%";
        }

        if (messageText != null)
            messageText.text = GetEndingMessage(score, successRate);
    }

    private static string GetEndingMessage(
        int score,
        float successRate)
    {
        if (successRate >= 80f)
            return "최고의 안전요원입니다!";

        if (successRate >= 50f)
            return "무사히 임무를 마쳤습니다.";

        if (score > 0)
            return "조금 더 주의 깊은 구조가 필요합니다.";

        return "다시 도전해서 해변을 안전하게 만들어보세요!";
    }

    private IEnumerator FadeInRoutine()
    {
        if (canvasGroup == null)
        {
            fadeCoroutine = null;
            yield break;
        }

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = true;

        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha =
                Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        fadeCoroutine = null;
    }

    private void HideImmediate()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (root != null && root != gameObject)
            root.SetActive(false);
    }
}
