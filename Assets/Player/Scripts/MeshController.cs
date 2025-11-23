using System;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class MeshController : MonoBehaviour {
    [Header("IK")]
    public Rig leftHand;

    public Rig rightHand;
    public Rig spine;
    public Transform ikTargetHand;
    public Transform ikTargetHandRight;
    public Transform ikTargetSpine;
    [SerializeField] TwoBoneIKConstraint leftHandIkConstraint;
    [SerializeField] TwoBoneIKConstraint rightHandIkConstraint;
    [SerializeField] TwoBoneIKConstraint headIkConstraint;
    [SerializeField] TwoBoneIKConstraint spineIkConstraint;

    [Header("Refs")]
    public Transform head = null;

    public Transform invocation;
    [CanBeNull] public GameObject cloak;

    [Serializable]
    public struct RigidbodyEntry {
        public Rigidbody body;
        public bool enableDetectCollisions;
    }

    public RigidbodyEntry[] rigidbodies;

#if UNITY_EDITOR
    private void OnValidate() {
        if (rigidbodies == null || rigidbodies.Length == 0) {
            var found = GetComponentsInChildren<Rigidbody>();
            rigidbodies = new RigidbodyEntry[found.Length];
            for (int i = 0; i < found.Length; i++) {
                rigidbodies[i].body = found[i];
                rigidbodies[i].enableDetectCollisions = true;
            }
        }
    }
#endif

    private CharacterJoint[] joints;
    private Animator animator;
    [CanBeNull] private Cloth cloth;

    public event Action<bool> OnCast;
    public event Action<bool> OnBurst;

    private void Awake() {
        if (ikTargetHand == null) {
            var player = GetComponentInParent<Player>();
            if (player != null) {
                ikTargetHand = player.GetComponentInChildren<HandIKTarget>().transform;
            }
        }

        if (ikTargetHandRight == null) {
            var player = GetComponentInParent<Player>();
            if (player != null) {
                ikTargetHandRight = player.GetComponentInChildren<HandIKTargetRight>().transform;
            }
        }

        if (ikTargetSpine == null) {
            var player = GetComponentInParent<Player>();
            if (player != null) {
                ikTargetSpine = player.GetComponentInChildren<HandIKTargetSpine>().transform;
            }
        }

        leftHandIkConstraint.data.target = ikTargetHand;
        rightHandIkConstraint.data.target = ikTargetHandRight;
        headIkConstraint.data.target = ikTargetSpine;
        spineIkConstraint.data.target = ikTargetSpine;

        animator = GetComponent<Animator>();
        animator.enabled = false;
        GetComponent<RigBuilder>().Build();
        animator.Rebind();
        animator.enabled = true;
        if (cloak != null)
            cloth = cloak.GetComponent<Cloth>();

        joints = GetComponentsInChildren<CharacterJoint>();

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
        if (cloth != null)
            cloth.enabled = !enable;
        foreach (var rbEntry in rigidbodies) {
            rbEntry.body.isKinematic = !enable;
            rbEntry.body.useGravity = enable;
            rbEntry.body.detectCollisions = rbEntry.enableDetectCollisions || enable;
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