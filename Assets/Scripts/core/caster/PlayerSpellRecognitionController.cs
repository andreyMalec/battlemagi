using System;
using System.Collections.Generic;
using System.Linq;

public class PlayerSpellRecognitionController {
    private readonly Mouth _mouth;
    private readonly Player _player;
    private SpellRecognizer _recognizer;

    public PlayerSpellRecognitionController(Mouth mouth, Player player) {
        _mouth = mouth;
        _player = player;
    }

    public void Initialize(Language language) {
        var archetype = ArchetypeDatabase.Instance.GetArchetype(_player.ArchetypeId);
        var spells = archetype.spells.ToList();
        _recognizer = new SpellRecognizer(spells, language);
        _mouth.RestrictWords(_recognizer.SpellWords());
    }

    public IReadOnlyList<SpellDefinition> Spells {
        get { return _recognizer.spells; }
    }

    public SpellRecognizer.RecognizedSpell Recognize(string[] lastWords) {
        return _recognizer.Recognize(lastWords);
    }

    public void ShutUp() {
        _mouth.ShutUp();
    }

    public void CanSpeak(bool canSpeak) {
        _mouth.CanSpeak(canSpeak);
    }

    public void EmulateRecognitionFromSpell(SpellData spell, Language language, Action<string[]> onTokens) {
        var words = language == Language.Ru ? spell.spellWordsRu : spell.spellWords;
        var tokens = SpellRecognizer.TokenizePhrase(words[0]);
        onTokens(tokens);
    }
}
