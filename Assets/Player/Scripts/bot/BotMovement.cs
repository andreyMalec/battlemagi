using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerPhysics))]
[RequireComponent(typeof(NavMeshAgent))]
public class BotMovement : MonoBehaviour {
    [SerializeField] private MovementSettings movementSettings;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float defaultStoppingDistance = 1.5f;
    [SerializeField] private float jumpCooldown = 0.75f;
    [SerializeField] private new Transform camera;

    public GroundCheck groundCheck;

    public float jumpStrength;
    public float movementSpeed;
    public float maxStamina;
    public event System.Action Jumped;

    public bool HasPath => _hasDestination && _agent.hasPath;
    public bool ReachedDestination => _hasDestination && !_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance;
    public Vector3 LocalVelocityNormalized { get; private set; }
    public Vector3 CurrentDestination => _destination;

    private PlayerPhysics _physics;
    private NavMeshAgent _agent;
    private Stats _stats;
    private bool _hasDestination;
    private Vector3 _destination;
    private Vector3 _desiredVelocity;
    private float _jumpCooldownTimer;
    private bool _hasLookDirectionOverride;
    private Vector3 _lookDirectionOverride;

    private void Awake() {
        _physics = GetComponent<PlayerPhysics>();
        _agent = GetComponent<NavMeshAgent>();
        _stats = GetComponent<Stats>();

        _physics.Configure(movementSettings, groundCheck);

        _agent.updatePosition = false;
        _agent.updateRotation = false;
        _agent.autoBraking = true;
        _agent.stoppingDistance = defaultStoppingDistance;
    }

    private void Update() {
        if (_jumpCooldownTimer > 0f)
            _jumpCooldownTimer -= Time.deltaTime;

        _agent.nextPosition = transform.position;

        if (_hasDestination) {
            if (!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance)
                Stop();

            if (_hasDestination) {
                var desired = _agent.desiredVelocity;
                desired.y = 0f;

                var speed = movementSpeed * (_stats?.GetFinal(StatType.MoveSpeed) ?? 1f);
                _desiredVelocity = desired.sqrMagnitude > 0.0001f ? desired.normalized * speed : Vector3.zero;
                var localVelocity = transform.InverseTransformDirection(_desiredVelocity);
                localVelocity.y = 0f;
                var localMagnitude = Mathf.Max(Mathf.Abs(localVelocity.x), Mathf.Abs(localVelocity.z));
                if (localMagnitude > 1f)
                    localVelocity /= localMagnitude;
                LocalVelocityNormalized = localVelocity;
            }
        }

        if (!_hasDestination) {
            _desiredVelocity = Vector3.zero;
            LocalVelocityNormalized = Vector3.zero;
        }

        var lookDirection = _hasLookDirectionOverride ? _lookDirectionOverride : _desiredVelocity;
        var bodyLookDirection = lookDirection;
        bodyLookDirection.y = 0f;
        if (bodyLookDirection.sqrMagnitude > 0.0001f) {
            var targetRotation = Quaternion.LookRotation(bodyLookDirection, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        if (camera != null && lookDirection.sqrMagnitude > 0.0001f) {
            var targetRotation = Quaternion.LookRotation(lookDirection, Vector3.up);
            camera.rotation = Quaternion.Slerp(camera.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private void FixedUpdate() {
        _physics.MoveWithGravity(_desiredVelocity);
    }

    public void SetDestination(Vector3 position, float stoppingDistance = -1f) {
        _destination = position;
        _hasDestination = true;
        _agent.stoppingDistance = stoppingDistance > 0f ? stoppingDistance : defaultStoppingDistance;
        _agent.SetDestination(position);
    }

    public void Repath() {
        if (!_hasDestination)
            return;
        _agent.ResetPath();
        _agent.SetDestination(_destination);
    }

    public void SetLookDirection(Vector3 worldDirection) {
        if (worldDirection.sqrMagnitude < 0.0001f)
            return;

        _hasLookDirectionOverride = true;
        _lookDirectionOverride = worldDirection;
    }

    public void ClearLookDirection() {
        _hasLookDirectionOverride = false;
        _lookDirectionOverride = Vector3.zero;
    }

    public void Stop() {
        _hasDestination = false;
        _desiredVelocity = Vector3.zero;
        LocalVelocityNormalized = Vector3.zero;
        _agent.ResetPath();
    }

    public bool TryMicroEscape(float radius) {
        var randomOffset = Random.insideUnitSphere * radius;
        randomOffset.y = 0f;
        var escapePoint = transform.position + randomOffset;
        if (!NavMesh.SamplePosition(escapePoint, out var hit, radius, NavMesh.AllAreas))
            return false;

        SetDestination(hit.position, 0.5f);
        return true;
    }

    public bool CanJumpNow() {
        return _jumpCooldownTimer <= 0f && groundCheck.isGrounded;
    }

    public bool TryJump(float jumpMultiplier = 1f, float forwardBoost = 0f) {
        if (!CanJumpNow())
            return false;

        _jumpCooldownTimer = jumpCooldown;
        var jumpPower = jumpStrength * jumpMultiplier;
        _physics.Jump(jumpPower);
        if (forwardBoost > 0f && _desiredVelocity.sqrMagnitude > 0.0001f)
            _physics.ApplyImpulse(_desiredVelocity.normalized * forwardBoost);
        Jumped?.Invoke();
        return true;
    }

    public bool TryGetNextPathCorner(out Vector3 corner) {
        corner = default;
        if (!_hasDestination || _agent.pathPending)
            return false;

        var corners = _agent.path.corners;
        if (corners == null || corners.Length < 2)
            return false;

        corner = corners[1];
        return true;
    }
}
