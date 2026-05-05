using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(BotMovement))]
public class BotMovementController : MonoBehaviour {
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private float waypointReachDistance = 1.5f;
    [SerializeField] private float repathInterval = 1.25f;
    [SerializeField] private float stuckCheckInterval = 1.2f;
    [SerializeField] private float stuckMinProgress = 0.4f;
    [SerializeField] private float randomPointRadius = 12f;
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

    private BotMovement _movement;
    private float _repathTimer;
    private float _stuckTimer;
    private int _patrolIndex = -1;
    private int _recoveryAttempts;
    private Vector3 _lastStuckCheckPosition;

    private void Awake() {
        _movement = GetComponent<BotMovement>();
    }

    private void Start() {
        _lastStuckCheckPosition = transform.position;
        SetNextDestination();
    }

    private void Update() {
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
        if (TryGetPatrolPoint(out var destination) || TryGetRandomNavPoint(out destination)) {
            _movement.SetDestination(destination, waypointReachDistance);
            _repathTimer = 0f;
        }
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
        var randomOffset = Random.insideUnitSphere * randomPointRadius;
        randomOffset.y = 0f;
        var target = transform.position + randomOffset;
        if (NavMesh.SamplePosition(target, out var hit, randomPointRadius, NavMesh.AllAreas)) {
            destination = hit.position;
            return true;
        }

        destination = transform.position;
        return false;
    }

    private void TryHandleJumpAssist() {
        if (!_movement.HasPath || !_movement.CanJumpNow())
            return;

        var localVelocity = _movement.LocalVelocityNormalized;
        var moveIntent = Mathf.Abs(localVelocity.x) + Mathf.Abs(localVelocity.z);
        if (moveIntent < minMoveIntent)
            return;

        if (ShouldJumpUp() && _movement.TryJump(1f, jumpForwardBoost))
            return;

        if (ShouldDropDown())
            _movement.TryJump(0.55f, jumpForwardBoost);
    }

    private bool ShouldJumpUp() {
        if (!_movement.TryGetNextPathCorner(out var nextCorner))
            return false;

        var deltaToCorner = nextCorner.y - transform.position.y;
        if (deltaToCorner < minJumpUpHeight || deltaToCorner > maxJumpableHeight)
            return false;

        var origin = transform.position + Vector3.up * obstacleProbeHeight;
        var hitCount = Physics.RaycastNonAlloc(new Ray(origin, transform.forward), _rayHits, obstacleCheckDistance,
            jumpProbeMask, QueryTriggerInteraction.Ignore);
        for (var i = 0; i < hitCount; i++) {
            var hit = _rayHits[i];
            if (hit.collider == null)
                continue;
            if (hit.transform.root == transform)
                continue;

            var topProbe = hit.point + transform.forward * 0.05f + Vector3.up * maxJumpableHeight;
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

        var ahead = transform.position + transform.forward * dropCheckDistance;
        var downOrigin = ahead + Vector3.up * 0.5f;
        var hasGroundAhead = Physics.Raycast(downOrigin, Vector3.down, downRayDistance, jumpProbeMask,
            QueryTriggerInteraction.Ignore);
        return !hasGroundAhead;
    }

    private readonly RaycastHit[] _rayHits = new RaycastHit[6];
}