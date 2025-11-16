using System;
using UnityEngine;

public class PlayerTester : MonoBehaviour {
    [SerializeField] private MovementSettings movementSettings;
    [SerializeField] private LookSettings lookSettings;
    [SerializeField] private GroundCheck groundCheck;
    [SerializeField] private Camera mainCamera;

    public float movementSpeed = 2;
    public float runSpeed = 5;

    private CharacterController _characterController;
    private float _velocityY;
    private Vector2 _currentRotation;
    private Vector2 _frameVelocity;

    private void Awake() {
        _characterController = GetComponent<CharacterController>();
    }

    private void Update() {
        Vector2 input = new(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));

        bool running = movementSettings.canRun && Input.GetKey(movementSettings.runningKey);

        ApplyMovement(input, running);
        TryJump();
        ProcessMouseInput();
    }

    private void ProcessMouseInput() {
        Vector2 mouseDelta = new(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
        Vector2 rawFrameVelocity = Vector2.Scale(mouseDelta, Vector2.one * lookSettings.sensitivity);

        _frameVelocity = Vector2.Lerp(_frameVelocity, rawFrameVelocity, 1 / lookSettings.smoothing);
        _currentRotation += _frameVelocity;
        _currentRotation.y = Mathf.Clamp(_currentRotation.y, lookSettings.yMin, lookSettings.yMax);

        mainCamera.transform.localRotation = Quaternion.AngleAxis(-_currentRotation.y, Vector3.right);
        transform.localRotation = Quaternion.AngleAxis(_currentRotation.x, Vector3.up);
    }

    private void ApplyMovement(Vector2 input, bool running) {
        float targetSpeed = running ? runSpeed : movementSpeed;
        float speedMultiplier = groundCheck.isGrounded ? 1f : movementSettings.flySpeedMultiplier;

        Vector3 moveDirection = transform.TransformDirection(new Vector3(
            input.x * targetSpeed * speedMultiplier,
            0f,
            input.y * targetSpeed * speedMultiplier
        ));

        ApplyGravity();
        _characterController.Move((moveDirection + Vector3.up * _velocityY) * Time.deltaTime);
    }

    private void TryJump() {
        if (Input.GetKeyDown(movementSettings.jumpKey) && groundCheck.isGrounded)
            PerformJump();
    }

    private void PerformJump() {
        _velocityY = Mathf.Sqrt(movementSettings.jumpStrength * -2f * movementSettings.gravity);
    }

    private void ApplyGravity() {
        if (groundCheck.isGrounded && _velocityY < 0)
            _velocityY = -2f;
        else
            _velocityY += movementSettings.gravity * (_velocityY < 0 ? movementSettings.fallGravityMultiplier : 1f) *
                          Time.deltaTime;
    }
}