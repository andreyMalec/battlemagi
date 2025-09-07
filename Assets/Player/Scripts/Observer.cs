using System;
using UnityEngine;

public class Observer : MonoBehaviour {
    [SerializeField] private LookSettings lookSettings;
    private Transform _target;
    private Camera _camera;

    private Vector2 _currentRotation;
    private Vector2 _frameVelocity;
    private Vector3 _positionVelocity;

    private void Awake() {
        _camera = GetComponentInChildren<Camera>();

        var movement = GetComponentInParent<FirstPersonMovement>();
        if (movement != null) {
            _target = movement.transform;
        }
    }

    private void OnEnable() {
        _camera.depth = 100;

        if (_target != null) {
            _camera.transform.LookAt(_target);
            SaveCurrentRotationToVariables();
        }
    }

    private void SaveCurrentRotationToVariables() {
        // Получаем текущие углы Эйлера
        Vector3 cameraEuler = _camera.transform.localEulerAngles;
        Vector3 observerEuler = _camera.transform.localEulerAngles;

        // Преобразуем в диапазон -180 до 180 градусов
        float cameraX = NormalizeAngle(cameraEuler.x);
        float observerY = NormalizeAngle(observerEuler.y);

        // Сохраняем в _currentRotation
        _currentRotation.x = observerY; // Горизонтальное вращение (Y)
        _currentRotation.y = -cameraX; // Вертикальное вращение (X, инвертируем)
    }

    private float NormalizeAngle(float angle) {
        // Приводим угол к диапазону -180 до 180 градусов
        angle %= 360;
        if (angle > 180) angle -= 360;
        if (angle < -180) angle += 360;
        return angle;
    }

    private void Update() {
        Vector2 mouseDelta = new(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
        Vector2 rawFrameVelocity = Vector2.Scale(mouseDelta, Vector2.one * lookSettings.sensitivity);

        _frameVelocity = Vector2.Lerp(_frameVelocity, rawFrameVelocity, 1 / lookSettings.smoothing);
        _currentRotation += _frameVelocity;
        _currentRotation.y = Mathf.Clamp(_currentRotation.y, lookSettings.yMin, lookSettings.yMax);

        _camera.transform.localRotation = Quaternion.AngleAxis(-_currentRotation.y, Vector3.right);
        transform.localRotation = Quaternion.AngleAxis(_currentRotation.x, Vector3.up);
    }
}