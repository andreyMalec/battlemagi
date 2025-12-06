using System;
using UnityEngine;

public class CameraSelector : MonoBehaviour {
    public GameObject firstPersonCamera;
    public GameObject thirdPersonCamera;
    private SkinnedMeshRenderer _renderer;

    private bool _isFirstPerson = true;

    public void BindAvatar(MeshController mc) {
        if (mc == null || mc.cloak == null) {
            _renderer = null;
            return;
        }
        _renderer = mc.cloak.GetComponent<SkinnedMeshRenderer>();
    }

    private void Update() {
        if (Input.GetKeyDown(KeyCode.P) && _isFirstPerson) {
            _isFirstPerson = false;
        } else if (Input.GetKeyDown(KeyCode.P) && !_isFirstPerson) {
            _isFirstPerson = true;
        }

        firstPersonCamera.SetActive(_isFirstPerson);
        thirdPersonCamera.SetActive(!_isFirstPerson);

        if (_renderer == null) return;
        if (firstPersonCamera.activeSelf)
            _renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
        else
            _renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
    }
}