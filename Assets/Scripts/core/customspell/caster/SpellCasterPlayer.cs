using System.Collections;
using UnityEngine;

public class SpellCasterPlayer : SpellCaster {
    public Transform spawnPos;

    [SerializeField] private SpellDefinition spellE;
    [SerializeField] private PlayerSpellInput input = new();
    [SerializeField] private ManaModule mana = new();
    [SerializeField] private MonoBehaviour bridge;
    [SerializeField] private bool animateCast = true;

    private ISpellCasterBridge _bridgeTyped;
    private Stats _stats;

    private SpellDefinition _spell;
    private SpellCasterPlayerPreview _preview;
    private SpellCasterPlayerAnimator _animator;

    private bool _manaInitialized;
    public bool Channeling { get; private set; }
    private float _channelingElapsed;
    private Coroutine _channelingRoutine;
    private SpellDefinition _channelingSpell;
    private SpellInstance _channelingSpellInstance;

    public bool Charging { get; private set; }
    private Coroutine _chargingRoutine;
    private SpellDefinition _chargingSpell;
    private float _chargingDamageMultiplier = 1f;

    private int _echoRemaining;
    private SpellDefinition _echoSpell;

    public ManaModule Mana => mana;

    public override Vector3 Origin => spawnPos.position;
    public override Vector3 Direction => spawnPos.forward;

    public override bool IsPlayer => true;
    public override bool IsSpell => false;

    public override bool CanCast => Authority != null && Authority.IsOwner;

    public void RestoreEcho(SpellDefinition spell, int amount = 1) {
        if (spell == null || spell.echoCount <= 0 || amount <= 0) return;

        if (_echoSpell != spell) {
            _echoSpell = spell;
            _echoRemaining = 0;
        }

        _echoRemaining = Mathf.Clamp(_echoRemaining + amount, 0, spell.echoCount);
        SyncEchoCount();

        if (_echoRemaining > 0)
            _spell = spell;
    }

    protected new void Awake() {
        base.Awake();
        _stats = GetComponent<Stats>();
        _preview = GetComponent<SpellCasterPlayerPreview>();
        if (animateCast)
            _animator = GetComponent<SpellCasterPlayerAnimator>();

        if (bridge != null)
            _bridgeTyped = (ISpellCasterBridge)bridge;
        else
            _bridgeTyped = GetComponentInParent<ISpellCasterBridge>();

        if (_bridgeTyped != null)
            _bridgeTyped.Bind(this);
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
    }

