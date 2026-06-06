using System.Collections.Generic;

public static class RuntimeLexiconSpellComposer {
    public static bool TryCompose(
        string rawPhrase,
        RuntimePhraseLexicon lexicon,
        out RuntimeSpellBlueprint blueprint,
        out SpellDefinition spell,
        out IReadOnlyList<string> errors
    ) {
        blueprint = RuntimePhraseLexiconEngine.BuildBlueprint(rawPhrase, lexicon);
        errors = RuntimeSpellBlueprintValidator.Validate(blueprint);

        if (errors.Count > 0) {
            spell = null;
            return false;
        }

        spell = RuntimeSpellDefinitionFactory.Create(blueprint);
        return true;
    }
}

