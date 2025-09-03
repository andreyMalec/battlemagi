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
    public float fallGravityMultiplier = 2.5f;
    public float maxFallSpeed = -25f;
    public float flySpeedMulti = 0.5f;
    public KeyCode runningKey = KeyCode.LeftShift;
    public KeyCode jumpKey = KeyCode.Space;
    public float jumpStrength = 2;
    public event System.Action Jumped;
    public GroundCheck groundCheck;
    public Rigidbody rb;

    // Сетевые переменные
    private NetworkVariable<Vector3> moveInput = new NetworkVariable<Vector3>();
    private NetworkVariable<bool> isRunning = new NetworkVariable<bool>();
    private NetworkVariable<bool> isJumping = new NetworkVariable<bool>();
    private NetworkVariable<Vector3> networkVelocity = new NetworkVariable<Vector3>();
    private NetworkVariable<Vector3> networkPosition = new NetworkVariable<Vector3>();

    private float jumpCooldownTimer = 0f;
    private Vector3 targetPosition;
    private Vector3 previousPosition;
    private float interpolationTime;

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();

        // Подписываемся на изменения сетевых переменных
        moveInput.OnValueChanged += OnMoveInputChanged;
        isRunning.OnValueChanged += OnIsRunningChanged;
        isJumping.OnValueChanged += OnIsJumpingChanged;
        networkVelocity.OnValueChanged += OnVelocityChanged;
        networkPosition.OnValueChanged += OnPositionChanged;

        // Для чужих объектов делаем kinematic
        if (!IsOwner) {
            rb.isKinematic = true;
            targetPosition = transform.position;
            previousPosition = transform.position;
        } else {
            rb.isKinematic = false;
        }
    }

    public override void OnNetworkDespawn() {
        base.OnNetworkDespawn();

        // Отписываемся от событий
        moveInput.OnValueChanged -= OnMoveInputChanged;
        isRunning.OnValueChanged -= OnIsRunningChanged;
        isJumping.OnValueChanged -= OnIsJumpingChanged;
        networkVelocity.OnValueChanged -= OnVelocityChanged;
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

    private void OnVelocityChanged(Vector3 oldValue, Vector3 newValue) {
        if (!IsOwner) {
            // Для интерполяции движения
        }
    }

    private void OnPositionChanged(Vector3 oldValue, Vector3 newValue) {
        if (!IsOwner) {
            previousPosition = transform.position;
            targetPosition = newValue;
            interpolationTime = 0f;
        }
    }

    void Update() {
        if (!IsOwner) return;

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

        // Запрос прыжка
        if (Input.GetKeyDown(jumpKey) && jumpCooldownTimer <= 0) {
            TryJumpServerRpc();
        }
    }

    void FixedUpdate() {
        if (IsServer) {
            // Сервер рассчитывает движение
            CalculateMovement();
        } else if (!IsOwner) {
            // Клиенты интерполируют позицию
            InterpolateMovement();
        }
    }

    [ServerRpc]
    private void SubmitInputServerRpc(Vector3 input, bool running) {
        moveInput.Value = input;
        isRunning.Value = running;
    }

    [ServerRpc]
    private void TryJumpServerRpc() {
        if (jumpCooldownTimer <= 0 && groundCheck.isGrounded) {
            jumpCooldownTimer = jumpCooldown;
            isJumping.Value = true;
            StartCoroutine(PerformJump());
        }
    }

    private void CalculateMovement() {
        float targetMovingSpeed = isRunning.Value ? runSpeed : speed;
        var speedMulti = groundCheck.isGrounded ? 1f : flySpeedMulti;

        Vector3 velocity = transform.rotation * new Vector3(
            moveInput.Value.x * targetMovingSpeed * speedMulti,
            rb.linearVelocity.y,
            moveInput.Value.z * targetMovingSpeed * speedMulti
        );

        // Применяем гравитацию
        if (rb.linearVelocity.y < 0) {
            velocity += Vector3.up * (Physics.gravity.y * (fallGravityMultiplier - 1) * Time.fixedDeltaTime);
        }

        // Ограничиваем скорость падения
        if (velocity.y < maxFallSpeed) {
            velocity.y = maxFallSpeed;
        }

        rb.linearVelocity = velocity;

        // Синхронизируем положение и скорость
        networkPosition.Value = transform.position;
        networkVelocity.Value = velocity;
    }

    private void InterpolateMovement() {
        if (!IsOwner) {
            interpolationTime += Time.fixedDeltaTime;
            float lerpFactor = Mathf.Clamp01(interpolationTime / Time.fixedDeltaTime);

            // Плавная интерполяция позиции
            transform.position = Vector3.Lerp(previousPosition, targetPosition, lerpFactor);

            // Можно также интерполировать rotation если нужно
        }
    }

    private IEnumerator PerformJump() {
        Jumped?.Invoke();
        yield return new WaitForSeconds(jumpDelay);

        Vector3 jumpForce = Vector3.up * (100 * jumpStrength);
        rb.AddForce(jumpForce);
        isJumping.Value = false;
    }

    private void UpdateVisualEffects() {
        if (!IsOwner) {
            // Здесь можно обновлять анимации и визуальные эффекты
            // based on moveInput.Value, isRunning.Value, isJumping.Value

            // Пример: управление аниматором
            // animator.SetBool("IsRunning", isRunning.Value);
            // animator.SetBool("IsJumping", isJumping.Value);
            // animator.SetFloat("MoveSpeed", moveInput.Value.magnitude);
        }
    }
}