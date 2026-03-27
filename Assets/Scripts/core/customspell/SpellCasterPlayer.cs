using System.Collections.Generic;
using UnityEngine;

public class SpellCasterPlayer : SpellCaster {
    public Transform spawnPos;

    [SerializeField] private PlayerSpellInput input = new();
    [SerializeField] private ManaModule mana = new();
    [SerializeField] private MonoBehaviour _bridge;

    private ISpellCasterBridge _bridgeTyped;
    private Stats _stats;

    private SpellDefinition _spell;
    private ISpellSpawnPreview _spawnPreview;

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

        if (_bridge != null)
            _bridgeTyped = (ISpellCasterBridge)_bridge;
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

        UpdatePreview();

        if (_spell != null && input.CastPressedThisFrame()) {
            if (!mana.IsPrimalManaLocked(_spell, EchoCount)) {
                if (mana.CanSpendForCast(_spell, EchoCount)) {
                    mana.SpendManaServer(mana.CostForCast(_spell));
                } else {
                    mana.AddPrimalManaServer(mana.PrimalManaMissing(mana.CostForCast(_spell)));
                }

                Cast(_spell);

                _spell = null;
                _spawnPreview?.Clear();
                _spawnPreview = null;
            } else {
                // TODO some feedback for primal mana locked
            }
        }

        if (_spell != null && input.CancelPressedThisFrame()) {
            _spell = null;
            _spawnPreview?.Clear();
            _spawnPreview = null;
        }
    }

    private void UpdatePreview() {
        if (_spell != null && _spawnPreview == null) {
            _spawnPreview = CreatePreview(_spell.spawn.preview);
            _spawnPreview.Show(new SpawnContext {
                spell = _spell,
                spawn = _spell.spawn,
                position = spawnPos.position,
                rotation = spawnPos.rotation,
                forward = spawnPos.forward,
                caster = this
            }, ISpellSpawn.GetMode(_spell.spawn.spawnMode));
        }

        _spawnPreview?.Update(new SpawnContext {
            spell = _spell,
            spawn = _spell.spawn,
            position = spawnPos.position,
            rotation = spawnPos.rotation,
            forward = spawnPos.forward,
            caster = this
        });
    }

    private static ISpellSpawnPreview CreatePreview(Preview previewFlags) {
        if (previewFlags == Preview.None)
            return new NonePreview();

        var list = new List<ISpellSpawnPreview>(4);

        if ((previewFlags & Preview.Mesh) != 0)
            list.Add(new MeshSpawnPreview());
        if ((previewFlags & Preview.Line) != 0)
            list.Add(new LinePreview());
        if ((previewFlags & Preview.GroundPoint) != 0)
            list.Add(new GroundPointPreview());
        if ((previewFlags & Preview.Disk) != 0) {
            list.Add(new GroundRayPreview());
            list.Add(new DiskPreview());
        }

        return list.Count switch {
            0 => new NonePreview(),
            1 => list[0],
            _ => new CompositeSpawnPreview(list)
        };
    }
}