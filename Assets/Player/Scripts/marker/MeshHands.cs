using System;
using UnityEngine;

public class MeshHands : MonoBehaviour {
    private static readonly int Scale = Shader.PropertyToID("_Scale");
    public Transform invocation;
    public Transform rightHand;
    [SerializeField] private Renderer mesh;
    [SerializeField] private AudioSource clapSource;
    [SerializeField] private AudioClip[] claps;

    private MeshController _meshController;

    private void Awake() {
        foreach (var material in mesh.materials) {
            if (material.HasFloat(Scale)) {
                material.SetFloat(Scale, 0.00005f);
            }
        }
    }

    public void Bind(MeshController meshController) {
        _meshController = meshController;
    }

    public void Cast() {
        _meshController.OnAnimationCast();
    }

    public void Clap() {
        clapSource.Play(claps);
    }
}