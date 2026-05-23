using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(BotMovement))]
[RequireComponent(typeof(SpellCasterPlayer))]
[RequireComponent(typeof(Damageable))]
public class BotMovementController : MonoBehaviour {
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private float waypointReachDistance = 1.5f;
    [SerializeField] private float repathInterval = 1.25f;
    [SerializeField] private float stuckCheckInterval = 1.2f;
    [SerializeField] private float stuckMinProgress = 0.4f;
    [SerializeField] private float randomPointRadius = 12f;
    [SerializeField] private float wanderTurnAngle = 28f;
    [SerializeField] private int wanderSampleAttempts = 5;
    [SerializeField] private float microEscapeRadius = 3.5f;
    [SerializeField] private LayerMask jumpProbeMask = ~0;
    [SerializeField] private float obstacleCheckDistance = 1.35f;
    [SerializeField] private float obstacleProbeHeight = 0.45f;
    [SerializeField] private float maxJumpableHeight = 1.25f;
    [SerializeField] private float maxDropHeight = 2.25f;
    [SerializeField] private float dropCheckDistance = 1.1f;
    [SerializeField] private float downRayDistance = 3.5f;
    [SerializeField] private float jumpForwardBoost = 1.25f;
    [SerializeField] private float minMoveIntent = 0.15f;
    [SerializeField] private float minJumpUpHeight = 0.2f;
    [SerializeField] private float minDropHeight = 0.6f;
    [SerializeField] private float pickupSearchRadius = 20f;
    [SerializeField] private float pickupDistancePenalty = 0.06f;
    [SerializeField] private float pickupMinScore = 0.2f;
    [SerializeField] [Range(0f, 2f)] private float attackNeedBias = 1f;
    [SerializeField] [Range(0f, 2f)] private float defenseNeedBias = 1f;
    [SerializeField] [Range(0f, 2f)] private float mobilityNeedBias = 0.5f;
    [SerializeField] [Range(0f, 1f)] private float baselineMobilityNeed = 0.2f;

    private BotMovement _movement;
    private SpellCasterPlayer _caster;
    private Damageable _damageable;
    private BotCombatController _combat;
    private float _repathTimer;
    private float _stuckTimer;
    private int _patrolIndex = -1;
    private int _recoveryAttempts;
    private Vector3 _lastStuckCheckPosition;
    private Vector3 _wanderDirection;
    private string _debugState = "Idle";
    private string _debugPickup = "-";

    public string DebugState => _debugState;
    public string DebugPickup => _debugPickup;

    private bool isServer;

    private void Awake() {
        _movement = GetComponent<BotMovement>();
        _caster = GetComponent<SpellCasterPlayer>();
        _damageable = GetComponent<Damageable>();
        _combat = GetComponent<BotCombatController>();
        isServer = NetworkManager.Singleton.IsServer;
    }

    private void Start() {
        if (!isServer) return;
        _lastStuckCheckPosition = transform.position;
        _wanderDirection = GetPlanarDirectionOrFallback(transform.forward);
        SetNextDestination();
    }

    private void Update() {
        if (!isServer) return;
        if (_combat != null && _combat.ShouldHoldCombat) {
            _debugState = "CombatMove";
            TryHandleJumpAssist();
            return;
        }

        _repathTimer += Time.deltaTime;
        _stuckTimer += Time.deltaTime;

        if (!_movement.HasPath && !_movement.ReachedDestination) {
            SetNextDestination();
            return;
        }

        if (_movement.ReachedDestination) {
            _recoveryAttempts = 0;
            SetNextDestination();
            return;
        }

        if (_repathTimer >= repathInterval) {
            _repathTimer = 0f;
            _movement.Repath();
        }

        if (_stuckTimer >= stuckCheckInterval) {
            _stuckTimer = 0f;
            CheckStuck();
        }

        TryHandleJumpAssist();
    }

    private void CheckStuck() {
        var current = transform.position;
        var moved = Vector3.Distance(_lastStuckCheckPosition, current);
        _lastStuckCheckPosition = current;

        if (moved >= stuckMinProgress) {
            _recoveryAttempts = 0;
            return;
        }

        RecoverFromStuck();
    }

