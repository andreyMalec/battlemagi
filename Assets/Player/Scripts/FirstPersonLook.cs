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
    private Vector2 _velocity;
    private Vector2 _frameVelocity;

    // Сетевые переменные для синхронизации ТЕКУЩЕГО вращения
    private NetworkVariable<float> syncYaw = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    private NetworkVariable<float> syncPitch = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    // Текущие значения вращения (отдельно для владельца и синхронизации)
    private float _currentYaw = 0f;
    private float _currentPitch = 0f;

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();

        // Подписываемся на изменение сетевых переменных
        syncYaw.OnValueChanged += OnYawChanged;
        syncPitch.OnValueChanged += OnPitchChanged;

        // Применяем начальные значения
        ApplyRemoteRotation();
    }

    public override void OnNetworkDespawn() {
        base.OnNetworkDespawn();
        syncYaw.OnValueChanged -= OnYawChanged;
        syncPitch.OnValueChanged -= OnPitchChanged;
    }

    private void OnYawChanged(float previousValue, float newValue) {
        if (!IsOwner) {
            _currentYaw = newValue;
            ApplyRemoteRotation();
        }
    }

    private void OnPitchChanged(float previousValue, float newValue) {
        if (!IsOwner) {
            _currentPitch = newValue;
            ApplyRemoteRotation();
        }
    }

    void Update() {
        if (IsOwner) {
            HandleOwnerInput();
        } else {
            ApplyRemoteRotation();
        }
    }

    private void HandleOwnerInput() {
        // Обработка ввода мыши
        Vector2 mouseDelta = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
        Vector2 rawFrameVelocity = Vector2.Scale(mouseDelta, Vector2.one * sensitivity);
        _frameVelocity = Vector2.Lerp(_frameVelocity, rawFrameVelocity, 1 / smoothing);
        _velocity += _frameVelocity;

        // Ограничиваем pitch (вверх-вниз)
        _velocity.y = Mathf.Clamp(_velocity.y, yMin, yMax);

        // Сохраняем текущие значения вращения
        _currentYaw = _velocity.x;
        _currentPitch = _velocity.y;

        // Применяем вращение локально
        firstPersonCamera.localRotation = Quaternion.AngleAxis(-_currentPitch, Vector3.right);
        transform.localRotation = Quaternion.AngleAxis(_currentYaw, Vector3.up);

        // Синхронизируем ТЕКУЩЕЕ вращение (не скорость!)
        if (syncYaw.Value != _currentYaw) {
            syncYaw.Value = _currentYaw;
        }

        if (syncPitch.Value != _currentPitch) {
            syncPitch.Value = _currentPitch;
        }
    }

    private void ApplyRemoteRotation() {
        if (!IsOwner && IsSpawned) {
            // Применяем синхронизированное вращение
            transform.localRotation = Quaternion.AngleAxis(_currentYaw, Vector3.up);

            if (firstPersonCamera != null) {
                firstPersonCamera.localRotation = Quaternion.AngleAxis(-_currentPitch, Vector3.right);
            }
        }
    }

    void LateUpdate() {
        UpdateCameraPosition();
    }

    private void UpdateCameraPosition() {
        if (headBone == null || firstPersonCamera == null) return;

        // Позиция камеры следует за головой (выполняется на всех клиентах)
        Vector3 targetPosition = headBone.position + headBone.TransformDirection(offset);
        firstPersonCamera.position = Vector3.SmoothDamp(
            firstPersonCamera.position,
            targetPosition,
            ref _positionVelocity,
            positionSmoothTime);

        // Вращение камеры следует за головой
        Quaternion targetRotation = headBone.rotation * Quaternion.Euler(rotation);
        firstPersonCamera.rotation = Quaternion.Slerp(
            firstPersonCamera.rotation,
            targetRotation,
            rotationSmoothTime
        );
    }
}