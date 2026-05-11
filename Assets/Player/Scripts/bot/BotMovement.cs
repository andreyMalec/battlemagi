using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerPhysics))]
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(ParticipantIdentity))]
public class BotMovement : MonoBehaviour {
    [SerializeField] private MovementSettings movementSettings;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float defaultStoppingDistance = 1.5f;
    [SerializeField] private float jumpCooldown = 0.75f;
    [SerializeField] private new Transform camera;
    [SerializeField] private bool avoidDynamicHazards = true;
    [SerializeField] private float hazardClearance = 0.8f;
    [SerializeField] private float hazardDetourPadding = 1.4f;
    [SerializeField] private int hazardDetourSamples = 8;
    [SerializeField] private float hazardSampleRadius = 2.5f;

    public GroundCheck groundCheck;

    public float jumpStrength;
    public float movementSpeed;
    public float maxStamina;
    public event System.Action Jumped;

    public bool HasPath => _hasDestination && _agent.hasPath;

    public bool ReachedDestination =>
        _hasDestination && !_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance;

    public Vector3 LocalVelocityNormalized { get; private set; }
    public Vector3 CurrentDestination => _destination;

    private PlayerPhysics _physics;
    private NavMeshAgent _agent;
    private Stats _stats;
    private ParticipantIdentity _selfIdentity;
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
        _selfIdentity = GetComponent<ParticipantIdentity>();

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
        var resolvedDestination = avoidDynamicHazards ? ResolveSafeDestination(position) : position;
        _destination = resolvedDestination;
        _hasDestination = true;
        _agent.stoppingDistance = stoppingDistance > 0f ? stoppingDistance : defaultStoppingDistance;
        _agent.SetDestination(resolvedDestination);
    }

    public void Repath() {
        if (!_hasDestination)
            return;
        _agent.ResetPath();
        var repathDestination = avoidDynamicHazards ? ResolveSafeDestination(_destination) : _destination;
        _destination = repathDestination;
        _agent.SetDestination(repathDestination);
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

    private Vector3 ResolveSafeDestination(Vector3 requestedDestination) {
        if (TryFindContainingHazard(transform.position, out var insideCenter, out var insideRadius)) {
            var escapeDirection = transform.position - insideCenter;
            escapeDirection.y = 0f;
            if (escapeDirection.sqrMagnitude <= 0.0001f)
                escapeDirection = (requestedDestination - transform.position).normalized;
            if (escapeDirection.sqrMagnitude <= 0.0001f)
                escapeDirection = transform.forward;

            var escapePoint = insideCenter + escapeDirection.normalized * (insideRadius + hazardClearance + hazardDetourPadding);
            if (TrySampleSafePoint(escapePoint, out var sampledEscapePoint))
                return sampledEscapePoint;
        }

        if (!TryFindNearestHazardOnPath(transform.position, requestedDestination, out var hazardCenter,
                out var hazardRadius))
            return requestedDestination;

        var baseDirection = hazardCenter - transform.position;
        baseDirection.y = 0f;
        if (baseDirection.sqrMagnitude <= 0.0001f) {
            baseDirection = requestedDestination - transform.position;
            baseDirection.y = 0f;
        }

        if (baseDirection.sqrMagnitude <= 0.0001f)
            return requestedDestination;

        var detourDistance = hazardRadius + hazardDetourPadding;
        var sampleCount = Mathf.Max(2, hazardDetourSamples);
        var bestScore = float.MaxValue;
        var hasBest = false;
        var bestPoint = requestedDestination;

        for (var i = 0; i < sampleCount; i++) {
            var ringIndex = i / 2 + 1;
            var sign = i % 2 == 0 ? 1f : -1f;
            var angle = sign * (180f / (sampleCount + 1)) * ringIndex;
            var sampleDirection = Quaternion.Euler(0f, angle, 0f) * baseDirection.normalized;
            var samplePoint = hazardCenter + sampleDirection * detourDistance;
            if (!TrySampleSafePoint(samplePoint, out var safePoint))
                continue;

            var score = Vector3.Distance(transform.position, safePoint) +
                        Vector3.Distance(safePoint, requestedDestination);
            if (score >= bestScore)
                continue;

            bestScore = score;
            bestPoint = safePoint;
            hasBest = true;
        }

        if (hasBest)
            return bestPoint;

        return requestedDestination;
    }

    private bool TrySampleSafePoint(Vector3 worldPoint, out Vector3 safePoint) {
        safePoint = default;
        if (!NavMesh.SamplePosition(worldPoint, out var hit, hazardSampleRadius, NavMesh.AllAreas))
            return false;

        if (IsPointInsideHazard(hit.position))
            return false;

        safePoint = hit.position;
        return true;
    }

    private bool IsPointInsideHazard(Vector3 point) {
        return TryFindContainingHazard(point, out _, out _);
    }

    private bool TryFindContainingHazard(Vector3 point, out Vector3 center, out float radius) {
        center = default;
        radius = 0f;

        for (var i = 0; i < SpellInstance.Active.Count; i++) {
            var spell = SpellInstance.Active[i];
            if (!TryGetSpellHazard(spell, out var hazardCenter, out var hazardRadius))
                continue;

            if (!IsInsidePlanarRadius(point, hazardCenter, hazardRadius + hazardClearance))
                continue;

            center = hazardCenter;
            radius = hazardRadius;
            return true;
        }

        return false;
    }

    private bool TryFindNearestHazardOnPath(Vector3 from, Vector3 to, out Vector3 center, out float radius) {
        center = default;
        radius = 0f;
        var found = false;
        var bestT = float.MaxValue;

        for (var i = 0; i < SpellInstance.Active.Count; i++) {
            var spell = SpellInstance.Active[i];
            if (!TryGetSpellHazard(spell, out var hazardCenter, out var hazardRadius))
                continue;

            if (!TryEvaluateHazardOnSegment(from, to, hazardCenter, hazardRadius, out var t))
                continue;
            if (t >= bestT)
                continue;

            bestT = t;
            center = hazardCenter;
            radius = hazardRadius;
            found = true;
        }

        return found;
    }

    private bool TryEvaluateHazardOnSegment(Vector3 from, Vector3 to, Vector3 center, float radius, out float t) {
        t = 0f;
        var from2 = new Vector2(from.x, from.z);
        var to2 = new Vector2(to.x, to.z);
        var center2 = new Vector2(center.x, center.z);
        var segment = to2 - from2;
        var lengthSqr = segment.sqrMagnitude;
        if (lengthSqr <= 0.0001f)
            return false;

        t = Mathf.Clamp01(Vector2.Dot(center2 - from2, segment) / lengthSqr);
        var closest = from2 + segment * t;
        var threshold = radius + hazardClearance;
        return (closest - center2).sqrMagnitude <= threshold * threshold;
    }

    private bool TryGetSpellHazard(SpellInstance spell, out Vector3 center, out float radius) {
        center = default;
        radius = 0f;
        if (spell == null || spell.Bind == null)
            return false;

        var context = spell.Bind.Context;
        if (context == null || context.Spell == null)
            return false;
        if (context.Spell.coreType != CoreType.Zone && context.Spell.coreType != CoreType.Summon)
            return false;
        // if (!IsHazardEnemy(context.OwnerId, spell.gameObject))
        //     return false;

        center = spell.transform.position;
        radius = GetZoneHazardRadius(context.Spell);
        return radius > 0f;
    }

    private bool IsHazardEnemy(ulong hazardOwnerId, GameObject hazardObject) {
        var hazardParticipant =
            DamageRelationship.TryGetTargetParticipant(hazardObject, out var participant)
                ? participant
                : ParticipantIdentityCodec.Decode(hazardOwnerId);

        if (TeamManager.Instance == null)
            return hazardParticipant != _selfIdentity.Id;

        return TeamManager.Instance.AreEnemies(_selfIdentity.Id, hazardParticipant);
    }

    private static float GetZoneHazardRadius(SpellDefinition spell) {
        var radius = Mathf.Max(0.75f, spell.scale);
        if (spell.zone != null && spell.zone.shapeType == ZoneShapeType.Plate)
            radius *= 1.2f;

        return radius;
    }

    private static bool IsInsidePlanarRadius(Vector3 point, Vector3 center, float radius) {
        var delta = point - center;
        delta.y = 0f;
        return delta.sqrMagnitude <= radius * radius;
    }
}