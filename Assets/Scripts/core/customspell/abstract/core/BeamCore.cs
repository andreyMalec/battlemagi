using UnityEngine;

public class BeamCore : ISpellCore {
    private readonly IBeamContext _context;
    private readonly IShape<IBeamContext, BeamRays> _shape;
    private readonly SpellTrigger[] _triggers;

    public void Tick(float deltaTime) {
        var rays = _shape.Sample(_context);

        foreach (var ray in rays.Rays) {
            if (Physics.Raycast(ray, out var hit, _context.MaxLength)) {
                HandleEvent(new OnHitEvent { Target = hit.collider.gameObject });
            }
        }
    }

    public void HandleEvent(SpellEvent evt) {
        foreach (var trigger in _triggers)
            trigger.TryFire(_context, evt);
    }
}