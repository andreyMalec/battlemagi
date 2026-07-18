using System;
using UnityEngine;

[CreateAssetMenu(fileName = "New Archetype", menuName = "Game/Archetype")]
public class ArchetypeData : ScriptableObject {
    public int id;
    public string archetypeName;
    public GameObject avatarPrefab;
    public GameObject avatarHandsPrefab;
    public Shader bodyShader;
    public Shader cloakShader;
    public SpellDefinition[] spells;
    public float maxHealth = 100f;
    public float maxMana = 125f;
    public float movementSpeed = 2f;
    public float runSpeed = 5f;
    public float healthRegen = 3f;
    public float manaRegen = 0.5f;
    public float jumpStrength = 2f;
    public ArchetypePassiveConfig passive = new();

    private void OnValidate() {
        var ddb = passive.distanceDamageBonus;
        if (ddb.maxDistance == 0)
            ddb.multiplierPerMeter = 0;
        else
            passive.distanceDamageBonus.multiplierPerMeter = ddb.maxMultiplier / ddb.maxDistance;
    }
}