using System;
using System.Collections;
using UnityEngine;

public class FirstPersonMovement : MonoBehaviour {
    public float speed = 4;

    [Header("Running")] public bool canRun = true;
    public bool IsRunning { get; private set; }
    public float runSpeed = 8;
    public float jumpDelay = 0.5f;
    public float jumpCooldown = 1f;
    public KeyCode runningKey = KeyCode.LeftShift;
    public KeyCode jumpKey = KeyCode.Space;

    public float jumpStrength = 2;
    public event Action Jumped;

    public GroundCheck groundCheck;
    public Rigidbody rb;

    private float jumpCooldownTimer = 0f;

    private void Start() {
        groundCheck.Grounded += Grounded;
    }

    void FixedUpdate() {
        IsRunning = canRun && Input.GetKey(runningKey);

        float targetMovingSpeed = IsRunning ? runSpeed : speed;

        var forwardZ = Input.GetAxis("Vertical");
        var rightX = Input.GetAxis("Horizontal");

        var v = new Vector2(rightX, forwardZ).normalized * targetMovingSpeed;

        rb.linearVelocity = transform.rotation * new Vector3(v.x, rb.linearVelocity.y, v.y);
    }

    void Update() {
        if (jumpCooldownTimer > 0) {
            jumpCooldownTimer -= Time.deltaTime;
        }

        if (Input.GetKeyDown(jumpKey) && groundCheck.isGrounded && jumpCooldownTimer <= 0) {
            jumpCooldownTimer = jumpCooldown;
            StartCoroutine(PerformJump());
        }
    }

    private void Grounded() {
    }

    private IEnumerator PerformJump() {
        Jumped?.Invoke();
        yield return new WaitForSeconds(jumpDelay);
        rb.AddForce(Vector3.up * (100 * jumpStrength));
    }
}