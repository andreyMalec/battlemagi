using System.Collections.Generic;
using UnityEngine;

public class ZoneEnemyColliderBlocker : MonoBehaviour {
    private const float SkinWidth = 0.01f;
    private const float PlateHeightFactor = 0.1f;

    private readonly Collider[] _overlapBuffer = new Collider[128];
    private readonly Dictionary<Damageable, Vector3> _directions = new();
    private readonly Dictionary<Damageable, Vector3> _displacements = new();

    private SpellView _view;
    private OwnerId _ownerId;
    private Collider _collider;
    private SphereCollider _sphereCollider;
    private BoxCollider _boxCollider;
    private ZoneShapeType _shapeType;
    private float _radius;
    private Vector3 _boxSize;

    public static void Attach(GameObject main, OwnerId ownerId, float radius, SpellView view) {
        Attach(main, ownerId, radius, ZoneShapeType.Sphere, view);
    }

    public static void Attach(GameObject main, OwnerId ownerId, float radius, ZoneShapeType shapeType, SpellView view) {
        var blocker = main.GetComponent<ZoneEnemyColliderBlocker>();
        if (blocker == null)
            blocker = main.AddComponent<ZoneEnemyColliderBlocker>();
        blocker.Init(ownerId, radius, shapeType, view);
    }

    public void Init(OwnerId ownerId, float radius, SpellView view) {
        Init(ownerId, radius, ZoneShapeType.Sphere, view);
    }

    public void Init(OwnerId ownerId, float radius, ZoneShapeType shapeType, SpellView view) {
        _ownerId = ownerId;
        _radius = radius;
        _shapeType = shapeType;
        var side = radius * 2f;
        _boxSize = new Vector3(side, side * PlateHeightFactor, side);
        _view = view;
        EnsureCollider();
        enabled = true;
        SyncCollider();
    }

    private void LateUpdate() {
        if (!_view.IsAlive) {
            _collider.enabled = false;
            enabled = false;
            return;
        }

        SyncCollider();
        PushEnemiesOutside();
    }

    private void EnsureCollider() {
        if (_sphereCollider == null)
            _sphereCollider = gameObject.GetComponent<SphereCollider>();
        if (_boxCollider == null)
            _boxCollider = gameObject.GetComponent<BoxCollider>();

        if (_shapeType is ZoneShapeType.Plate) {
            if (_boxCollider == null)
                _boxCollider = gameObject.AddComponent<BoxCollider>();
            _boxCollider.isTrigger = true;
            _boxCollider.enabled = true;
            if (_sphereCollider != null)
                _sphereCollider.enabled = false;
            _collider = _boxCollider;
            return;
        }

        if (_sphereCollider == null)
            _sphereCollider = gameObject.AddComponent<SphereCollider>();
        _sphereCollider.isTrigger = true;
        _sphereCollider.enabled = true;
        if (_boxCollider != null)
            _boxCollider.enabled = false;
        _collider = _sphereCollider;
    }

    private void SyncCollider() {
        if (_shapeType is ZoneShapeType.Plate) {
            SyncBoxSize();
            return;
        }

        var scale = Mathf.Abs(transform.lossyScale.x);
        _sphereCollider.radius = scale <= 0.0001f ? _radius : _radius / scale;
    }

    private void SyncBoxSize() {
        var lossyScale = transform.lossyScale;
        _boxCollider.size = new Vector3(
            ToLocalSize(_boxSize.x, lossyScale.x),
            ToLocalSize(_boxSize.y, lossyScale.y),
            ToLocalSize(_boxSize.z, lossyScale.z)
        );
    }

    private static float ToLocalSize(float worldSize, float lossyScaleAxis) {
        var scale = Mathf.Abs(lossyScaleAxis);
        return scale <= 0.0001f ? worldSize : worldSize / scale;
    }

    private void PushEnemiesOutside() {
        var center = transform.position;
        _directions.Clear();
        _displacements.Clear();

        var count = _shapeType is ZoneShapeType.Plate
            ? Physics.OverlapBoxNonAlloc(
                center,
                _boxSize * 0.5f,
                _overlapBuffer,
                transform.rotation,
                Physics.AllLayers,
                QueryTriggerInteraction.Ignore
            )
            : Physics.OverlapSphereNonAlloc(
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
            if (TeamManager.Instance.AreAllies(_ownerId, gameObject, owner, damageable.gameObject)) continue;
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


