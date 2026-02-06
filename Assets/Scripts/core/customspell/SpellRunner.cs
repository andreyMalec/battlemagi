using System;
using UnityEngine;

public class SpellRunner : MonoBehaviour {
    public SpellDefinition def;
    public SpellDefinition def2;
    public Transform spawnPos;
    public NetworkStatSystem statSystem;

    public Vector3 Direction => spawnPos.forward;

    private void Awake() {
        statSystem = GetComponent<NetworkStatSystem>();
    }

    void Update() {
        if (Input.GetKeyDown(KeyCode.E))
            RequestSpawn(def);
        if (Input.GetKeyDown(KeyCode.R))
            RequestSpawn(def2);
    }

    private void Spawn(SpawnContext context, int index) {
        switch (context.spell.coreType) {
            case CoreType.Projectile:
                SpellFactory.CreateProjectile(context);
                break;
            case CoreType.Zone:
                SpellFactory.CreateZone(context);
                break;
            case CoreType.Beam:
                SpellFactory.CreateBeam(context);
                break;
        }
    }

    private void RequestSpawn(SpellDefinition spell) {
        var context = new SpawnContext {
            spell = spell,
            data = spell.spawn,
            position = spawnPos.position,
            rotation = spawnPos.rotation,
            forward = spawnPos.forward,
            caster = this
        };
        ISpellSpawn spawn = null;
        switch (spell.spawn.spawnMode) {
            case SpawnMode.Direct:
                spawn = new NewDirectSpawn(spell.spawn.multiInstanceDelay);
                break;
        }

        StartCoroutine(spawn!.Request(context, Spawn));
    }
}