    private void RecoverFromStuck() {
        _debugState = "Recovering";
        _recoveryAttempts++;
        if (_recoveryAttempts == 1) {
            _movement.Repath();
            return;
        }

        if (_recoveryAttempts == 2 && _movement.TryMicroEscape(microEscapeRadius))
            return;

        _recoveryAttempts = 0;
        SetNextDestination();
    }

    private void SetNextDestination() {
        if (TryGetBestPickUpPoint(out var destination, out var pickUp)) {
            _debugState = "Pickup";
            _debugPickup = pickUp != null ? pickUp.name : "-";
            _movement.SetDestination(destination, waypointReachDistance);
            _repathTimer = 0f;
            return;
        }

        if (TryGetPatrolPoint(out destination)) {
            _debugState = "Patrol";
            _debugPickup = "-";
            _movement.SetDestination(destination, waypointReachDistance);
            _repathTimer = 0f;
            return;
        }

        if (TryGetRandomNavPoint(out destination)) {
            _debugState = "Wander";
            _debugPickup = "-";
            _movement.SetDestination(destination, waypointReachDistance);
            _repathTimer = 0f;
        }
    }

    private bool TryGetBestPickUpPoint(out Vector3 destination, out PickUp selectedPickUp) {
        destination = default;
        selectedPickUp = null;
        var active = PickUp.Active;
        if (active.Count == 0)
            return false;

        var current = transform.position;
        var maxDistanceSqr = pickupSearchRadius * pickupSearchRadius;

        var mana = _caster.Mana;
        var manaRatio = mana.MaxMana > 0.001f ? mana.Mana / mana.MaxMana : 1f;
        var healthRatio = _damageable.Health.maxHealth > 0.001f
            ? _damageable.CurrentHealth / _damageable.Health.maxHealth
            : 1f;

        var attackNeed = Mathf.Clamp01((1f - manaRatio) * attackNeedBias);
        var defenseNeed = Mathf.Clamp01((1f - healthRatio) * defenseNeedBias);
        var mobilityNeed =
            Mathf.Clamp01(baselineMobilityNeed + (1f - Mathf.Max(attackNeed, defenseNeed)) * mobilityNeedBias);
        var need = $"A{attackNeed:F2} D{defenseNeed:F2} M{mobilityNeed:F2}";
        _debugPickup += need;

        var bestScore = pickupMinScore;
        var found = false;
        for (var i = 0; i < active.Count; i++) {
            var pickUp = active[i];
            if (pickUp == null)
                continue;

            var toPickup = pickUp.transform.position - current;
            var distanceSqr = toPickup.sqrMagnitude;
            if (distanceSqr > maxDistanceSqr)
                continue;

            var weights = pickUp.BotPriorityWeights;
            var needScore =
                weights.x * attackNeed +
                weights.y * defenseNeed +
                weights.z * mobilityNeed;
            var distance = Mathf.Sqrt(distanceSqr);
            var score = needScore - distance * pickupDistancePenalty;
            if (score <= bestScore)
                continue;

            if (!NavMesh.SamplePosition(pickUp.transform.position, out var hit, 1.2f, NavMesh.AllAreas))
                continue;

            bestScore = score;
            destination = hit.position;
            selectedPickUp = pickUp;
            found = true;
        }

        return found;
    }

    private bool TryGetPatrolPoint(out Vector3 destination) {
        if (patrolPoints != null && patrolPoints.Length > 0) {
            _patrolIndex = (_patrolIndex + 1) % patrolPoints.Length;
            destination = patrolPoints[_patrolIndex].position;
            return true;
        }

        destination = default;
        return false;
    }

