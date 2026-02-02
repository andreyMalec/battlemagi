using System.Collections.Generic;
using UnityEngine;

public class SpellRunner : MonoBehaviour {
    public SpellDefinition def;
    public Transform spawnPos;
    public LayerMask playerLayer;
    private readonly List<SpellBind> _activeSpells = new();

    public void Cast(SpellBind spell) {
        _activeSpells.Add(spell);
    }

    public void BindAvatar(MeshController mc) {
        // spawnPos = mc.invocation;
    }

    void FixedUpdate() {
        if (Input.GetKeyDown(KeyCode.G)) {
            var s = SpellFactory.CreateProjectile(def, this, spawnPos.position, spawnPos.rotation, spawnPos.forward);
            Cast(s);
        }

        for (int i = _activeSpells.Count - 1; i >= 0; i--) {
            _activeSpells[i].Tick(Time.deltaTime);

            if (!_activeSpells[i].IsAlive)
                _activeSpells.RemoveAt(i);
        }
    }
}