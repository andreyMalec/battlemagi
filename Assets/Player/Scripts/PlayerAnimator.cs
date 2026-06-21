using System;
using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

[RequireComponent(typeof(FirstPersonMovement))]
[RequireComponent(typeof(Player))]
public class PlayerAnimator : NetworkBehaviour {
    private static readonly int VelocityZ = Animator.StringToHash("Velocity Z");
    private static readonly int VelocityX = Animator.StringToHash("Velocity X");
    private static readonly int VelocityAny = Animator.StringToHash("Velocity Any");
    private static readonly int JumpStart = Animator.StringToHash("Jump Start");
    private static readonly int FallStart = Animator.StringToHash("Fall Start");

    private static readonly float eps = 0.05f;
    public Transform ikHand;
    public Transform ikHandRight;

    [HideInInspector] public Animator animator;
    [HideInInspector] public Animator secondaryAnimator;
    [HideInInspector] public NetworkAnimator networkAnimator;
    [HideInInspector] public MeshController meshController;
    private FirstPersonMovement movement;
    private Stats _stats;

    public float acceleration = 3f;
    public AnimationCurve decelerationCurve;

    private float velocityZ = 0f;
    private float velocityX = 0f;

    private bool isRunning => movement.IsRunning;

    // private float maxVelocity => (2f) * statSystem.Stats.GetFinal(StatType.MoveSpeed);
    private float maxVelocity => (isRunning ? 0.5f : 2f) * _stats?.GetFinal(StatType.MoveSpeed) ?? 1f;

    private bool jumpStart = false;
    private bool fallStart = false;
    private float lastPositionY;

    private Vector3 ikPos;
    private Quaternion ikRot;

    public override void OnNetworkSpawn() {
        movement = GetComponent<FirstPersonMovement>();
        _stats = GetComponent<Stats>();
    }

    private void Start() {
        if (!IsOwner) return;

        movement.Jumped += Jumped;
        ikPos = ikHand.localPosition;
        ikRot = ikHand.localRotation;
    }

    private void Update() {
        if (!IsOwner) return;

        AnimateBool(JumpStart, jumpStart);
        AnimateBool(FallStart, fallStart);

        if (fallStart)
            fallStart = false;

        if (lastPositionY - movement.transform.position.y > eps
            && !jumpStart
            && !fallStart
            && !movement.groundCheck.isGrounded)
            fallStart = true;
        lastPositionY = movement.transform.position.y;

        if (jumpStart && movement.groundCheck.isGrounded)
            jumpStart = false;

        var forward = Input.GetKey(KeyCode.W);
        var backward = Input.GetKey(KeyCode.S);
        var left = Input.GetKey(KeyCode.A);
        var right = Input.GetKey(KeyCode.D);

        if (!forward && !backward && velocityZ != 0f && velocityZ > -0.05f && velocityZ < 0.05f) {
            velocityZ = 0;
        }

        if (!left && !right && velocityX != 0f && velocityX > -0.05f && velocityX < 0.05f) {
            velocityX = 0;
        }

        velocityZ = applyPositive(forward, velocityZ);
        velocityZ = applyNegative(backward, velocityZ);

        velocityX = applyPositive(right, velocityX);
        velocityX = applyNegative(left, velocityX);

        AnimateFloat(VelocityZ, velocityZ);
        AnimateFloat(VelocityX, velocityX);
        AnimateFloat(VelocityAny, (Math.Abs(velocityZ) + Math.Abs(velocityX)) / 2);
    }

    private void Jumped() {
        jumpStart = true;
    }

    private float decelerate(float value) {
        return decelerationCurve.Evaluate(Math.Abs(value)) * 1.5f;
    }

    private float applyPositive(bool keyPressed, float velocity) {
        if (keyPressed && velocity < maxVelocity) {
            velocity += acceleration * Time.deltaTime;
        }

        if (!keyPressed && velocity > 0f) {
            velocity -= decelerate(velocity) * Time.deltaTime;
        }

        if (keyPressed && isRunning && velocity > maxVelocity) {
            velocity = maxVelocity;
        } else if (keyPressed && velocity > maxVelocity) {
            velocity -= decelerate(velocity) * Time.deltaTime;
            if (velocity > maxVelocity && velocity < (maxVelocity + eps)) {
                velocity = maxVelocity;
            }
        } else if (keyPressed && velocity < maxVelocity && velocity > (maxVelocity - eps)) {
            velocity = maxVelocity;
        }

        return velocity;
    }

    private float applyNegative(bool keyPressed, float velocity) {
        if (keyPressed && velocity > -maxVelocity) {
            velocity -= acceleration * Time.deltaTime;
        }

        if (!keyPressed && velocity < 0f) {
            velocity += decelerate(velocity) * Time.deltaTime;
        }

        if (keyPressed && isRunning && velocity < -maxVelocity) {
            velocity = -maxVelocity;
        } else if (keyPressed && velocity < -maxVelocity) {
            velocity += decelerate(velocity) * Time.deltaTime;
            if (velocity < -maxVelocity && velocity > (-maxVelocity - eps)) {
                velocity = -maxVelocity;
            }
        } else if (keyPressed && velocity > -maxVelocity && velocity < (-maxVelocity + eps)) {
            velocity = -maxVelocity;
        }

        return velocity;
    }

    public void AnimateBool(int key, bool value) {
        if (IsOwner) {
            animator.SetBool(key, value);
            secondaryAnimator?.SetBool(key, value);
        }
    }

    public void AnimateFloat(int key, float value) {
        if (IsOwner) {
            animator.SetFloat(key, value);
            secondaryAnimator?.SetFloat(key, value);
        }
    }

    public void AnimateTrigger(int key) {
        if (IsOwner) {
            networkAnimator.SetTrigger(key);
            secondaryAnimator?.SetTrigger(key);
        }
    }
}