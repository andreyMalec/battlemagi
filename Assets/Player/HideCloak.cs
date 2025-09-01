using System;
using UnityEngine;

public class HideCloak : MonoBehaviour {
    public GameObject firstPersonCamera;
    public GameObject cloak;
    private SkinnedMeshRenderer renderer;

    private void Start() {
        renderer = cloak.GetComponent<SkinnedMeshRenderer>();
    }

    private void Update() {
        if (firstPersonCamera.activeSelf)
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
        else
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
    }
}