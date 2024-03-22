using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerTestController : MonoBehaviour
{
    public float speed = 1f;
    public float angularSpeed = 1f;

    [SerializeField] private Transform camPoint;

    private Vector3 _moveInput = Vector3.zero;
    private Terrain _terrain;

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
        GameManager.Instance.onGameStateChanged.AddListener(HandleGameStateChanged);
    }

    private void Update()
    {
        if (GameManager.Instance.State != GameManager.GameState.Playing) return;
        HandleMovementInput();
        HandleRotationInput();
        StickToTerrainHeight();
    }

    private void HandleGameStateChanged(GameManager.GameState state)
    {
        switch (state)
        {
            case GameManager.GameState.Playing:
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = true;
                break;
            case GameManager.GameState.Paused: break;
        }
    }

    private void HandleRotationInput()
    {
        var mouseDelta = Mouse.current.delta.ReadValue();

        if (mouseDelta == Vector2.zero) return;

        // CAM POINT Rotation in X Axis
        var rotation = camPoint.rotation;
        rotation *= Quaternion.Euler(-mouseDelta.y * angularSpeed / 2 * Time.deltaTime, 0, 0);

        // Convertir el ángulo a un rango de -180 a 180
        var angle = rotation.eulerAngles.x > 180 ? rotation.eulerAngles.x - 360 : rotation.eulerAngles.x;

        // Límites de la rotación
        var minAngle = -45f; // Ángulo mínimo
        var maxAngle = 45f; // Ángulo máximo

        // Clamp to 89º
        rotation = Quaternion.Euler(
            Mathf.Clamp(angle, minAngle, maxAngle),
            rotation.eulerAngles.y,
            rotation.eulerAngles.z
        );
        camPoint.rotation = rotation;

        // PLAYER
        transform.rotation *= Quaternion.Euler(0, mouseDelta.x * angularSpeed * Time.deltaTime, 0);
    }

    private void HandleMovementInput()
    {
        transform.position += Forward * (_moveInput.y * Time.deltaTime * speed) +
                              Right * (_moveInput.x * Time.deltaTime * speed);
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