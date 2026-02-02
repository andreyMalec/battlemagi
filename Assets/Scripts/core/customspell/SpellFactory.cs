using Unity.VisualScripting;
using UnityEngine;

public class SpellFactory {
    public static SpellBind CreateProjectile(
        SpellDefinition def,
        SpellRunner caster,
        Vector3 position,
        Quaternion rotation,
        Vector3 direction
    ) {
        var view = Object.Instantiate(
            def.MainPrefab,
            position,
            rotation
        ).GetComponent<SpellView>();
        var onHitTrigger = new SpellTrigger {
            EventType = typeof(OnHitEvent),
            Actions = new ISpellAction[] {
                new DealDamageAction(35f),
                new SpawnZoneAction(def.OnHitSpawnZone),
            }
        };

        var context = new ProjectileContext(
            caster,
            view,
            direction * def.ProjectileSpeed,
            def.ProjectileLifetime
        );

        var shape = new LinearProjectileShape();
        var core = new ProjectileCore(
            context,
            shape,
            new SpellTrigger[] { onHitTrigger }
        );

        return new SpellBind(core, view);
    }

    public static SpellBind CreateZone(
        SpellDefinition def,
        SpellRunner caster,
        Vector3 position,
        Quaternion rotation
    ) {
        var view = Object.Instantiate(
            def.MainPrefab,
            position,
            rotation
        ).GetComponent<SpellView>();

        var onHitTrigger = new SpellTrigger {
            EventType = typeof(OnZoneStayEvent),
            Actions = new ISpellAction[] {
                new ZoneDamageAction(3f),
            }
        };

        var context = new ZoneContext(
            caster,
            view
        );

        var shape = new CircleZoneShape(def.ZoneRadius);
        var core = new ZoneCore(
            context,
            shape,
            new SpellTrigger[] { onHitTrigger },
            def.ZoneDuration
        );

        return new SpellBind(core, view);
    }
}