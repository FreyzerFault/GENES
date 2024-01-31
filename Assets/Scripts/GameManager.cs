using UnityEngine;
using UnityEngine.Events;

public class GameManager : SingletonPersistent<GameManager>
{
    public enum GameState
    {
        Playing,
        Paused
    }

    public UnityEvent<GameState> onGameStateChanged;

    [SerializeField] private GameState state = GameState.Playing;

    public GameState State
    {
        get => state;
        set
        {
            onGameStateChanged?.Invoke(value);
            state = value;
        }
    }

    public bool IsPlaying => State == GameState.Playing;
    public bool IsPaused => State == GameState.Paused;


    private new void Awake()
    {
        base.Awake();

        onGameStateChanged ??= new UnityEvent<GameState>();

        onGameStateChanged.AddListener(HandleChangeState);
    }

    private void HandleChangeState(GameState newState)
    {
        switch (newState)
        {
            case GameState.Playing:
                break;
            case GameState.Paused:
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                break;
        }
    }
}