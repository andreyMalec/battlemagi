using UnityEngine;
using Unity.Netcode;
using System.Collections;
using Unity.Netcode.Components;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(NetworkTransform))]
public class FirstPersonMovement : NetworkBehaviour {
    public MovementSettings movementSettings;
    public GroundCheck groundCheck;
    public NetworkVariable<float> globalSpeedMultiplier = new(1f);

    public bool IsRunning { get; private set; }
    public event System.Action Jumped;

    // Сетевые переменные
    private readonly NetworkVariable<bool> _isRunningNetwork = new();
    private readonly NetworkVariable<bool> _isJumpingNetwork = new();
    public readonly NetworkVariable<float> stamina = new();
    public readonly NetworkVariable<Vector3> spawnPoint = new();
    private int spawnTick = 0;

    private CharacterController _characterController;
    private float _jumpCooldownTimer;
    private float _velocityY;

    // Ключи/локи для бега
    private bool lastSentRunKeyHeld = false; // клиентская оптимизация: шлём RPC только при изменении
    private bool runKeyHeldServer = false; // серверный флаг (который ставит SetRunKeyHeldServerRpc)
    private bool runLock = false; // серверный лок: запрещает авто-включение пока не отпустили кнопку

    private const float minStaminaThreshold = 0.05f;

    private void Awake() {
        _characterController = GetComponent<CharacterController>();
    }

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();

        _isRunningNetwork.OnValueChanged += OnIsRunningChanged;
        _isJumpingNetwork.OnValueChanged += OnIsJumpingChanged;

        if (IsServer) {
            stamina.Value = movementSettings.maxStamina;
        }

        if (IsOwner) {
            spawnPoint.OnValueChanged += OnSpawnPointChanged;
        }
    }

    private void OnSpawnPointChanged(Vector3 previousValue, Vector3 newValue) {
        Debug.Log($"OnSpawnPointChanged: {previousValue} -> {newValue}");
        spawnTick = 5;
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
            if (spawnTick > 0) {
                spawnTick--;
                transform.position = spawnPoint.Value;
            } else {
                HandleOwnerInput();
            }
        }

        // 2) Серверная логика стамины и состояния бега
        if (!IsServer) return;

        // Если игрок бежит — тратим стамину
        if (_isRunningNetwork.Value) {
            stamina.Value -= Time.deltaTime;
            if (stamina.Value <= minStaminaThreshold) {
                stamina.Value = 0f;
                // Отключаем бег и ставим лок: запрещаем авто-возобновление, пока не отпущена клавиша
                _isRunningNetwork.Value = false;
                runLock = true;
            }
        } else {
            // Восстанавливаем стамину только если кнопка не зажата и лок не выставлен
            // (т.е. пока держат кнопку — стамина не растёт)
            if (!runKeyHeldServer && !runLock) {
                float missing = movementSettings.maxStamina - stamina.Value;
                float restoreRate = movementSettings.staminaRestore * (missing / movementSettings.maxStamina);
                stamina.Value += Time.deltaTime * restoreRate;
            }
        }

        stamina.Value = Mathf.Clamp(stamina.Value, 0f, movementSettings.maxStamina);

        // Сервер может сам обновлять состояние бега исходя из флага held/lock/stamina
        UpdateRunningStateServer();
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

        // Отслеживаем состояние клавиши бега — шлём на сервер только при изменении,
        // чтобы сервер знaл, держит ли игрок Shift.
        bool runKeyHeld = movementSettings.canRun && Input.GetKey(movementSettings.runningKey);
        if (runKeyHeld != lastSentRunKeyHeld) {
            SetRunKeyHeldServerRpc(runKeyHeld);
            lastSentRunKeyHeld = runKeyHeld;
        }

        // Используем сетевую переменную _isRunningNetwork для определения фактического состояния бега.
        // (сервер сам решает, разрешить ли бег)
        bool running = _isRunningNetwork.Value;

        ApplyMovement(input, running);
    }

    [ServerRpc(RequireOwnership = true)]
    private void SetRunKeyHeldServerRpc(bool held, ServerRpcParams rpcParams = default) {
        // Этот метод вызывается владельцем игрока. Сервер сохраняет состояние кнопки.
        runKeyHeldServer = held;

        // Если игрок отпустил кнопку — снимаем runLock (теперь можно заново побежать при новом нажатии)
        if (!held) {
            runLock = false;
        }

        // и тут же пересчитаем shouldRun (сервер принимает решение)
        UpdateRunningStateServer();
    }

    // Серверный метод — решает, должен ли игрок бежать прямо сейчас
    private void UpdateRunningStateServer() {
        // условие для запуска бега: кнопка зажата на клиенте, нет runLock и есть стамина выше минимума
        bool shouldRun = runKeyHeldServer && !runLock && stamina.Value > minStaminaThreshold;

        if (shouldRun != _isRunningNetwork.Value) {
            _isRunningNetwork.Value = shouldRun;
        }
    }

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
}