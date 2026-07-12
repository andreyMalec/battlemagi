using System;
using UnityEngine;
using Unity.Netcode;
using System.Collections;
using Unity.Netcode.Components;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(NetworkTransform))]
[RequireComponent(typeof(PlayerPhysics))]
[RequireComponent(typeof(IceSlideMovementModule))]
[RequireComponent(typeof(FirstPersonLook))]
public class FirstPersonMovement : NetworkBehaviour {
    public MovementSettings movementSettings;
    public GroundCheck groundCheck;

    public float jumpStrength;
    public float movementSpeed;
    public float runSpeed;

    public bool IsRunning { get; private set; }
    public event System.Action Jumped;
    public event System.Action Climbing;

    // Сетевые переменные
    private readonly NetworkVariable<bool> _isRunningNetwork = new();
    private readonly NetworkVariable<bool> _isJumpingNetwork = new();
    public readonly NetworkVariable<Vector3> spawnPoint = new();
    private int _spawnTick;

    private Stats _stats;
    private PlayerPhysics _physics;
    private IceSlideMovementModule _iceSlide;
    private CharacterController _controller;
    private FirstPersonLook _look;
    private float _jumpCooldownTimer;

    // Ключи/локи для бега
    private bool _lastSentRunKeyHeld; // клиентская оптимизация: шлём RPC только при изменении
    private bool _runKeyHeldServer; // серверный флаг (который ставит SetRunKeyHeldServerRpc)
    private bool _runLock; // серверный лок: запрещает авто-включение пока не отпустили кнопку

    private Vector3 _teleportPosition;
    private Quaternion _teleportRotation;
    private bool _teleporting;

    private bool _isClimbing;
    private Vector3 _climbStartPosition;
    private Vector3 _climbTargetPosition;
    private float _climbElapsed;
    private float _climbDuration;

    private void Awake() {
        _stats = GetComponent<Stats>();
        _physics = GetComponent<PlayerPhysics>();
        _iceSlide = GetComponent<IceSlideMovementModule>();
        _controller = GetComponent<CharacterController>();
        _look = GetComponent<FirstPersonLook>();
        _physics.Configure(movementSettings, groundCheck);
    }

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();

        _isRunningNetwork.OnValueChanged += OnIsRunningChanged;
        _isJumpingNetwork.OnValueChanged += OnIsJumpingChanged;

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

