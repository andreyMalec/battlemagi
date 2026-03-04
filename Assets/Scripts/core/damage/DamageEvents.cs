using System;

public readonly struct DamageRequest {
    public readonly string source;
    public readonly ulong fromId;
    public readonly float amount;
    public readonly DamageKind kind;

    public DamageRequest(string source, ulong fromId, float amount, DamageKind kind) {
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

    public DamageApplied(DamageRequest request, float incoming, float final, float armorApplied, float healthApplied) {
        this.request = request;
        this.incoming = incoming;
        this.final = final;
        this.armorApplied = armorApplied;
        this.healthApplied = healthApplied;
    }
}

public readonly struct DeathInfo {
    public readonly ulong ownerId;
    public readonly ulong fromId;
    public readonly string source;

    public DeathInfo(ulong ownerId, ulong fromId, string source) {
        this.ownerId = ownerId;
        this.fromId = fromId;
        this.source = source;
    }
}

