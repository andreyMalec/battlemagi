using System;
using System.Collections.Generic;
using System.Linq;
using Voice;

public class PlayerSpellRecognitionController {
    private readonly Mouth _mouth;
    private SpellRecognizer _recognizer;

    public PlayerSpellRecognitionController(Mouth mouth) {
        _mouth = mouth;
    }

    public void Initialize(ulong ownerClientId, Language language) {
        var arch = PlayerManager.Instance.FindByClientId(ownerClientId)!.Value.Archetype;
        var archetype = ArchetypeDatabase.Instance.GetArchetype(arch);
        var spells = archetype.spells.ToList();
        _recognizer = new SpellRecognizer(spells, language);
        _mouth.RestrictWords(_recognizer.SpellWords());
    }

    public IReadOnlyList<SpellData> Spells {
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
