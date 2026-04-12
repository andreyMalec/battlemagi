using System.Collections.Generic;
using UnityEngine;

public class SpellSystem {
    private readonly IAuthorityService _authority;

    public SpellSystem(
        IAuthorityService authority
    ) {
        _authority = authority;
    }

    public void CastSpell(
        SpawnContext spawnContext
    ) {
        switch (spawnContext.spell.coreType) {
            case CoreType.Projectile:
                CreateProjectile(spawnContext, spawnContext.branch);
                break;
            case CoreType.Zone:
                CreateZone(spawnContext, spawnContext.branch);
                break;
            case CoreType.Beam:
                CreateBeam(spawnContext, spawnContext.branch);
                break;
            case CoreType.Summon:
                CreateSummon(spawnContext, spawnContext.branch);
                break;
        }
    }

    public void ShowSpell(
        GameObject main,
        CoreType coreType,
        int prefabId
    ) {
        var prefab = coreType switch {
            CoreType.Projectile => SpellPrefabDatabase.Instance.Get((SpellProjectilePrefabId)prefabId),
            CoreType.Zone => SpellPrefabDatabase.Instance.Get((SpellZonePrefabId)prefabId),
            CoreType.Beam => SpellPrefabDatabase.Instance.Get((SpellBeamPrefabId)prefabId),
            CoreType.Summon => SpellPrefabDatabase.Instance.Get((SpellSummonPrefabId)prefabId),
            _ => null
        };
        if (prefab == null) return;
        main.name = "Spell " + coreType;
        Object.Instantiate(prefab, main.transform);
    }