        if (_isClimbing)
            StopClimb();
    }

    private void OnIsRunningChanged(bool _, bool newValue) => IsRunning = newValue;

    private void OnIsJumpingChanged(bool oldValue, bool newValue) {
        if (newValue && !oldValue && !IsOwner)
            Jumped?.Invoke();
    }

    public void Teleport(Transform target) {
        TeleportClientRpc(target.position, target.rotation, new ClientRpcParams() {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { OwnerClientId } }
        });
    }

    [ClientRpc]
    private void TeleportClientRpc(Vector3 position, Quaternion rotation, ClientRpcParams clientRpcParams = default) {
        _teleportPosition = position;
        _teleportRotation = rotation;
        _teleporting = true;
    }

    private void FixedUpdate() {
        if (IsOwner && _teleporting) {
            _teleporting = false;
            transform.position = _teleportPosition;
            transform.rotation = _teleportRotation;
        }
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

        if (_isClimbing) {
            TickClimb();
            return;
        }

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

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void SetRunKeyHeldServerRpc(bool held) {
        _runKeyHeldServer = held;
        if (!held) {
            _runLock = false;
        }

        UpdateRunningStateServer();
    }

    private void UpdateRunningStateServer() {
        bool shouldRun = _runKeyHeldServer && !_runLock;
        if (shouldRun != _isRunningNetwork.Value) {
            _isRunningNetwork.Value = shouldRun;
        }
    }

    private void ApplyMovement(Vector2 input, bool running) {
        var moveDirection = ResolveMoveDirection(input, running);
        if (_iceSlide.IsActive)
            moveDirection = _iceSlide.ResolveVelocity(moveDirection, input.sqrMagnitude > 0.0001f, Time.deltaTime);
        _physics.MoveWithGravity(moveDirection);
    }

    private Vector3 ResolveMoveDirection(Vector2 input, bool running) {
        float targetSpeed = running ? runSpeed : movementSpeed;
        float speedMultiplier = groundCheck.isGrounded ? 1f : movementSettings.flySpeedMultiplier;

        speedMultiplier *= _stats?.GetFinal(StatType.MoveSpeed) ?? 1f;
        return transform.TransformDirection(new Vector3(
            input.x * targetSpeed * speedMultiplier,
            0f,
            input.y * targetSpeed * speedMultiplier
        ));
    }

    private void TryJump() {
        if (!Input.GetKeyDown(movementSettings.jumpKey))
            return;

        if (TryStartClimb())
            return;

        if (CanJump())
            PerformJump();
    }

    private bool CanJump() => _jumpCooldownTimer <= 0 && groundCheck.isGrounded && !_iceSlide.IsActive;

    private void PerformJump() {
        _jumpCooldownTimer = movementSettings.jumpCooldown;
        JumpServerRpc(true);
        ApplyJumpForce();
    }

    private bool TryStartClimb() {
        if (_isClimbing || !movementSettings.canClimb || _iceSlide.IsActive)
            return false;

        if (!TryResolveClimbTarget(out var climbTarget, out var surfaceNormal))
            return false;

        BeginClimb(climbTarget, surfaceNormal);
        return true;
    }

    private bool TryResolveClimbTarget(out Vector3 targetPosition, out Vector3 surfaceNormal) {
        targetPosition = default;
        surfaceNormal = Vector3.forward;

        Vector3 up = Vector3.up;
        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, up).normalized;
        if (forward.sqrMagnitude <= 0.0001f)
            return false;

        float probeHeight = Mathf.Max(0.2f, _controller.radius + 0.05f);
        Vector3 lowerProbeOrigin = transform.position + up * probeHeight;

        if (!Physics.Raycast(
                lowerProbeOrigin,
                forward,
                out var wallHit,
                movementSettings.climbMaxDistance,
                movementSettings.climbCollisionMask,
                QueryTriggerInteraction.Ignore
            ))
            return false;

        if (wallHit.transform.IsChildOf(transform))
            return false;

        if (Vector3.Dot(wallHit.normal, up) > 0.3f)
            return false;

        Vector3 upperProbeOrigin = lowerProbeOrigin + up * movementSettings.climbMaxHeight;
        if (Physics.Raycast(
                upperProbeOrigin,
                forward,
                out var upperHit,
                movementSettings.climbMaxDistance,
                movementSettings.climbCollisionMask,
                QueryTriggerInteraction.Ignore
            ) && !upperHit.transform.IsChildOf(transform))
            return false;

        float offsetMin = Mathf.Min(movementSettings.climbSurfaceForwardOffsetMin,
            movementSettings.climbSurfaceForwardOffsetMax);
        float offsetMax = Mathf.Max(movementSettings.climbSurfaceForwardOffsetMin,
            movementSettings.climbSurfaceForwardOffsetMax);
        float offsetStep = Mathf.Max(0.01f, movementSettings.climbSurfaceForwardOffsetStep);

        bool hasCandidate = false;
        float minHeight = float.MaxValue;
        Vector3 bestTarget = default;

        for (float offset = offsetMin; offset <= offsetMax + 0.0001f; offset += offsetStep) {
            Vector3 topProbeOrigin = transform.position
                                     + up * (movementSettings.climbMaxHeight + _controller.stepOffset + 0.1f)
                                     + forward * (wallHit.distance + _controller.radius + offset);

            if (!Physics.Raycast(
                    topProbeOrigin,
                    Vector3.down,
                    out var topHit,
                    movementSettings.climbMaxHeight + 1f,
                    movementSettings.climbCollisionMask,
                    QueryTriggerInteraction.Ignore
                ))
                continue;

            float climbHeight = topHit.point.y - transform.position.y;
            if (climbHeight <= 0.05f || climbHeight > movementSettings.climbMaxHeight)
                continue;

            if (Vector3.Dot(topHit.normal, up) < 0.65f)
                continue;

            Vector3 candidateTarget = topHit.point
                                      + forward * offset
                                      + up * movementSettings.climbSurfaceUpOffset;

            if (!HasClimbClearance(candidateTarget))
                continue;

            if (topHit.point.y < minHeight) {
                minHeight = topHit.point.y;
                bestTarget = candidateTarget;
                hasCandidate = true;
            }
        }

        if (!hasCandidate)
            return false;

        targetPosition = bestTarget;
        surfaceNormal = wallHit.normal;
        return true;
    }

    private bool HasClimbClearance(Vector3 targetPosition) {
        Vector3 up = Vector3.up;
        Vector3 center = targetPosition + _controller.center;
        float half = Mathf.Max(_controller.height * 0.5f - _controller.radius, 0f) +
                     movementSettings.climbCeilingCheckExtra;
        Vector3 bottom = center - up * half;
        Vector3 top = center + up * half;
        float radius = Mathf.Max(0.01f, _controller.radius * 0.95f);

        var overlaps = Physics.OverlapCapsule(
            bottom,
            top,
            radius,
            movementSettings.climbCollisionMask,
            QueryTriggerInteraction.Ignore
        );

        for (int i = 0; i < overlaps.Length; i++) {
            if (!overlaps[i].transform.IsChildOf(transform))
                return false;
        }

        return true;
    }

    private void BeginClimb(Vector3 targetPosition, Vector3 surfaceNormal) {
        _isClimbing = true;
        _climbStartPosition = transform.position;
        _climbTargetPosition = targetPosition;
        _climbElapsed = 0f;
        _climbDuration = Mathf.Max(0.01f, movementSettings.climbDuration);
        Climbing?.Invoke();
        _physics.ClearJump();

        if (_lastSentRunKeyHeld) {
            SetRunKeyHeldServerRpc(false);
            _lastSentRunKeyHeld = false;
        }
    }

    private void TickClimb() {
        _climbElapsed += Time.deltaTime;
        float t = Mathf.Clamp01(_climbElapsed / _climbDuration);
        float smoothT = t * t * (3f - 2f * t);
        transform.position = Vector3.Lerp(_climbStartPosition, _climbTargetPosition, smoothT);

        if (t >= 1f)
            StopClimb();
    }

    private void StopClimb() {
        _isClimbing = false;
    }

    [ServerRpc]
    private void JumpServerRpc(bool jumping) => _isJumpingNetwork.Value = jumping;

    private void ApplyJumpForce() {
        Jumped?.Invoke();
        _physics.Jump(jumpStrength);
        JumpServerRpc(false);
    }

    [ClientRpc]
    public void ApplyImpulseClientRpc(Vector3 impulse, ClientRpcParams clientRpcParams = default) {
        _physics.ApplyImpulse(impulse);
    }

    [ClientRpc]
    public void SetPointForceClientRpc(
        int id,
        Vector3 point,
        float forcePerSecond,
        float duration,
        SpellKnockbackVectorMode vectorMode,
        float upBias,
        ClientRpcParams clientRpcParams = default
    ) {
        _physics.SetPointForce(id, point, forcePerSecond, duration, vectorMode, upBias);
    }

    [ClientRpc]
    public void SetVelocitySourceClientRpc(
        int id, Vector3 velocity, float duration,
        ClientRpcParams clientRpcParams = default
    ) {
        _physics.SetVelocitySource(id, velocity, duration);
    }

    [ClientRpc]
    public void ClearVelocitySourceClientRpc(int id, ClientRpcParams clientRpcParams = default) {
        _physics.ClearVelocitySource(id);
    }
}