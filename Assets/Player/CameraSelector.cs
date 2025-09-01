using System;
using UnityEngine;

public class CameraSelector : MonoBehaviour {
    public GameObject firstPersonCamera;
    public GameObject thirdPersonCamera;
    public GameObject cloak;
    private SkinnedMeshRenderer renderer;

    private bool isFirstPerson = true;

    private void Start() {
        renderer = cloak.GetComponent<SkinnedMeshRenderer>();
    }

    private void Update() {
        if (Input.GetKeyDown(KeyCode.F5) && isFirstPerson) {
            isFirstPerson = false;
        } else if (Input.GetKeyDown(KeyCode.F5) && !isFirstPerson) {
            isFirstPerson = true;
        }

        firstPersonCamera.SetActive(isFirstPerson);
        thirdPersonCamera.SetActive(!isFirstPerson);

        if (firstPersonCamera.activeSelf)
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
        else
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
    }
}