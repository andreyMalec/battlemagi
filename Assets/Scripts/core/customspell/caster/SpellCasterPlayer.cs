using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Damageable))]
public class SpellCasterPlayer : SpellCaster {
    public Transform spawnPos;

    [SerializeField] private SpellDefinition spellE;
    [SerializeField] public PlayerSpellInput input = new();
    [SerializeField] private ManaModule mana = new();
    [SerializeField] private MonoBehaviour bridge;
    [SerializeField] private bool animateCast = true;
    [SerializeField] private bool animateHand = true;
    [SerializeField] private StatusEffectData primalManaStatus;

    private ISpellCasterBridge _bridgeTyped;
    private Stats _stats;
    private Statusable _statusable;
    private Damageable _damageable;

    private SpellDefinition _spell;
    private SpellCasterPlayerPreview _preview;
    private SpellCasterPlayerAnimator _animator;

    private bool _manaInitialized;
    public bool Channeling { get; private set; }
    private float _channelingElapsed;
    private Coroutine _channelingRoutine;
    private SpellDefinition _channelingSpell;

    public bool Charging { get; private set; }
    private Coroutine _chargingRoutine;
    private SpellDefinition _chargingSpell;
    private float _chargingDamageMultiplier = 1f;
    private bool _chargingUsedEcho;

    private int _echoRemaining;

    public int EchoCount {
        get {
            if (_spell != null && _spell.echoCount > 0 && _echoSpell == null) return _spell.echoCount + 1;
            return _echoRemaining;
        }
    }

    private SpellDefinition _echoSpell;

    private List<SpellDefinition> _availableSpells;

    public ManaModule Mana => mana;

    public bool CastWaiting => _spell != null || _echoSpell != null;
    public bool CanSelectSpell => !CastWaiting && !Channeling && !Charging;

    public override Vector3 Origin => spawnPos.position;
    public override Vector3 Direction => spawnPos.forward;

    public override bool IsPlayer => true;
    public override bool IsSpell => false;

    public override bool CanCast => Authority != null && Authority.IsOwner;

    public void RestoreEcho(SpellDefinition spell, int amount = 1) {
        _bridgeTyped.RestoreEcho(spell, amount);
    }

    internal void ApplyRestoreEcho(SpellDefinition spell, int amount) {
        if (spell == null || spell.echoCount <= 0 || amount <= 0) return;

        if (_echoSpell != spell) {
            _echoSpell = spell;
            _echoRemaining = 0;
        }

        _echoRemaining = Mathf.Clamp(_echoRemaining + amount, 0, spell.echoCount);

        if (_echoRemaining > 0) {
            _spell = spell;

            if (animateHand) {
                _animator.CastWaitingAnim(true, spell.castWaitingIndex);
            }
        }
    }

    internal void ApplyRestoreEcho(string spellWords, int amount) {
        if (string.IsNullOrEmpty(spellWords) || amount <= 0) return;

        var spell = DefaultSpells.Get(spellWords)?.spell;
        if (spell == null) return;
        ApplyRestoreEcho(spell, amount);
    }

    protected new void Awake() {
        base.Awake();
        _stats = GetComponent<Stats>();
        _statusable = GetComponent<Statusable>();
        _damageable = GetComponent<Damageable>();
        _preview = GetComponent<SpellCasterPlayerPreview>();

        if (animateCast || animateHand)
            _animator = GetComponent<SpellCasterPlayerAnimator>();

        if (bridge != null)
            _bridgeTyped = (ISpellCasterBridge)bridge;
        else
            _bridgeTyped = GetComponentInParent<ISpellCasterBridge>();

        if (_bridgeTyped != null)
            _bridgeTyped.Bind(this);
    }

    public void UpdateAvailableSpells(List<SpellDefinition> availableSpells) {
        _availableSpells = availableSpells;
    }

    internal void InitializeServerMana() {
        if (_manaInitialized) return;
        _manaInitialized = true;
        mana.InitializeServer(_stats);
    }

    internal void TickServerMana(float dt) {
        if (!_manaInitialized) InitializeServerMana();
        if (Authority == null) return;
        if (!Authority.IsServer) return;
        mana.TickServer(dt);
        if (mana.PrimalMana > 0)
            _statusable.AddEffect(OwnerId, primalManaStatus);
    }

