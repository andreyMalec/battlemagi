using UnityEngine;

[CreateAssetMenu(fileName = "New Archetype", menuName = "Game/Archetype")]
public class ArchetypeData : ScriptableObject {
    public int id;
    public string archetypeName;
    public GameObject avatarPrefab;
    public Shader bodyShader;
    public Shader cloakShader;
    public SpellData[] spells;
    public float maxHealth = 100f;
    public float maxMana = 125f;
    public float maxStamina = 3f;
    public float movementSpeed = 2f;
    public float runSpeed = 5f;
    public Vector3 cameraOffset;
}