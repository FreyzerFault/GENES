using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerTestController : MonoBehaviour
{
    public enum PlayerState
    {
        Playing,
        Pause
    }

    public float speed = 1f;
    public float angularSpeed = 1f;

    [SerializeField] private Transform camPoint;

    private Vector3 _moveInput = Vector3.zero;

    private PlayerState _state = PlayerState.Playing;
    private Terrain _terrain;

    public PlayerState State
    {
        get => _state;
        set
        {
            if (value == _state) return;
            HandleStateChanged(value);
            _state = value;
        }
    }

    private Vector3 Position => transform.position;
    private Vector3 Forward => transform.forward;
    private Vector3 Right => transform.right;

    private void Awake()
    {
        _terrain = FindObjectOfType<Terrain>();

        transform.rotation = Quaternion.identity;
        camPoint.rotation = Quaternion.identity;
    }

    private void Start()
    {
        HandleStateChanged(_state);

        if (GameManager.Instance == null) return;
        GameManager.Instance.onGameStateChanged.AddListener(HandleGameStateChanged);
    }

    private void Update()
    {
        if (_state == PlayerState.Pause) return;
        HandleMovementInput();
        HandleRotationInput();
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

    private void HandleStateChanged(PlayerState newState)
    {
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

    private void HandleRotationInput()
    {
        var mouseDelta = Mouse.current.delta.ReadValue();

        if (mouseDelta == Vector2.zero) return;

        // Vertical Rotation
        var angle = -mouseDelta.y * angularSpeed * Time.deltaTime;

        // Convertir el ángulo a un rango de -180 a 180
        if (angle > 180) angle -= 360;

        // Clamp to 89º
        camPoint.localPosition = Quaternion.Euler(angle, 0, 0) * camPoint.localPosition;

        // Limitar a un angulo maximo y minimo
        var minAngle = -45f; // Ángulo mínimo
        var maxAngle = 45f; // Ángulo máximo
        var forward = Vector3.forward * camPoint.localPosition.magnitude;
        var angleDif = Vector3.SignedAngle(forward, camPoint.localPosition, Vector3.right);
        if (angleDif > maxAngle) camPoint.localPosition = Quaternion.Euler(maxAngle, 0, 0) * forward;
        if (angleDif < minAngle) camPoint.localPosition = Quaternion.Euler(minAngle, 0, 0) * forward;

        // Horizontal Rotation
        // PLAYER
        transform.rotation *= Quaternion.Euler(0, mouseDelta.x * angularSpeed * Time.deltaTime, 0);
    }

    private void HandleMovementInput()
    {
        transform.position +=
            Forward * (_moveInput.y * Time.deltaTime * speed)
            + Right * (_moveInput.x * Time.deltaTime * speed);
    }

    private void StickToTerrainHeight()
    {
        var position = transform.position;
        var height = _terrain.SampleHeight(position);
        position.y = height + 1;
        transform.position = position;
    }

    private void OnMove(InputValue value)
    {
        _moveInput = value.Get<Vector2>();
    }
}