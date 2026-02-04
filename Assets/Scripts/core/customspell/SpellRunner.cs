using UnityEngine;

public class SpellRunner : MonoBehaviour {
    public SpellDefinition def;
    public SpellDefinition def2;
    public Transform spawnPos;

    public Vector3 Direction => spawnPos.forward;

    void Update() {
        if (Input.GetKeyDown(KeyCode.E))
            SpellFactory.CreateProjectile(def, this, spawnPos.position, Direction, spawnPos.rotation);
        if (Input.GetKeyDown(KeyCode.R))
            SpellFactory.CreateBeam(def2, this, spawnPos.position, Direction, spawnPos.rotation);
    }
}