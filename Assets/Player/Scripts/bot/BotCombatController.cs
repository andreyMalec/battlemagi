using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(BotMovement))]
[RequireComponent(typeof(SpellCasterPlayer))]
[RequireComponent(typeof(ParticipantIdentity))]
public class BotCombatController : MonoBehaviour {
    private enum BotCombatState {
        NoTarget,
        SearchTarget,
        PrepareSpell,
        AcquireTarget,
        Shoot,
        TargetLost
    }

    [SerializeField] private float targetSearchRadius = 45f;
    [SerializeField] private float retargetInterval = 0.5f;
    [SerializeField] private float thinkInterval = 0.15f;
    [SerializeField] private float repathInterval = 0.25f;
    [SerializeField] private float castAngleTolerance = 18f;
    [SerializeField] private float strafeRadius = 3f;
    [SerializeField] private float strafeSwitchInterval = 1.1f;
    [SerializeField] [Range(0.1f, 1f)] private float targetBodyHeightFactor = 0.75f;
    [SerializeField] private float ballisticTargetLift = 1f;
    [SerializeField] private float switchSpellDelay = 2f;
    [SerializeField] private float castPrepareDelay = 0.5f;
    [SerializeField] private float targetLostSightTimeout = 2.25f;
    [SerializeField] private float targetVisibleAngle = 85f;
    [SerializeField] private float lostTargetReacquireDelay = 2f;
    [SerializeField] private float echoRecastDelay = 0.3f;
    [SerializeField] [Range(0f, 1f)] private float manaCombatRatio = 0.15f;
    [SerializeField] private LayerMask sightMask = ~0;
    [SerializeField] private BotSpellDecisionWeights decisionWeights = new();
    [SerializeField] private BotVoiceBundle voiceBundle;
    [SerializeField] private AudioSource voiceAudioSource;

    private BotMovement _movement;
    private SpellCasterPlayer _caster;
    private ParticipantIdentity _selfIdentity;
    private Damageable _damageable;
    private BotSpellDecisionEngine _decisionEngine;

    private readonly List<SpellWeights> _spells = new();
    private ITarget _target;
    private SpellDecision _lastSpellDecision;
    private SpellDefinition _lastCastSpell;
    private SpellDefinition _preparedSpell;
    private ITarget _preparedTarget;
    private float _preparedCastTimer;
    private float _spellSwitchTimer;
    private bool _hasPreparedSpell;
    private BotCombatState _state = BotCombatState.NoTarget;
    private SpellDecision _currentDecision;
    private Vector3 _currentTargetPosition;
    private Vector3 _currentToTarget;
    private float _currentPlanarDistance;
    private int _echoCounter = 0;

    private float _retargetTimer;
    private float _thinkTimer;
    private float _repathTimer;
    private float _castLockTimer;
    private float _strafeTimer;
    private float _targetOutOfSightTimer;
    private float _suppressedTargetTimer;
    private int _strafeDirection = 1;
    private int _suppressedTargetRootId;
    private string _debugLostReason = "";
    private bool isServer;
    private bool _hasVoiceParticipantId;

    private readonly RaycastHit[] _sightHits = new RaycastHit[16];

    public bool HasCombatTarget => IsTargetValid(_target) || IsTargetValid(_preparedTarget);

    public bool ShouldHoldCombat {
        get {
            if (cantCast)
                return false;
            if (_caster.Mana.Mana / _caster.Mana.MaxMana < manaCombatRatio)
                return false;
            if (!HasCombatTarget)
                return false;
            if (!HasCombatTarget)
                return false;
            if (_hasPreparedSpell && _preparedSpell != null)
                return true;
            if (_castLockTimer > 0f)
                return false;
            return HasAnySpellReadyNow();
        }
    }

