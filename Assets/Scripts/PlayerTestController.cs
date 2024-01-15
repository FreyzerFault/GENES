using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerTestController : MonoBehaviour
{
    public float speed = 1f;
    public float angularSpeed = 1f;
    private GameObject body;

    private Vector3 moveInput = Vector3.zero;
    private Terrain terrain;

    private void Awake()
    {
        terrain = FindObjectOfType<Terrain>();
        body = GetComponentInChildren<Collider>().gameObject;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        transform.rotation = Quaternion.identity;
    }

    private void Update()
    {
        HandleMovementInput();
        // HandleRotationInput();
        StickToTerrainHeight();
    }

    private void HandleRotationInput()
    {
        var mouseDelta = Mouse.current.delta.ReadValue();

        if (mouseDelta == Vector2.zero) return;

        // BODY
        body.transform.rotation = Quaternion.identity;

        var rotation = transform.rotation;

        rotation.eulerAngles += new Vector3(-mouseDelta.y * angularSpeed / 2 * Time.deltaTime,
            mouseDelta.x * angularSpeed * Time.deltaTime, 0);
        transform.rotation = rotation;
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