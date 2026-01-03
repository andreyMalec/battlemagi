using System;
using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

[RequireComponent(typeof(FirstPersonMovement))]
[RequireComponent(typeof(NetworkStatSystem))]
[RequireComponent(typeof(Player))]
public class PlayerAnimator : NetworkBehaviour {
    private static readonly int VelocityZ = Animator.StringToHash("Velocity Z");
    private static readonly int VelocityX = Animator.StringToHash("Velocity X");
    private static readonly int VelocityAny = Animator.StringToHash("Velocity Any");
    private static readonly int JumpStart = Animator.StringToHash("Jump Start");
    private static readonly int FallStart = Animator.StringToHash("Fall Start");
    private static readonly int Invocation = Animator.StringToHash("Invocation");
    private static readonly int CastWaiting = Animator.StringToHash("Cast Waiting");
    private static readonly int CastSpeed = Animator.StringToHash("CastSpeed");
    private static readonly int CastWaitingIndex = Animator.StringToHash("CastWaitingIndex");
    private static readonly int CancelChanneling = Animator.StringToHash("CancelChanneling");

    private static readonly float eps = 0.05f;
    public Transform ikHand;
    public Transform ikHandRight;

    [HideInInspector] public Animator animator;
    [HideInInspector] public NetworkAnimator networkAnimator;
    [HideInInspector] public MeshController meshController;
    private FirstPersonMovement movement;
    private NetworkStatSystem statSystem;

    public float acceleration = 3f;
    public AnimationCurve decelerationCurve;

    private float velocityZ = 0f;
    private float velocityX = 0f;

    private bool isRunning => movement.IsRunning;

    // private float maxVelocity => (2f) * statSystem.Stats.GetFinal(StatType.MoveSpeed);
    private float maxVelocity => (isRunning ? 0.5f : 2f) * statSystem.Stats.GetFinal(StatType.MoveSpeed);

    private bool jumpStart = false;
    private bool fallStart = false;
    private float lastPositionY;

    private Vector3 ikPos;
    private Quaternion ikRot;

    public override void OnNetworkSpawn() {
        movement = GetComponent<FirstPersonMovement>();
        statSystem = GetComponent<NetworkStatSystem>();
    }

    private void Start() {
        if (!IsOwner) return;

        movement.Jumped += Jumped;
        ikPos = ikHand.localPosition;
        ikRot = ikHand.localRotation;
    }

    public void CastWaitingAnim(bool waiting, int index = 0) {
        AnimateBool(CastWaiting, waiting);
        if (waiting)
            AnimateFloat(CastWaitingIndex, index);
        if (index == 0) {
            meshController.leftHand.weight = 1;
            meshController.rightHand.weight = 0;
            if (waiting) {
                ikHand.localPosition = new Vector3(-0.55f, -0.24f, 0.44f);
                ikHand.localRotation = Quaternion.Euler(0, -90, 0);
            } else {
                ikHand.localPosition = ikPos;
                ikHand.localRotation = ikRot;
            }

            ikHandRight.localPosition = ikHand.localPosition;
            ikHandRight.localRotation = ikHand.localRotation;
        }

        if (index == 1) {
            meshController.leftHand.weight = 1f; //TODO
            meshController.rightHand.weight = 1f;
            if (waiting) {
                ikHand.localPosition = new Vector3(0.14f, -0.225f, 0.23f);
                ikHand.localRotation = Quaternion.Euler(-192f, -74.6f, -108f);
                ikHandRight.localPosition = new Vector3(0.08f, -0.178f, 0.252f);
                ikHandRight.localRotation = Quaternion.Euler(-210f, -82f, -96f);
            } else {
                ikHand.localPosition = ikPos;
                ikHand.localRotation = ikRot;
                ikHandRight.localPosition = ikHand.localPosition;
                ikHandRight.localRotation = ikHand.localRotation;
            }
        }
    }

    public void CancelSpellChanneling() {
        AnimateTrigger(CancelChanneling);
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
        }
    }

    public void AnimateFloat(int key, float value) {
        if (IsOwner) {
            animator.SetFloat(key, value);
        }
    }

    public void AnimateTrigger(int key) {
        if (IsOwner) {
            networkAnimator.SetTrigger(key);
        }
    }
}