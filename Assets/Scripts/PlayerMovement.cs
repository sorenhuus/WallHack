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
    private Vector3 _serverVelocity;
    private float _verticalRotation;
    private float _localYaw;

    // Latest input state stored on the server
    private float _inputH;
    private float _inputV;
    private float _inputYaw;
    private bool _jumpQueued;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
    }

    public override void OnStartLocalPlayer()
    {
        if (Camera.main != null)
            Camera.main.gameObject.SetActive(false);

        Camera cam = cameraHolder.GetComponentInChildren<Camera>(includeInactive: true);
        if (cam != null)
            cam.gameObject.SetActive(true);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Start()
    {
        if (!isLocalPlayer)
        {
            Camera cam = cameraHolder.GetComponentInChildren<Camera>();
            if (cam != null)
                cam.gameObject.SetActive(false);
        }
    }

    // Client: read input every frame and send to server
    private void Update()
    {
        if (!isLocalPlayer) return;

        HandleMouseLook();
        HandleCursorLock();

        bool jump = Input.GetButtonDown("Jump");
        CmdSetInput(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical"),
            jump,
            transform.eulerAngles.y
        );
    }

    // Server: store latest input — movement applied in FixedUpdate
    [Command]
    private void CmdSetInput(float horizontal, float vertical, bool jump, float yaw)
    {
        _inputH = horizontal;
        _inputV = vertical;
        _inputYaw = yaw;
        if (jump) _jumpQueued = true; // latch jump so it isn't missed between ticks
    }

    // Server: apply movement once per tick using fixed deltaTime
    private void FixedUpdate()
    {
        if (!isServer) return;

        transform.rotation = Quaternion.Euler(0f, _inputYaw, 0f);

        bool isGrounded = _controller.isGrounded;

        if (isGrounded && _serverVelocity.y < 0f)
            _serverVelocity.y = -2f;

        Vector3 move = transform.right * _inputH + transform.forward * _inputV;
        if (move.magnitude > 1f)
            move.Normalize();

        _controller.Move(move * moveSpeed * Time.fixedDeltaTime);

        if (_jumpQueued && isGrounded)
        {
            _serverVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            _jumpQueued = false;
        }

        _serverVelocity.y += gravity * Time.fixedDeltaTime;
        _controller.Move(_serverVelocity * Time.fixedDeltaTime);
    }

    // Re-apply local rotation after NetworkTransform overwrites it
    private void LateUpdate()
    {
        if (!isLocalPlayer) return;
        transform.rotation = Quaternion.Euler(0f, _localYaw, 0f);
        cameraHolder.localRotation = Quaternion.Euler(_verticalRotation, 0f, 0f);
    }

    private void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);
        _localYaw = transform.eulerAngles.y;

        _verticalRotation -= mouseY;
        _verticalRotation = Mathf.Clamp(_verticalRotation, -verticalLookLimit, verticalLookLimit);
        cameraHolder.localRotation = Quaternion.Euler(_verticalRotation, 0f, 0f);
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
