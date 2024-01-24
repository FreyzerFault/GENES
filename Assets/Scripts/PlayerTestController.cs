using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerTestController : MonoBehaviour
{
    public float speed = 1f;
    public float angularSpeed = 1f;

    [SerializeField] private Transform camPoint;

    private GameObject body;
    private float initialCamRotationEulerX;

    private Vector3 moveInput = Vector3.zero;
    private Terrain terrain;

    private void Awake()
    {
        terrain = FindObjectOfType<Terrain>();
        body = GetComponentInChildren<Collider>().gameObject;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = true;

        transform.rotation = Quaternion.identity;
        camPoint.rotation = Quaternion.identity;
        initialCamRotationEulerX = camPoint.rotation.eulerAngles.x;
    }

    private void Update()
    {
        HandleMovementInput();
        HandleRotationInput();
        StickToTerrainHeight();
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
        transform.position += transform.forward * (moveInput.y * Time.deltaTime * speed) +
                              transform.right * (moveInput.x * Time.deltaTime * speed);
    }

    private void StickToTerrainHeight()
    {
        var position = transform.position;
        var height = terrain.SampleHeight(position);
        position.y = height + 1;
        transform.position = position;
    }

    private void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }
}