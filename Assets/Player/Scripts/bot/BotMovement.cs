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
    [SerializeField] private bool usePortals = true;
    [SerializeField] private float portalStoppingDistance = 0.1f;
    [SerializeField] private float portalUseMinGain = 2f;
    [SerializeField] private float portalTraversalPenalty = 1f;
    [SerializeField] private float portalSampleRadius = 2.5f;
    [SerializeField] private float portalRepathDelayAfterTeleport = 0.25f;

    public GroundCheck groundCheck;

    public float jumpStrength;
    public float movementSpeed;
    public event System.Action Jumped;

    public bool HasPath => _hasDestination && (_agent.hasPath || _activePortal != null);

    public bool ReachedDestination =>
        _activePortal == null && _hasDestination && !_agent.pathPending &&
        _agent.remainingDistance <= _agent.stoppingDistance;

    public Vector3 LocalVelocityNormalized { get; private set; }
    public Vector3 CurrentDestination => _destination;

    private PlayerPhysics _physics;
    private NavMeshAgent _agent;
    private Stats _stats;
    private ParticipantIdentity _selfIdentity;
    private bool _hasDestination;
    private Vector3 _destination;
    private Vector3 _finalDestination;
    private Vector3 _desiredVelocity;
    private float _jumpCooldownTimer;
    private float _destinationStoppingDistance;
    private float _portalRoutingCooldownTimer;
    private bool _hasLookDirectionOverride;
    private Vector3 _lookDirectionOverride;
    private Portal _activePortal;

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

    private void OnEnable() {
        Portal.Teleported += HandlePortalTeleported;
    }

    private void OnDisable() {
        Portal.Teleported -= HandlePortalTeleported;
    }

    private void Update() {
        if (_jumpCooldownTimer > 0f)
            _jumpCooldownTimer -= Time.deltaTime;
        if (_portalRoutingCooldownTimer > 0f)
            _portalRoutingCooldownTimer -= Time.deltaTime;

        _agent.nextPosition = transform.position;

        if (_hasDestination) {
            if (_activePortal == null && !_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance)
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
        _destinationStoppingDistance = stoppingDistance > 0f ? stoppingDistance : defaultStoppingDistance;
        _finalDestination = avoidDynamicHazards ? ResolveSafeDestination(position) : position;
        ApplyCurrentDestination();
    }

    public void Repath() {
        if (!_hasDestination)
            return;

        _agent.ResetPath();
        _finalDestination = avoidDynamicHazards ? ResolveSafeDestination(_finalDestination) : _finalDestination;
        ApplyCurrentDestination();
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
        _activePortal = null;
        _finalDestination = Vector3.zero;
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

    private void ApplyCurrentDestination() {
        if (usePortals && _portalRoutingCooldownTimer <= 0f &&
            TryGetPortalRoute(_finalDestination, out var portal, out var portalDestination)) {
            _activePortal = portal;
            _destination = portalDestination;
            _hasDestination = true;
            _agent.stoppingDistance = portalStoppingDistance;
            _agent.SetDestination(portalDestination);
            return;
        }

        _activePortal = null;
        _destination = _finalDestination;
        _hasDestination = true;
        _agent.stoppingDistance = _destinationStoppingDistance;
        _agent.SetDestination(_finalDestination);
    }

    private bool TryGetPortalRoute(Vector3 requestedDestination, out Portal portal, out Vector3 portalDestination) {
        portal = null;
        portalDestination = default;

        var hasDirectPath = TryGetPathLength(transform.position, requestedDestination, out var directLength);
        var bestScore = hasDirectPath ? directLength - portalUseMinGain : float.MaxValue;
        var found = false;

        for (var i = 0; i < Portal.Active.Count; i++) {
            var candidate = Portal.Active[i];
            if (candidate == null || candidate.Linked == null)
                continue;

            if (!TrySampleNavMeshPoint(candidate.EntryPosition, out var entryPoint))
                continue;
            if (!candidate.TryGetLinkedExitPosition(out var linkedExitPosition))
                continue;
            if (!TrySampleNavMeshPoint(linkedExitPosition, out var exitPoint))
                continue;
            if (!TryGetPathLength(transform.position, entryPoint, out var toEntryLength))
                continue;
            if (!TryGetPathLength(exitPoint, requestedDestination, out var fromExitLength))
                continue;

            var routeScore = toEntryLength + fromExitLength + portalTraversalPenalty;
            if (routeScore >= bestScore)
                continue;

            bestScore = routeScore;
            portal = candidate;
            portalDestination = entryPoint;
            found = true;
        }

        return found;
    }

    private bool TrySampleNavMeshPoint(Vector3 point, out Vector3 sampledPoint) {
        sampledPoint = default;
        if (!NavMesh.SamplePosition(point, out var hit, portalSampleRadius, NavMesh.AllAreas))
            return false;

        sampledPoint = hit.position;
        return true;
    }

    private bool TryGetPathLength(Vector3 from, Vector3 to, out float length) {
        length = 0f;
        if (!TrySampleNavMeshPoint(from, out var sampledFrom))
            return false;
        if (!TrySampleNavMeshPoint(to, out var sampledTo))
            return false;

        var path = new NavMeshPath();
        if (!NavMesh.CalculatePath(sampledFrom, sampledTo, NavMesh.AllAreas, path))
            return false;
        if (path.status != NavMeshPathStatus.PathComplete)
            return false;

        var corners = path.corners;
        if (corners == null || corners.Length < 2)
            return false;

        for (var i = 1; i < corners.Length; i++)
            length += Vector3.Distance(corners[i - 1], corners[i]);

        return true;
    }

    private void HandlePortalTeleported(Transform target, Portal source, Portal destination) {
        if (target != transform.root)
            return;

        _activePortal = null;
        _portalRoutingCooldownTimer = portalRepathDelayAfterTeleport;
        if (!_hasDestination)
            return;

        _agent.ResetPath();
        ApplyCurrentDestination();
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

        if (Ctx.Teams == null)
            return hazardParticipant != _selfIdentity.Id;

        return Ctx.AreEnemies(_selfIdentity.Id, hazardParticipant);
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