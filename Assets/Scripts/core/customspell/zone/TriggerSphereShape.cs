using System.Collections.Generic;
using UnityEngine;

public class TriggerSphereShape : IShape {
    private readonly HashSet<GameObject> _inside = new();
    private readonly List<GameObject> _buffer = new();
    private readonly List<GameObject> _insideCopy = new();

    private ISpellContext _context;
    private float _radius;
    private float _radiusSqr;

    public void Init(ISpellContext context) {
        _context = context;
        _radius = context.Spell.scale / 2;
        _radiusSqr = _radius * _radius;
    }

    public IEnumerable<ShapeHit> Query() {
        var center = _context.View.transform.position;

        _buffer.Clear();
        foreach (var inst in SpellInstance.Active) {
            if (inst == null) continue;
            if (inst.Bind == null) continue;
            if (inst.Bind.Context == _context) continue;
            if (!inst.Bind.Context.View.IsAlive) continue;

            var targetGo = inst.Bind.Context.View.gameObject;
            var d = targetGo.transform.position - center;
            if (d.sqrMagnitude > _radiusSqr) continue;

            _buffer.Add(targetGo);
            if (_inside.Add(targetGo))
                _context.SendEvent(new OnZoneEnterEvent(targetGo));
        }

        if (_inside.Count != _buffer.Count) {
            _insideCopy.Clear();
            foreach (var go in _inside)
                _insideCopy.Add(go);

            foreach (var go in _insideCopy) {
                if (_buffer.Contains(go)) continue;
                _inside.Remove(go);
                _context.SendEvent(new OnZoneExitEvent(go));
            }
        }

        foreach (var go in _inside) {
            yield return new ShapeHit { Target = go, Point = go.transform.position };
        }

#if UNITY_EDITOR
        Debug.DrawRay(center, Vector3.down * _radius, Color.red);
        Debug.DrawRay(center, Vector3.up * _radius, Color.red);
        Debug.DrawRay(center, Vector3.left * _radius, Color.red);
        Debug.DrawRay(center, Vector3.right * _radius, Color.red);
        Debug.DrawRay(center, Vector3.forward * _radius, Color.red);
        Debug.DrawRay(center, Vector3.back * _radius, Color.red);
#endif
    }
}