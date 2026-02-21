using System;
using UnityEngine;

public abstract class SpellCaster : MonoBehaviour {
    public GameObject spellPrefab;

    public abstract bool CanCast { get; }

    public abstract Vector3 Origin { get; }
    public abstract Vector3 Direction { get; }

    public OwnerId OwnerId { get; private set; }
    public SpellSystem SpellSystem { get; private set; }
    public SpellSystemEvent SpellSystemEvent => SpellSystem.Event;
    public IAuthorityService Authority { get; private set; }

    private SpellCasterNet _casterNet;

    private void Awake() {
        _casterNet = GetComponent<SpellCasterNet>();
        if (_casterNet == null) {
            _casterNet = GetComponentInParent<SpellCasterNet>();
        }
    }

    public void Initialize(OwnerId ownerId, SpellSystem spellSystem, IAuthorityService authority) {
        SpellSystem = spellSystem;
        OwnerId = ownerId;
        Authority = authority;
        Debug.Log($"SpellCaster initialized, ownerId={ownerId}, spellSystem={spellSystem}");
    }

    /**
     * Ручной каст
     */
    public virtual void Cast(SpellDefinition spell) {
        if (_casterNet != null && _casterNet.IsSpawned) {
            _casterNet.RequestCast(spell);
            return;
        }

        var caster = this;
        var context = new SpawnContext {
            spell = spell,
            spawn = spell.spawn,
            position = caster.Origin,
            rotation = Quaternion.LookRotation(caster.Direction, Vector3.up),
            forward = caster.Direction,
            caster = caster
        };
        var spellSpawn = ISpellSpawn.GetMode(spell.spawn.spawnMode);
        StartCoroutine(spellSpawn!.Request(context, SpawnMain));
    }

    /**
     * Каст вложенного спелла (например, при срабатывании SpawnOnEventAction)
     */
    public void Spawn(SpawnContext context) {
        if (_casterNet != null && _casterNet.IsSpawned) {
            _casterNet.RequestSpawn(context);
            return;
        }

        context.branch = true;
        SpawnMain(context);
    }

    /**
     * Каст по цели (наводка из суммона)
     */
    public virtual void Cast(SpellDefinition spell, ITarget target) {
        throw new System.NotImplementedException("TODO");
    }

    private void SpawnMain(SpawnContext context) {
        var main = Instantiate(spellPrefab, context.position, context.rotation);
        SpellSystem.CastSpell(context with { main = main });
    }
}