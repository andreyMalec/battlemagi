using System;
using Unity.Netcode;
using UnityEngine;

public class FirstPersonLook : NetworkBehaviour {
    [SerializeField] private LookSettings lookSettings;
    [SerializeField] private Transform firstPersonCamera;
    private Transform headBone;

    // Сетевые переменные
    private readonly NetworkVariable<Vector2> _syncRotation = new(
        Vector2.zero,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    private Vector2 _currentRotation;
    private Vector2 _frameVelocity;
    private Vector3 _positionVelocity;
    [SerializeField] private Vector3 offset;

    public Vector2 ViewRotation => _currentRotation;

    public void BindAvatar(MeshController mc) {
        headBone = mc != null ? mc.head : null;
    }

    public void SetCameraOffset(Vector3 of) {
        offset = of;
    }

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        _syncRotation.OnValueChanged += OnRotationChanged;

        if (!IsOwner) {
            ApplyRemoteRotation();
            GetComponent<AudioListener>().enabled = false;
        }
    }

    public override void OnNetworkDespawn() {
        base.OnNetworkDespawn();
        _syncRotation.OnValueChanged -= OnRotationChanged;
    }

    private void OnRotationChanged(Vector2 _, Vector2 newValue) {
        if (!IsOwner) {
            _currentRotation = newValue;
            ApplyRemoteRotation();
        }
    }

    private void Update() {
        if (IsOwner)
            HandleOwnerInput();
        else
            ApplyRemoteRotation();
    }

    private void HandleOwnerInput() {
        if (Cursor.lockState != CursorLockMode.Locked) return;
        ProcessMouseInput();
        ApplyLocalRotation();
        SyncRotation();
    }

    private void ProcessMouseInput() {
        Vector2 mouseDelta = new(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
        Vector2 rawFrameVelocity = Vector2.Scale(mouseDelta, Vector2.one * lookSettings.sensitivity);

        _frameVelocity = Vector2.Lerp(_frameVelocity, rawFrameVelocity, 1 / lookSettings.smoothing);
        _currentRotation += _frameVelocity;
        _currentRotation.y = Mathf.Clamp(_currentRotation.y, lookSettings.yMin, lookSettings.yMax);
    }

    private void ApplyLocalRotation() {
        firstPersonCamera.localRotation = Quaternion.AngleAxis(-_currentRotation.y, Vector3.right);
        transform.localRotation = Quaternion.AngleAxis(_currentRotation.x, Vector3.up);
    }

    private void SyncRotation() {
        if (_syncRotation.Value != _currentRotation)
            _syncRotation.Value = _currentRotation;
    }

    private void ApplyRemoteRotation() {
        if (!IsOwner && IsSpawned) {
            transform.localRotation = Quaternion.AngleAxis(_currentRotation.x, Vector3.up);
            firstPersonCamera.localRotation = Quaternion.AngleAxis(-_currentRotation.y, Vector3.right);
        }
    }

    private void LateUpdate() => UpdateCameraPosition();

    private void UpdateCameraPosition() {
        if (headBone == null) return;

        Vector3 targetPosition = headBone.position + offset + headBone.TransformDirection(lookSettings.offset);
        firstPersonCamera.position = Vector3.SmoothDamp(
            firstPersonCamera.position,
            targetPosition,
            ref _positionVelocity,
            lookSettings.positionSmoothTime
        );

        // Quaternion targetRotation = headBone.rotation * Quaternion.Euler(lookSettings.rotation);
        // firstPersonCamera.rotation = Quaternion.Slerp(
        //     firstPersonCamera.rotation,
        //     targetRotation,
        //     lookSettings.rotationSmoothTime
        // );
    }

    public void ApplyInitialRotation(Quaternion worldRotation) {
        // worldRotation содержит "куда смотреть"
        // Берём yaw из Y и pitch = 0 (или можно рассчитать из камеры)
        Vector3 euler = worldRotation.eulerAngles;

        _currentRotation = new Vector2(euler.y, 0f);
        transform.rotation = Quaternion.Euler(0f, _currentRotation.x, 0f);
        firstPersonCamera.localRotation = Quaternion.Euler(-_currentRotation.y, 0f, 0f);

        // Сразу синкнем на сервере
        if (IsOwner)
            _syncRotation.Value = _currentRotation;
    }
}