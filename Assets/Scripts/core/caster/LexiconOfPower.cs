using System.Linq;
using Unity.Netcode;
using Voice;

public class LexiconOfPower : NetworkBehaviour {
    private Mouth _mouth;
    private SpellCasterPlayer _caster;
    private SpellRecognizer _recognizer;

    private void Awake() {
        _mouth = GetComponent<Mouth>();
        _caster = GetComponent<SpellCasterPlayer>();
        _mouth.OnMouthClose += OnMouthClose;
    }

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();

        if (IsOwner)
            Initialize(OwnerClientId, SpeechToTextHolder.Instance.Language);
        else {
            _mouth.enabled = false;
        }
    }

    private void Initialize(ulong ownerClientId, Language language) {
        var arch = PlayerManager.Instance.FindByClientId(ownerClientId)!.Value.Archetype;
        var archetype = ArchetypeDatabase.Instance.GetArchetype(arch);
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
        if (_caster.CastWaiting || _caster.Channeling || _caster.Charging) return;

        var result = _recognizer.Recognize(lastWords);

        if (result.similarity < GameConfig.Instance.recognitionThreshold)
            return;

        _mouth.ShutUp();
        _caster.SelectSpell(result.spell);
    }
}