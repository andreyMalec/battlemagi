using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BotMovement))]
[RequireComponent(typeof(SpellCasterPlayer))]
[RequireComponent(typeof(ParticipantIdentity))]
public class BotCombatController : MonoBehaviour {
    [SerializeField] private float targetSearchRadius = 45f;
    [SerializeField] private float retargetInterval = 0.5f;
    [SerializeField] private float thinkInterval = 0.15f;
    [SerializeField] private float repathInterval = 0.25f;
    [SerializeField] private float castAngleTolerance = 18f;
    [SerializeField] private float strafeRadius = 3f;
    [SerializeField] private float strafeSwitchInterval = 1.1f;
    [SerializeField] [Range(0.1f, 1f)] private float targetBodyHeightFactor = 0.75f;
    [SerializeField] private float ballisticTargetLift = 1f;
    [SerializeField] private LayerMask sightMask = ~0;
    [SerializeField] private BotSpellDecisionWeights decisionWeights = new();

    private BotMovement _movement;
    private SpellCasterPlayer _caster;
    private ParticipantIdentity _selfIdentity;
    private Damageable _damageable;
    private BotSpellDecisionEngine _decisionEngine;

    private readonly List<SpellDefinition> _spells = new();
    private ITarget _target;
    private SpellDefinition _lastCastSpell;

    private float _retargetTimer;
    private float _thinkTimer;
    private float _repathTimer;
    private float _castLockTimer;
    private float _trackAimTimer;
    private float _strafeTimer;
    private int _strafeDirection = 1;

    private readonly RaycastHit[] _sightHits = new RaycastHit[16];

    private void Awake() {
        _movement = GetComponent<BotMovement>();
        _caster = GetComponent<SpellCasterPlayer>();
        _selfIdentity = GetComponent<ParticipantIdentity>();
        _damageable = GetComponent<Damageable>();
        _decisionEngine = new BotSpellDecisionEngine(decisionWeights);
        _decisionEngine.RebindWeights();
    }

    public void SetAvailableSpells(SpellDefinition[] spells) {
        _spells.Clear();
        if (spells == null)
            return;

        for (var i = 0; i < spells.Length; i++) {
            var spell = spells[i];
            if (spell == null)
                continue;
            if (!_spells.Contains(spell))
                _spells.Add(spell);
        }
    }

    private void Update() {
        if (_spells.Count == 0) {
            _movement.ClearLookDirection();
            return;
        }

        _retargetTimer += Time.deltaTime;
        _thinkTimer += Time.deltaTime;
        _repathTimer += Time.deltaTime;
        _castLockTimer -= Time.deltaTime;
        _trackAimTimer -= Time.deltaTime;
        _strafeTimer -= Time.deltaTime;

        if (_retargetTimer >= retargetInterval || !IsTargetValid(_target)) {
            _retargetTimer = 0f;
            _target = FindBestTarget();
        }

        if (_target == null) {
            _movement.ClearLookDirection();
            return;
        }

        var targetPosition = BallisticCastTargetBuilder.GetAimPoint(_target, targetBodyHeightFactor);
        var toTarget = targetPosition - transform.position;
        var lookToTarget = targetPosition - _caster.Origin;
        _movement.SetLookDirection(lookToTarget);

        if (_thinkTimer < thinkInterval)
            return;

        _thinkTimer = 0f;
        var planarDistance = new Vector2(toTarget.x, toTarget.z).magnitude;

        if (!TryChooseSpell(planarDistance, toTarget, out var spellDecision)) {
            if (_repathTimer >= repathInterval) {
                _movement.SetDestination(targetPosition, 1.5f);
                _repathTimer = 0f;
            }

            return;
        }

        var desiredDistance = Mathf.Max(1f, spellDecision.PreferredDistance);
        var moveDirection = new Vector3(toTarget.x, 0f, toTarget.z).normalized;

        if (_repathTimer >= repathInterval) {
            if (planarDistance > desiredDistance * 1.1f) {
                _movement.SetDestination(targetPosition, desiredDistance * 0.85f);
                _repathTimer = 0f;
            } else if (planarDistance < desiredDistance * 0.55f) {
                var retreatPoint = transform.position - moveDirection * (desiredDistance - planarDistance + 1.5f);
                _movement.SetDestination(retreatPoint, 0.75f);
                _repathTimer = 0f;
            } else {
                if (_strafeTimer <= 0f) {
                    _strafeDirection *= -1;
                    _strafeTimer = strafeSwitchInterval;
                }

                var side = Vector3.Cross(Vector3.up, moveDirection) * _strafeDirection;
                var strafePoint = transform.position + side * strafeRadius;
                _movement.SetDestination(strafePoint, 0.5f);
                _repathTimer = 0f;
            }
        }

        var hasLos = HasLineOfSight(_caster.Origin, targetPosition, _target.Get.transform.root);
        var castAngle = Vector3.Angle(transform.forward, toTarget.normalized);
        if (_castLockTimer > 0f || !hasLos || castAngle > castAngleTolerance)
            return;

        var castTarget = BuildCastTarget(spellDecision.Spell, _target);
        if (_caster.TryCastBot(spellDecision.Spell, castTarget)) {
            _lastCastSpell = spellDecision.Spell;
            _castLockTimer = GetCastCooldown(spellDecision.Spell);
            _trackAimTimer = Mathf.Max(_trackAimTimer, spellDecision.TrackTargetDuration);
        }
    }

    private ITarget BuildCastTarget(SpellDefinition spell, ITarget target) {
        return BallisticCastTargetBuilder.Build(_caster, target, spell, ballisticTargetLift, targetBodyHeightFactor);
    }

    private bool TryChooseSpell(float distance, Vector3 toTarget, out SpellDecision decision) {
        decision = default;
        var hasChoice = false;
        var bestScore = float.MinValue;

        var mana = _caster.Mana;
        var manaRatio = mana.MaxMana > 0.001f ? mana.Mana / mana.MaxMana : 1f;
        var healthRatio = _damageable.Health.maxHealth > 0.001f
            ? _damageable.CurrentHealth / _damageable.Health.maxHealth
            : 1f;

        for (var i = 0; i < _spells.Count; i++) {
            var spell = _spells[i];
            if (spell == null)
                continue;
            if (spell == _lastCastSpell)
                continue;

            var input = new BotSpellDecisionInput {
                Spell = spell,
                Distance = distance,
                ToTarget = toTarget,
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

    private ITarget FindBestTarget() {
        var best = default(ITarget);
        var bestDistanceSqr = float.MaxValue;
        var selfPosition = transform.position;

        for (var i = 0; i < SpellCaster.Active.Count; i++) {
            var candidate = SpellCaster.Active[i];
            if (candidate == null || !candidate.IsPlayer)
                continue;
            if (candidate.gameObject == gameObject)
                continue;
            if (!IsEnemy(candidate))
                continue;

            var dSqr = (candidate.Position - selfPosition).sqrMagnitude;
            if (dSqr > targetSearchRadius * targetSearchRadius)
                continue;
            if (dSqr >= bestDistanceSqr)
                continue;

            bestDistanceSqr = dSqr;
            best = candidate;
        }

        return best;
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
        return cooldown;
    }

    private struct SpellDecision {
        public SpellDefinition Spell;
        public float Score;
        public float PreferredDistance;
        public float TrackTargetDuration;
    }

}