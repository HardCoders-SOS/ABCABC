using UnityEngine;

public class GameWorldController : MonoBehaviour
{
    [SerializeField] private NPCSpawner[] npcSpawners;
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
            case GameState.Rescue:
                SetSpawningEnabled(true);
                SetInteractionEnabled(true);
                break;

            case GameState.DayTransition:
                SetSpawningEnabled(false);
                SetInteractionEnabled(false);
                break;

            case GameState.GameClear:
            case GameState.GameOver:
                SetSpawningEnabled(false);
                SetInteractionEnabled(false);
                break;
        }
    }

    private void SetSpawningEnabled(bool enabled)
    {
        if (npcSpawners != null)
        {
            foreach (NPCSpawner npcSpawner in npcSpawners)
                npcSpawner?.SetSpawningEnabled(enabled);
        }

        trashSpawner?.SetSpawningEnabled(enabled);
        runningNPCSpawner?.SetSpawningEnabled(enabled);
    }

    private void SetInteractionEnabled(bool enabled)
    {
        trashSpawner?.SetInteractionEnabled(enabled);
        runningNPCSpawner?.SetInteractionEnabled(enabled);
    }
}
