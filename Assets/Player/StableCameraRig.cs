using UnityEngine;

public class StableCameraRig : MonoBehaviour {
    public Transform headBone; // Цель - кость головы анимированной модели. Перетащите сюда объект "Head" из иерархии.
    public float positionSmoothTime = 0.01f; // Время сглаживания позиции (меньше = резче)
    public float rotationSmoothTime = 0.05f; // Время сглаживания поворота

    public Vector3 offset;
    public Vector3 rotation;
    private Vector3 _positionVelocity; // Служебная переменная для SmoothDamp
    private Vector3 _refVelocity; // Еще одна служебная переменная

    void LateUpdate() {
        if (headBone == null) {
            Debug.LogWarning("HeadBone not assigned to StableCameraRig!");
            return;
        }
        Vector3 targetPosition = headBone.position + headBone.TransformDirection(offset);
        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref _positionVelocity,
            positionSmoothTime);

        // 2. СГЛАЖИВАЕМ ПОВОРОТ: Плавно поворачиваем CameraRig к повороту головы
        // Quaternion.Slerp обеспечивает плавную сферическую интерполяцию вращения

        Quaternion targetRotation = headBone.rotation * Quaternion.Euler(rotation);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSmoothTime);
    }
}