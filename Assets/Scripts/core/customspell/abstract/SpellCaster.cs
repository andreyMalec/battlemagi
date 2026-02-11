using UnityEngine;

public abstract class SpellCaster : MonoBehaviour {
    public abstract bool CanCast { get; }

    public abstract Vector3 Origin { get; }
    public abstract Vector3 Direction { get; }

    public virtual void Cast(SpawnContext context) {
        SpellFactory.CreateSpell(context);
    }

    public virtual void Cast(SpellDefinition spell) {
        var context = new SpawnContext {
            spell = spell,
            spawn = spell.spawn,
            position = Origin,
            rotation = Quaternion.LookRotation(Direction, Vector3.up),
            forward = Direction,
            caster = this
        };
        Cast(context);
    }

    public virtual void Cast(SpellDefinition spell, ITarget target) {
        var context = new SpawnContext {
            spell = spell,
            spawn = spell.spawn,
            position = target.Position,
            rotation = Quaternion.identity,
            forward = Vector3.up,
            caster = this
        };
        Cast(context);
    }
}