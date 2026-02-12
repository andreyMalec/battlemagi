public readonly struct OwnerId {
    public readonly ulong Value;

    public OwnerId(ulong value) {
        Value = value;
    }

    public static implicit operator OwnerId(ulong value) {
        return new OwnerId(value);
    }

    public static implicit operator ulong(OwnerId value) {
        return value.Value;
    }

    public override string ToString() => Value.ToString();
}