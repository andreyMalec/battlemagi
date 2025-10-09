using System;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class MeshController : MonoBehaviour {
    [Header("IK")] public Rig leftHand;
    public Rig spine;
    public Transform ikTargetHand;
    public Transform ikTargetSpine;
    [SerializeField] TwoBoneIKConstraint leftHandIkConstraint;
    [SerializeField] TwoBoneIKConstraint headIkConstraint;
    [SerializeField] TwoBoneIKConstraint spineIkConstraint;

    [Header("Refs")] public Transform head = null;
    public ParticleSystem invocation;
    public GameObject cloak;

    [Header("Ragdoll")] public Collider[] colliders;
    private CharacterJoint[] joints;
    private Rigidbody[] rigidbodies;

    private Animator animator;
    private Cloth cloth;

    public event Action<bool> OnCast;
    public event Action<bool> OnBurst;

    private void Awake() {
        leftHandIkConstraint .data.target = ikTargetHand;
        headIkConstraint.data.target = ikTargetSpine;
        spineIkConstraint.data.target = ikTargetSpine;
        
        animator = GetComponent<Animator>();
        animator.enabled = false;
        GetComponent<RigBuilder>().Build();
        animator.Rebind();
        animator.enabled = true;
        cloth = cloak.GetComponent<Cloth>();
        
        joints = GetComponentsInChildren<CharacterJoint>();
        rigidbodies = GetComponentsInChildren<Rigidbody>();

        SetRagdoll(false);
    }

    private void Update() {
        if (Input.GetKeyDown(KeyCode.C))
            SetRagdoll(true);
        else if (Input.GetKeyUp(KeyCode.C))
            SetRagdoll(false);
    }

    public void SetRagdoll(bool enable) {
        animator.enabled = !enable;
        cloth.enabled = !enable;
        foreach (var rigidbody in rigidbodies) {
            rigidbody.detectCollisions = enable;
            rigidbody.useGravity = enable;
        }

        foreach (var collider in colliders) {
            collider.enabled = enable;
        }

        foreach (var joint in joints) {
            joint.enableCollision = enable;
            joint.enableProjection = enable;
            joint.enablePreprocessing = enable;
        }
    }

    public void Cast() {
        OnCast?.Invoke(true);
    }

    public void Burst() {
        OnBurst?.Invoke(true);
    }
}