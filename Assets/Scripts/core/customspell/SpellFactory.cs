using UnityEngine;

public class SpellFactory {
    public static SpellBind CreateProjectile(
        SpellDefinition def,
        SpellRunner caster,
        Vector3 position,
        Quaternion rotation,
        Vector3 direction
    ) {
        var viewGo = Object.Instantiate(
            def.mainPrefab,
            position,
            rotation
        );
        var view = viewGo.GetComponent<SpellView>();
        var instance = viewGo.GetComponent<SpellInstance>();

        var onHitTrigger = new SpellTrigger {
            eventType = typeof(OnHitEvent),
            actions = new ISpellAction[] {
                new DealDamageAction(35f),
                new SpawnZoneAction(def.onHitSpawnZone),
            }
        };

        var context = new ProjectileContext(
            caster,
            view,
            def
        );

        var shape = viewGo.AddComponent<ForwardCapsuleShape>();
        shape.Init(context);
        var core = new ProjectileCore(
            context,
            shape,
            new[] { onHitTrigger }
        );

        var move = new LinearMoveTransform(direction, def.projectileSpeed);
        var bind = new SpellBind(core, view, context, move);
        instance.Init(bind);
        return bind;
    }

    public static SpellBind CreateZone(
        SpellDefinition def,
        SpellRunner caster,
        Vector3 position,
        Quaternion rotation
    ) {
        var viewGo = Object.Instantiate(
            def.mainPrefab,
            position,
            rotation
        );
        var view = viewGo.GetComponent<SpellView>();
        var instance = viewGo.GetComponent<SpellInstance>();

        var onHitTrigger = new SpellTrigger {
            eventType = typeof(OnZoneStayEvent),
            actions = new ISpellAction[] {
                new ZoneDamageAction(10f),
            }
        };

        var context = new ZoneContext(
            caster,
            view,
            def
        );

        var move = new StaticTransform();
        var shape = viewGo.AddComponent<SphereShape>();
        shape.Init(context);
        var core = new ZoneCore(
            context,
            shape,
            new[] { onHitTrigger }
        );

        var bind = new SpellBind(core, view, context, move);
        instance.Init(bind);
        return bind;
    }
}