using System.Collections.Generic;

public interface IShape<C, R>
    where C : ISpellContext
    where R : IShapeResult {
    R Sample(C context);

    public IEnumerable<ShapeHit> Query(C ctx, R result);
}