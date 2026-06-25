using System;
using UnityEngine;

public class MeshHands : MonoBehaviour {
    private static readonly int Scale = Shader.PropertyToID("_Scale");
    [SerializeField] private Transform invocation;

    public void Bind() {
        GetComponentInParent<Player>().meshController.invocation = invocation;
        foreach (var material in GetComponentInChildren<SkinnedMeshRenderer>().materials) {
            if (material.HasFloat(Scale)) {
                material.SetFloat(Scale, 0.35f);
            }
        }
    }
}