using System;
using UnityEngine;

public class CameraSelector : MonoBehaviour {
    public GameObject firstPersonCamera;
    public GameObject thirdPersonCamera;
    private SkinnedMeshRenderer _renderer;

    private bool _isFirstPerson = true;

    private void Awake() {
        _renderer = GetComponentInChildren<MeshController>().cloak.GetComponent<SkinnedMeshRenderer>();
    }

    private void Update() {
        if (Input.GetKeyDown(KeyCode.F5) && _isFirstPerson) {
            _isFirstPerson = false;
        } else if (Input.GetKeyDown(KeyCode.F5) && !_isFirstPerson) {
            _isFirstPerson = true;
        }

        firstPersonCamera.SetActive(_isFirstPerson);
        thirdPersonCamera.SetActive(!_isFirstPerson);

        if (firstPersonCamera.activeSelf)
            _renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
        else
            _renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
    }
}