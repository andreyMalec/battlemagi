using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(Collider))]
public class CTFFlagBase : NetworkBehaviour {
    [SerializeField] public TeamManager.Team team = TeamManager.Team.Red;
    [SerializeField] private Renderer[] colorRenderers;
    [SerializeField] private Material redMaterial;
    [SerializeField] private Material blueMaterial;

    private void Awake() {
        ApplyTeamMaterial();
    }

    private void ApplyTeamMaterial() {
        var mat = team == TeamManager.Team.Red ? redMaterial : blueMaterial;
        if (colorRenderers != null) {
            foreach (var r in colorRenderers) {
                if (r != null && mat != null) r.material = mat;
            }
        }
    }

    private void OnTriggerEnter(Collider other) {
        if (!IsServer) return;
        var no = other.GetComponentInParent<NetworkObject>();
        if (no == null || !no.IsPlayerObject) return;
        var clientId = no.OwnerClientId;
        var playerTeam = TeamManager.Instance.GetTeam(clientId);
        if (playerTeam != team) return; // Only allies can capture at their base

        // Check enemy flags being carried by this player
        foreach (var flag in CTFFlag.All) {
            if (flag == null) continue;
            if (flag.team == team) continue; // only enemy flags captured here
            if (flag.IsCarriedBy(clientId)) {
                flag.ReturnToBase();
                TeamManager.Instance.AddScore(team, 1);
                CTFAnnouncer.Instance?.CaptureFlag(team);
            }
        }
    }
}
