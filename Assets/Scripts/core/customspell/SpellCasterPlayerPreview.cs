using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

public class SpellCasterPlayerPreview : MonoBehaviour {
    private SpellCasterPlayer _caster;
    private ISpellSpawnPreview _spawnPreview;
    private SpellDefinition _spell;

    private void Awake() {
        _caster = GetComponent<SpellCasterPlayer>();
    }

    public void SetSpell([CanBeNull] SpellDefinition spell) {
        if (spell == null && _spell != null) {
            _spawnPreview?.Clear();
            _spawnPreview = null;
        }

        _spell = spell;

        UpdatePreview();
    }

    private void UpdatePreview() {
        if (_spell != null && _spawnPreview == null) {
            _spawnPreview = CreatePreview(_spell.spawn.preview);
            _spawnPreview.Show(new SpawnContext {
                spell = _spell,
                spawn = _spell.spawn,
                position = _caster.spawnPos.position,
                rotation = _caster.spawnPos.rotation,
                forward = _caster.spawnPos.forward,
                caster = _caster
            }, ISpellSpawn.GetMode(_spell.spawn.spawnMode));
        }

        _spawnPreview?.Update(new SpawnContext {
            spell = _spell,
            spawn = _spell.spawn,
            position = _caster.spawnPos.position,
            rotation = _caster.spawnPos.rotation,
            forward = _caster.spawnPos.forward,
            caster = _caster
        });
    }

    private static ISpellSpawnPreview CreatePreview(Preview previewFlags) {
        if (previewFlags == Preview.None)
            return new NonePreview();

        var list = new List<ISpellSpawnPreview>(4);

        if ((previewFlags & Preview.Mesh) != 0)
            list.Add(new MeshSpawnPreview());
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
}