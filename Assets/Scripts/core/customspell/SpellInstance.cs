using UnityEngine;

public class SpellInstance : MonoBehaviour {
    private SpellBind _bind;

    public void Init(SpellBind bind) {
        _bind = bind;
    }

    void Update() {
        _bind.Tick(Time.deltaTime);

        if (!_bind.IsAlive)
            Destroy(gameObject);
    }
}
