using Mirror;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : NetworkBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float jumpHeight = 1.2f;
    [SerializeField] private float gravity = -20f;

    [Header("Mouse Look")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float verticalLookLimit = 80f;

    [Header("References")]
    [SerializeField] private Transform cameraHolder;

    private CharacterController _controller;
    private Vector3 _velocity;
    private float _verticalRotation;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
    }

    public override void OnStartLocalPlayer()
    {
        // Disable the scene overview camera so only the player's FPS camera renders
        if (Camera.main != null)
            Camera.main.gameObject.SetActive(false);

        // Enable the camera only for the local player
        Camera cam = cameraHolder.GetComponentInChildren<Camera>(includeInactive: true);
        if (cam != null)
            cam.gameObject.SetActive(true);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Start()
    {
        // Disable camera for remote players (it starts disabled in prefab)
        if (!isLocalPlayer)
        {
            Camera cam = cameraHolder.GetComponentInChildren<Camera>();
            if (cam != null)
                cam.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (!isLocalPlayer) return;

        HandleMouseLook();
        HandleMovement();
        HandleCursorLock();
    }

    private void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Rotate player body left/right
        transform.Rotate(Vector3.up * mouseX);

        // Rotate camera up/down (clamped)
        _verticalRotation -= mouseY;
        _verticalRotation = Mathf.Clamp(_verticalRotation, -verticalLookLimit, verticalLookLimit);
        cameraHolder.localRotation = Quaternion.Euler(_verticalRotation, 0f, 0f);
    }

    private void HandleMovement()
    {
        bool isGrounded = _controller.isGrounded;

        if (isGrounded && _velocity.y < 0f)
            _velocity.y = -2f; // keep grounded

        float horizontal = Input.GetAxisRaw("Horizontal"); // A/D
        float vertical   = Input.GetAxisRaw("Vertical");   // W/S

        Vector3 move = transform.right * horizontal + transform.forward * vertical;

        // Normalize diagonal movement
        if (move.magnitude > 1f)
            move.Normalize();

        _controller.Move(move * moveSpeed * Time.deltaTime);

        // Jump
        if (Input.GetButtonDown("Jump") && isGrounded)
            _velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

        // Apply gravity
        _velocity.y += gravity * Time.deltaTime;
        _controller.Move(_velocity * Time.deltaTime);
    }

    private void HandleCursorLock()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else if (Input.GetMouseButtonDown(0) && Cursor.lockState == CursorLockMode.None)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
