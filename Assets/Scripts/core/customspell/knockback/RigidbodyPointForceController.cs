

using System.Collections.Generic;
using UnityEngine;

public class RigidbodyPointForceController : MonoBehaviour {
    private class PointForceState {
        public Vector3 Point;
        public float ForcePerSecond;
        public float Remaining;
        public SpellKnockbackVectorMode VectorMode;
        public float UpBias;
    }

    private readonly Dictionary<int, PointForceState> _pointForces = new();
    private readonly List<int> _pointForcesToRemove = new();
    private Rigidbody _rigidbody;

    public static RigidbodyPointForceController GetOrAdd(Rigidbody rigidbody) {
        if (rigidbody.TryGetComponent(out RigidbodyPointForceController controller))
            return controller;
        return rigidbody.gameObject.AddComponent<RigidbodyPointForceController>();
    }

    private void Awake() {
        _rigidbody = GetComponent<Rigidbody>();
    }

    public void SetPointForce(
        int id,
        Vector3 point,
        float forcePerSecond,
        float duration,
        SpellKnockbackVectorMode vectorMode,
        float upBias
    ) {
        if (forcePerSecond <= 0f || duration <= 0f) return;
        if (_rigidbody == null)
            _rigidbody = GetComponent<Rigidbody>();

        if (_pointForces.TryGetValue(id, out var state)) {
            state.Point = point;
            state.ForcePerSecond = forcePerSecond;
            state.Remaining = duration;
            state.VectorMode = vectorMode;
            state.UpBias = upBias;
            return;
        }

        _pointForces.Add(id, new PointForceState {
            Point = point,
            ForcePerSecond = forcePerSecond,
            Remaining = duration,
            VectorMode = vectorMode,
            UpBias = upBias,
        });
    }

    private void FixedUpdate() {
        if (_rigidbody == null || _pointForces.Count == 0) return;

        var dt = Time.fixedDeltaTime;
        _pointForcesToRemove.Clear();
        foreach (var pair in _pointForces) {
            var state = pair.Value;
            state.Remaining -= dt;
            if (state.Remaining <= 0f) {
                _pointForcesToRemove.Add(pair.Key);
                continue;
            }

            var direction = SpellKnockbackDirectionUtility.ComputeDirection(_rigidbody.transform, state.Point,
                state.VectorMode, state.UpBias);
            _rigidbody.AddForce(direction * state.ForcePerSecond, ForceMode.Acceleration);
        }

        for (var i = 0; i < _pointForcesToRemove.Count; i++)
            _pointForces.Remove(_pointForcesToRemove[i]);
    }
}



