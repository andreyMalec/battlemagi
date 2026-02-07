using System.Collections.Generic;
using UnityEngine;

public class SpellFactory {
    public static void CreateSpell(
        SpawnContext spawnContext,
        bool spawned = false
    ) {
        switch (spawnContext.spell.coreType) {
            case CoreType.Projectile:
                CreateProjectile(spawnContext, spawned);
                break;
            case CoreType.Zone:
                CreateZone(spawnContext, spawned);
                break;
            case CoreType.Beam:
                CreateBeam(spawnContext, spawned);
                break;
        }
    }

    private static void CreateProjectile(
        SpawnContext spawnContext,
        bool spawned = false
    ) {
        SpellDefinition def = spawnContext.spell;
        var viewGo = Object.Instantiate(
            def.mainPrefab,
            spawnContext.position,
            spawnContext.rotation
        );
        var view = viewGo.GetComponent<SpellView>();
        var instance = viewGo.GetComponent<SpellInstance>();

        var triggers = new List<SpellTrigger>();
        var onHitTrigger = new SpellTrigger {
            eventType = typeof(OnHitEvent),
            actions = HitActions(def).ToArray()
        };
        triggers.Add(onHitTrigger);
        triggers.Add(new SpellTrigger {
            eventType = typeof(OnMaxDistanceEvent),
            actions = new ISpellAction[] {
                new SpawnAtMaxDistanceAction(),
            }
        });
        triggers.Add(new SpellTrigger {
            eventType = typeof(OnLifetimeEndingEvent),
            actions = new ISpellAction[] {
                new RemoveParticlesAction(),
                new FadeOutAudioSourcesAction(),
                new SpawnOnLifetimeEndAction(),
            }
        });
        triggers.Add(new SpellTrigger {
            eventType = typeof(OnLifetimeHalfEvent),
            actions = new ISpellAction[] {
                new SpawnOnLifetimeHalfAction(),
            }
        });

        var move = Move(def, spawnContext.forward);

        var context = new ProjectileContext(
            spawnContext.caster,
            view,
            move,
            def,
            spawned
        );

        var shape = new LineProjectileShape();
        shape.Init(context);
        var core = new ProjectileCore(
            context,
            shape,
            triggers.ToArray()
        );

        var bind = new SpellBind<ProjectileContext>(core, view, context, move);
        instance.Init(bind);
    }

    private static SpellBind<ZoneContext> CreateZone(
        SpawnContext spawnContext,
        bool spawned = false
    ) {
        SpellDefinition def = spawnContext.spell;
        var viewGo = Object.Instantiate(
            def.mainPrefab,
            spawnContext.position,
            spawnContext.rotation
        );
        var view = viewGo.GetComponent<SpellView>();
        var instance = viewGo.GetComponent<SpellInstance>();

        var triggers = new List<SpellTrigger>();
        // var onHitTrigger = new SpellTrigger {
        //     eventType = typeof(OnZoneStayEvent),
        //     actions = new ISpellAction[] {
        //         new ZoneDamageAction(10f),
        //     }
        // };
        // triggers.Add(onHitTrigger);
        triggers.Add(new SpellTrigger {
            eventType = typeof(OnMaxDistanceEvent),
            actions = new ISpellAction[] {
                new SpawnAtMaxDistanceAction(),
            }
        });
        triggers.Add(new SpellTrigger {
            eventType = typeof(OnLifetimeEndingEvent),
            actions = new ISpellAction[] {
                new RemoveParticlesAction(),
                new FadeOutAudioSourcesAction(),
                new SpawnOnLifetimeEndAction(),
            }
        });
        triggers.Add(new SpellTrigger {
            eventType = typeof(OnLifetimeHalfEvent),
            actions = new ISpellAction[] {
                new SpawnOnLifetimeHalfAction(),
            }
        });

        var move = Move(def, spawnContext.forward);

        var context = new ZoneContext(
            spawnContext.caster,
            view,
            move,
            def,
            spawned
        );

        var shape = new TriggerSphereShape();
        shape.Init(context);
        var core = new ZoneCore(
            context,
            shape,
            triggers.ToArray()
        );

        var bind = new SpellBind<ZoneContext>(core, view, context, move);
        instance.Init(bind);
        return bind;
    }

