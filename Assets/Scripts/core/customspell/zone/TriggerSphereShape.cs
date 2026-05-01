using UnityEngine;

public class TriggerSphereShape : TriggerZoneShapeBase {
    private float _radius;
    private float _radiusSqr;

    protected override void InitShape(ISpellContext context) {
        _radius = context.Spell.scale;
        _radiusSqr = _radius * _radius;
    }

    protected override Bounds GetBroadphaseBounds(Vector3 center) {
        return new Bounds(center, Vector3.one * (_radius * 2f));
    }

    protected override bool ContainsPoint(Vector3 point) {
        var center = Context.Movement.Transform.position;
        var delta = point - center;
        return delta.sqrMagnitude <= _radiusSqr;
    }

    protected override bool TryGetStructurePoint(DamageableCollider collider, out Vector3 point) {
        var center = Context.Movement.Transform.position;
        var localPoint = collider.transform.InverseTransformPoint(center) - collider.center;
        var clampedLocalPoint = new Vector3(
            Mathf.Clamp(localPoint.x, -collider.halfExtents.x, collider.halfExtents.x),
            Mathf.Clamp(localPoint.y, -collider.halfExtents.y, collider.halfExtents.y),
            Mathf.Clamp(localPoint.z, -collider.halfExtents.z, collider.halfExtents.z)
        );
        point = collider.transform.TransformPoint(clampedLocalPoint + collider.center);
        return ContainsPoint(point);
    }

    protected override void DrawDebug(Vector3 center) {
        Debug.DrawRay(center, Vector3.down * _radius, Color.red);
        Debug.DrawRay(center, Vector3.up * _radius, Color.red);
        Debug.DrawRay(center, Vector3.left * _radius, Color.red);
        Debug.DrawRay(center, Vector3.right * _radius, Color.red);
        Debug.DrawRay(center, Vector3.forward * _radius, Color.red);
        Debug.DrawRay(center, Vector3.back * _radius, Color.red);
    }
}