    void Update() {
        if (!CanCast) return;

        if (input.AlternativeSpawnPressedThisFrame()) {
            alternativeSpawn = !alternativeSpawn;
        }

        var index = input.GetSpellIndexPressedThisFrame();
        if (CanSelectSpell && index >= 0 && index < _availableSpells.Count) {
            var selected = _availableSpells[index];
            SelectSpell(selected);
        }

        if (input.CancelPressedThisFrame()) {
            CancelCast();
        }

        if (Charging && input.CastPressedThisFrame()) {
            ReleaseCharged(_chargingSpell);
        }

        if (!Channeling && !Charging && _spell != null && input.CastPressedThisFrame()) {
            if (TryCastEcho(_spell)) {
            } else if (CanStartCast(_spell)) {
                if (_spell.charging) {
                    StartCharging(_spell);
                } else {
                    if (animateCast) {
                        _animator.AnimateCast(_spell);
                    } else
                        Cast(_spell);
                }
            }
        }

        _preview?.SetSpell(_spell);
    }

    public void SelectSpell(SpellDefinition spell) {
        if (spell != _spell)
            ResetEcho();
        _spell = spell;

        SpellLog.Log($"{gameObject.name} Selected spell: " + spell?.spellName);
        if (animateHand)
            _animator.CastWaitingAnim(true, _spell.castWaitingIndex);
    }

    public override void Cast(SpellDefinition spell) {
        var usedEcho = spell.charging ? _chargingUsedEcho : ConsumeCostOrEcho(spell);
        _chargingUsedEcho = false;
        base.Cast(spell);

        if (spell.channeling) {
            _channelingRoutine = StartCoroutine(Channel(spell));
        }

        _spell = null;
        StartCoroutine(BeginEcho(spell, usedEcho));
    }

    private bool ConsumeCostOrEcho(SpellDefinition spell) {
        if (_echoSpell == spell && _echoRemaining > 0) {
            _echoRemaining--;
            return true;
        }

        SpendResourceServer(spell, mana.CostForCast(spell));
        return false;
    }

    private bool CanStartCast(SpellDefinition spell) {
        if (spell == null) return false;
        if (spell.bloodMagic) {
            if (_echoSpell == spell && _echoRemaining > 0)
                return true;

            return _damageable.CanSpendHealthCost(mana.CostForCast(spell));
        }

        return !mana.IsPrimalManaLocked(spell, GetEchoBudget(spell));
    }

    private bool SpendResourceServer(SpellDefinition spell, float amount) {
        if (spell == null || amount <= 0f) return true;

        if (spell.bloodMagic)
            return _bridgeTyped.TrySpendHealth(amount);

        return _bridgeTyped.TrySpendMana(amount);
    }

    internal void BindChannelingSpell(ulong spellObjectId, string spellName) {
        _bridgeTyped.BindChannelingSpell(spellObjectId, spellName);
    }

    internal void StopChannelingSpell(ulong spellObjectId) {
        _bridgeTyped.StopChannelingSpell(spellObjectId);
    }

    internal void StopChannelingFromBridge() {
        if (!Channeling) return;
        StopChanneling(false);
    }

    private bool TryCastEcho(SpellDefinition spell) {
        if (_echoSpell != spell) return false;
        if (_echoRemaining <= 0) return false;

        if (animateCast)
            _animator.AnimateCast(spell);
        else
            Cast(spell);

        return true;
    }

    private IEnumerator BeginEcho(SpellDefinition spell, bool usedEcho) {
        if (spell == null || spell.echoCount <= 0) {
            ResetEcho();
            yield break;
        }

        if (usedEcho) {
            if (_echoRemaining <= 0) {
                ResetEcho();
                yield break;
            }

            if (animateHand) {
                yield return new WaitForEndOfFrame(); // задержка чтобы успел обновиться Rig для руки
                _animator.CastWaitingAnim(true, spell.castWaitingIndex);
            }

            _spell = spell;
            yield break;
        }

        if (animateHand) {
            yield return new WaitForEndOfFrame();
            _animator.CastWaitingAnim(true, spell.castWaitingIndex);
        }

        _spell = spell;
        _echoSpell = spell;
        _echoRemaining = spell.echoCount;
    }