    private void CreateProjectile(
        SpawnContext spawnContext,
        bool spawned = false
    ) {
        SpellDefinition def = spawnContext.spell;
        var prefab = SpellPrefabDatabase.Instance.Get(def.projectile.prefabId);
        spawnContext.main.name = "Spell " + def.name;
        var viewGo = Object.Instantiate(prefab, spawnContext.main.transform);
        var view = viewGo.GetComponent<SpellView>();
        var instance = viewGo.GetComponent<SpellInstance>();
        var spellEvent = spawnContext.main.GetComponent<SpellSystemEvent>();

        var triggers = new List<SpellTrigger>();
        var onHitTrigger = new SpellTrigger {
            eventType = typeof(OnHitEvent),
            actions = HitActions(def, def.projectile).ToArray()
        };
        triggers.Add(onHitTrigger);
        triggers.Add(new SpellTrigger {
            eventType = typeof(OnMaxDistanceEvent),
            actions = new ISpellAction[] {
                new SpawnAtMaxDistanceAction(),
            }
        });
        if (def.effects != null && def.effects.Count > 0) {
            triggers.Add(new SpellTrigger {
                eventType = typeof(OnLifetimeStartEvent),
                actions = new ISpellAction[] {
                    new SelfStatusEffectAction(),
                }
            });
        }

        triggers.Add(new SpellTrigger {
            eventType = typeof(OnStepDistanceEvent),
            actions = new ISpellAction[] {
                new SpawnAtStepDistanceAction(),
            }
        });
        triggers.Add(new SpellTrigger {
            eventType = typeof(OnLifetimeHalfEvent),
            actions = new ISpellAction[] {
                new SpawnOnLifetimeHalfAction(),
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

        var move = Move(def.projectile, spawnContext.forward);

        var context = new ProjectileContext(
            spawnContext.caster,
            view,
            move,
            def,
            spellEvent,
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
        instance.Init(bind, _authority);
        if (!Mathf.Approximately(spawnContext.spellDamageMultiplier, 1f))
            view.Stats.AddModifier(StatType.SpellDamage, spawnContext.spellDamageMultiplier);
    }

    private void CreateZone(
        SpawnContext spawnContext,
        bool spawned = false
    ) {
        SpellDefinition def = spawnContext.spell;
        var prefab = SpellPrefabDatabase.Instance.Get(def.zone.prefabId);
        spawnContext.main.name = "Spell " + def.name;
        var viewGo = Object.Instantiate(prefab, spawnContext.main.transform);
        var view = viewGo.GetComponent<SpellView>();
        var instance = viewGo.GetComponent<SpellInstance>();
        var spellEvent = spawnContext.main.GetComponent<SpellSystemEvent>();

        var triggers = new List<SpellTrigger>();

        if (def.damage != null) {
            triggers.Add(new SpellTrigger {
                eventType = typeof(OnZoneStayEvent),
                actions = new ISpellAction[] { new ZoneDamageModuleAction() }
            });
        }

        if (def.knockback != null) {
            triggers.Add(new SpellTrigger {
                eventType = typeof(OnZoneStayEvent),
                actions = new ISpellAction[] { new ZoneKnockbackAction() }
            });
        }

        if (def.effects != null && def.effects.Count > 0) {
            triggers.Add(new SpellTrigger {
                eventType = typeof(OnZoneStayEvent),
                actions = new ISpellAction[] { new ZoneStatusEffectAction() }
            });
            triggers.Add(new SpellTrigger {
                eventType = typeof(OnLifetimeStartEvent),
                actions = new ISpellAction[] {
                    new SelfStatusEffectAction(),
                }
            });
        }

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
            eventType = typeof(OnLifetimeHalfEvent),
            actions = new ISpellAction[] {
                new SpawnOnLifetimeHalfAction(),
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
        if (def.zone.teleportOnSpawn) {
            triggers.Add(new SpellTrigger {
                eventType = typeof(OnLifetimeStartEvent),
                actions = new ISpellAction[] {
                    new TeleportCasterToSpellAction(),
                }
            });
        }

        if (def.zone.destroyIncomingSpells) {
            triggers.Add(new SpellTrigger {
                eventType = typeof(OnZoneEnterEvent),
                actions = new ISpellAction[] {
                    new KillOnZoneEnterAction(),
                }
            });
        }

        var move = Move(def.zone, spawnContext.forward);

        var context = new ZoneContext(
            spawnContext.caster,
            view,
            move,
            def,
            spellEvent,
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
        instance.Init(bind, _authority);
        if (def.zone.impassableForEnemies)
            ZoneEnemyColliderBlocker.Attach(spawnContext.main, context.OwnerId, def.scale, view);
        if (!Mathf.Approximately(spawnContext.spellDamageMultiplier, 1f))
            view.Stats.AddModifier(StatType.SpellDamage, spawnContext.spellDamageMultiplier);
    }

    private void CreateBeam(
        SpawnContext spawnContext,
        bool spawned = false
    ) {
        SpellDefinition def = spawnContext.spell;
        var prefab = SpellPrefabDatabase.Instance.Get(def.beam.prefabId);
        spawnContext.main.name = "Spell " + def.name;
        var viewGo = Object.Instantiate(prefab, spawnContext.main.transform);
        var view = viewGo.GetComponent<SpellView>();
        var instance = viewGo.GetComponent<SpellInstance>();
        var spellEvent = spawnContext.main.GetComponent<SpellSystemEvent>();

        var triggers = new List<SpellTrigger>();
        if (def.damage != null) {
            triggers.Add(new SpellTrigger {
                eventType = typeof(OnHitEvent),
                actions = new ISpellAction[] { new BeamDamageModuleAction() }
            });
        }

        var beamKnockback = KnockbackAction(def);
        if (beamKnockback != null) {
            triggers.Add(new SpellTrigger {
                eventType = typeof(OnHitEvent),
                actions = new ISpellAction[] { beamKnockback }
            });
        }

        if (def.effects != null && def.effects.Count > 0) {
            triggers.Add(new SpellTrigger {
                eventType = typeof(OnHitEvent),
                actions = new ISpellAction[] { new BeamStatusEffectAction() }
            });
            triggers.Add(new SpellTrigger {
                eventType = typeof(OnLifetimeStartEvent),
                actions = new ISpellAction[] {
                    new SelfStatusEffectAction(),
                }
            });
        }

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
            spellEvent,
            spawned
        );

        var shape = BeamShape(def.beam);
        shape.Init(context);

        var core = new BeamCore(
            context,
            shape,
            triggers.ToArray()
        );

        var bind = new SpellBind<BeamContext>(core, view, context, move);
        instance.Init(bind, _authority);
        if (!Mathf.Approximately(spawnContext.spellDamageMultiplier, 1f))
            view.Stats.AddModifier(StatType.SpellDamage, spawnContext.spellDamageMultiplier);
    }

    private void CreateSummon(
        SpawnContext spawnContext,
        bool spawned = false
    ) {
        SpellDefinition def = spawnContext.spell;
        var prefab = SpellPrefabDatabase.Instance.Get(def.summon.prefabId);
        spawnContext.main.name = "Spell " + def.name;
        var viewGo = Object.Instantiate(prefab, spawnContext.main.transform);
        var view = viewGo.GetComponent<SpellView>();
        var instance = viewGo.GetComponent<SpellInstance>();
        var caster = viewGo.GetComponent<SpellCaster>();
        var spellEvent = spawnContext.main.GetComponent<SpellSystemEvent>();

        var triggers = new List<SpellTrigger>();
        triggers.Add(new SpellTrigger {
            eventType = typeof(OnLifetimeEndingEvent),
            actions = new ISpellAction[] {
                new RemoveParticlesAction(),
                new FadeOutAudioSourcesAction(),
            }
        });
        if (def.effects != null && def.effects.Count > 0) {
            triggers.Add(new SpellTrigger {
                eventType = typeof(OnLifetimeStartEvent),
                actions = new ISpellAction[] {
                    new SelfStatusEffectAction(),
                }
            });
        }

        if (def.summon.prefabId == SpellSummonPrefabId.Trap) {
            triggers.Add(new SpellTrigger {
                eventType = typeof(OnSummonAttackEvent),
                actions = new ISpellAction[] {
                    new DestroyOnAttackAction(),
                }
            });
        }

        var context = new SummonContext(
            spawnContext.caster,
            view,
            def,
            spellEvent,
            spawned
        );

        ILocomotion move = def.summon.motion switch {
            SummonMotion.NoMotion => new NoMotion(),
            SummonMotion.Stationary => new StationaryMotion(),
            SummonMotion.Floating => new FloatingMotion(def.summon.moveSpeed, 2, def.summon.floatingHeight),
            _ => new NoMotion()
        };
        IBrain brain = def.summon.brain switch {
            SummonBrain.AlwaysAttack => new AlwaysAttackBrain(),
            SummonBrain.Aggressive => new AggressiveBrain(),
            SummonBrain.Defensive => new DefensiveBrain(),
            _ => new BrainDead()
        };
        var sensors = new List<ISensor>();
        if ((def.summon.sensors & SummonSensor.Radius) != 0)
            sensors.Add(new RadiusSensor(def.summon.sensorRadius));
        var core = new SummonCore(
            context,
            triggers.ToArray()
        );

        var bind = new SummonBind<SummonContext>(core, view, context, move, caster, brain, sensors);
        instance.Init(bind, _authority);
        if (!Mathf.Approximately(spawnContext.spellDamageMultiplier, 1f))
            view.Stats.AddModifier(StatType.SpellDamage, spawnContext.spellDamageMultiplier);
    }

    private static ISpellTransform Move(ProjectileDefinition def, Vector3 direction) {
        ISpellTransform move = def.moveType switch {
            SpellMovement.Linear => new LinearMoveTransform(direction, def.moveSpeed),
            SpellMovement.LookAtPoint => new LookAtPointTransform(def.moveSpeed, def.lookAtMaxDistance,
                def.lookAtRayMask),
            SpellMovement.Spiral => new SpiralMoveTransform(
                direction,
                def.spiralAxis,
                def.spiralRadius,
                def.angularSpeed,
                def.moveSpeed
            ),
            _ => new StaticTransform()
        };

        if (def.enableHoming) {
            move = new HomingTransform(
                move,
                def.homingMaxTurnDegrees,
                def.homingSlerp,
                def.homingRadius,
                def.obstacleMask,
                new UnityWorldQuery()
            );
        }

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
            SpellMovement.Linear => new LinearMoveTransform(direction, def.moveSpeed),
            SpellMovement.LookAtPoint => new LookAtPointTransform(def.moveSpeed, def.lookAtMaxDistance,
                def.lookAtRayMask),
            SpellMovement.Spiral => new SpiralMoveTransform(
                direction,
                def.spiralAxis,
                def.spiralRadius,
                def.angularSpeed,
                def.moveSpeed
            ),
            SpellMovement.FollowCaster => new FollowCasterTransform(def.followTarget),
            _ => new StaticTransform()
        };

        if (def.enableHoming) {
            move = new HomingTransform(
                move,
                def.homingMaxTurnDegrees,
                def.homingSlerp,
                def.homingRadius,
                def.obstacleMask,
                new UnityWorldQuery()
            );
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

    private static ISpellTransform Move(BeamDefinition def, Vector3 direction) {
        ISpellTransform move = def.moveType switch {
            SpellMovement.Linear => new LinearMoveTransform(direction, def.moveSpeed),
            SpellMovement.LookAtPoint => new LookAtPointTransform(def.moveSpeed, def.lookAtMaxDistance,
                def.lookAtRayMask),
            SpellMovement.FollowCaster => new FollowCasterTransform(def.followTarget),
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

    private static IShape BeamShape(BeamDefinition def) {
        return def.shapeType switch {
            BeamShapeType.Cone => new ConeBeamShape(),
            _ => new StraightBeamShape()
        };
    }

    private static List<ISpellAction> HitActions(SpellDefinition spell, ProjectileDefinition def) {
        var actions = new List<ISpellAction>();
        if (def.enablePierce)
            actions.Add(new PierceOnHitAction(def.maxPierces));
        if (def.enableBounce)
            actions.Add(new BounceOnHitAction(def.maxBounces, def.bounceSpeedMultiplier));
        if (def.enableFork)
            actions.Add(new ForkOnHitAction(def.forkCount, def.forkSpreadAngle));
        if (def.onHitSpawn != null)
            actions.Add(new SpawnOnHitAction());
        if (spell.damage != null)
            actions.Add(new ProjectileInstantDamageAction());
        var knockback = KnockbackAction(spell);
        if (knockback != null)
            actions.Add(knockback);
        if (spell.effects != null && spell.effects.Count > 0)
            actions.Add(new ProjectileStatusEffectAction());
        if (def.echoOnHit)
            actions.Add(new EchoOnHitAction());
        return actions;
    }

    private static ISpellAction KnockbackAction(SpellDefinition spell) {
        if (spell == null || spell.knockback == null) return null;

        return spell.knockback.mode switch {
            SpellKnockbackMode.Impulse => new ImpulseKnockbackOnHitAction(),
            SpellKnockbackMode.Continuous => new ContinuousPointKnockbackOnHitAction(),
            _ => null
        };
    }
}