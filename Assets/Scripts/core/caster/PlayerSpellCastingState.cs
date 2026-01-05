public sealed class PlayerSpellCastingState {
    public SpellData RecognizedSpell;
    public SpellData SpellEcho;
    public int EchoCount;

    public bool CastWaiting;
    public bool Channeling;

    public float ChannelingElapsed;

    public SpellData SpellToCast() {
        return SpellEcho != null ? SpellEcho : RecognizedSpell;
    }
}

