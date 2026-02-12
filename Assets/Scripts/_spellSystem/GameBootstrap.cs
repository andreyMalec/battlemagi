using UnityEngine;

public class GameBootstrap : MonoBehaviour {
    void Awake() {
        IEntityManager manager;
        IAuthorityService authority;

        if (GameConfig.Instance.useNetwork) {
            manager = new NgoEntityManager();
            authority = new NgoAuthority();
        } else {
            manager = new LocalEntityManager();
            authority = new LocalAuthority();
        }

        var spellSystem = new SpellSystem(manager, authority);

        DI.Register<IEntityManager>(manager);
        DI.Register<IAuthorityService>(authority);
        DI.Register(spellSystem);
    }
}