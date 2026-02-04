using UnityEngine;

public class SpellRunner : MonoBehaviour {
    public SpellDefinition def;
    public Transform spawnPos;

    public Vector3 Direction => spawnPos.forward;

    void Update() {
        if (Input.GetKeyDown(KeyCode.E))
            SpellFactory.CreateProjectile(def, this, spawnPos.position, spawnPos.rotation);
    }
}