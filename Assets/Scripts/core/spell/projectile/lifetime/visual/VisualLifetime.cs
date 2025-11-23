using Unity.Netcode;

public class VisualLifetime : NetworkBehaviour {
    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();

        GetComponent<SpellLifetime>().LifetimePercent += LifetimePercent;
    }

    protected virtual void LifetimePercent(float percent) {
    }
}