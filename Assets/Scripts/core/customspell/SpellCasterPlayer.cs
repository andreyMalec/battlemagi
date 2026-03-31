using System;
using System.Collections;
using System.Collections.Generic;
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
    public int EchoCount;
    public bool Channeling { get; private set; }
    private float _channelingElapsed;
    private Coroutine _channelingRoutine;
    private SpellDefinition _channelingSpell;
    private SpellInstance _channelingSpellInstance;

    public ManaModule Mana => mana;

    public override Vector3 Origin => spawnPos.position;
    public override Vector3 Direction => spawnPos.forward;

    public override bool IsPlayer => true;
    public override bool IsSpell => false;

    public override bool CanCast => Authority != null && Authority.IsOwner;

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
            _spell = SpellDatabase.Instance.data[index];
        }

        if (Input.GetKeyDown(KeyCode.E)) {
            _spell = spellE;
        }

        if (!Channeling && _spell != null && input.CastPressedThisFrame()) {
            if (!mana.IsPrimalManaLocked(_spell, EchoCount)) {
                if (animateCast)
                    _animator.AnimateCast(_spell);
                else
                    Cast(_spell);
            } else {
                // TODO some feedback for primal mana locked
            }
        }

        if (input.CancelPressedThisFrame()) {
            CancelCast();
        }

        _preview?.SetSpell(_spell);
    }

    public override void Cast(SpellDefinition spell) {
        base.Cast(spell);
        if (mana.CanSpendForCast(_spell, EchoCount)) {
            mana.SpendManaServer(mana.CostForCast(_spell));
        } else {
            mana.AddPrimalManaServer(mana.PrimalManaMissing(mana.CostForCast(_spell)));
        }

        if (_spell?.channeling == true) {
            _channelingRoutine = StartCoroutine(Channel(_spell));
        }

        _spell = null;
    }

    private void CancelCast() {
        _animator.CancelAnimate();

        if (CastCoroutine != null) {
            StopCoroutine(CastCoroutine);
        }

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

    private IEnumerator Channel(SpellDefinition spell) {
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
}