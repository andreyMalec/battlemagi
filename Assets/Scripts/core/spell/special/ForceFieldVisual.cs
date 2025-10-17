using System;
using Unity.Netcode;
using UnityEngine;

public class ForceFieldVisual : MonoBehaviour {
    private static readonly int Alpha = Shader.PropertyToID("Alpha");
    private MeshRenderer _meshRenderer;

    private void Awake() {
        _meshRenderer = GetComponent<MeshRenderer>();
    }

    private void OnTriggerEnter(Collider other) {
        RenderBack(other, true);
    }

    private void OnTriggerExit(Collider other) {
        RenderBack(other, false);
    }

    private void RenderBack(Collider other, bool renderBack) {
        other.gameObject.TryGetComponent<FirstPersonMovement>(out var player);
        if (player == null) return;
        if (NetworkManager.Singleton.LocalClient.ClientId != player.OwnerClientId) return;
        foreach (var material in _meshRenderer.materials) {
            if (!material.HasFloat(Alpha)) continue;
            material.SetFloat(Alpha, renderBack ? 1f : 0f);
        }
    }
}