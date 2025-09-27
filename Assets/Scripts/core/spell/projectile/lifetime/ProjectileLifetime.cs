using UnityEngine;

public class ProjectileLifetime : IProjectileLifetime {
    private readonly SpellData data;
    private readonly SpellProjectile projectile;
    private float currentLifeTime;

    public ProjectileLifetime(SpellProjectile p, SpellData d) {
        projectile = p;
        data = d;
    }

    public void Initialize() {
        currentLifeTime = 0f;
    }

    public void Tick() {
        currentLifeTime += Time.deltaTime;
        if (currentLifeTime >= data.lifeTime)
            Destroy();
    }

    public void Destroy() {
        projectile.DestroyProjectileServerRpc(projectile.NetworkObjectId);
    }
}