using Unity.Netcode;
using UnityEngine;

public class FirstPersonLook : NetworkBehaviour {
    public Transform firstPersonCamera;
    [Header("Camera control")] public float sensitivity = 2;
    public float smoothing = 1.5f;

    [Header("Rotation Y Clamp")] public float yMin = -90f;
    public float yMax = 90f;

    [Header("Head following")] public Transform headBone;
    public float positionSmoothTime = 0.01f;
    public float rotationSmoothTime = 0.2f;

    public Vector3 offset;
    public Vector3 rotation;

    private Vector3 _positionVelocity;
    private Vector3 _refVelocity;
    private Vector2 _velocity;
    private Vector2 _frameVelocity;

    // Сетевые переменные для синхронизации вращения
    private NetworkVariable<float> pitchRotation = new NetworkVariable<float>(); // Вверх-вниз
    private NetworkVariable<float> yawRotation = new NetworkVariable<float>(); // Влево-вправо

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();

        // Подписываемся на изменение сетевых переменных
        yawRotation.OnValueChanged += OnYawRotationChanged;
        pitchRotation.OnValueChanged += OnPitchRotationChanged;

        // Применяем начальные значения
        ApplyRemoteRotation();
    }

    private void OnPitchRotationChanged(float previousValue, float newValue) {
        ApplyRemoteRotation();
    }

    private void OnYawRotationChanged(float previousValue, float newValue) {
        ApplyRemoteRotation();
    }

    void Update() {
        if (!IsOwner) return;

        Vector2 mouseDelta = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
        Vector2 rawFrameVelocity = Vector2.Scale(mouseDelta, Vector2.one * sensitivity);
        _frameVelocity = Vector2.Lerp(_frameVelocity, rawFrameVelocity, 1 / smoothing);
        _velocity += _frameVelocity;
        _velocity.y = Mathf.Clamp(_velocity.y, yMin, yMax);

        // Применяем вращение локально
        firstPersonCamera.localRotation = Quaternion.AngleAxis(-_velocity.y, Vector3.right);
        transform.localRotation = Quaternion.AngleAxis(_velocity.x, Vector3.up);

        // Отправляем оба угла на сервер
        UpdateRotationServerRpc(_velocity.x, _velocity.y);
    }

    [ServerRpc]
    private void UpdateRotationServerRpc(float yaw, float pitch) {
        yawRotation.Value = yaw;
        pitchRotation.Value = pitch;
    }

    private void ApplyRemoteRotation() {
        // Применяем вращение только для чужих игроков
        if (!IsOwner && IsSpawned) {
            transform.localRotation = Quaternion.AngleAxis(yawRotation.Value, Vector3.up);

            if (firstPersonCamera != null) {
                firstPersonCamera.localRotation = Quaternion.AngleAxis(-pitchRotation.Value, Vector3.right);
            }
        }
    }

    void LateUpdate() {
        Vector3 targetPosition = headBone.position + headBone.TransformDirection(offset);
        firstPersonCamera.position = Vector3.SmoothDamp(
            firstPersonCamera.position,
            targetPosition,
            ref _positionVelocity,
            positionSmoothTime);

        Quaternion targetRotation = headBone.rotation * Quaternion.Euler(rotation);
        firstPersonCamera.rotation =
            Quaternion.Slerp(firstPersonCamera.rotation, targetRotation, rotationSmoothTime);
    }
}