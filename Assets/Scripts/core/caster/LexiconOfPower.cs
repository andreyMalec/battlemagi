using System.Linq;
using Unity.Netcode;
using Voice;

public class LexiconOfPower : NetworkBehaviour {
    private Mouth _mouth;
    private SpellCasterPlayer _caster;
    private SpellRecognizer _recognizer;
    private Player _player;

    private void Awake() {
        _mouth = GetComponent<Mouth>();
        _caster = GetComponent<SpellCasterPlayer>();
        _player = GetComponent<Player>();
    }

    public override void OnNetworkDespawn() {
        if (IsOwner) {
            _mouth.OnMouthClose -= OnMouthClose;
            _mouth.Close();
        }
        base.OnNetworkDespawn();
    }

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();

        if (IsOwner) {
            _mouth.Open();
            Initialize(SpeechToTextHolder.Instance.Language);
            _mouth.OnMouthClose += OnMouthClose;
        }
    }

    private void Initialize(Language language) {
        var archetype = ArchetypeDatabase.Instance.GetArchetype(_player.ArchetypeId);
        var spells = archetype.spells.ToList();
        _recognizer = new SpellRecognizer(spells, language);
        _mouth.RestrictWords(_recognizer.SpellWords());
        _caster.UpdateAvailableSpells(spells);
    }

    private void Update() {
        _mouth.CanSpeak(!_caster.CastWaiting && !_caster.Channeling || _caster.Charging);
    }

    private void OnMouthClose(string[] lastWords) {
        if (_caster.Mana.PrimalMana > 0) return;
        if (!_caster.CanSelectSpell) return;
        if (_recognizer == null) return;

        var result = _recognizer.Recognize(lastWords);

        if (result.similarity < GameConfig.Instance.recognitionThreshold)
            return;

        _mouth.ShutUp();
        _caster.SelectSpell(result.spell);
    }
}