using Unity.Netcode;

public class VisualLifetime : NetworkBehaviour {
    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();

        GetComponent<OLDSpellLifetime>().LifetimePercent += LifetimePercent;
    }

    protected virtual void LifetimePercent(float percent) {
    }
}