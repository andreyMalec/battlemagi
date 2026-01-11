public struct PlayerId {
    public ulong Value;

    public static implicit operator PlayerId(ulong value) {
        return new PlayerId { Value = value };
    }

    public static implicit operator ulong(PlayerId value) {
        return value.Value;
    }

    public override string ToString() {
        return IsPlayer ? $"Player_{Value.ToString()}" : "Environment";
    }

    public bool IsPlayer => Value != EnvironmentId;

    public const ulong EnvironmentId = ulong.MaxValue;
}