    public string DebugState {
        get {
            var targetLabel = GetTargetLabel(_target);
            var preparedTargetLabel = GetTargetLabel(_preparedTarget);

            if (!IsTargetValid(_target) && !IsTargetValid(_preparedTarget))
                return "NoTarget";
            if (_hasPreparedSpell && _preparedSpell != null)
                return $"Prepare:{_preparedSpell.spellName} -> {preparedTargetLabel}";
            if (_castLockTimer > 0f)
                return $"Cooldown:{_castLockTimer:0.0}s -> {targetLabel}";
            if (_target != null)
                return string.IsNullOrEmpty(_debugLostReason)
                    ? $"Engage:{targetLabel}"
                    : $"Engage:{targetLabel} [{_debugLostReason}]";
            return "Combat";
        }
    }

    private void Awake() {
        _movement = GetComponent<BotMovement>();
        _caster = GetComponent<SpellCasterPlayer>();
        _selfIdentity = GetComponent<ParticipantIdentity>();
        _damageable = GetComponent<Damageable>();
        _decisionEngine = new BotSpellDecisionEngine(decisionWeights);
        isServer = NetworkManager.Singleton.IsServer;
    }

    public void Init() {
        EnsureVoiceBundleByParticipantId();
    }

    public void SetAvailableSpells(SpellDefinition[] spells) {
        _spells.Clear();
        if (spells == null)
            return;

        _spells.AddRange(BotSpellWeights.Instance.weights.Filter(it => spells.Contains(it.spell)));
    }

    private Vector3 _lastTargetPosition;
    private bool cantCast = false;

    private void FixedUpdate() {
        if (!isServer) return;
        if (_spells.Count == 0) {
            ClearPreparedSpell();
            _movement.ClearLookDirection();
            return;
        }

        _retargetTimer += Time.deltaTime;
        _thinkTimer += Time.deltaTime;
        _repathTimer += Time.deltaTime;
        _castLockTimer -= Time.deltaTime;
        _strafeTimer -= Time.deltaTime;
        _preparedCastTimer -= Time.deltaTime;
        _spellSwitchTimer -= Time.deltaTime;
        _suppressedTargetTimer -= Time.deltaTime;

        switch (_state) {
            case BotCombatState.NoTarget:
                TickNoTargetState();
                break;
            case BotCombatState.SearchTarget:
                TickSearchTargetState();
                break;
            case BotCombatState.PrepareSpell:
                TickPrepareSpellState();
                break;
            case BotCombatState.AcquireTarget:
                TickAcquireTargetState();
                break;
            case BotCombatState.Shoot:
                TickShootState();
                break;
            case BotCombatState.TargetLost:
                TickTargetLostState();
                break;
        }
    }

    private void TickNoTargetState() {
        ClearPreparedSpell();
        _movement.ClearLookDirection();
        _targetOutOfSightTimer = 0f;
        _debugLostReason = "";
        _state = BotCombatState.SearchTarget;
    }

    private void TickSearchTargetState() {
        if (_retargetTimer >= retargetInterval || !IsTargetValid(_target)) {
            _retargetTimer = 0f;
            _target = FindBestTarget();
        }

        if (_target == null)
            return;

        _state = BotCombatState.PrepareSpell;
    }

    private void TickPrepareSpellState() {
        if (!TryUpdateTargetContext())
            return;

        if (_thinkTimer < thinkInterval)
            return;

        _thinkTimer = 0f;
        if (!TryChooseSpell(transform.position, _currentTargetPosition, _currentTargetVelocity, _currentPlanarDistance,
                out var spellDecision)) {
            cantCast = true;
            if (_repathTimer >= repathInterval) {
                _movement.SetDestination(transform.position, 1.5f);
                _repathTimer = 0f;
            }

            return;
        }

        cantCast = false;
        _currentDecision = spellDecision;
        UpdateMovementByDecision(spellDecision, _currentToTarget, _currentTargetPosition, _currentPlanarDistance);

        var hasLos = HasLineOfSight(_caster.Origin, _currentTargetPosition, _target.Get.transform.root);
        var castAngle = GetPlanarAngleTo(transform.forward, _currentToTarget);
        if (!_hasPreparedSpell) {
            if (_castLockTimer > 0f || !hasLos || castAngle > castAngleTolerance)
                return;

            BeginPrepare(spellDecision.Spell, _target, spellDecision.TrackTargetDuration);
            _state = BotCombatState.AcquireTarget;
            return;
        }

        if (_preparedCastTimer > 0f && spellDecision.Spell != _preparedSpell && _castLockTimer <= 0f &&
            _spellSwitchTimer <= 0f && hasLos && castAngle <= castAngleTolerance) {
            BeginPrepare(spellDecision.Spell, _target, spellDecision.TrackTargetDuration);
        }

        _state = BotCombatState.AcquireTarget;
    }

