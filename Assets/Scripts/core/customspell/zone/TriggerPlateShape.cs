using UnityEngine;

public class TriggerPlateShape : TriggerZoneShapeBase {
    private const float HeightFactor = 0.2f;
    private const float SatEpsilon = 0.0001f;

    private float _side;
    private float _height;
    private Vector3 _halfExtents;

    protected override void InitShape(ISpellContext context) {
        _side = context.Spell.scale * 2f;
        _height = _side * HeightFactor;
        _halfExtents = new Vector3(_side * 0.5f, _height * 0.5f, _side * 0.5f);
    }

    protected override Bounds GetBroadphaseBounds(Vector3 center) {
        var extents = GetWorldExtents(Context.Movement.Transform.rotation, _halfExtents);
        return new Bounds(center, extents * 2f);
    }

    protected override bool ContainsPoint(Vector3 point) {
        var transform = Context.Movement.Transform;
        var localPoint = Quaternion.Inverse(transform.rotation) * (point - transform.position);
        return Mathf.Abs(localPoint.x) <= _halfExtents.x
            && Mathf.Abs(localPoint.y) <= _halfExtents.y
            && Mathf.Abs(localPoint.z) <= _halfExtents.z;
    }

    protected override bool TryGetStructurePoint(DamageableCollider collider, out Vector3 point) {
        var zoneTransform = Context.Movement.Transform;
        var zoneCenter = zoneTransform.position;
        var zoneRotation = zoneTransform.rotation;

        var colliderLossyScale = collider.transform.lossyScale;
        var structureHalfExtents = new Vector3(
            collider.halfExtents.x * Mathf.Abs(colliderLossyScale.x),
            collider.halfExtents.y * Mathf.Abs(colliderLossyScale.y),
            collider.halfExtents.z * Mathf.Abs(colliderLossyScale.z)
        );
        var structureCenter = collider.transform.TransformPoint(collider.center);
        var structureRotation = collider.transform.rotation;

        point = ClosestPointOnBox(structureCenter, zoneCenter, zoneRotation, _halfExtents);
        return OverlapsBoxBox(zoneCenter, zoneRotation, _halfExtents, structureCenter, structureRotation, structureHalfExtents);
    }

    protected override void DrawDebug(Vector3 center) {
        var transform = Context.Movement.Transform;
        var rotation = transform.rotation;
        var corners = new Vector3[8];
        var signs = new[] {
            new Vector3(-1f, -1f, -1f),
            new Vector3(1f, -1f, -1f),
            new Vector3(1f, -1f, 1f),
            new Vector3(-1f, -1f, 1f),
            new Vector3(-1f, 1f, -1f),
            new Vector3(1f, 1f, -1f),
            new Vector3(1f, 1f, 1f),
            new Vector3(-1f, 1f, 1f)
        };

        for (var i = 0; i < corners.Length; i++)
            corners[i] = center + rotation * Vector3.Scale(_halfExtents, signs[i]);

        DrawEdge(corners, 0, 1);
        DrawEdge(corners, 1, 2);
        DrawEdge(corners, 2, 3);
        DrawEdge(corners, 3, 0);
        DrawEdge(corners, 4, 5);
        DrawEdge(corners, 5, 6);
        DrawEdge(corners, 6, 7);
        DrawEdge(corners, 7, 4);
        DrawEdge(corners, 0, 4);
        DrawEdge(corners, 1, 5);
        DrawEdge(corners, 2, 6);
        DrawEdge(corners, 3, 7);
    }

    private static void DrawEdge(Vector3[] corners, int from, int to) {
        Debug.DrawLine(corners[from], corners[to], Color.red);
    }

    private static Vector3 GetWorldExtents(Quaternion rotation, Vector3 localHalfExtents) {
        var x = rotation * new Vector3(localHalfExtents.x, 0f, 0f);
        var y = rotation * new Vector3(0f, localHalfExtents.y, 0f);
        var z = rotation * new Vector3(0f, 0f, localHalfExtents.z);
        return new Vector3(
            Mathf.Abs(x.x) + Mathf.Abs(y.x) + Mathf.Abs(z.x),
            Mathf.Abs(x.y) + Mathf.Abs(y.y) + Mathf.Abs(z.y),
            Mathf.Abs(x.z) + Mathf.Abs(y.z) + Mathf.Abs(z.z)
        );
    }

    private static Vector3 ClosestPointOnBox(Vector3 point, Vector3 center, Quaternion rotation, Vector3 halfExtents) {
        var localPoint = Quaternion.Inverse(rotation) * (point - center);
        localPoint.x = Mathf.Clamp(localPoint.x, -halfExtents.x, halfExtents.x);
        localPoint.y = Mathf.Clamp(localPoint.y, -halfExtents.y, halfExtents.y);
        localPoint.z = Mathf.Clamp(localPoint.z, -halfExtents.z, halfExtents.z);
        return center + rotation * localPoint;
    }

