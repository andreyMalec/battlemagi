using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Trampoline : MonoBehaviour {
    [SerializeField] private Transform vector;
    [SerializeField] private float force = 20;
    [SerializeField] private AudioClip[] jumpSound;
    [SerializeField] private AudioSource audioSource;

    private readonly List<ulong> _affected = new();

    private void OnTriggerEnter(Collider other) {
        if (DamageUtils.TryGetOwnerFromCollider(other, out var damageable, out var owner) &&
            !_affected.Contains(owner)) {
            if (!damageable.TryGetComponent<PlayerPhysics>(out var physics)) return;
            _affected.Add(owner);
            audioSource.Play(jumpSound);
            StartCoroutine(ApplyImpulse(owner, physics));
        }
    }

    private IEnumerator ApplyImpulse(ulong owner, PlayerPhysics physics) {
        physics.ApplyImpulseWithoutSnap(new Vector3(vector.up.x * 4, vector.up.y, vector.up.z * 4) * force);
        yield return null;

        _affected.Remove(owner);
    }

    private void OnDrawGizmos() {
        Gizmos.color = Color.cyan;
        Gizmos.DrawCube(transform.position, new Vector3(1.5f, 0.15f, 1.5f));

        var start = transform.position;
        var end = transform.position + new Vector3(vector.up.x * 4, vector.up.y, vector.up.z * 4);
        Gizmos.DrawLine(start, end);

        var dir = end - start;
        if (dir.sqrMagnitude < 0.000001f) {
            return;
        }

        dir.Normalize();

        var headLength = 0.6f;
        var headAngle = 25f;

        var axis = Vector3.Cross(dir, Vector3.up);
        if (axis.sqrMagnitude < 0.000001f) {
            axis = Vector3.Cross(dir, Vector3.forward);
        }

        axis.Normalize();

        var right = Quaternion.AngleAxis(headAngle, axis) * (-dir);
        var left = Quaternion.AngleAxis(-headAngle, axis) * (-dir);
        Gizmos.DrawLine(end, end + right * headLength);
        Gizmos.DrawLine(end, end + left * headLength);
    }
}