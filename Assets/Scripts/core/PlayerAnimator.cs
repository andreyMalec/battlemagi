using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerAnimator : NetworkBehaviour {
    private static readonly int VelocityZ = Animator.StringToHash("Velocity Z");
    private static readonly int VelocityX = Animator.StringToHash("Velocity X");
    private static readonly int VelocityAny = Animator.StringToHash("Velocity Any");
    private static readonly int JumpStart = Animator.StringToHash("Jump Start");
    private static readonly int FallStart = Animator.StringToHash("Fall Start");
    private static readonly int Invocation = Animator.StringToHash("Invocation");
    private static readonly int CastStart = Animator.StringToHash("Cast Start");
    private static readonly int CastCharge = Animator.StringToHash("Cast Charge");
    private static readonly int CastWaiting = Animator.StringToHash("Cast Waiting");
    private static readonly int Channeling = Animator.StringToHash("Channeling");

    private static readonly float eps = 0.05f;
    public Transform ikHand;
    public PlayerNetwork network;

    [Header("FirstPersonMovement")]
    public FirstPersonMovement movement;

    public float acceleration = 2f;
    public AnimationCurve decelerationCurve;

    private float velocityZ = 0f;
    private float velocityX = 0f;

    private bool isRunning => movement.IsRunning;
    private float maxVelocity => (isRunning ? 2f : 0.5f) * movement.globalSpeedMultiplier.Value;

    private bool jumpStart = false;
    private bool fallStart = false;
    private float lastPositionY;

    private ParticleSystem chargingParticles;
    private AudioSource chargingAudio;

    private NetworkVariable<float> castCharge = new(0, NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner);

    private Vector3 ikPos;
    private Quaternion ikRot;

    private void Awake() {
        chargingParticles = GetComponentInChildren<MeshController>().invocation;
        chargingAudio = GetComponent<AudioSource>();
    }

    private void Start() {
        if (!IsOwner) return;

        movement.Jumped += Jumped;
        ikPos = ikHand.localPosition;
        ikRot = ikHand.localRotation;
    }

    public void TriggerChanneling() {
        network.AnimateTrigger(Channeling);
    }

    public void CastWaitingAnim(bool waiting) {
        network.AnimateBool(CastWaiting, waiting);
        if (waiting) {
            ikHand.localPosition = new Vector3(-0.55f, -0.24f, 0.44f);
            ikHand.localRotation = Quaternion.Euler(0, -90, 0);
        } else {
            ikHand.localPosition = ikPos;
            ikHand.localRotation = ikRot;
        }
    }

    private void OnEnable() {
        castCharge.OnValueChanged += OnEffectChanged;
    }

    private void OnDisable() {
        castCharge.OnValueChanged -= OnEffectChanged;
    }

    private void OnEffectChanged(float oldValue, float newValue) {
        var emission = chargingParticles.emission;
        emission.rateOverTime = newValue;
        chargingAudio.volume = Math.Clamp(newValue / 200, 0, 0.15f);
    }

    public IEnumerator CastSpell(SpellData spell) {
        network.AnimateFloat(Invocation, spell.invocationIndex);
        yield return new WaitForSeconds(spell.castTime);
        network.AnimateFloat(Invocation, 0);
    }

    private void Update() {
        if (!IsOwner) return;

        network.AnimateBool(JumpStart, jumpStart);
        network.AnimateBool(FallStart, fallStart);

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

        network.AnimateFloat(VelocityZ, velocityZ);
        network.AnimateFloat(VelocityX, velocityX);
        network.AnimateFloat(VelocityAny, (Math.Abs(velocityZ) + Math.Abs(velocityX)) / 2);

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