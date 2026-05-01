public static class ParticipantOwnerCodec {
    public const ulong BotMask = 1UL << 63;

    public static ulong EncodeHuman(ulong clientId) {
        return clientId & ~BotMask;
    }

    public static ulong EncodeBot(ulong botId) {
        return BotMask | (botId & ~BotMask);
    }

    public static ParticipantId Decode(ulong ownerId) {
        var isBot = (ownerId & BotMask) != 0;
        var value = ownerId & ~BotMask;
        return isBot ? ParticipantId.Bot(value) : ParticipantId.Human(value);
    }

    public static ulong Encode(ParticipantId participantId) {
        return participantId.IsBot ? EncodeBot(participantId.Value) : EncodeHuman(participantId.Value);
    }
}

