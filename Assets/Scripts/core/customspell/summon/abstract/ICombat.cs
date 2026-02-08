public interface ICombat {
    bool CanAttack(AIContext ctx);
    void Attack(AIContext ctx, ITarget target);
}