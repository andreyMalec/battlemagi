using System.Collections.Generic;
using UnityEngine;

public class SpellCasterPlayer : SpellCaster {
    public Transform spawnPos;

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

        if (_spell != null && input.CastPressedThisFrame()) {
            if (!mana.IsPrimalManaLocked(_spell, EchoCount)) {
                if (animateCast)
                    _animator.AnimateCast(_spell);
                else
                    Cast(_spell);
            } else {
                // TODO some feedback for primal mana locked
            }
        }

        if (_spell != null && input.CancelPressedThisFrame()) {
            _spell = null;
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

        _spell = null;
    }
}