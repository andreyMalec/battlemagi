using System.Collections.Generic;
using UnityEngine;

public abstract class SpellCaster : MonoBehaviour, ITarget {
    public static readonly List<SpellCaster> Active = new();

    public abstract bool CanCast { get; }

    public abstract Vector3 Origin { get; }
    public abstract Vector3 Direction { get; }

    public OwnerId OwnerId { get; private set; }
    public ulong ObjectId => Authority.ObjectId;
    public SpellSystem SpellSystem { get; private set; }
    public IAuthorityService Authority { get; private set; }

    private bool _useNetwork;
    private SpellCasterNet _casterNet;

    public Vector3 Position => transform.position;
    public abstract bool IsPlayer { get; }
    public abstract bool IsSpell { get; }
    public GameObject Get => gameObject;

    protected Coroutine CastCoroutine => _useNetwork ? _casterNet.CastCoroutine : _castCoroutine;
    private Coroutine _castCoroutine;

    protected virtual void Awake() {
        _casterNet = GetComponentInParent<SpellCasterNet>();
        _useNetwork = _casterNet != null;

        Active.Add(this);
    }

    private void OnDestroy() {
        Active.Remove(this);
    }

    public void Initialize(
        OwnerId ownerId,
        SpellSystem spellSystem,
        IAuthorityService authority
    ) {
        SpellSystem = spellSystem;
        OwnerId = ownerId;
        Authority = authority;
        Debug.Log($"{gameObject.name} initialized, ownerId={ownerId}, spellSystem={spellSystem}");
    }

    /**
     * Ручной каст
     */
    public virtual void Cast(SpellDefinition spell) {
        if (_useNetwork && _casterNet.IsSpawned) {
            _casterNet.RequestCast(CastContext(spell));
            return;
        }

        Debug.Log($"{gameObject.name} Cast = {spell.words}");
        var spellSpawn = ISpellSpawn.GetMode(spell.spawn.spawnMode);
        _castCoroutine = StartCoroutine(spellSpawn!.Request(CastContext(spell), SpawnMain));
    }

    public virtual SpawnContext CastContext(SpellDefinition spell) {
        var caster = this;
        var context = new SpawnContext {
            spell = spell,
            spawn = spell.spawn,
            position = caster.Origin,
            rotation = Quaternion.LookRotation(caster.Direction, Vector3.up),
            forward = caster.Direction,
            caster = caster
        };
        return context;
    }

    /**
     * Каст вложенного спелла (например, при срабатывании SpawnOnEventAction)
     */
    public void Spawn(SpawnContext context) {
        if (_useNetwork && _casterNet.IsSpawned) {
            _casterNet.RequestSpawn(context);
            return;
        }

        Debug.Log($"{gameObject.name} Spawn = {context.spell.coreType}");
        context.branch = true;
        var spellSpawn = ISpellSpawn.GetMode(context.spawn.spawnMode);
        StartCoroutine(spellSpawn!.Request(context, SpawnMain));
    }

    /**
     * Каст по цели (наводка из суммона)
     */
    public virtual void Cast(SpellDefinition spell, ITarget target) {
        if (_useNetwork && _casterNet.IsSpawned) {
            var castContext = CastContext(spell);
            castContext.target = target;
            _casterNet.RequestCast(castContext);
            return;
        }

        Debug.Log($"{gameObject.name} Cast2 = {spell.coreType}");
        var spellSpawn = ISpellSpawn.GetMode(spell.spawn.spawnMode);
        var context = CastContext(spell);
        context.target = target;
        StartCoroutine(spellSpawn!.Request(context, SpawnMain));
    }

    private void SpawnMain(SpawnContext context) {
        var prefab = SpellPrefab.Instance.GetPrefab(_useNetwork);
        var main = Instantiate(prefab, context.position, context.rotation);
        HandleSpellLimit(context.spell, main);
        SpellSystem.CastSpell(context with { main = main });
        Debug.Log($"{gameObject.name} SpawnMain = {context.spell.coreType}, main={main.name}");
    }

    public void HandleSpellLimit(SpellDefinition spell, GameObject target) {
        if (spell.spawn.instanceLimit > 0) {
            var removed = SpellInstanceLimiter.Register(OwnerId, spell, target);
            foreach (var old in removed) {
                if (old == null) continue;
                var view = old.GetComponentInChildren<SpellView>();
                var instance = old.GetComponentInChildren<SpellInstance>();
                view.Kill(instance.Bind.Context);
            }
        }
    }
}