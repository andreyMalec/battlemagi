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
        if (viewGo.TryGetComponent<Collider>(out var _))
            viewGo.AddComponent<TriggerHitBuffer>();

        var onHitTrigger = new SpellTrigger {
            EventType = typeof(OnHitEvent),
            Actions = new ISpellAction[] {
                new DealDamageAction(35f),
                new SpawnZoneAction(def.onHitSpawnZone),
            }
        };

        var context = new ProjectileContext(
            caster,
            view,
            def
        );

        var shape = new LinearProjectileShape();
        var core = new ProjectileCore(
            context,
            shape,
            new[] { onHitTrigger }
        );

        var bind = new SpellBind(core, view, context);
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
        if (viewGo.TryGetComponent<Collider>(out var _))
            viewGo.AddComponent<TriggerHitBuffer>();

        var onHitTrigger = new SpellTrigger {
            EventType = typeof(OnZoneStayEvent),
            Actions = new ISpellAction[] {
                new ZoneDamageAction(3f),
            }
        };

        var context = new ZoneContext(
            caster,
            view,
            def
        );

        var shape = new CircleZoneShape(def.zoneRadius);
        var core = new ZoneCore(
            context,
            shape,
            new[] { onHitTrigger },
            def.zoneDuration
        );

        var bind = new SpellBind(core, view, context);
        instance.Init(bind);
        return bind;
    }
}