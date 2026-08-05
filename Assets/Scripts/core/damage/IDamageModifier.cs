public interface IDamageModifier {
    float ModifyIncoming(Damageable damageable, ref DamageRequest request, float current);
}

