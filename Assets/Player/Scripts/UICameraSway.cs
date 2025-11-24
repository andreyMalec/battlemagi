using System;
using UnityEngine;
using System.Collections.Generic;

public class UICameraSway : MonoBehaviour {
    [Header("Motion Settings")]
    [Tooltip("Масштаб смещения UI по горизонтали (X) в зависимости от yaw")] [SerializeField]
    float positionAmplitudeX = 20f;

    [Tooltip("Масштаб смещения UI по вертикали (Y) в зависимости от pitch")] [SerializeField]
    float positionAmplitudeY = 20f;

    [Tooltip("Масштаб наклона UI при горизонтальном повороте (yaw)")] [SerializeField]
    float rotationAmplitudeYaw = 5f;

    [Tooltip("Масштаб наклона UI при вертикальном повороте (pitch)")] [SerializeField]
    float rotationAmplitudePitch = 0f;

    [Tooltip("Насколько быстро UI догоняет целевое положение")] [SerializeField]
    float followSmoothTime = 0.1f;

    [Header("Filtering")]
    [Tooltip("Сглаживание оценки скорости yaw/pitch (чем больше, тем плавнее и более тяжёлая реакция)")]
    [SerializeField]
    float angularSmoothTime = 0.05f;

    Vector2 _lastViewRotation;
    Vector2 _viewAngularVelocity;
    Vector3 _uiVelocity;
    Vector3 _baseLocalPos;
    Quaternion _baseLocalRot;
    private RectTransform _uiContainer;
    private FirstPersonLook _firstPersonLook;

    public void Bind(RectTransform uiContainer) {
        _firstPersonLook = GetComponent<FirstPersonLook>();
        _uiContainer = uiContainer;
        _lastViewRotation = _firstPersonLook.ViewRotation;

        _baseLocalPos = _uiContainer.localPosition;
        _baseLocalRot = _uiContainer.localRotation;
    }

    void LateUpdate() {
        if (_uiContainer == null) return;
        UpdateAngularVelocityFromFirstPersonLook();
        UpdateUISway();
    }

    void UpdateAngularVelocityFromFirstPersonLook() {
        Vector2 currentView = _firstPersonLook.ViewRotation;
        Vector2 delta = currentView - _lastViewRotation;
        _lastViewRotation = currentView;

        float dt = Mathf.Max(Time.deltaTime, 0.0001f);

        Vector2 rawVelocity = delta / dt;

        float k = 1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(angularSmoothTime, 0.0001f));
        _viewAngularVelocity = Vector2.Lerp(_viewAngularVelocity, rawVelocity, k);
    }

    void UpdateUISway() {
        float maxAngularSpeed = 720f;
        Vector2 clamped = Vector2.ClampMagnitude(_viewAngularVelocity, maxAngularSpeed) / maxAngularSpeed;

        float yawSpeed = clamped.x;
        float pitchSpeed = clamped.y;

        Vector2 offset = new Vector2(
            -yawSpeed * positionAmplitudeX,
            -pitchSpeed * positionAmplitudeY
        );

        float rollYaw = yawSpeed * rotationAmplitudeYaw;
        float rollPitch = pitchSpeed * rotationAmplitudePitch;
        float roll = rollYaw + rollPitch;

        var targetLocalPos = _baseLocalPos + new Vector3(offset.x, offset.y, 0f);
        var targetLocalRot = _baseLocalRot * Quaternion.Euler(0f, 0f, roll);

        _uiContainer.localPosition = Vector3.SmoothDamp(
            _uiContainer.localPosition,
            targetLocalPos,
            ref _uiVelocity,
            followSmoothTime
        );

        _uiContainer.localRotation = Quaternion.Slerp(
            _uiContainer.localRotation,
            targetLocalRot,
            1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(followSmoothTime, 0.0001f))
        );
    }
}