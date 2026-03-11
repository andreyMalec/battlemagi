using UnityEngine;

public class StatSystemDamageModifier : MonoBehaviour, IDamageModifier {
    [SerializeField] private Stats stats;

    private void Awake() {
        if (stats == null)
            stats = GetComponent<Stats>();
    }

    public float ModifyIncoming(Damageable damageable, in DamageRequest request, float current) {
        if (stats == null) return current;
        return current * stats.GetFinal(StatType.DamageReduction);
    }
}

