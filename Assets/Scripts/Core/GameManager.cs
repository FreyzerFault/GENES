using DavidUtils;
using DavidUtils.PlayerControl;
using Procrain.Core;
using UnityEngine;
using UnityEngine.Events;

namespace Core
{
    public class GameManager : SingletonPersistent<GameManager>
    {
        public enum GameState
        {
            Playing,
            Paused
        }

        public Player player;

        public UnityEvent<GameState> onGameStateChanged;
        [SerializeField] private GameState state = GameState.Playing;
        public bool IsPlaying => state == GameState.Playing;
        public bool IsPaused => state == GameState.Paused;

        public GameState State
        {
            get => state;
            set
            {
                state = value;
                OnStateChange(value);
            }
        }
        
        protected override void Awake()
        {
            base.Awake();
            
            OnStateChange(State);
            onGameStateChanged ??= new UnityEvent<GameState>();
            
            player = FindObjectOfType<Player>();
        }
        
        private void OnDestroy()
        {
            onGameStateChanged.RemoveAllListeners();
        }

        private void OnStateChange(GameState newState)
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
            
            onGameStateChanged?.Invoke(newState);
        }
        
        private void OnPause() => State = State == GameState.Paused ? GameState.Playing : GameState.Paused; 
    }
}