    private void ResetEcho() {
        _spell = null;
        _echoSpell = null;
        _echoRemaining = 0;
    }

    private int GetEchoBudget(SpellDefinition spell) {
        if (spell == null) return 0;
        if (_echoSpell == spell) return _echoRemaining;
        return spell.echoCount;
    }

    private void CancelCast() {
        if (Charging) {
            ReleaseCharged(_chargingSpell);
            return;
        }

        ResetEcho();

        if (animateCast || animateHand) {
            _animator.CancelAnimate();
        }

        if (CastCoroutine != null) {
            StopCoroutine(CastCoroutine);
        }

        if (_chargingRoutine != null) {
            StopCoroutine(_chargingRoutine);
            _chargingRoutine = null;
        }

        Charging = false;
        _chargingUsedEcho = false;
        _chargingDamageMultiplier = 1f;

        StopChanneling(true);

        _spell = null;
    }

    private void StopChanneling(bool requestBridgeStop) {
        if (_channelingRoutine != null) {
            StopCoroutine(_channelingRoutine);
            _channelingRoutine = null;
        }

        if (_channelingSpell?.channeling != true) return;

        ResetEcho();

        if (animateCast || animateHand) {
            _animator.CancelAnimate();
        }

        if (requestBridgeStop)
            _bridgeTyped.RequestStopChanneling();

        Channeling = false;
        _channelingSpell = null;
        _bridgeTyped.EndChanneling();
        _spell = null;
    }

    private void StartCharging(SpellDefinition spell) {
        if (_chargingRoutine != null) StopCoroutine(_chargingRoutine);
        _chargingUsedEcho = ConsumeCostOrEcho(spell);
        Charging = true;
        _chargingSpell = spell;
        _chargingDamageMultiplier = 1f;
        _preview.StartCharging();

        if (animateCast) {
            _animator.AnimateCast(spell);
        }

        _chargingRoutine = StartCoroutine(ChargingTick(spell));
    }

    private IEnumerator ChargingTick(SpellDefinition spell) {
        var duration = Mathf.Max(0f, spell.chargeDuration);
        var costPerSecond = mana.CostPerSecond(spell);

        var elapsed = 0f;
        while (Charging && elapsed < duration) {
            var dt = Time.deltaTime;
            elapsed += dt;

            var cost = costPerSecond * dt;
            if (!SpendResourceServer(spell, cost)) {
                ReleaseCharged(spell);
                yield break;
            }

            _chargingDamageMultiplier = Mathf.Clamp01((float)Math.Pow(2, elapsed / duration));
            yield return null;
        }

        ReleaseCharged(spell);
    }

    private void ReleaseCharged(SpellDefinition spell) {
        if (!Charging) return;
        ResetEcho();
        if (animateCast || animateHand) {
            _animator.CancelAnimate();
        }

        if (_chargingRoutine != null) {
            StopCoroutine(_chargingRoutine);
            _chargingRoutine = null;
        }

        Charging = false;
        var toCast = spell;
        _chargingSpell = null;

        Cast(toCast);

        _chargingDamageMultiplier = 1f;
    }

    private IEnumerator Channel(SpellDefinition spell) {
        ResetEcho();
        _channelingSpell = spell;
        _channelingElapsed = 0;
        Channeling = true;
        _bridgeTyped.BeginChanneling(spell);
        var stoppedByBridge = false;
        var costPerSecond = mana.CostPerSecond(spell);
        while (_channelingElapsed < spell.channelDuration) {
            if (!Channeling)
                break;

            if (_bridgeTyped.ShouldStopChanneling()) {
                stoppedByBridge = true;
                break;
            }

            var dt = Time.deltaTime;
            var costPerTick = costPerSecond * dt;
            if (!SpendResourceServer(spell, costPerTick)) {
                break;
            }

            yield return new WaitForSeconds(dt);
            _channelingElapsed += dt;
        }

        StopChanneling(!stoppedByBridge);
    }

    public override SpawnContext CastContext(SpellDefinition spell) {
        var c = base.CastContext(spell);
        c.spellDamageMultiplier = _chargingDamageMultiplier;
        return c;
    }
}