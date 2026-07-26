using System.Collections.Generic;
using UnityEngine;

public static class FallController {
    private struct LaunchState {
        public ulong AttackerEncodedId;
        public float LaunchedAt;
    }

    private static readonly Dictionary<ParticipantId, LaunchState> LastLaunchByVictim = new();
    private static readonly float LaunchFallKillWindow = 3f;

    public static void ReportEnemyLaunchedServer(ParticipantId attackerId, ParticipantId victimId) {
        if (attackerId == victimId)
            return;

        LastLaunchByVictim[victimId] = new LaunchState {
            AttackerEncodedId = ParticipantIdentityCodec.Encode(attackerId),
            LaunchedAt = Time.time
        };
    }

    public static bool TryApplyPhysicsKillCredit(ParticipantId owner, DeathInfo deathInfo) {
        if (deathInfo.source != "Killbox")
            return false;

        if (!LastLaunchByVictim.TryGetValue(owner, out var launchState))
            return false;

        if (Time.time - launchState.LaunchedAt > LaunchFallKillWindow)
            return false;

        var launcherId = ParticipantIdentityCodec.Decode(launchState.AttackerEncodedId);
        if (launcherId == deathInfo.fromId)
            return false;

        Ctx.Players.AddKill(launcherId);
        if (launcherId.IsHuman)
            Ctx.PlayerAchievements?.ReportPhysicsExeStoppedServer(launcherId);

        Ctx.Killfeed?.HandleClientRpc(ParticipantIdentityCodec.Encode(launcherId),
            ParticipantIdentityCodec.Encode(owner));

        return true;
    }
}