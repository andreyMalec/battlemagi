using System;
using Unity.Netcode;
using UnityEngine;

public class Colorable : NetworkBehaviour {
    private static readonly int OutlineColor = Shader.PropertyToID("OutlineColor");
    private static readonly int OutlineAlpha = Shader.PropertyToID("OutlineAlpha");

    [ClientRpc]
    public void ApplyEffectColorClientRpc(Color color) {
        ApplyColor(prev => prev + color);
    }

    [ClientRpc]
    public void RemoveEffectColorClientRpc(Color color) {
        ApplyColor(prev => prev - color);
    }

    private void ApplyColor(Func<Color, Color> operation) {
        var bodyMats = GetComponentInChildren<MeshBody>().GetComponent<SkinnedMeshRenderer>().materials;
        foreach (var material in bodyMats) {
            ApplyColor(material, operation);
        }

        var hands = GetComponentInChildren<MeshHands>()?.GetComponentInChildren<SkinnedMeshRenderer>();
        if (hands != null) {
            foreach (var material in hands.materials) {
                ApplyColor(material, operation);
            }
        }
    }

    private void ApplyColor(Material material, Func<Color, Color> operation) {
        if (!material.HasColor(OutlineColor)) return;
        var prev = material.GetColor(OutlineColor);
        var next = operation.Invoke(prev);
        var alpha = 0f;
        if (next.a > 0)
            alpha = 1f;
        material.SetFloat(OutlineAlpha, alpha);
        material.SetColor(OutlineColor, next);
    }
}