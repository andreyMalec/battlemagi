using System.Collections.Generic;
using UnityEngine;

public class SpellSystem {
    private readonly IEntityManager _manager;
    private readonly IAuthorityService _authority;

    public SpellSystem(
        IEntityManager manager,
        IAuthorityService authority
    ) {
        _manager = manager;
        _authority = authority;
    }

    public void CastSpell(
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
            case CoreType.Summon:
                CreateSummon(spawnContext, spawned);
                break;
        }
    }

    private void CreateProjectile(
        SpawnContext spawnContext,
        bool spawned = false
    ) {
        SpellDefinition def = spawnContext.spell;
        var prefab = SpellPrefabDatabase.Instance.Get(def.projectile.prefabId);
        var viewGo = _manager.Spawn(spawnContext.caster.OwnerId, prefab, spawnContext.position, spawnContext.rotation);
        var view = viewGo.GetComponent<SpellView>();
        var instance = viewGo.GetComponent<SpellInstance>();

        var triggers = new List<SpellTrigger>();
        var onHitTrigger = new SpellTrigger {
            eventType = typeof(OnHitEvent),
            actions = HitActions(def.projectile).ToArray()
        };
        triggers.Add(onHitTrigger);
        triggers.Add(new SpellTrigger {
            eventType = typeof(OnMaxDistanceEvent),
            actions = new ISpellAction[] {
                new SpawnAtMaxDistanceAction(),
            }
        });
        triggers.Add(new SpellTrigger {
            eventType = typeof(OnStepDistanceEvent),
            actions = new ISpellAction[] {
                new SpawnAtStepDistanceAction(),
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

        var move = Move(def.projectile, spawnContext.forward);

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

    private void CreateZone(
        SpawnContext spawnContext,
        bool spawned = false
    ) {
        SpellDefinition def = spawnContext.spell;
        var prefab = SpellPrefabDatabase.Instance.Get(def.zone.prefabId);
        var viewGo = _manager.Spawn(spawnContext.caster.OwnerId, prefab, spawnContext.position, spawnContext.rotation);
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

        var move = Move(def.zone, spawnContext.forward);

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
    }

    private void CreateBeam(
        SpawnContext spawnContext,
        bool spawned = false
    ) {
        SpellDefinition def = spawnContext.spell;
        var prefab = SpellPrefabDatabase.Instance.Get(def.beam.prefabId);
        var viewGo = _manager.Spawn(spawnContext.caster.OwnerId, prefab, spawnContext.position, spawnContext.rotation);
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

        ISpellTransform move = Move(def.beam, spawnContext.forward);

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
    }

    private void CreateSummon(
        SpawnContext spawnContext,
        bool spawned = false
    ) {
        SpellDefinition def = spawnContext.spell;
        var prefab = SpellPrefabDatabase.Instance.Get(def.summon.prefabId);
        var viewGo = _manager.Spawn(spawnContext.caster.OwnerId, prefab, spawnContext.position, spawnContext.rotation);
        var view = viewGo.GetComponent<SpellView>();
        var instance = viewGo.GetComponent<SpellInstance>();
        var caster = viewGo.GetComponent<SpellCaster>();
        caster.Initialize(spawnContext.caster.OwnerId, spawnContext.caster.SpellSystem);

        var triggers = new List<SpellTrigger>();
        triggers.Add(new SpellTrigger {
            eventType = typeof(OnLifetimeEndingEvent),
            actions = new ISpellAction[] {
                new RemoveParticlesAction(),
                new FadeOutAudioSourcesAction(),
            }
        });

        var context = new SummonContext(
            spawnContext.caster,
            view,
            def,
            spawned
        );

        ILocomotion move = def.summon.motion switch {
            SummonMotion.Stationary => new StationaryMotion(),
            _ => new StationaryMotion()
        };
        IBrain brain = def.summon.brain switch {
            SummonBrain.AlwaysAttack => new AlwaysAttackBrain(),
            _ => new BrainDead()
        };
        var sensors = new List<ISensor>();
        if ((def.summon.sensors & SummonSensor.Radius) != 0)
            sensors.Add(new RadiusSensor(10f));
        var core = new SummonCore(
            context,
            triggers.ToArray()
        );

        var bind = new SummonBind<SummonContext>(core, view, context, move, caster, brain, sensors);
        instance.Init(bind);
    }

    private static ISpellTransform Move(ProjectileDefinition def, Vector3 direction) {
        ISpellTransform move = def.moveType switch {
            SpellTransform.Linear => new LinearMoveTransform(direction, def.moveSpeed),
            SpellTransform.LookAtPoint => new LookAtPointTransform(def.moveSpeed, def.lookAtMaxDistance,
                def.lookAtRayMask),
            SpellTransform.Spiral => new SpiralMoveTransform(
                direction,
                def.spiralAxis,
                def.spiralRadius,
                def.angularSpeed,
                def.moveSpeed
            ),
            _ => new StaticTransform()
        };

        if (def.enableGravity) {
            move = new GravityTransform(move, def.gravity);
        }

        if (def.spawnAtStep) {
            move = new StepDistanceTransform(
                move,
                def.spawnStep
            );
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

    private static ISpellTransform Move(ZoneDefinition def, Vector3 direction) {
        ISpellTransform move = def.moveType switch {
            SpellTransform.Linear => new LinearMoveTransform(direction, def.moveSpeed),
            SpellTransform.LookAtPoint => new LookAtPointTransform(def.moveSpeed, def.lookAtMaxDistance,
                def.lookAtRayMask),
            SpellTransform.Spiral => new SpiralMoveTransform(
                direction,
                def.spiralAxis,
                def.spiralRadius,
                def.angularSpeed,
                def.moveSpeed
            ),
            SpellTransform.FollowCaster => new FollowCasterTransform(def.followTarget),
            _ => new StaticTransform()
        };

        if (def.spawnAtStep) {
            move = new StepDistanceTransform(
                move,
                def.spawnStep
            );
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

    private static ISpellTransform Move(BeamDefinition def, Vector3 direction) {
        ISpellTransform move = def.moveType switch {
            SpellTransform.Linear => new LinearMoveTransform(direction, def.moveSpeed),
            SpellTransform.LookAtPoint => new LookAtPointTransform(def.moveSpeed, def.lookAtMaxDistance,
                def.lookAtRayMask),
            SpellTransform.FollowCaster => new FollowCasterTransform(def.followTarget),
            _ => new StaticTransform()
        };

        if (def.spawnAtStep) {
            move = new StepDistanceTransform(
                move,
                def.spawnStep
            );
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

    private static List<ISpellAction> HitActions(ProjectileDefinition def) {
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