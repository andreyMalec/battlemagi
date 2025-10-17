using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(FirstPersonMovement))]
[RequireComponent(typeof(NetworkStatSystem))]
[RequireComponent(typeof(PlayerNetwork))]
public class PlayerAnimator : NetworkBehaviour {
    private static readonly int VelocityZ = Animator.StringToHash("Velocity Z");
    private static readonly int VelocityX = Animator.StringToHash("Velocity X");
    private static readonly int VelocityAny = Animator.StringToHash("Velocity Any");
    private static readonly int JumpStart = Animator.StringToHash("Jump Start");
    private static readonly int FallStart = Animator.StringToHash("Fall Start");
    private static readonly int Invocation = Animator.StringToHash("Invocation");
    private static readonly int CastWaiting = Animator.StringToHash("Cast Waiting");
    private static readonly int Channeling = Animator.StringToHash("Channeling");
    private static readonly int CastSpeed = Animator.StringToHash("CastSpeed");
    private static readonly int CastWaitingIndex = Animator.StringToHash("CastWaitingIndex");

    private static readonly float eps = 0.05f;
    public Transform ikHand;

    [SerializeField] private Animator animator;
    private FirstPersonMovement movement;
    private NetworkStatSystem statSystem;
    private MeshController _meshController;

    public float acceleration = 2f;
    public AnimationCurve decelerationCurve;

    private float velocityZ = 0f;
    private float velocityX = 0f;

    private bool isRunning => movement.IsRunning;
    private float maxVelocity => (isRunning ? 2f : 0.5f) * statSystem.Stats.GetFinal(StatType.MoveSpeed);

    private bool jumpStart = false;
    private bool fallStart = false;
    private float lastPositionY;

    private Vector3 ikPos;
    private Quaternion ikRot;

    private void Awake() {
        movement = GetComponent<FirstPersonMovement>();
        statSystem = GetComponent<NetworkStatSystem>();
        _meshController = GetComponentInChildren<MeshController>();
    }

    private void Start() {
        if (!IsOwner) return;

        movement.Jumped += Jumped;
        ikPos = ikHand.localPosition;
        ikRot = ikHand.localRotation;
    }

    public void TriggerChanneling() {
        AnimateTrigger(Channeling);
    }

    public void CastWaitingAnim(bool waiting, int index = 0) {
        AnimateBool(CastWaiting, waiting);
        if (waiting)
            AnimateFloat(CastWaitingIndex, index);
        if (index == 0) {
            _meshController.leftHand.weight = 1;
            _meshController.rightHand.weight = 0;
            if (waiting) {
                ikHand.localPosition = new Vector3(-0.55f, -0.24f, 0.44f);
                ikHand.localRotation = Quaternion.Euler(0, -90, 0);
            } else {
                ikHand.localPosition = ikPos;
                ikHand.localRotation = ikRot;
            }
        }

        if (index == 1) {
            _meshController.leftHand.weight = 0.5f;
            _meshController.rightHand.weight = 1f;
            if (waiting) {
                ikHand.localPosition = new Vector3(0.09f, -0.22f, 0.27f);
                ikHand.localRotation = Quaternion.Euler(11.7f, 70.6f, 71.4f);
            } else {
                ikHand.localPosition = ikPos;
                ikHand.localRotation = ikRot;
            }
        }
    }

    public IEnumerator CastSpell(SpellData spell) {
        AnimateFloat(CastSpeed, statSystem.Stats.GetFinal(StatType.CastSpeed));
        AnimateFloat(Invocation, spell.invocationIndex);
        yield return new WaitForSeconds(0.1f);
        AnimateFloat(Invocation, 0);
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

    public void AnimateBool(int key, bool value) {
        if (IsOwner) {
            animator.SetBool(key, value);
            AnimateBoolServerRpc(key, value);
        }
    }

    public void AnimateFloat(int key, float value) {
        if (IsOwner) {
            animator.SetFloat(key, value);
            AnimateFloatServerRpc(key, value);
        }
    }

    public void AnimateTrigger(int key) {
        if (IsOwner) {
            animator.SetTrigger(key);
            AnimateTriggerServerRpc(key);
        }
    }

//=====================================================

    [ServerRpc]
    private void AnimateBoolServerRpc(int key, bool value) {
        animator.SetBool(key, value);
    }

    [ServerRpc]
    private void AnimateFloatServerRpc(int key, float value) {
        animator.SetFloat(key, value);
    }

    [ServerRpc]
    private void AnimateTriggerServerRpc(int key) {
        animator.SetTrigger(key);
    }
}