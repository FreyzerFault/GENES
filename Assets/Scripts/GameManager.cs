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

    private GameState _state = GameState.Playing;

    public GameState State
    {
        get => _state;
        set
        {
            onGameStateChanged?.Invoke(value);
            _state = value;
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
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                break;
            case GameState.Paused:
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                break;
        }
    }
}