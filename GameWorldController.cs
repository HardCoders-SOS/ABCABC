using UnityEngine;

public class GameWorldController : MonoBehaviour
{
    [SerializeField] private NPCSpawner npcSpawner;
    [SerializeField] private TrashSpawner trashSpawner;
    [SerializeField] private RunningNPCSpawner runningNPCSpawner;

    private GameManager gameManager;

    private void Start()
    {
        gameManager = GameManager.Instance;

        if (gameManager == null)
        {
            Debug.LogError(
                "[GameWorldController] GameManager is missing.",
                this
            );
            enabled = false;
            return;
        }

        gameManager.StateChanged += HandleStateChanged;
        HandleStateChanged(gameManager.State);
    }

    private void OnDestroy()
    {
        if (gameManager != null)
            gameManager.StateChanged -= HandleStateChanged;
    }

    private void HandleStateChanged(GameState state)
    {
        switch (state)
        {
            case GameState.Playing:
                SetSpawningEnabled(true);
                SetInteractionEnabled(true);
                break;

            case GameState.Rescue:
            case GameState.DayTransition:
                SetSpawningEnabled(false);
                SetInteractionEnabled(false);
                break;

            case GameState.GameClear:
                SetSpawningEnabled(false);
                SetInteractionEnabled(false);
                break;
        }
    }

    private void SetSpawningEnabled(bool enabled)
    {
        npcSpawner?.SetSpawningEnabled(enabled);
        trashSpawner?.SetSpawningEnabled(enabled);
        runningNPCSpawner?.SetSpawningEnabled(enabled);
    }

    private void SetInteractionEnabled(bool enabled)
    {
        trashSpawner?.SetInteractionEnabled(enabled);
        runningNPCSpawner?.SetInteractionEnabled(enabled);
    }
}
