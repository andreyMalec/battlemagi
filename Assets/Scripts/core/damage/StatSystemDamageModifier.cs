using UnityEngine;

public class StatSystemDamageModifier : MonoBehaviour, IDamageModifier {
    [SerializeField] private NetworkStatSystem _stats;

    private void Awake() {
        if (_stats == null)
            _stats = GetComponent<NetworkStatSystem>();
    }

    public float ModifyIncoming(Damageable damageable, in DamageRequest request, float current) {
        if (_stats == null) return current;
        return current * _stats.Stats.GetFinal(StatType.DamageReduction);
    }
}

