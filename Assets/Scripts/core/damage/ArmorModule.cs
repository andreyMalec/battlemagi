using UnityEngine;

[System.Serializable]
public class ArmorModule : IDamageModule {
    [Min(0f)] public float maxArmor = 50f;
    [Range(0f, 1f)] public float armorEffect = 0.75f;

    public float Armor { get; private set; }

    public void Initialize(Damageable damageable, Stats stats) {
        Armor = 0f;
    }

    public void TickServer(float dt) {
    }

    public void TakeArmor(float amount) {
        if (amount <= 0f) return;
        Armor = Mathf.Clamp(Armor + amount, 0f, maxArmor);
    }

    public float ApplyArmorPart(in DamageRequest request, float incomingAfterModifiers) {
        if (Armor <= 0f) return 0f;
        var armorDamageTarget = incomingAfterModifiers * armorEffect;
        var armorDamageApplied = Mathf.Min(Armor, armorDamageTarget);
        Armor -= armorDamageApplied;
        return armorDamageApplied;
    }

    public void SetArmorServer(float value) {
        Armor = Mathf.Clamp(value, 0f, maxArmor);
    }
}