    private static SpellBind<BeamContext> CreateBeam(
        SpawnContext spawnContext,
        bool spawned = false
    ) {
        SpellDefinition def = spawnContext.spell;
        var viewGo = Object.Instantiate(
            def.mainPrefab,
            spawnContext.position,
            spawnContext.rotation
        );
        var view = viewGo.GetComponent<SpellView>();
        var instance = viewGo.GetComponent<SpellInstance>();

        var triggers = new List<SpellTrigger>();
        triggers.Add(new SpellTrigger {
            eventType = typeof(OnBeamTickEvent),
            actions = new ISpellAction[] {
                new DealDamageAction(10f)
            }
        });
        triggers.Add(new SpellTrigger {
            eventType = typeof(OnMaxDistanceEvent),
            actions = new ISpellAction[] {
                new SpawnAtMaxDistanceAction(),
            }
        });
        triggers.Add(new SpellTrigger {
            eventType = typeof(OnLifetimeEndingEvent),
            actions = new ISpellAction[] {
                new RemoveParticlesAction(),
                new FadeOutAudioSourcesAction(),
                new SpawnOnLifetimeEndAction(),
            }
        });
        triggers.Add(new SpellTrigger {
            eventType = typeof(OnLifetimeHalfEvent),
            actions = new ISpellAction[] {
                new SpawnOnLifetimeHalfAction(),
            }
        });

        ISpellTransform move = Move(def, spawnContext.forward);

        var context = new BeamContext(
            spawnContext.caster,
            view,
            move,
            def,
            spawned
        );

        var shape = new StraightBeamShape();
        shape.Init(context);

        var core = new BeamCore(
            context,
            shape,
            triggers.ToArray()
        );

        var bind = new SpellBind<BeamContext>(core, view, context, move);
        instance.Init(bind);
        return bind;
    }

    private static ISpellTransform Move(SpellDefinition def, Vector3 direction) {
        ISpellTransform move = def.moveType switch {
            SpellTransform.Linear => new LinearMoveTransform(direction, def.projectileSpeed),
            SpellTransform.LookAtPoint => new LookAtPointTransform(def.projectileSpeed, def.lookAtMaxDistance,
                def.lookAtRayMask),
            SpellTransform.Spiral => new SpiralMoveTransform(
                direction,
                def.spiralAxis,
                def.spiralRadius,
                def.angularSpeed,
                def.projectileSpeed
            ),
            SpellTransform.FollowCaster => new FollowCasterTransform(def.followTarget),
            _ => new StaticTransform()
        };

        if (def.enableGravity) {
            move = new GravityTransform(move, def.gravity);
        }

        if (def.enableMaxDistance) {
            move = new MaxDistanceTransform(
                move,
                def.maxDistance
            );
        }

        if (def.enableSquashStretch) {
            move = new SquashStretchTransform(
                move,
                def.stretchAmplitude,
                def.stretchFrequency,
                def.stretchDamping
            );
        }

        return move;
    }

    private static List<ISpellAction> HitActions(SpellDefinition def) {
        var actions = new List<ISpellAction>();
        if (def.enablePierce)
            actions.Add(new PierceOnHitAction(def.maxPierces));
        if (def.enableBounce)
            actions.Add(new BounceOnHitAction(def.maxBounces, def.bounceSpeedMultiplier));
        if (def.enableFork)
            actions.Add(new ForkOnHitAction(def.forkCount, def.forkSpreadAngle));
        // actions.Add(new DealDamageAction(35f));
        if (def.onHitSpawnZone != null)
            actions.Add(new SpawnZoneOnHitAction());
        return actions;
    }
}