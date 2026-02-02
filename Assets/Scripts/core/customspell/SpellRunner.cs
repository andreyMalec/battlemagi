using UnityEngine;

public class SpellRunner : MonoBehaviour {
    public SpellDefinition def;
    public Transform spawnPos;

    void Update() {
        if (Input.GetKeyDown(KeyCode.G))
            SpellFactory.CreateProjectile(def, this, spawnPos.position, spawnPos.rotation, spawnPos.forward);
    }
}