    private void TickAcquireTargetState() {
        if (!TryUpdateTargetContext())
            return;

        if (!_hasPreparedSpell || !IsTargetValid(_preparedTarget)) {
            ClearPreparedSpell();
            _state = BotCombatState.PrepareSpell;
            return;
        }

        if (!CanUseSpellNow(_preparedSpell)) {
            ClearPreparedSpell();
            _state = BotCombatState.PrepareSpell;
            return;
        }

        if (_preparedCastTimer > 0f)
            return;

        _state = BotCombatState.Shoot;
    }

    private void TickShootState() {
        if (!TryUpdateTargetContext())
            return;

        if (_thinkTimer < thinkInterval)
            return;
        _thinkTimer = 0f;

        if (!_hasPreparedSpell || !IsTargetValid(_preparedTarget)) {
            ClearPreparedSpell();
            _state = BotCombatState.PrepareSpell;
            return;
        }

        var preparedTargetPosition = BallisticCastTargetBuilder.GetAimPoint(_preparedTarget, targetBodyHeightFactor);
        var preparedHasLos = HasLineOfSight(_caster.Origin, preparedTargetPosition, _preparedTarget.Get.transform.root);
        var preparedCastAngle = GetPlanarAngleTo(transform.forward, preparedTargetPosition - transform.position);
        if (_castLockTimer > 0f || !preparedHasLos || preparedCastAngle > castAngleTolerance) {
            _state = BotCombatState.AcquireTarget;
            return;
        }

        var castTarget = BuildCastTarget(_preparedSpell, _preparedTarget);
        if (_caster.TryCastEcho(_preparedSpell, castTarget)) {
            _echoCounter++;
            var castedSpell = _preparedSpell;
            _lastSpellDecision = _currentDecision;
            _lastCastSpell = castedSpell;
            _castLockTimer = echoRecastDelay;

            if (_caster.EchoCount <= 1) {
                _castLockTimer = GetCastCooldown(castedSpell);
                ClearPreparedSpell();
            }
        } else {
            var cast = _caster.TryCastBot(_preparedSpell, castTarget);
            var castedSpell = _preparedSpell;
            if (!IsSuccess(cast)) {
                _state = BotCombatState.AcquireTarget;
                return;
            }

            _lastSpellDecision = _currentDecision;
            _lastCastSpell = castedSpell;
            if (cast == SpellCasterPlayer.BotCastResult.StartEcho && _echoCounter == 0) {
                _castLockTimer = echoRecastDelay;
            } else {
                _castLockTimer = GetCastCooldown(castedSpell);
                ClearPreparedSpell();
            }

            _echoCounter = 0;
        }

        _debugLostReason = "";
        _state = IsTargetValid(_target) ? BotCombatState.PrepareSpell : BotCombatState.SearchTarget;
    }

    private void TickTargetLostState() {
        ClearPreparedSpell();
        _movement.ClearLookDirection();
        _targetOutOfSightTimer = 0f;
        _target = null;
        _state = BotCombatState.SearchTarget;
    }

