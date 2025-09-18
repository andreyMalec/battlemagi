using UnityEngine;
using Unity.Netcode;
using System.Collections;
using UnityEngine.SceneManagement;

public class FirstPersonMovement : NetworkBehaviour {
    [SerializeField] private MovementSettings movementSettings;
    public GroundCheck groundCheck;
    public NetworkVariable<float> globalSpeedMultiplier = new(1f);

    public bool IsRunning { get; private set; }
    public event System.Action Jumped;

    // Сетевые переменные
    private readonly NetworkVariable<Vector3> _networkPosition = new();
    private readonly NetworkVariable<bool> _isRunningNetwork = new();
    private readonly NetworkVariable<bool> _isJumpingNetwork = new();

    private CharacterController _characterController;
    private float _jumpCooldownTimer;
    private float _velocityY;

    private void Awake() {
        _characterController = GetComponent<CharacterController>();
        if (_characterController == null)
            _characterController = gameObject.AddComponent<CharacterController>();
    }

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();

        _isRunningNetwork.OnValueChanged += OnIsRunningChanged;
        _isJumpingNetwork.OnValueChanged += OnIsJumpingChanged;
        _networkPosition.OnValueChanged += OnPositionChanged;
    }

    public override void OnNetworkDespawn() {
        base.OnNetworkDespawn();
        _isRunningNetwork.OnValueChanged -= OnIsRunningChanged;
        _isJumpingNetwork.OnValueChanged -= OnIsJumpingChanged;
        _networkPosition.OnValueChanged -= OnPositionChanged;
    }

    private void OnIsRunningChanged(bool _, bool newValue) => IsRunning = newValue;

    private void OnIsJumpingChanged(bool oldValue, bool newValue) {
        if (newValue && !oldValue && !IsOwner)
            Jumped?.Invoke();
    }

    private void OnPositionChanged(Vector3 _, Vector3 newValue) {
        if (!IsOwner)
            transform.position = newValue;
    }

    private void Update() {
        if (IsOwner)
            HandleOwnerInput();
    }

    private void HandleOwnerInput() {
        UpdateJumpCooldown();
        HandleMovementInput();
        TryJump();
        SyncPosition();
    }

    private void UpdateJumpCooldown() {
        if (_jumpCooldownTimer > 0)
            _jumpCooldownTimer -= Time.deltaTime;
    }

    private void HandleMovementInput() {
        Vector2 input = new(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        bool running = movementSettings.canRun && Input.GetKey(movementSettings.runningKey);

        if (running != _isRunningNetwork.Value)
            SetRunningServerRpc(running);

        ApplyMovement(input, running);
    }

    [ServerRpc]
    private void SetRunningServerRpc(bool running) => _isRunningNetwork.Value = running;

    private void ApplyMovement(Vector2 input, bool running) {
        float targetSpeed = running ? movementSettings.runSpeed : movementSettings.speed;
        float speedMultiplier = groundCheck.isGrounded ? 1f : movementSettings.flySpeedMultiplier;

        Vector3 moveDirection = transform.TransformDirection(new Vector3(
            input.x * targetSpeed * speedMultiplier * globalSpeedMultiplier.Value,
            0f,
            input.y * targetSpeed * speedMultiplier * globalSpeedMultiplier.Value
        ));

        ApplyGravity();
        _characterController.Move((moveDirection + Vector3.up * _velocityY) * Time.deltaTime);
    }

    private void ApplyGravity() {
        if (groundCheck.isGrounded && _velocityY < 0)
            _velocityY = -2f;
        else
            _velocityY += movementSettings.gravity * (_velocityY < 0 ? movementSettings.fallGravityMultiplier : 1f) *
                          Time.deltaTime;

        _velocityY = Mathf.Max(_velocityY, movementSettings.maxFallSpeed);
    }

    private void TryJump() {
        if (Input.GetKeyDown(movementSettings.jumpKey) && CanJump())
            PerformJump();
    }

    private bool CanJump() => _jumpCooldownTimer <= 0 && groundCheck.isGrounded;

    private void PerformJump() {
        _jumpCooldownTimer = movementSettings.jumpCooldown;
        JumpServerRpc(true);
        StartCoroutine(ApplyJumpForce());
    }

    [ServerRpc]
    private void JumpServerRpc(bool jumping) => _isJumpingNetwork.Value = jumping;

    private IEnumerator ApplyJumpForce() {
        Jumped?.Invoke();
        yield return new WaitForSeconds(movementSettings.jumpDelay);
        _velocityY = Mathf.Sqrt(movementSettings.jumpStrength * -2f * movementSettings.gravity);
        JumpServerRpc(false);
    }

    private void SyncPosition() {
        if (Vector3.Distance(transform.position, _networkPosition.Value) > 0.1f)
            UpdatePositionServerRpc(transform.position);
    }

    [ServerRpc]
    private void UpdatePositionServerRpc(Vector3 position) => _networkPosition.Value = position;
}