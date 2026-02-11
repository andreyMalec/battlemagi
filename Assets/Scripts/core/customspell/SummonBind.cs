using System.Collections.Generic;
using UnityEngine;

public class SummonBind<TContext> : ISpellBind
    where TContext : SummonContext {
    public ISpellCore<TContext> Core { get; }
    public SpellView View { get; }
    public TContext Context { get; }

    private AIContext _ai;
    private IBrain _brain;
    private readonly IAICommands _commands;
    private IEnumerable<ISensor> _sensors;

    ISpellContext ISpellBind.Context => Context;

    public bool IsAlive { get; private set; } = true;

    public SummonBind(
        ISpellCore<TContext> core,
        SpellView view,
        TContext context,
        ILocomotion move,
        SpellCaster caster,
        IBrain brain,
        IEnumerable<ISensor> sensors
    ) {
        Core = core;
        View = view;
        Context = context;
        _brain = brain;
        _sensors = sensors;
        _commands = new SummonCommands(move, caster);
        _ai = new AIContext {
            Spell = context.Spell.summon.mainSpell,
            Self = view.transform,
            HomePosition = view.transform.position,
            Commands = _commands,
            World = new UnityWorldQuery()
        };
    }

    public void Tick(float deltaTime) {
        Core.Tick(deltaTime);
        foreach (var sensor in _sensors)
            sensor.Sense(_ai);

        _brain.Tick(_ai);
        _commands.Tick(_ai);

        if (!View.IsAlive)
            IsAlive = false;
    }
}