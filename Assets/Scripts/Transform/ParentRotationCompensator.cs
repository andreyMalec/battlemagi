using UnityEngine;

[DisallowMultipleComponent]
public class ParentRotationCompensator : MonoBehaviour {
    public enum BaseRotation {
        WorldOnEnable,
        LocalOnEnable
    }

    public enum CompensationMode {
        InverseParent,
        MinusParentEuler
    }

    public bool compensateX = true;
    public bool compensateY = true;
    public bool compensateZ = true;

    public BaseRotation baseRotation = BaseRotation.WorldOnEnable;
    public CompensationMode compensationMode = CompensationMode.InverseParent;

    private Transform _t;
    private Transform _parent;

    private Quaternion _baseWorld;
    private Quaternion _baseLocal;

    private void Awake() {
        Cache();
    }

    private void OnEnable() {
        Cache();
    }

    private void Cache() {
        _t = transform;
        _parent = _t.parent;
        _baseWorld = _t.rotation;
        _baseLocal = _t.localRotation;
    }

    private void LateUpdate() {
        if (_parent != _t.parent)
            Cache();

        if (_parent == null) return;

        var desiredWorld = GetDesiredWorld();
        var desiredLocal = Quaternion.Inverse(_parent.rotation) * desiredWorld;

        if (compensateX && compensateY && compensateZ) {
            _t.localRotation = desiredLocal;
            return;
        }

        _t.localRotation = ApplyAxisMask(_t.localRotation, desiredLocal);
    }

    private Quaternion GetDesiredWorld() {
        var baseWorld = baseRotation == BaseRotation.WorldOnEnable ? _baseWorld : _parent.rotation * _baseLocal;

        if (compensationMode == CompensationMode.InverseParent)
            return baseWorld;

        var eParent = _parent.eulerAngles;
        var eBase = baseWorld.eulerAngles;

        var x = compensateX ? eBase.x - eParent.x : eBase.x;
        var y = compensateY ? eBase.y - eParent.y : eBase.y;
        var z = compensateZ ? eBase.z - eParent.z : eBase.z;

        return Quaternion.Euler(x, y, z);
    }

    private Quaternion ApplyAxisMask(Quaternion current, Quaternion desired) {
        var cur = current.eulerAngles;
        var des = desired.eulerAngles;

        if (compensateX) cur.x = des.x;
        if (compensateY) cur.y = des.y;
        if (compensateZ) cur.z = des.z;

        return Quaternion.Euler(cur);
    }
}
