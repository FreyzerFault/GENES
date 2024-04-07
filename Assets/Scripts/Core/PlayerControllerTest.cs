using System;
using DavidUtils.PlayerControl;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Core
{
    public class PlayerControllerTest : FPVplayer
    {
        public float speed = 1f;

        private Terrain _terrain;

        public event Action<Vector3> OnPositionChanged;
        

        protected override void Awake()
        {
            base.Awake();
            _terrain = FindObjectOfType<UnityEngine.Terrain>();
            
            StickToTerrainHeight();
        }

        protected override void Start()
        {
            base.Start();

            if (GameManager.Instance == null) return;
            GameManager.Instance.onGameStateChanged.AddListener(HandleGameStateChanged);
        }

        protected override void Update()
        {
            base.Update();
            if (_moveInput == Vector3.zero) return;
            
            HandleMovementInput();
            StickToTerrainHeight();
        }

        private void HandleGameStateChanged(GameManager.GameState gameState)
        {
            State = gameState switch
            {
                GameManager.GameState.Playing => PlayerState.Playing,
                GameManager.GameState.Paused => PlayerState.Pause,
                _ => State
            };
        }

        protected override void HandleStateChanged(PlayerState newState)
        {
            base.HandleStateChanged(newState);
            switch (newState)
            {
                case PlayerState.Playing:
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                    break;
                case PlayerState.Pause:
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    break;
            }
        }

        private void HandleMovementInput()
        {
            transform.position +=
                Forward * (_moveInput.y * Time.deltaTime * speed)
                + Right * (_moveInput.x * Time.deltaTime * speed);
        }

        private void StickToTerrainHeight()
        {
            Vector3 position = transform.position;
            float height = _terrain.SampleHeight(position);
            position.y = height + 1;
            transform.position = position;
        }
        
        protected override void OnMove(InputValue value)
        {
            if (_state == PlayerState.Pause) return;
            base.OnMove(value);
        }
    }
}