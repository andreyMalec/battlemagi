using System.Collections.Generic;
using UnityEngine;

public class SpellFactory {
    public static SpellBind<ProjectileContext> CreateProjectile(
        SpellDefinition def,
        SpellRunner caster,
        Vector3 position,
        Quaternion rotation,
        Vector3 direction,
        bool spawned = false
    ) {
        var viewGo = Object.Instantiate(
            def.mainPrefab,
            position,
            rotation
        );
        var view = viewGo.GetComponent<SpellView>();
        var instance = viewGo.GetComponent<SpellInstance>();

        var actions = new List<ISpellAction>();
        if (def.enablePierce)
            actions.Add(new PierceOnHitAction(def.maxPierces));
        if (def.enableBounce)
            actions.Add(new BounceOnHitAction(def.maxBounces, def.bounceSpeedMultiplier));
        if (def.enableFork)
            actions.Add(new ForkOnHitAction(def.forkCount, def.forkSpreadAngle));
        actions.Add(new DealDamageAction(35f));
        actions.Add(new SpawnZoneAction(def.onHitSpawnZone));

        var onHitTrigger = new SpellTrigger {
            eventType = typeof(OnHitEvent),
            actions = actions.ToArray()
        };

        ISpellTransform move = def.moveType switch {
            SpellTransform.Linear => new LinearMoveTransform(direction, def.projectileSpeed),
            SpellTransform.Spiral => new SpiralMoveTransform(
                direction,
                def.spiralAxis,
                def.spiralRadius,
                def.angularSpeed,
                def.projectileSpeed
            ),
            SpellTransform.FollowCaster => new FollowCasterTransform(),
            _ => new StaticTransform()
        };

        if (def.enableGravity) {
            move = new GravityTransform(move, def.gravity);
        }

        var context = new ProjectileContext(
            caster,
            view,
            move,
            def,
            spawned
        );

        var shape = viewGo.AddComponent<CapsuleProjectileShape>();
        shape.Init(context);
        var core = new ProjectileCore(
            context,
            shape,
            new[] { onHitTrigger }
        );

        var bind = new SpellBind<ProjectileContext>(core, view, context, move);
        instance.Init(bind);
        return bind;
    }

    public static SpellBind<ZoneContext> CreateZone(
        SpellDefinition def,
        SpellRunner caster,
        Vector3 position,
        Quaternion rotation,
        bool spawned = false
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

        ISpellTransform move = new StaticTransform();
        if (def.enableGravity) {
            move = new GravityTransform(move, def.gravity);
        }

        var context = new ZoneContext(
            caster,
            view,
            move,
            def,
            spawned
        );

        var shape = viewGo.AddComponent<TriggerSphereShape>();
        shape.Init(context);
        var core = new ZoneCore(
            context,
            shape,
            new[] { onHitTrigger }
        );

        var bind = new SpellBind<ZoneContext>(core, view, context, move);
        instance.Init(bind);
        return bind;
    }
}