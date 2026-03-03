using UnityEngine;

[DefaultExecutionOrder(-100)]
public class LocalGameBootstrap : MonoBehaviour, SpellBootstrap {
    private bool _initialized;

    public void Init(SpellCaster caster) {
        if (_initialized) return;

        var (spellSystem, authority) = InitializeSpellSystem();
        caster?.Initialize(0, spellSystem, authority);
        _initialized = true;
    }

    private (SpellSystem, IAuthorityService) InitializeSpellSystem() {
        IEntityManager manager = new LocalEntityManager();
        IAuthorityService authority = new LocalAuthority();

        var spellSystem = new SpellSystem(authority);
        Debug.Log(
            $" Local SpellSystem initialized with manager={manager}, authority={authority}");

        DI.Register(manager);
        DI.Register(authority);
        DI.Register(spellSystem);

        return (spellSystem, authority);
    }
}