    void Update() {
        if (!CanCast) return;

        var index = input.GetSpellIndexPressedThisFrame();
        if (index >= 0 && index < SpellDatabase.Instance.data.Count) {
            var selected = SpellDatabase.Instance.data[index];
            if (selected != _spell)
                ResetEcho();
            _spell = selected;

            if (animateCast)
                _animator.CastWaitingAnim(true, _spell.castWaitingIndex);
        }

        if (input.CancelPressedThisFrame()) {
            CancelCast();
        }

        if (Charging && input.CastPressedThisFrame()) {
            ReleaseCharged(_chargingSpell);
        }

        if (!Channeling && !Charging && _spell != null && input.CastPressedThisFrame()) {
            if (TryCastEcho(_spell)) {
            } else if (!mana.IsPrimalManaLocked(_spell, GetEchoBudget(_spell))) {
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

    public override void Cast(SpellDefinition spell) {
        var usedEcho = _echoSpell == spell && _echoRemaining > 0;
        base.Cast(spell);

        if (!spell.charging && !spell.channeling) {
            ConsumeManaOrEcho(spell);
        }

        if (spell.channeling) {
            _channelingRoutine = StartCoroutine(Channel(spell));
        }

        _spell = null;
        BeginEcho(spell, usedEcho);
    }

    private void ConsumeManaOrEcho(SpellDefinition spell) {
        if (_echoSpell == spell && _echoRemaining > 0) {
            _echoRemaining--;
            SyncEchoCount();
            return;
        }

        if (mana.CanSpendForCast(spell, GetEchoBudget(spell))) {
            mana.SpendManaServer(mana.CostForCast(spell));
        } else {
            mana.AddPrimalManaServer(mana.PrimalManaMissing(mana.CostForCast(spell)));
        }
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

    private void BeginEcho(SpellDefinition spell, bool usedEcho) {
        if (spell == null || spell.echoCount <= 0) {
            ResetEcho();
            return;
        }

        if (usedEcho) {
            if (_echoRemaining <= 0) {
                ResetEcho();
                return;
            }

            if (animateCast) {
                _animator.CastWaitingAnim(true, spell.castWaitingIndex);
            }

            _spell = spell;
            SyncEchoCount();
            return;
        }

        if (animateCast) {
            _animator.CastWaitingAnim(true, spell.castWaitingIndex);
        }

        _spell = spell;
        _echoSpell = spell;
        _echoRemaining = spell.echoCount;
        SyncEchoCount();
    }

    private void ResetEcho() {
        _spell = null;
        _echoSpell = null;
        _echoRemaining = 0;
        SyncEchoCount();
    }

    private int GetEchoBudget(SpellDefinition spell) {
        if (spell == null) return 0;
        if (_echoSpell == spell) return _echoRemaining;
        return spell.echoCount;
    }

    private void SyncEchoCount() {
    }

    private void CancelCast() {
        if (Charging) {
            ReleaseCharged(_chargingSpell);
            return;
        }

        ResetEcho();

        if (animateCast) {
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
        _chargingDamageMultiplier = 1f;

        if (_channelingRoutine != null) {
            StopCoroutine(_channelingRoutine);
        }

        if (_channelingSpell?.channeling == true) {
            Channeling = false;
            foreach (var active in SpellInstance.Active) {
                if (active.OwnerId != OwnerId) continue;
                if (!active.IsAlive) continue;
                var spell = active.Bind.Context.Spell;
                if (_channelingSpell.words != spell.words) continue;
                active.Bind.Context.View.Kill(active.Bind.Context);
                break;
            }

            _channelingSpell = null;
            _channelingSpellInstance = null;
        }

        _spell = null;
    }

    private void StartCharging(SpellDefinition spell) {
        if (_chargingRoutine != null) StopCoroutine(_chargingRoutine);
        Charging = true;
        _chargingSpell = spell;
        _chargingDamageMultiplier = 1f;

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
            if (mana.CanSpendForChannelTick(spell, dt)) {
                mana.SpendManaServer(cost);
            } else {
                mana.AddPrimalManaServer(mana.PrimalManaMissing(cost));
                ReleaseCharged(spell);
                yield break;
            }

            _chargingDamageMultiplier = Mathf.Clamp01(elapsed / duration);
            yield return null;
        }

        ReleaseCharged(spell);
    }

    private void ReleaseCharged(SpellDefinition spell) {
        if (!Charging) return;
        ResetEcho();
        if (animateCast) {
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
        var costPerSecond = mana.CostPerSecond(spell);
        while (_channelingElapsed < spell.channelDuration) {
            if (!Channeling)
                break;

            if (_channelingSpellInstance == null) {
                _channelingSpellInstance = SpellInstance.Active.Find(it =>
                    it.OwnerId == OwnerId && it.IsAlive && it.Bind.Context.Spell.words == spell.words);
            }

            if (_channelingSpellInstance?.IsAlive == false) {
                break;
            }

            var dt = Time.deltaTime;
            var costPerTick = costPerSecond * dt;
            if (mana.CanSpendForChannelTick(spell, dt)) {
                mana.SpendManaServer(costPerTick);
            } else {
                mana.AddPrimalManaServer(mana.PrimalManaMissing(costPerTick));
                break;
            }

            yield return new WaitForSeconds(dt);
            _channelingElapsed += dt;
        }

        CancelCast();
    }

    public override SpawnContext CastContext(SpellDefinition spell) {
        var c = base.CastContext(spell);
        c.spellDamageMultiplier = _chargingDamageMultiplier;
        return c;
    }
}