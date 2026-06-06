using System.Collections.Generic;

public static class RuntimeSpellBlueprintValidator {
    public static IReadOnlyList<string> Validate(RuntimeSpellBlueprint blueprint) {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(blueprint.rawPhrase))
            errors.Add("SPRB-0001");
        if (string.IsNullOrWhiteSpace(blueprint.spellKey))
            errors.Add("SPRB-0002");
        if (blueprint.spawn == null)
            errors.Add("SPRB-0004");

        var coreBlocks = 0;
        if (blueprint.projectile != null) coreBlocks++;
        if (blueprint.zone != null) coreBlocks++;
        if (blueprint.beam != null) coreBlocks++;
        if (blueprint.summon != null) coreBlocks++;
        if (blueprint.self != null) coreBlocks++;
        if (coreBlocks != 1)
            errors.Add("SPRB-0005");

        if (blueprint.coreType is CoreType.Projectile && blueprint.projectile == null)
            errors.Add("SPRB-0103");
        if (blueprint.coreType is CoreType.Zone && blueprint.zone == null)
            errors.Add("SPRB-0101");
        if (blueprint.coreType is CoreType.Beam && blueprint.beam == null)
            errors.Add("SPRB-0102");
        if (blueprint.coreType is CoreType.Summon && blueprint.summon == null)
            errors.Add("SPRB-0104");
        if (blueprint.coreType is CoreType.Self && blueprint.self == null)
            errors.Add("SPRB-0105");

        ValidateMovement(errors, blueprint);

        return errors;
    }

    private static void ValidateMovement(ICollection<string> errors, RuntimeSpellBlueprint blueprint) {
        if (blueprint.projectile != null) {
            var move = blueprint.projectile.moveType;
            if (move is not (SpellMovement.Static or SpellMovement.Linear or SpellMovement.Accelerated or SpellMovement.LookAtPoint or SpellMovement.Spiral))
                errors.Add("SPRB-0201");

            if (blueprint.projectile.returnToCaster && move is not (SpellMovement.Linear or SpellMovement.Spiral or SpellMovement.Accelerated))
                errors.Add("SPRB-0203");

            if (blueprint.projectile.enableHoming && move is not (SpellMovement.Linear or SpellMovement.Spiral or SpellMovement.Accelerated))
                errors.Add("SPRB-0204");
        }

        if (blueprint.zone != null) {
            var move = blueprint.zone.moveType;
            if (move is not (SpellMovement.Static or SpellMovement.Linear or SpellMovement.Accelerated or SpellMovement.LookAtPoint or SpellMovement.Spiral or SpellMovement.FollowCaster))
                errors.Add("SPRB-0201");

            if (blueprint.zone.returnToCaster && move is not (SpellMovement.Linear or SpellMovement.Spiral or SpellMovement.Accelerated))
                errors.Add("SPRB-0203");

            if (blueprint.zone.enableHoming && move is not (SpellMovement.Linear or SpellMovement.Spiral or SpellMovement.Accelerated))
                errors.Add("SPRB-0204");
        }

        if (blueprint.beam != null) {
            var move = blueprint.beam.moveType;
            if (move is not (SpellMovement.Static or SpellMovement.Linear or SpellMovement.Accelerated or SpellMovement.LookAtPoint or SpellMovement.FollowCaster))
                errors.Add("SPRB-0201");

            if (blueprint.beam.returnToCaster && move is not (SpellMovement.Linear or SpellMovement.Accelerated))
                errors.Add("SPRB-0203");
        }
    }
}

