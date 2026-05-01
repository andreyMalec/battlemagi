public interface IDamageModifier {
    float ModifyIncoming(Damageable damageable, in DamageRequest request, float current);
}

