using System;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class MeshController : MonoBehaviour {
    [HideInInspector] public Transform ikTargetHand;

    [SerializeField] TwoBoneIKConstraint spineIkConstraint;

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
        var player = GetComponentInParent<Player>();
        if (player != null) {
            if (ikTargetHand == null) {
                ikTargetHand = player.GetComponentInChildren<HandIKTarget>().transform;
            }
        } else {
            var bot = GetComponentInParent<Bot>();
            if (bot != null) {
                if (ikTargetHand == null) {
                    ikTargetHand = bot.GetComponentInChildren<HandIKTarget>().transform;
                }
            }
        }

        spineIkConstraint.data.target = ikTargetHand;

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
        // if (Input.GetKeyDown(KeyCode.C))
        //     SetRagdoll(true);
        // else if (Input.GetKeyUp(KeyCode.C))
        //     SetRagdoll(false);
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

    public void OnAnimationCast() {
        OnCast?.Invoke(true);
    }

    public void Burst() {
        OnBurst?.Invoke(true);
    }
}