    private bool TryUpdateTargetContext() {
        if (!IsTargetValid(_target)) {
            _state = BotCombatState.SearchTarget;
            return false;
        }

        _currentTargetVelocity = (_lastTargetPosition - _target.Position) / Time.deltaTime;
        _lastTargetPosition = _target.Position;
        _currentTargetPosition = BallisticCastTargetBuilder.GetAimPoint(_target, targetBodyHeightFactor);
        _currentToTarget = _currentTargetPosition - transform.position;
        _currentPlanarDistance = new Vector2(_currentToTarget.x, _currentToTarget.z).magnitude;
        _movement.SetLookDirection(_currentTargetPosition - _caster.Origin);
        if (UpdateTargetVisibility(_currentTargetPosition, _currentToTarget))
            return true;

        _state = BotCombatState.TargetLost;
        return false;
    }

    private Vector3 _currentTargetVelocity;

    private void UpdateMovementByDecision(
        SpellDecision spellDecision, Vector3 toTarget, Vector3 targetPosition,
        float planarDistance
    ) {
        var desiredDistance = Mathf.Max(1f, spellDecision.PreferredDistance);
        var moveDirection = new Vector3(toTarget.x, 0f, toTarget.z).normalized;

        if (_repathTimer < repathInterval)
            return;

        if (planarDistance > desiredDistance * 1.1f) {
            _movement.SetDestination(targetPosition, desiredDistance * 0.85f);
            _repathTimer = 0f;
            return;
        }

        if (planarDistance < desiredDistance * 0.55f) {
            var retreatPoint = transform.position - moveDirection * (desiredDistance - planarDistance + 1.5f);
            _movement.SetDestination(retreatPoint, 0.75f);
            _repathTimer = 0f;
            return;
        }

        if (_strafeTimer <= 0f) {
            _strafeDirection *= -1;
            _strafeTimer = strafeSwitchInterval;
        }

        var side = Vector3.Cross(Vector3.up, moveDirection) * _strafeDirection;
        var strafePoint = transform.position + side * strafeRadius;
        _movement.SetDestination(strafePoint, 0.5f);
        _repathTimer = 0f;
    }

    private bool UpdateTargetVisibility(Vector3 targetPosition, Vector3 toTarget) {
        var targetRoot = _target.Get.transform.root;
        var hasLos = HasLineOfSight(_caster.Origin, targetPosition, targetRoot);
        var lookAngle = GetPlanarAngleTo(transform.forward, toTarget);
        var inView = hasLos && lookAngle <= targetVisibleAngle;

        if (inView) {
            _targetOutOfSightTimer = 0f;
            _debugLostReason = "";
            return true;
        }

        _targetOutOfSightTimer += Time.deltaTime;
        _debugLostReason = hasLos ? $"OutOfFov:{lookAngle:0}" : "NoLoS";

        if (_targetOutOfSightTimer < targetLostSightTimeout)
            return true;

        SuppressTarget(_target);
        _target = null;
        ClearPreparedSpell();
        _movement.ClearLookDirection();
        _targetOutOfSightTimer = 0f;
        return false;
    }

    private void SuppressTarget(ITarget target) {
        if (target == null || !target.CanGet)
            return;

        var go = target.Get;
        if (go == null)
            return;

        _suppressedTargetRootId = go.transform.root.GetInstanceID();
        _suppressedTargetTimer = lostTargetReacquireDelay;
    }

    private void BeginPrepare(SpellDefinition spell, ITarget target, float trackTargetDuration) {
        if (!CanUseSpellNow(spell))
            return;

        _preparedSpell = spell;
        _preparedTarget = target;
        _preparedCastTimer = castPrepareDelay;
        _spellSwitchTimer = switchSpellDelay;
        _hasPreparedSpell = true;
        _caster.SelectSpell(_preparedSpell);
    }

    public void PlayVoice(string spellName) {
        if (_hasVoiceParticipantId)
            voiceAudioSource.PlayOneShot(voiceBundle.voices.Find(it => it.words == spellName).line);
    }

    private bool CanUseSpellNow(SpellDefinition spell) {
        return HasEchoContinuation(spell) || (_caster.CanStartCast(spell) && !_caster.Channeling && !_caster.Charging);
    }

