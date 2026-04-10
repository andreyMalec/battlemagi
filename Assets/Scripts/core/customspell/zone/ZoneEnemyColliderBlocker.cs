using System.Collections.Generic;
using UnityEngine;

public class ZoneEnemyColliderBlocker : MonoBehaviour {
    private const float SkinWidth = 0.01f;

    private readonly Collider[] _overlapBuffer = new Collider[128];
    private readonly Dictionary<Damageable, Vector3> _directions = new();
    private readonly Dictionary<Damageable, Vector3> _displacements = new();

    private SpellView _view;
    private OwnerId _ownerId;
    private SphereCollider _collider;
    private float _radius;

    public static void Attach(GameObject main, OwnerId ownerId, float radius, SpellView view) {
        var blocker = main.GetComponent<ZoneEnemyColliderBlocker>();
        if (blocker == null)
            blocker = main.AddComponent<ZoneEnemyColliderBlocker>();
        blocker.Init(ownerId, radius, view);
    }

    public void Init(OwnerId ownerId, float radius, SpellView view) {
        _ownerId = ownerId;
        _radius = radius;
        _view = view;
        if (_collider == null)
            _collider = gameObject.GetComponent<SphereCollider>();
        if (_collider == null)
            _collider = gameObject.AddComponent<SphereCollider>();
        _collider.isTrigger = true;
        _collider.enabled = true;
        enabled = true;
        SyncRadius();
    }

    private void LateUpdate() {
        if (!_view.IsAlive) {
            _collider.enabled = false;
            enabled = false;
            return;
        }

        SyncRadius();
        PushEnemiesOutside();
    }

    private void SyncRadius() {
        var scale = Mathf.Abs(transform.lossyScale.x);
        _collider.radius = scale <= 0.0001f ? _radius : _radius / scale;
    }

    private void PushEnemiesOutside() {
        var center = transform.position;
        _directions.Clear();
        _displacements.Clear();

        var count = Physics.OverlapSphereNonAlloc(
            center,
            _radius,
            _overlapBuffer,
            Physics.AllLayers,
            QueryTriggerInteraction.Ignore
        );

        for (var i = 0; i < count; i++) {
            var other = _overlapBuffer[i];
            if (other == _collider) continue;
            if (!DamageUtils.TryGetOwnerFromCollider(other, out var damageable, out var owner)) continue;
            if (damageable.IsDead) continue;
            if (TeamManager.Instance.AreAllies(_ownerId, owner)) continue;
            var outward = GetOutwardDirection(damageable, other, center);
            if (!Physics.ComputePenetration(
                    _collider,
                    center,
                    transform.rotation,
                    other,
                    other.transform.position,
                    other.transform.rotation,
                    out _,
                    out var distance))
                continue;

            var displacement = outward * (distance + SkinWidth);
            if (_displacements.TryGetValue(damageable, out var current)) {
                if (displacement.sqrMagnitude > current.sqrMagnitude)
                    _displacements[damageable] = displacement;
                continue;
            }

            _displacements.Add(damageable, displacement);
        }

        foreach (var pair in _displacements) {
            pair.Key.transform.position += pair.Value;
        }
    }

    private Vector3 GetOutwardDirection(Damageable damageable, Collider other, Vector3 center) {
        if (_directions.TryGetValue(damageable, out var direction))
            return direction;

        direction = damageable.transform.position - center;
        if (direction.sqrMagnitude <= 0.0001f)
            direction = other.ClosestPoint(center) - center;
        if (direction.sqrMagnitude <= 0.0001f)
            direction = other.bounds.center - center;
        if (direction.sqrMagnitude <= 0.0001f)
            direction = damageable.transform.forward;
        if (direction.sqrMagnitude <= 0.0001f)
            direction = Vector3.forward;

        direction.Normalize();
        _directions[damageable] = direction;
        return direction;
    }
}


