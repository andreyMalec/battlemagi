using System;

public readonly struct DamageRequest {
    public readonly string source;
    public readonly ParticipantId fromId;
    public readonly float amount;
    public readonly DamageKind kind;

    public DamageRequest(string source, ParticipantId fromId, float amount, DamageKind kind) {
        this.source = source;
        this.fromId = fromId;
        this.amount = amount;
        this.kind = kind;
    }
}

public readonly struct DamageApplied {
    public readonly DamageRequest request;
    public readonly float incoming;
    public readonly float final;
    public readonly float armorApplied;
    public readonly float healthApplied;
    public readonly float overkill;

    public DamageApplied(DamageRequest request, float incoming, float final, float armorApplied, float healthApplied, float overkill) {
        this.request = request;
        this.incoming = incoming;
        this.final = final;
        this.armorApplied = armorApplied;
        this.healthApplied = healthApplied;
        this.overkill = overkill;
    }
}

public readonly struct DeathInfo {
    public readonly ParticipantId ownerId;
    public readonly ParticipantId fromId;
    public readonly string source;

    public DeathInfo(ParticipantId ownerId, ParticipantId fromId, string source) {
        this.ownerId = ownerId;
        this.fromId = fromId;
        this.source = source;
    }
}

