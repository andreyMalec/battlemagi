using System;
using UnityEngine;

[CreateAssetMenu(fileName = "SpellPrefabDatabase", menuName = "Spells/Spell Prefab Database")]
public class SpellPrefabDatabase : ScriptableObject {
    [Serializable]
    public struct ProjectileEntry {
        public SpellProjectilePrefabId id;
        public GameObject prefab;
    }

    [Serializable]
    public struct ZoneEntry {
        public SpellZonePrefabId id;
        public GameObject prefab;
    }

    [Serializable]
    public struct BeamEntry {
        public SpellBeamPrefabId id;
        public GameObject prefab;
    }

    [Serializable]
    public struct SummonEntry {
        public SpellSummonPrefabId id;
        public GameObject prefab;
    }

    public ProjectileEntry[] projectiles;
    public ZoneEntry[] zones;
    public BeamEntry[] beams;
    public SummonEntry[] summons;

    public GameObject Get(SpellDefinition def) {
        return def.coreType switch {
            CoreType.Projectile => Get(def.projectile.prefabId),
            CoreType.Zone => Get(def.zone.prefabId),
            CoreType.Beam => Get(def.beam.prefabId),
            CoreType.Summon => Get(def.summon.prefabId),
            _ => null
        };
    }

    public GameObject Get(SpellProjectilePrefabId id) {
        for (int i = 0; i < projectiles.Length; i++) {
            if (projectiles[i].id == id)
                return projectiles[i].prefab;
        }

        return null;
    }

    public GameObject Get(SpellZonePrefabId id) {
        for (int i = 0; i < zones.Length; i++) {
            if (zones[i].id == id)
                return zones[i].prefab;
        }

        return null;
    }

    public GameObject Get(SpellBeamPrefabId id) {
        for (int i = 0; i < beams.Length; i++) {
            if (beams[i].id == id)
                return beams[i].prefab;
        }

        return null;
    }

    public GameObject Get(SpellSummonPrefabId id) {
        for (int i = 0; i < summons.Length; i++) {
            if (summons[i].id == id)
                return summons[i].prefab;
        }

        return null;
    }

    private static SpellPrefabDatabase _instance;

    public static SpellPrefabDatabase Instance {
        get {
            if (_instance == null)
                _instance = Resources.Load<SpellPrefabDatabase>("SpellPrefabDatabase");
            return _instance;
        }
    }
}