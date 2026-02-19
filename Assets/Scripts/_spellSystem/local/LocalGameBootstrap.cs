using UnityEngine;

[DefaultExecutionOrder(-100)]
public class LocalGameBootstrap : MonoBehaviour {
    [SerializeField] private GameObject spellPrefab;

    private void Awake() {
        var (spellSystem, authority) = InitializeSpellSystem();
        GetComponent<SpellCaster>().Initialize(0, spellSystem, authority);
    }

    private (SpellSystem, IAuthorityService) InitializeSpellSystem() {
        IEntityManager manager = new LocalEntityManager(spellPrefab);
        IAuthorityService authority = new LocalAuthority();
        SpellSystemEvent spellSystemEvent = GetComponent<LocalSpellSystemEvent>();

        var spellSystem = new SpellSystem(manager, authority, spellSystemEvent);
        Debug.Log(
            $" Local SpellSystem initialized with manager={manager}, authority={authority}, spellSystemEvent={spellSystemEvent}");

        DI.Register(spellSystemEvent);
        DI.Register(manager);
        DI.Register(authority);
        DI.Register(spellSystem);

        return (spellSystem, authority);
    }
}