    private bool HasAnySpellReadyNow() {
        if (_spells.Count == 0)
            return false;

        if (TryChooseEchoContinuation(out var echoDecision))
            return echoDecision.Spell != null;

        var hasAlternative = false;
        for (var i = 0; i < _spells.Count; i++) {
            var spell = _spells[i];
            if (spell == null || !spell.available)
                continue;
            if (spell.spell != _lastCastSpell)
                hasAlternative = true;
            if (!CanUseSpellNow(spell.spell))
                continue;
            if (spell.spell == _lastCastSpell && hasAlternative && !HasEchoContinuation(spell.spell))
                continue;
            return true;
        }

        return false;
    }

    private void ClearPreparedSpell() {
        _preparedSpell = null;
        _preparedTarget = null;
        _preparedCastTimer = 0f;
        _hasPreparedSpell = false;
    }

    private ITarget BuildCastTarget(SpellDefinition spell, ITarget target) {
        return BallisticCastTargetBuilder.Build(_caster, target, spell, ballisticTargetLift, targetBodyHeightFactor);
    }

    private bool TryChooseSpell(
        Vector3 start, Vector3 target, Vector3 targetVelocity, float distance, out SpellDecision decision
    ) {
        if (TryChooseEchoContinuation(out decision))
            return true;

        decision = default;
        decision.PreferredDistance = distance;
        var hasChoice = false;
        var bestScore = float.MinValue;

        var mana = _caster.Mana;
        var manaRatio = mana.Mana / mana.MaxMana;
        var healthRatio = _damageable.CurrentHealth / _damageable.Health.maxHealth;

        for (var i = 0; i < _spells.Count; i++) {
            var spell = _spells[i];
            if (spell == null || !spell.available)
                continue;
            if (spell.spell == _lastCastSpell)
                continue;
            if (!CanUseSpellNow(spell.spell))
                continue;

            var input = new BotSpellDecisionInput {
                SpellWeights = spell,
                Start = start,
                Target = target,
                TargetVelocity = targetVelocity,
                Distance = distance,
                HealthRatio = healthRatio,
                ManaRatio = manaRatio,
                MaxMana = mana.MaxMana
            };

            var evaluated = _decisionEngine.Evaluate(input);
            var current = new SpellDecision {
                Spell = evaluated.Spell,
                Score = evaluated.Score,
                PreferredDistance = evaluated.PreferredDistance,
                TrackTargetDuration = evaluated.TrackTargetDuration
            };

            if (current.Score <= bestScore)
                continue;

            bestScore = current.Score;
            decision = current;
            hasChoice = true;
        }

        return hasChoice;
    }

    private bool TryChooseEchoContinuation(
        out SpellDecision decision
    ) {
        decision = _lastSpellDecision;
        if (_lastCastSpell == null) return false;
        if (_lastCastSpell.echoCount <= 0) return false;
        return _lastCastSpell.echoCount == _caster.EchoCount;
    }

    private bool HasEchoContinuation(SpellDefinition spell) {
        return spell != null && spell.echoCount > 0 && _lastCastSpell == spell && _caster.EchoCount > 0;
    }

    private ITarget FindBestTarget() {
        var best = default(ITarget);
        var bestDistanceSqr = float.MaxValue;
        var selfPosition = transform.position;
        var hasSuppressedTarget = _suppressedTargetTimer > 0f;

        for (var i = 0; i < SpellCaster.Active.Count; i++) {
            var candidate = SpellCaster.Active[i];
            if (candidate == null || !candidate.IsPlayer)
                continue;
            if (candidate.gameObject == gameObject)
                continue;
            if (!IsEnemy(candidate))
                continue;
            if (hasSuppressedTarget && candidate.Get != null &&
                candidate.Get.transform.root.GetInstanceID() == _suppressedTargetRootId)
                continue;

            var dSqr = (candidate.Position - selfPosition).sqrMagnitude;
            if (dSqr > targetSearchRadius * targetSearchRadius)
                continue;
            if (!CanAcquireTarget(candidate))
                continue;
            if (dSqr >= bestDistanceSqr)
                continue;

            bestDistanceSqr = dSqr;
            best = candidate;
        }

        return best;
    }

