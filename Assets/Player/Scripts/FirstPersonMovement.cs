using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class FirstPersonMovement : NetworkBehaviour {
    public float speed = 4;
    public bool IsRunning { get; private set; }
    public bool canRun = true;
    public float runSpeed = 8;
    public float jumpDelay = 0.5f;
    public float jumpCooldown = 1f;
    public float gravity = -9.81f;
    public float fallGravityMultiplier = 2.5f;
    public float maxFallSpeed = -25f;
    public float flySpeedMulti = 0.5f;
    public KeyCode runningKey = KeyCode.LeftShift;
    public KeyCode jumpKey = KeyCode.Space;
    public float jumpStrength = 2;
    public event System.Action Jumped;
    public GroundCheck groundCheck;

    // Сетевые переменные
    private NetworkVariable<Vector3> networkPosition = new NetworkVariable<Vector3>();
    private NetworkVariable<Vector3> moveInput = new NetworkVariable<Vector3>();
    private NetworkVariable<bool> isRunning = new NetworkVariable<bool>();
    private NetworkVariable<bool> isJumping = new NetworkVariable<bool>();

    private float jumpCooldownTimer = 0f;
    private Vector3 velocity;
    private Vector3 targetPosition;
    private Vector3 previousPosition;
    private float interpolationTime;
    private CharacterController characterController;

    void Awake() {
        characterController = GetComponent<CharacterController>();
        if (characterController == null) {
            characterController = gameObject.AddComponent<CharacterController>();
        }
    }

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();

        // Подписываемся на изменения сетевых переменных
        moveInput.OnValueChanged += OnMoveInputChanged;
        isRunning.OnValueChanged += OnIsRunningChanged;
        isJumping.OnValueChanged += OnIsJumpingChanged;
        networkPosition.OnValueChanged += OnPositionChanged;

        // Для чужих объектов отключаем CharacterController
        if (!IsOwner) {
            characterController.enabled = false;
            targetPosition = transform.position;
            previousPosition = transform.position;
        } else {
            characterController.enabled = true;
        }
    }

    public override void OnNetworkDespawn() {
        base.OnNetworkDespawn();

        // Отписываемся от событий
        moveInput.OnValueChanged -= OnMoveInputChanged;
        isRunning.OnValueChanged -= OnIsRunningChanged;
        isJumping.OnValueChanged -= OnIsJumpingChanged;
        networkPosition.OnValueChanged -= OnPositionChanged;
    }

    private void OnMoveInputChanged(Vector3 oldValue, Vector3 newValue) {
        UpdateVisualEffects();
    }

    private void OnIsRunningChanged(bool oldValue, bool newValue) {
        IsRunning = newValue;
        UpdateVisualEffects();
    }

    private void OnIsJumpingChanged(bool oldValue, bool newValue) {
        if (newValue && !oldValue && !IsOwner) {
            // Визуальные эффекты прыжка для других игроков
            Jumped?.Invoke();
        }

        UpdateVisualEffects();
    }

    private void OnPositionChanged(Vector3 oldValue, Vector3 newValue) {
        if (!IsOwner) {
            previousPosition = transform.position;
            targetPosition = newValue;
            interpolationTime = 0f;
        }
    }

    void Update() {
        if (IsOwner) {
            HandleOwnerInput();
        } else {
            InterpolateMovement();
        }
    }

    private void HandleOwnerInput() {
        // Таймер прыжка
        if (jumpCooldownTimer > 0) {
            jumpCooldownTimer -= Time.deltaTime;
        }

        // Сбор ввода
        Vector3 input = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
        bool running = canRun && Input.GetKey(runningKey);

        // Отправка ввода на сервер
        if (input != moveInput.Value || running != isRunning.Value) {
            SubmitInputServerRpc(input.normalized, running);
        }

        // Обработка движения (клиентская авторизация)
        HandleMovement(input, running);

        // Запрос прыжка
        if (Input.GetKeyDown(jumpKey) && jumpCooldownTimer <= 0) {
            TryJump();
        }

        // Синхронизация позиции с сервером
        if (Vector3.Distance(transform.position, networkPosition.Value) > 0.1f) {
            UpdatePositionServerRpc(transform.position);
        }
    }

    private void HandleMovement(Vector3 input, bool running) {
        float targetMovingSpeed = running ? runSpeed : speed;
        var speedMulti = groundCheck.isGrounded ? 1f : flySpeedMulti;

        // Движение вперед/назад и влево/вправо
        Vector3 moveDirection = transform.TransformDirection(new Vector3(
            input.x * targetMovingSpeed * speedMulti,
            0,
            input.z * targetMovingSpeed * speedMulti
        ));

        // Применяем гравитацию
        if (groundCheck.isGrounded && velocity.y < 0) {
            velocity.y = -2f; // Небольшая сила прижимающая к земле
        } else {
            float gravityMultiplier = velocity.y < 0 ? fallGravityMultiplier : 1f;
            velocity.y += gravity * gravityMultiplier * Time.deltaTime;
        }

        // Ограничиваем скорость падения
        if (velocity.y < maxFallSpeed) {
            velocity.y = maxFallSpeed;
        }

        // Применяем движение
        characterController.Move((moveDirection + Vector3.up * velocity.y) * Time.deltaTime);
    }

    private void InterpolateMovement() {
        interpolationTime += Time.deltaTime;
        float lerpFactor = Mathf.Clamp01(interpolationTime / Time.deltaTime);

        // Плавная интерполяция позиции
        transform.position = Vector3.Lerp(previousPosition, targetPosition, lerpFactor);
    }

    [ServerRpc]
    private void SubmitInputServerRpc(Vector3 input, bool running) {
        moveInput.Value = input;
        isRunning.Value = running;
    }

    [ServerRpc]
    private void UpdatePositionServerRpc(Vector3 position) {
        networkPosition.Value = position;
    }

    private void TryJump() {
        if (jumpCooldownTimer <= 0 && groundCheck.isGrounded) {
            jumpCooldownTimer = jumpCooldown;
            isJumping.Value = true;
            StartCoroutine(PerformJump());
            SubmitJumpServerRpc();
        }
    }

    [ServerRpc]
    private void SubmitJumpServerRpc() {
        isJumping.Value = true;
        StartCoroutine(PerformJump());
    }

    private IEnumerator PerformJump() {
        Jumped?.Invoke();
        yield return new WaitForSeconds(jumpDelay);

        velocity.y = Mathf.Sqrt(jumpStrength * -2f * gravity);
        isJumping.Value = false;
    }

    private void UpdateVisualEffects() {
        if (!IsOwner) {
            // Здесь можно обновлять анимации и визуальные эффекты
            // based on moveInput.Value, isRunning.Value, isJumping.Value
        }
    }
}