using UnityEngine;
using Unity.Netcode;
using System.Collections;
using Unity.Netcode.Components;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(NetworkTransform))]
[RequireComponent(typeof(NetworkStatSystem))]
[RequireComponent(typeof(PlayerPhysics))]
public class FirstPersonMovement : NetworkBehaviour {
    public MovementSettings movementSettings;
    public GroundCheck groundCheck;

    public float jumpStrength;
    public float movementSpeed;
    public float runSpeed;
    public float maxStamina;

    public bool IsRunning { get; private set; }
    public event System.Action Jumped;

    // Сетевые переменные
    private readonly NetworkVariable<bool> _isRunningNetwork = new();
    private readonly NetworkVariable<bool> _isJumpingNetwork = new();
    public readonly NetworkVariable<float> stamina = new();
    public readonly NetworkVariable<Vector3> spawnPoint = new();
    private int _spawnTick;

    private NetworkStatSystem _statSystem;
    private PlayerPhysics _physics;
    private float _jumpCooldownTimer;

    // Ключи/локи для бега
    private bool _lastSentRunKeyHeld; // клиентская оптимизация: шлём RPC только при изменении
    private bool _runKeyHeldServer; // серверный флаг (который ставит SetRunKeyHeldServerRpc)
    private bool _runLock; // серверный лок: запрещает авто-включение пока не отпустили кнопку

    private const float MinStaminaThreshold = 0.05f;

    private void Awake() {
        _statSystem = GetComponent<NetworkStatSystem>();
        _physics = GetComponent<PlayerPhysics>();
        _physics.Configure(movementSettings, groundCheck);
    }

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();

        _isRunningNetwork.OnValueChanged += OnIsRunningChanged;
        _isJumpingNetwork.OnValueChanged += OnIsJumpingChanged;

        if (IsServer) {
            stamina.Value = maxStamina;
        }

        if (IsOwner) {
            spawnPoint.OnValueChanged += OnSpawnPointChanged;
        }
    }

    private void OnSpawnPointChanged(Vector3 previousValue, Vector3 newValue) {
        Debug.Log($"OnSpawnPointChanged: {previousValue} -> {newValue}");
        _spawnTick = 5;
    }

    public override void OnNetworkDespawn() {
        base.OnNetworkDespawn();
        _isRunningNetwork.OnValueChanged -= OnIsRunningChanged;
        _isJumpingNetwork.OnValueChanged -= OnIsJumpingChanged;

        if (IsOwner) {
            spawnPoint.OnValueChanged -= OnSpawnPointChanged;
        }
    }

    private void OnIsRunningChanged(bool _, bool newValue) => IsRunning = newValue;

    private void OnIsJumpingChanged(bool oldValue, bool newValue) {
        if (newValue && !oldValue && !IsOwner)
            Jumped?.Invoke();
    }

    private void Update() {
        // 1) Обработка ввода — делаем это ДО возврата для серверного блока.
        //    Input доступен только на клиенте/владельце, поэтому проверяем IsOwner.
        if (IsOwner) {
            if (_spawnTick > 0) {
                _spawnTick--;
                transform.position = spawnPoint.Value;
            } else {
                HandleOwnerInput();
            }
        }
    }

    private void HandleOwnerInput() {
        UpdateJumpCooldown();
        HandleMovementInput();
        TryJump();
    }

    private void UpdateJumpCooldown() {
        if (_jumpCooldownTimer > 0)
            _jumpCooldownTimer -= Time.deltaTime;
    }

    private void HandleMovementInput() {
        Vector2 input = new(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));

        bool runKeyHeld = movementSettings.canRun && Input.GetKey(movementSettings.runningKey);
        if (runKeyHeld != _lastSentRunKeyHeld) {
            SetRunKeyHeldServerRpc(runKeyHeld);
            _lastSentRunKeyHeld = runKeyHeld;
        }

        bool running = _isRunningNetwork.Value;
        ApplyMovement(input, running);
    }

    [ServerRpc(RequireOwnership = true)]
    private void SetRunKeyHeldServerRpc(bool held) {
        _runKeyHeldServer = held;
        if (!held) {
            _runLock = false;
        }

        UpdateRunningStateServer();
    }

    private void UpdateRunningStateServer() {
        bool shouldRun = _runKeyHeldServer && !_runLock && stamina.Value > MinStaminaThreshold;
        if (shouldRun != _isRunningNetwork.Value) {
            _isRunningNetwork.Value = shouldRun;
        }
    }

    private void ApplyMovement(Vector2 input, bool running) {
        float targetSpeed = running ? runSpeed : movementSpeed;
        float speedMultiplier = groundCheck.isGrounded ? 1f : movementSettings.flySpeedMultiplier;

        speedMultiplier *= _statSystem.Stats.GetFinal(StatType.MoveSpeed);
        Vector3 moveDirection = transform.TransformDirection(new Vector3(
            input.x * targetSpeed * speedMultiplier,
            0f,
            input.y * targetSpeed * speedMultiplier
        ));

        _physics.MoveWithGravity(moveDirection);
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
        _physics.Jump(jumpStrength);
        JumpServerRpc(false);
    }

    [ClientRpc]
    public void ApplyImpulseClientRpc(Vector3 impulse, ClientRpcParams clientRpcParams = default) {
        _physics.ApplyImpulse(impulse);
    }
}