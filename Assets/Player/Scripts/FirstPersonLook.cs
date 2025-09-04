using Unity.Netcode;
using UnityEngine;

public class FirstPersonLook : NetworkBehaviour {
    [SerializeField] private LookSettings lookSettings;
    [SerializeField] private Transform firstPersonCamera;
    [SerializeField] private Transform headBone;

    // Сетевые переменные
    private readonly NetworkVariable<Vector2> syncRotation = new(
        Vector2.zero,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    private Vector2 currentRotation;
    private Vector2 frameVelocity;
    private Vector3 positionVelocity;

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        syncRotation.OnValueChanged += OnRotationChanged;

        if (!IsOwner)
            ApplyRemoteRotation();
    }

    public override void OnNetworkDespawn() {
        base.OnNetworkDespawn();
        syncRotation.OnValueChanged -= OnRotationChanged;
    }

    private void OnRotationChanged(Vector2 _, Vector2 newValue) {
        if (!IsOwner) {
            currentRotation = newValue;
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
        ProcessMouseInput();
        ApplyLocalRotation();
        SyncRotation();
    }

    private void ProcessMouseInput() {
        Vector2 mouseDelta = new(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
        Vector2 rawFrameVelocity = Vector2.Scale(mouseDelta, Vector2.one * lookSettings.sensitivity);

        frameVelocity = Vector2.Lerp(frameVelocity, rawFrameVelocity, 1 / lookSettings.smoothing);
        currentRotation += frameVelocity;
        currentRotation.y = Mathf.Clamp(currentRotation.y, lookSettings.yMin, lookSettings.yMax);
    }

    private void ApplyLocalRotation() {
        firstPersonCamera.localRotation = Quaternion.AngleAxis(-currentRotation.y, Vector3.right);
        transform.localRotation = Quaternion.AngleAxis(currentRotation.x, Vector3.up);
    }

    private void SyncRotation() {
        if (syncRotation.Value != currentRotation)
            syncRotation.Value = currentRotation;
    }

    private void ApplyRemoteRotation() {
        if (!IsOwner && IsSpawned) {
            transform.localRotation = Quaternion.AngleAxis(currentRotation.x, Vector3.up);
            firstPersonCamera.localRotation = Quaternion.AngleAxis(-currentRotation.y, Vector3.right);
        }
    }

    private void LateUpdate() => UpdateCameraPosition();

    private void UpdateCameraPosition() {
        if (headBone == null || firstPersonCamera == null) return;

        Vector3 targetPosition = headBone.position + headBone.TransformDirection(lookSettings.offset);
        firstPersonCamera.position = Vector3.SmoothDamp(
            firstPersonCamera.position,
            targetPosition,
            ref positionVelocity,
            lookSettings.positionSmoothTime
        );

        Quaternion targetRotation = headBone.rotation * Quaternion.Euler(lookSettings.rotation);
        firstPersonCamera.rotation = Quaternion.Slerp(
            firstPersonCamera.rotation,
            targetRotation,
            lookSettings.rotationSmoothTime
        );
    }
}