    private bool TryGetRandomNavPoint(out Vector3 destination) {
        var attempts = Mathf.Max(1, wanderSampleAttempts);
        var baseDirection = GetPlanarDirectionOrFallback(_wanderDirection);

        for (var i = 0; i < attempts; i++) {
            var t = attempts == 1 ? 1f : i / (attempts - 1f);
            var maxTurn = Mathf.Lerp(wanderTurnAngle, 180f, t);
            var signedAngle = Random.Range(-maxTurn, maxTurn);
            var candidateDirection = Quaternion.Euler(0f, signedAngle, 0f) * baseDirection;
            candidateDirection = GetPlanarDirectionOrFallback(candidateDirection);

            var target = transform.position + candidateDirection * randomPointRadius;
            if (!NavMesh.SamplePosition(target, out var hit, randomPointRadius, NavMesh.AllAreas))
                continue;

            _wanderDirection = candidateDirection;
            destination = hit.position;
            return true;
        }

        destination = transform.position;
        return false;
    }

    private static Vector3 GetPlanarDirectionOrFallback(Vector3 source) {
        source.y = 0f;
        if (source.sqrMagnitude > 0.0001f)
            return source.normalized;

        var random = Random.insideUnitSphere;
        random.y = 0f;
        if (random.sqrMagnitude > 0.0001f)
            return random.normalized;

        return Vector3.forward;
    }

    private void TryHandleJumpAssist() {
        if (!_movement.HasPath || !_movement.CanJumpNow())
            return;

        var localVelocity = _movement.LocalVelocityNormalized;
        var moveIntent = Mathf.Abs(localVelocity.x) + Mathf.Abs(localVelocity.z);
        if (moveIntent < minMoveIntent)
            return;

        if (ShouldJumpUp() && _movement.TryJump(1f, jumpForwardBoost)) {
            _debugState = "JumpUp";
            return;
        }

        if (ShouldDropDown() && _movement.TryJump(0.55f, jumpForwardBoost))
            _debugState = "DropDown";
    }

    private bool ShouldJumpUp() {
        if (!_movement.TryGetNextPathCorner(out var nextCorner))
            return false;

        var deltaToCorner = nextCorner.y - transform.position.y;
        if (deltaToCorner < minJumpUpHeight || deltaToCorner > maxJumpableHeight)
            return false;

        var origin = transform.position + Vector3.up * obstacleProbeHeight;
        var forward = GetPlanarDirectionOrFallback(nextCorner - transform.position);
        Debug.DrawLine(origin, origin + forward * obstacleCheckDistance, Color.blue, 0.25f);
        var hitCount = Physics.RaycastNonAlloc(new Ray(origin, forward), _rayHits, obstacleCheckDistance,
            jumpProbeMask, QueryTriggerInteraction.Ignore);
        for (var i = 0; i < hitCount; i++) {
            var hit = _rayHits[i];
            if (hit.collider == null)
                continue;
            if (hit.transform.root == transform)
                continue;

            var topProbe = hit.point + forward * 0.05f + Vector3.up * maxJumpableHeight;
            if (!Physics.Raycast(topProbe, Vector3.down, out var topHit, maxJumpableHeight + 0.2f, jumpProbeMask,
                    QueryTriggerInteraction.Ignore))
                continue;

            var obstacleHeight = topHit.point.y - transform.position.y;
            if (obstacleHeight >= minJumpUpHeight && obstacleHeight <= maxJumpableHeight)
                return true;
        }

        return false;
    }

    private bool ShouldDropDown() {
        if (!_movement.TryGetNextPathCorner(out var nextCorner))
            return false;

        var deltaToCorner = nextCorner.y - transform.position.y;
        if (deltaToCorner > -minDropHeight || Mathf.Abs(deltaToCorner) > maxDropHeight)
            return false;

        var forward = GetPlanarDirectionOrFallback(nextCorner - transform.position);
        var ahead = transform.position + forward * dropCheckDistance;
        var downOrigin = ahead + Vector3.up * 0.5f;
        var hasGroundAhead = Physics.Raycast(downOrigin, Vector3.down, downRayDistance, jumpProbeMask,
            QueryTriggerInteraction.Ignore);
        return !hasGroundAhead;
    }

    private readonly RaycastHit[] _rayHits = new RaycastHit[6];
}