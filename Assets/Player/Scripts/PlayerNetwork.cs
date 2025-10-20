using System;
using Unity.Netcode;
using UnityEngine;

public class PlayerNetwork : NetworkBehaviour {
    private static readonly int OutlineColor = Shader.PropertyToID("OutlineColor");
    private static readonly int OutlineAlpha = Shader.PropertyToID("OutlineAlpha");

    [SerializeField] private Behaviour[] scriptsToDisable;
    [SerializeField] private GameObject[] objectsToDisable;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private MeshController meshController;

    private void Awake() {
        if (!TryGetComponent<Player>(out _))
            gameObject.AddComponent<Player>();
    }

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();

        gameObject.name = $"Player_{OwnerClientId}";

        if (IsOwner) {
            mainCamera.GetComponent<Camera>().depth = 100;
        } else {
            foreach (var script in scriptsToDisable) {
                script.enabled = false;
            }

            foreach (var obj in objectsToDisable) {
                obj.SetActive(false);
            }

            meshController.leftHand.weight = 0f;
            meshController.spine.weight *= 3f;
            mainCamera.GetComponent<Camera>().enabled = false;
        }
    }

    [ClientRpc]
    public void ApplyEffectColorClientRpc(Color color) {
        ApplyColor(prev => prev + color);
    }

    [ClientRpc]
    public void RemoveEffectColorClientRpc(Color color) {
        ApplyColor(prev => prev - color);
    }

    private void ApplyColor(Func<Color, Color> operation) {
        var materials = GetComponentInChildren<MeshBody>().GetComponent<SkinnedMeshRenderer>().materials;
        foreach (var material in materials) {
            if (!material.HasColor(OutlineColor)) continue;
            var prev = material.GetColor(OutlineColor);
            var next = operation.Invoke(prev);
            var alpha = 0f;
            if (next.a > 0)
                alpha = 1f;
            material.SetFloat(OutlineAlpha, alpha);
            material.SetColor(OutlineColor, next);
        }
    }
}