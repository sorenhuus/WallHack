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

    [Header("Prediction")]
    [SerializeField] private float reconcileThreshold = 0.1f;  // min error before reconciling

    [Header("Footsteps")]
    [SerializeField] private float stepDistance = 1.5f;

    [Header("References")]
    [SerializeField] private Transform cameraHolder;

    // --- Snapshot history ---
    private struct TickSnapshot
    {
        public int    tick;
        public float  horizontal;
        public float  vertical;
        public bool   jump;
        public float  yaw;
        public Vector3 position;
        public Vector3 velocity;
    }

    private const int HistorySize = 128;
    private TickSnapshot[] _history = new TickSnapshot[HistorySize];

    // --- Component references ---
    private CharacterController _controller;

    // --- Shared state ---
    private float _verticalRotation;
    private float _localYaw;

    // --- Client prediction state ---
    private Vector3 _clientVelocity;

    // --- Server input state ---
    private float  _inputH;
    private float  _inputV;
    private float  _inputYaw;
    private bool   _jumpQueued;
    private int    _serverTick;

    // --- Server authoritative velocity ---
    private Vector3 _serverVelocity;

    // --- Footstep tracking ---
    private float _clientStepAccum;
    private bool  _wasMoving;
    private float _lastStepTime;
    private float _serverStepAccum;
    private bool  _serverWasMoving;
    private float _serverLastStepTime;

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

    // Client: predict locally, store snapshot, send input to server
    private void Update()
    {
        if (!isLocalPlayer) return;

        HandleMouseLook();
        HandleCursorLock();

        int tick = TickSystem.Instance != null ? TickSystem.Instance.Tick : 0;
        float h   = Input.GetAxisRaw("Horizontal");
        float v   = Input.GetAxisRaw("Vertical");
        bool  jump = Input.GetButtonDown("Jump");

        // Run prediction locally
        SimulateMovement(h, v, jump, Time.deltaTime);

        // Store snapshot AFTER moving so position reflects this tick's result
        int index = tick % HistorySize;
        _history[index] = new TickSnapshot
        {
            tick       = tick,
            horizontal = h,
            vertical   = v,
            jump       = jump,
            yaw        = transform.eulerAngles.y,
            position   = transform.position,
            velocity   = _clientVelocity
        };

        CmdSetInput(h, v, jump, transform.eulerAngles.y, tick);

        // Local footstep sounds — play immediately without network round-trip
        bool movingHorizontally = Mathf.Abs(h) > 0.1f || Mathf.Abs(v) > 0.1f;
        if (movingHorizontally && _controller.isGrounded)
        {
            if (!_wasMoving && Time.time - _lastStepTime >= stepDistance / moveSpeed)
            {
                _clientStepAccum = 0f;
                _lastStepTime = Time.time;
                if (SoundManager.Instance != null)
                    SoundManager.Instance.PlayFootstepAt(transform.position, showMarker: false);
            }
            else
            {
                _clientStepAccum += moveSpeed * Time.deltaTime;
                if (_clientStepAccum >= stepDistance)
                {
                    _clientStepAccum = 0f;
                    _lastStepTime = Time.time;
                    if (SoundManager.Instance != null)
                        SoundManager.Instance.PlayFootstepAt(transform.position, showMarker: false);
                }
            }
            _wasMoving = true;
        }
        else
        {
            _clientStepAccum = 0f;
            _wasMoving = false;
        }
    }

    private void SimulateMovement(float h, float v, bool jump, float deltaTime)
    {
        bool isGrounded = _controller.isGrounded;

        if (isGrounded && _clientVelocity.y < 0f)
            _clientVelocity.y = -2f;

        Vector3 move = transform.right * h + transform.forward * v;
        if (move.magnitude > 1f)
            move.Normalize();

        _controller.Move(move * moveSpeed * deltaTime);

        if (jump && isGrounded)
            _clientVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

        _clientVelocity.y += gravity * deltaTime;
        _controller.Move(_clientVelocity * deltaTime);
    }

    // Server: receive and store input
    [Command]
    private void CmdSetInput(float horizontal, float vertical, bool jump, float yaw, int tick)
    {
        _inputH   = horizontal;
        _inputV   = vertical;
        _inputYaw = yaw;
        if (jump) _jumpQueued = true;
        _serverTick = tick;
    }

    // Server: apply movement once per tick and send correction back
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

        // Send authoritative position tagged with the client's tick
        if (connectionToClient != null)
            RpcCorrectPosition(connectionToClient, transform.position, _serverVelocity, _serverTick);

        // Server-side footstep detection — broadcast to all other clients
        bool serverMoving = (Mathf.Abs(_inputH) > 0.1f || Mathf.Abs(_inputV) > 0.1f) && _controller.isGrounded;
        if (serverMoving)
        {
            if (!_serverWasMoving && Time.fixedTime - _serverLastStepTime >= stepDistance / moveSpeed)
            {
                _serverStepAccum = 0f;
                _serverLastStepTime = Time.fixedTime;
                RpcPlayFootstep(transform.position);
            }
            else
            {
                _serverStepAccum += moveSpeed * Time.fixedDeltaTime;
                if (_serverStepAccum >= stepDistance)
                {
                    _serverStepAccum = 0f;
                    _serverLastStepTime = Time.fixedTime;
                    RpcPlayFootstep(transform.position);
                }
            }
            _serverWasMoving = true;
        }
        else
        {
            _serverStepAccum = 0f;
            _serverWasMoving = false;
        }
    }

    [ClientRpc(includeOwner = false)]
    private void RpcPlayFootstep(Vector3 position)
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayFootstepAt(position, showMarker: true);
    }

    // Called by VisibilitySystem to update this player's position on an observing client
    [TargetRpc]
    public void RpcSetRemotePosition(NetworkConnectionToClient target, Vector3 position, Quaternion rotation)
    {
        if (isLocalPlayer) return;
        transform.position = position;
        transform.rotation = rotation;
    }

    // Client: receive server correction and reconcile using rollback + replay
    [TargetRpc]
    private void RpcCorrectPosition(NetworkConnection target, Vector3 serverPosition, Vector3 serverVelocity, int tick)
    {
        int index = tick % HistorySize;
        TickSnapshot snapshot = _history[index];

        // Only reconcile if this correction matches the tick we have stored
        if (snapshot.tick != tick) return;

        float error = Vector3.Distance(snapshot.position, serverPosition);

        // No meaningful error — nothing to correct
        if (error < reconcileThreshold) return;

        // Rollback: teleport to server's authoritative position at this tick
        _controller.enabled = false;
        transform.position = serverPosition;
        _controller.enabled = true;
        _clientVelocity = serverVelocity;

        // Replay: re-simulate all inputs from corrected tick up to current tick
        int currentTick = TickSystem.Instance != null ? TickSystem.Instance.Tick : tick;

        for (int t = tick + 1; t <= currentTick; t++)
        {
            int replayIndex = t % HistorySize;
            TickSnapshot replaySnap = _history[replayIndex];
            if (replaySnap.tick != t) break; // gap in history, stop replay

            transform.rotation = Quaternion.Euler(0f, replaySnap.yaw, 0f);
            SimulateMovement(replaySnap.horizontal, replaySnap.vertical, replaySnap.jump, Time.fixedDeltaTime);

            // Update stored position to reflect corrected replay
            _history[replayIndex].position = transform.position;
            _history[replayIndex].velocity = _clientVelocity;
        }
    }


    // Re-apply local rotation after any network update
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
