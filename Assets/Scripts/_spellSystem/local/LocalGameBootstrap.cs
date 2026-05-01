using System;
using System.Collections;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class LocalGameBootstrap : MonoBehaviour, SpellBootstrap {
    [SerializeField] private ulong ownerId;
    private bool _initialized;

    private void Awake() {
        StartCoroutine(Start());
    }

    public IEnumerator Start() {
        yield return new WaitForEndOfFrame();
        var caster = GetComponentInChildren<SpellCaster>();
        Init(caster);
    }

    public void Init(SpellCaster caster) {
        if (_initialized) return;

        var (spellSystem, authority) = InitializeSpellSystem();
        caster?.Initialize(ownerId, spellSystem, authority);
        _initialized = true;
        Debug.Log($"Local SpellSystem initialized for [{caster}], ownerId={ownerId}");
    }

    private (SpellSystem, IAuthorityService) InitializeSpellSystem() {
        IEntityManager manager = new LocalEntityManager();
        IAuthorityService authority = new LocalAuthority(ownerId);

        var spellSystem = new SpellSystem(authority);

        DI.Register(manager);
        DI.Register(authority);
        DI.Register(spellSystem);

        return (spellSystem, authority);
    }
}