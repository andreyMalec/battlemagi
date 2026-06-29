using System;
using UnityEngine;

public class MeshHands : MonoBehaviour {
    private static readonly int Scale = Shader.PropertyToID("_Scale");
    [SerializeField] private Transform invocation;
    [SerializeField] private Renderer mesh;

    private void Awake() {
        foreach (var material in mesh.materials) {
            if (material.HasFloat(Scale)) {
                material.SetFloat(Scale, 0.35f);
            }
        }
    }

    public void Bind() {
        GetComponentInParent<Player>().meshController.invocation = invocation;
    }
}