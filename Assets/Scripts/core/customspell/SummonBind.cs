using System.Collections.Generic;
using UnityEngine;

public class SummonBind<TContext> : ISpellBind
    where TContext : SummonContext {
    private readonly ISpellCore<TContext> _core;
    private readonly TContext _context;

    private readonly AIContext _ai;
    private readonly IBrain _brain;
    private readonly IAICommands _commands;
    private readonly IEnumerable<ISensor> _sensors;

    ISpellContext ISpellBind.Context => _context;

    public SummonBind(
        ISpellCore<TContext> core,
        SpellView view,
        TContext context,
        ILocomotion move,
        SpellCaster caster,
        IBrain brain,
        IEnumerable<ISensor> sensors
    ) {
        _core = core;
        _context = context;
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
        _core.Tick(deltaTime);
        foreach (var sensor in _sensors)
            sensor.Sense(_ai);

        _brain.Tick(_ai);
        _commands.Tick(_ai);
    }
}