    private bool CanAcquireTarget(ITarget candidate) {
        if (candidate == null || !candidate.CanGet)
            return false;

        var targetGo = candidate.Get;
        if (targetGo == null)
            return false;

        var targetPosition = BallisticCastTargetBuilder.GetAimPoint(candidate, targetBodyHeightFactor);
        var toTarget = targetPosition - transform.position;
        var lookAngle = GetPlanarAngleTo(transform.forward, toTarget);
        if (lookAngle > targetVisibleAngle)
            return false;

        return HasLineOfSight(_caster.Origin, targetPosition, targetGo.transform.root);
    }

    private bool IsTargetValid(ITarget target) {
        if (target == null)
            return false;
        if (!target.CanGet)
            return false;
        if (!target.IsPlayer)
            return false;
        if (!IsEnemy(target))
            return false;

        var delta = target.Position - transform.position;
        return delta.sqrMagnitude <= targetSearchRadius * targetSearchRadius;
    }

    private bool IsEnemy(ITarget target) {
        var targetIdentity = target.Get.GetComponent<ParticipantIdentity>();
        if (targetIdentity == null)
            return false;

        if (TeamManager.Instance == null)
            return targetIdentity.Id != _selfIdentity.Id;

        return TeamManager.Instance.AreEnemies(_selfIdentity.Id, targetIdentity.Id);
    }

    private bool HasLineOfSight(Vector3 from, Vector3 to, Transform targetRoot) {
        var direction = to - from;
        var distance = direction.magnitude;
        if (distance <= 0.01f)
            return true;

        direction /= distance;
        var hits = Physics.RaycastNonAlloc(new Ray(from, direction), _sightHits, distance, sightMask,
            QueryTriggerInteraction.Ignore);

        if (hits == 0)
            return true;

        for (var i = 0; i < hits; i++) {
            var hit = _sightHits[i];
            if (hit.collider == null)
                continue;

            var root = hit.transform.root;
            if (root == transform.root)
                continue;
            if (root == targetRoot)
                return true;
            return false;
        }

        return true;
    }

    private static float GetCastCooldown(SpellDefinition spell) {
        var cooldown = 3f;
        if (spell.channeling)
            cooldown += Mathf.Min(0.75f, spell.channelDuration * 0.4f);
        if (spell.charging)
            cooldown += spell.chargeDuration;
        return cooldown;
    }

    private static float GetPlanarAngleTo(Vector3 forward, Vector3 toTarget) {
        forward.y = 0f;
        toTarget.y = 0f;
        if (forward.sqrMagnitude < 0.0001f || toTarget.sqrMagnitude < 0.0001f)
            return 0f;
        return Vector3.Angle(forward.normalized, toTarget.normalized);
    }

    private static string GetTargetLabel(ITarget target) {
        if (target == null || !target.CanGet)
            return "-";

        var go = target.Get;
        if (go == null)
            return "-";

        var identity = go.GetComponent<ParticipantIdentity>();
        if (identity == null)
            return go.name;

        return identity.Id.IsBot ? $"Bot:{identity.Id.Value}" : $"P:{identity.Id.Value}";
    }

    public bool IsSuccess(SpellCasterPlayer.BotCastResult result) {
        return result == SpellCasterPlayer.BotCastResult.StartCharging ||
               result == SpellCasterPlayer.BotCastResult.EchoUsed ||
               result == SpellCasterPlayer.BotCastResult.StartEcho ||
               result == SpellCasterPlayer.BotCastResult.Casted;
    }

    private void EnsureVoiceBundleByParticipantId() {
        var participantId = _selfIdentity.Id;
        if (_hasVoiceParticipantId)
            return;

        var bundle = BotSpellVoice.Instance.GetBundleByParticipantId(participantId);

        voiceBundle = bundle;
        _hasVoiceParticipantId = true;
    }

    private struct SpellDecision {
        public SpellDefinition Spell;
        public float Score;
        public float PreferredDistance;
        public float TrackTargetDuration;
    }
}