using System;
using UnityEngine;

public class CameraSelector : MonoBehaviour {
    public GameObject firstPersonCamera;
    public GameObject thirdPersonCamera;
    public GameObject cloak;
    private SkinnedMeshRenderer _renderer;

    private bool _isFirstPerson = true;

    private void Start() {
        _renderer = cloak.GetComponent<SkinnedMeshRenderer>();
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