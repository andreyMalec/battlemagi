using System;
using System.Collections;
using UnityEngine;

public class PlayerAnimator : MonoBehaviour {
    private static readonly int VelocityZ = Animator.StringToHash("Velocity Z");
    private static readonly int VelocityX = Animator.StringToHash("Velocity X");
    private static readonly int VelocityAny = Animator.StringToHash("Velocity Any");
    private static readonly int JumpStart = Animator.StringToHash("Jump Start");
    private static readonly int FallStart = Animator.StringToHash("Fall Start");
    private static readonly int Invocation = Animator.StringToHash("Invocation");
    private static readonly int CastStart = Animator.StringToHash("Cast Start");
    private static readonly int CastCharge = Animator.StringToHash("Cast Charge");

    private static readonly float eps = 0.05f;
    public ParticleSystem chargingParticles;
    [Header("Animator")] public Animator animator;
    [Header("FirstPersonMovement")] public FirstPersonMovement movement;

    public float acceleration = 2f;
    public AnimationCurve decelerationCurve;

    private float velocityZ = 0f;
    private float velocityX = 0f;

    private bool isRunning => movement.IsRunning;
    private float maxVelocity => isRunning ? 2f : 0.5f;

    private bool jumpStart = false;
    private bool fallStart = false;
    private float lastPositionY;

    private void Start() {
        movement.Jumped += Jumped;
    }

    public void Casting(bool start, float charge) {
        animator.SetBool(CastStart, start);
        animator.SetFloat(CastCharge, charge);
        
        var emission = chargingParticles.emission;
        emission.rateOverTime = charge * 10f;
    }

    public IEnumerator CastSpell(SpellData spell) {
        animator.SetFloat(Invocation, spell.invocationIndex);
        yield return new WaitForSeconds(1f);
        animator.SetFloat(Invocation, 0);
    }

    private void Update() {
        animator.SetBool(JumpStart, jumpStart);
        animator.SetBool(FallStart, fallStart);

        if (fallStart)
            fallStart = false;

        if (lastPositionY - movement.rb.position.y > eps && !jumpStart && !fallStart &&
            !movement.groundCheck.isGrounded)
            fallStart = true;
        lastPositionY = movement.rb.position.y;

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

        animator.SetFloat(VelocityZ, velocityZ);
        animator.SetFloat(VelocityX, velocityX);
        animator.SetFloat(VelocityAny, (Math.Abs(velocityZ) + Math.Abs(velocityX)) / 2);

        // Debug.Log($"forward_Z: {velocityX}; right_X: {velocityZ} maxVelocity: {maxVelocity}");
    }

    private void Jumped() {
        jumpStart = true;
    }

    private float decelerate(float value) {
        return decelerationCurve.Evaluate(Math.Abs(value));
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
}