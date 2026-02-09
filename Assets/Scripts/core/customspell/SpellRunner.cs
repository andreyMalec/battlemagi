using System.Collections.Generic;
using UnityEngine;

public class SpellRunner : MonoBehaviour {
    public SpellDefinition def;
    public SpellDefinition def2;
    public SpellDefinition def3;
    public SpellDefinition def4;
    public Transform spawnPos;
    // public NetworkStatSystem statSystem;

    private SpellDefinition _spell;
    private ISpellSpawnPreview _spawnPreview;

    public Vector3 Direction => spawnPos.forward;

    void Update() {
        if (Input.GetKeyDown(KeyCode.E))
            _spell = def;
        if (Input.GetKeyDown(KeyCode.R))
            _spell = def2;
        if (Input.GetKeyDown(KeyCode.Q))
            _spell = def3;
        if (Input.GetKeyDown(KeyCode.F))
            _spell = def4;

        UpdatePreview();

        if (_spell != null && Input.GetKeyDown(KeyCode.Mouse0)) {
            RequestSpawn(_spell);
            _spell = null;
            _spawnPreview?.Clear();
            _spawnPreview = null;
        }

        if (_spell != null && Input.GetKeyDown(KeyCode.Mouse1)) {
            _spell = null;
            _spawnPreview?.Clear();
            _spawnPreview = null;
        }
    }

    private void UpdatePreview() {
        if (_spell != null && _spawnPreview == null) {
            _spawnPreview = CreatePreview(_spell.spawn.preview);
            _spawnPreview.Show(new SpawnContext {
                spell = _spell,
                spawn = _spell.spawn,
                position = spawnPos.position,
                rotation = spawnPos.rotation,
                forward = spawnPos.forward,
                caster = this
            }, ISpellSpawn.GetMode(_spell.spawn.spawnMode));
        }

        _spawnPreview?.Update(new SpawnContext {
            spell = _spell,
            spawn = _spell.spawn,
            position = spawnPos.position,
            rotation = spawnPos.rotation,
            forward = spawnPos.forward,
            caster = this
        });
    }

    private static ISpellSpawnPreview CreatePreview(Preview previewFlags) {
        if (previewFlags == Preview.None)
            return new NonePreview();

        var list = new List<ISpellSpawnPreview>(4);

        if ((previewFlags & Preview.Mesh) != 0)
            list.Add(new MeshSpawnPreview());
        if ((previewFlags & Preview.Sphere) != 0)
            list.Add(new NonePreview());
        if ((previewFlags & Preview.Line) != 0)
            list.Add(new LinePreview());
        if ((previewFlags & Preview.GroundPoint) != 0)
            list.Add(new GroundPointPreview());
        if ((previewFlags & Preview.Disk) != 0) {
            list.Add(new GroundRayPreview());
            list.Add(new DiskPreview());
        }

        return list.Count switch {
            0 => new NonePreview(),
            1 => list[0],
            _ => new CompositeSpawnPreview(list)
        };
    }

    private void Spawn(SpawnContext context, int index) {
        SpellFactory.CreateSpell(context);
    }

    private void RequestSpawn(SpellDefinition spell) {
        var context = new SpawnContext {
            spell = spell,
            spawn = spell.spawn,
            position = spawnPos.position,
            rotation = spawnPos.rotation,
            forward = spawnPos.forward,
            caster = this
        };
        ISpellSpawn spawn = ISpellSpawn.GetMode(spell.spawn.spawnMode);

        StartCoroutine(spawn!.Request(context, Spawn));
    }
}