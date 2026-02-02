using System.Collections.Generic;
using UnityEngine;

public interface IShape {
    void Init(ISpellContext context);

    IEnumerable<ShapeHit> Query();
}