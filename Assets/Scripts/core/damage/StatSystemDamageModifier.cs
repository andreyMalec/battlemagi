using UnityEngine;

public class StatSystemDamageModifier : MonoBehaviour, IDamageModifier {
    [SerializeField] private Stats stats;

    private void Awake() {
        if (stats == null)
            stats = GetComponent<Stats>();
    }

    public float ModifyIncoming(Damageable damageable, in DamageRequest request, float current) {
        if (stats == null) return current;
        Debug.Log($"Damage modifier applied: {current} -> {current * stats.GetFinal(StatType.DamageReduction)}");
        return current * stats.GetFinal(StatType.DamageReduction);
    }
}