    private static bool OverlapsBoxBox(
        Vector3 centerA,
        Quaternion rotationA,
        Vector3 halfExtentsA,
        Vector3 centerB,
        Quaternion rotationB,
        Vector3 halfExtentsB
    ) {
        var ax = rotationA * Vector3.right;
        var ay = rotationA * Vector3.up;
        var az = rotationA * Vector3.forward;
        var bx = rotationB * Vector3.right;
        var by = rotationB * Vector3.up;
        var bz = rotationB * Vector3.forward;

        var r00 = Vector3.Dot(ax, bx);
        var r01 = Vector3.Dot(ax, by);
        var r02 = Vector3.Dot(ax, bz);
        var r10 = Vector3.Dot(ay, bx);
        var r11 = Vector3.Dot(ay, by);
        var r12 = Vector3.Dot(ay, bz);
        var r20 = Vector3.Dot(az, bx);
        var r21 = Vector3.Dot(az, by);
        var r22 = Vector3.Dot(az, bz);

        var absR00 = Mathf.Abs(r00) + SatEpsilon;
        var absR01 = Mathf.Abs(r01) + SatEpsilon;
        var absR02 = Mathf.Abs(r02) + SatEpsilon;
        var absR10 = Mathf.Abs(r10) + SatEpsilon;
        var absR11 = Mathf.Abs(r11) + SatEpsilon;
        var absR12 = Mathf.Abs(r12) + SatEpsilon;
        var absR20 = Mathf.Abs(r20) + SatEpsilon;
        var absR21 = Mathf.Abs(r21) + SatEpsilon;
        var absR22 = Mathf.Abs(r22) + SatEpsilon;

        var delta = centerB - centerA;
        var tx = Vector3.Dot(delta, ax);
        var ty = Vector3.Dot(delta, ay);
        var tz = Vector3.Dot(delta, az);

        var ra = halfExtentsA.x;
        var rb = halfExtentsB.x * absR00 + halfExtentsB.y * absR01 + halfExtentsB.z * absR02;
        if (Mathf.Abs(tx) > ra + rb)
            return false;

        ra = halfExtentsA.y;
        rb = halfExtentsB.x * absR10 + halfExtentsB.y * absR11 + halfExtentsB.z * absR12;
        if (Mathf.Abs(ty) > ra + rb)
            return false;

        ra = halfExtentsA.z;
        rb = halfExtentsB.x * absR20 + halfExtentsB.y * absR21 + halfExtentsB.z * absR22;
        if (Mathf.Abs(tz) > ra + rb)
            return false;

        ra = halfExtentsA.x * absR00 + halfExtentsA.y * absR10 + halfExtentsA.z * absR20;
        rb = halfExtentsB.x;
        if (Mathf.Abs(tx * r00 + ty * r10 + tz * r20) > ra + rb)
            return false;

        ra = halfExtentsA.x * absR01 + halfExtentsA.y * absR11 + halfExtentsA.z * absR21;
        rb = halfExtentsB.y;
        if (Mathf.Abs(tx * r01 + ty * r11 + tz * r21) > ra + rb)
            return false;

        ra = halfExtentsA.x * absR02 + halfExtentsA.y * absR12 + halfExtentsA.z * absR22;
        rb = halfExtentsB.z;
        if (Mathf.Abs(tx * r02 + ty * r12 + tz * r22) > ra + rb)
            return false;

        ra = halfExtentsA.y * absR20 + halfExtentsA.z * absR10;
        rb = halfExtentsB.y * absR02 + halfExtentsB.z * absR01;
        if (Mathf.Abs(tz * r10 - ty * r20) > ra + rb)
            return false;

        ra = halfExtentsA.y * absR21 + halfExtentsA.z * absR11;
        rb = halfExtentsB.x * absR02 + halfExtentsB.z * absR00;
        if (Mathf.Abs(tz * r11 - ty * r21) > ra + rb)
            return false;

        ra = halfExtentsA.y * absR22 + halfExtentsA.z * absR12;
        rb = halfExtentsB.x * absR01 + halfExtentsB.y * absR00;
        if (Mathf.Abs(tz * r12 - ty * r22) > ra + rb)
            return false;

        ra = halfExtentsA.x * absR20 + halfExtentsA.z * absR00;
        rb = halfExtentsB.y * absR12 + halfExtentsB.z * absR11;
        if (Mathf.Abs(tx * r20 - tz * r00) > ra + rb)
            return false;

        ra = halfExtentsA.x * absR21 + halfExtentsA.z * absR01;
        rb = halfExtentsB.x * absR12 + halfExtentsB.z * absR10;
        if (Mathf.Abs(tx * r21 - tz * r01) > ra + rb)
            return false;

        ra = halfExtentsA.x * absR22 + halfExtentsA.z * absR02;
        rb = halfExtentsB.x * absR11 + halfExtentsB.y * absR10;
        if (Mathf.Abs(tx * r22 - tz * r02) > ra + rb)
            return false;

        ra = halfExtentsA.x * absR10 + halfExtentsA.y * absR00;
        rb = halfExtentsB.y * absR22 + halfExtentsB.z * absR21;
        if (Mathf.Abs(ty * r00 - tx * r10) > ra + rb)
            return false;

        ra = halfExtentsA.x * absR11 + halfExtentsA.y * absR01;
        rb = halfExtentsB.x * absR22 + halfExtentsB.z * absR20;
        if (Mathf.Abs(ty * r01 - tx * r11) > ra + rb)
            return false;

        ra = halfExtentsA.x * absR12 + halfExtentsA.y * absR02;
        rb = halfExtentsB.x * absR21 + halfExtentsB.y * absR20;
        if (Mathf.Abs(ty * r02 - tx * r12) > ra + rb)
            return false;

        return true;
    }
}
