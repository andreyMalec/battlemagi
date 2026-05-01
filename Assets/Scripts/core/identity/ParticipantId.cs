using System;

[Serializable]
public readonly struct ParticipantId : IEquatable<ParticipantId> {
    public readonly ParticipantKind Kind;
    public readonly ulong Value;

    public ParticipantId(ParticipantKind kind, ulong value) {
        Kind = kind;
        Value = value;
    }

    public static ParticipantId Human(ulong clientId) {
        return new ParticipantId(ParticipantKind.Human, clientId);
    }

    public static ParticipantId Bot(ulong botId) {
        return new ParticipantId(ParticipantKind.Bot, botId);
    }

    public bool IsHuman => Kind == ParticipantKind.Human;
    public bool IsBot => Kind == ParticipantKind.Bot;

    public bool Equals(ParticipantId other) {
        return Kind == other.Kind && Value == other.Value;
    }

    public override bool Equals(object obj) {
        return obj is ParticipantId other && Equals(other);
    }

    public override int GetHashCode() {
        unchecked {
            return ((int)Kind * 397) ^ Value.GetHashCode();
        }
    }

    public static bool operator ==(ParticipantId left, ParticipantId right) {
        return left.Equals(right);
    }

    public static bool operator !=(ParticipantId left, ParticipantId right) {
        return !left.Equals(right);
    }

    public override string ToString() {
        return $"{Kind}:{Value}";
    }
}

