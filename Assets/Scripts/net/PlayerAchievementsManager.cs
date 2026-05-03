using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerAchievementsManager : NetworkBehaviour {
    private class AchievementState {
        public readonly HashSet<string> unlocked = new();
        public readonly Queue<float> deathTimes = new();
        public readonly Queue<float> voiceCastTimes = new();
        public readonly Queue<float> castKindTimes = new();
        public readonly Queue<int> castKindHistory = new();
        public readonly Dictionary<string, Queue<float>> killBySourceTimes = new();
        public bool tookDamageThisLife;
        public int noDamageKillStreak;
        public int killStreak;
        public int echoRestored;
        public float lastDeathAt = -999f;
        public float roundDamageDealt;
        public float manaBurstStartedAt = -999f;
        public float manaBurstStartValue;
        public float currentAirTime;
        public ulong lastLaunchBy = ulong.MaxValue;
        public float lastLaunchAt = -999f;
    }

    public static PlayerAchievementsManager Instance { get; private set; }

    private readonly Dictionary<ulong, AchievementState> _achievementStates = new();
    private bool _matchStarted;
    private float _matchStartedAt = -999f;

    [SerializeField] private float parkourAirtimeThreshold = 3f;
    [SerializeField] private float gravityAirtimeThreshold = 12f;
    [SerializeField] private float launchFallKillWindow = 3f;

    private void Awake() {
        if (Instance == null) Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start() {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnConnectionEvent += OnConnectionEvent;
    }

    private void Update() {
        if (!IsServer) return;
        if (!IsSpawned) return;
        TrackAirTimeServer(Time.deltaTime);
    }

    public override void OnDestroy() {
        base.OnDestroy();
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnConnectionEvent -= OnConnectionEvent;
    }

    private void OnConnectionEvent(NetworkManager _, ConnectionEventData eventData) {
        if (!IsServer) return;
        if (eventData.EventType != ConnectionEvent.ClientDisconnected) return;
        _achievementStates.Remove(eventData.ClientId);
    }

    private AchievementState GetAchievementState(ulong clientId) {
        if (_achievementStates.TryGetValue(clientId, out var state))
            return state;

        state = new AchievementState();
        _achievementStates[clientId] = state;
        return state;
    }

    private void UnlockServer(ulong clientId, string achievementId) {
        if (!IsServer) return;
        var state = GetAchievementState(clientId);
        if (!state.unlocked.Add(achievementId)) return;

        UnlockAchievementClientRpc(achievementId, new ClientRpcParams {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } }
        });
    }

    private static void TrimQueue(Queue<float> queue, float now, float windowSec) {
        while (queue.Count > 0 && now - queue.Peek() > windowSec)
            queue.Dequeue();
    }

    private void TrackAirTimeServer(float dt) {
        foreach (var pair in NetworkManager.Singleton.ConnectedClients) {
            var clientId = pair.Key;
            var playerObject = pair.Value.PlayerObject;
            if (playerObject == null) continue;
            var gc = playerObject.GetComponentInChildren<GroundCheck>();
            if (gc == null) continue;

            var state = GetAchievementState(clientId);
            if (gc.isGrounded) {
                state.currentAirTime = 0f;
                continue;
            }

            state.currentAirTime += dt;
            if (state.currentAirTime >= gravityAirtimeThreshold)
                UnlockServer(clientId, SteamAchievementsCatalog.ToBeyond);
        }
    }

    public void ReportEnemyLaunchedServer(ulong attackerId, ulong victimId) {
        if (!IsServer) return;
        if (attackerId == victimId) return;
        if (!TeamManager.Instance.AreEnemies(attackerId, victimId)) return;

        var victimState = GetAchievementState(victimId);
        victimState.lastLaunchBy = attackerId;
        victimState.lastLaunchAt = Time.time;
    }

    [ClientRpc]
    private void UnlockAchievementClientRpc(string achievementId, ClientRpcParams rpcParams = default) {
        _ = rpcParams;
        SteamAchievementsClient.Unlock(achievementId);
    }

    public void ReportVoiceRecognizedServer(ulong clientId) {
        if (!IsServer) return;
        var state = GetAchievementState(clientId);
        var now = Time.time;
        state.voiceCastTimes.Enqueue(now);
        TrimQueue(state.voiceCastTimes, now, 10f);
        if (state.voiceCastTimes.Count >= 7)
            UnlockServer(clientId, SteamAchievementsCatalog.RapGod);
    }

    public void ReportEchoConsumedServer(ulong clientId) {
        var state = GetAchievementState(clientId);
        if (++state.echoRestored >= 5)
            UnlockServer(clientId, SteamAchievementsCatalog.EchoRound);
    }

    public void ReportEchoStartedServer(ulong clientId) {
        var state = GetAchievementState(clientId);
        state.echoRestored = 0;
    }

    public void ReportSpellCastServer(ulong clientId, DamageKind castKind) {
        if (!IsServer) return;

        var now = Time.time;
        var state = GetAchievementState(clientId);

        if (castKind != DamageKind.Default) {
            state.castKindTimes.Enqueue(now);
            state.castKindHistory.Enqueue((int)castKind);
            TrimQueue(state.castKindTimes, now, 5f);
            while (state.castKindHistory.Count > state.castKindTimes.Count)
                state.castKindHistory.Dequeue();

            if (state.castKindHistory.Count >= 3) {
                var arr = state.castKindHistory.ToArray();
                var a = arr[arr.Length - 1];
                var b = arr[arr.Length - 2];
                var c = arr[arr.Length - 3];
                if (a != b && b != c && a != c)
                    UnlockServer(clientId, SteamAchievementsCatalog.ArcaneBlender);
            }
        }
    }

    public void ReportManaSpentServer(ulong clientId, float manaBefore, float manaAfter, float maxMana) {
        if (!IsServer) return;
        var now = Time.time;
        var state = GetAchievementState(clientId);

        if (manaBefore >= maxMana * 0.9f) {
            state.manaBurstStartedAt = now;
            state.manaBurstStartValue = manaBefore;
        }

        // if (now - state.manaBurstStartedAt <= 3f && state.manaBurstStartValue >= maxMana * 0.9f && manaAfter <= 0.5f)
        //     UnlockServer(clientId, SteamAchievementsCatalog.TooMuchMana);

        if (now - state.manaBurstStartedAt > 3f) {
            state.manaBurstStartedAt = -999f;
            state.manaBurstStartValue = 0f;
        }
    }

    public void ReportDamageAppliedServer(ulong victimId, DamageApplied applied) {
        if (!IsServer) return;

        var victimState = GetAchievementState(victimId);
        if (applied.final > 0f) {
            victimState.tookDamageThisLife = true;
            victimState.noDamageKillStreak = 0;
        }

        if (applied.request.fromId == victimId)
            return;

        var attackerId = applied.request.fromId;
        if (applied.overkill > 50f)
            UnlockServer(attackerId, SteamAchievementsCatalog.Overkill);
    }

    public void ReportLifeDamageServer(Dictionary<ulong, float> damageByAttacker) {
        if (!IsServer) return;

        foreach (var pair in damageByAttacker) {
            if (pair.Value <= 0f) continue;
            var attackerState = GetAchievementState(pair.Key);
            attackerState.roundDamageDealt += pair.Value;
        }
    }

    public void ReportDeathServer(ulong victimId, in DeathInfo deathInfo) {
        if (!IsServer) return;

        var now = Time.time;
        var victimState = GetAchievementState(victimId);
        victimState.deathTimes.Enqueue(now);
        TrimQueue(victimState.deathTimes, now, 16f);
        if (victimState.deathTimes.Count >= 5)
            UnlockServer(victimId, SteamAchievementsCatalog.SkillIssue);

        if (_matchStarted && now - _matchStartedAt <= 3f && deathInfo.fromId != deathInfo.ownerId)
            UnlockServer(victimId, SteamAchievementsCatalog.PressF);

        if (deathInfo.fromId == victimId && deathInfo.source != "Suicide")
            UnlockServer(victimId, SteamAchievementsCatalog.Plan);

        if (deathInfo.source == "Pain Mirror")
            UnlockServer(deathInfo.fromId, SteamAchievementsCatalog.UnoReverse);

        if (deathInfo.fromId != victimId) {
            var killerState = GetAchievementState(deathInfo.fromId);

            if (killerState.currentAirTime >= parkourAirtimeThreshold)
                UnlockServer(deathInfo.fromId, SteamAchievementsCatalog.Parkour80);

            if (Mathf.Abs(killerState.lastDeathAt - now) <= 1.2f)
                UnlockServer(victimId, SteamAchievementsCatalog.ClownFiesta);

            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(deathInfo.fromId, out var killerClient)) {
                var killerDamageable = killerClient.PlayerObject.GetComponent<Damageable>();
                if (killerDamageable.IsAlive && killerDamageable.CurrentHealth <= 3f)
                    UnlockServer(victimId, SteamAchievementsCatalog.NotLikeThis);
            }

            killerState.killStreak++;
            if (killerState.killStreak >= 9)
                UnlockServer(deathInfo.fromId, SteamAchievementsCatalog.Godlike);

            if (!killerState.tookDamageThisLife) {
                killerState.noDamageKillStreak++;
                if (killerState.noDamageKillStreak >= 5)
                    UnlockServer(deathInfo.fromId, SteamAchievementsCatalog.WizardDiff);
            }

            if (!killerState.killBySourceTimes.TryGetValue(deathInfo.source ?? string.Empty, out var sourceKills)) {
                sourceKills = new Queue<float>();
                killerState.killBySourceTimes[deathInfo.source ?? string.Empty] = sourceKills;
            }

            sourceKills.Enqueue(now);
            TrimQueue(sourceKills, now, 2f);
            if (sourceKills.Count >= 3)
                UnlockServer(deathInfo.fromId, SteamAchievementsCatalog.ChainReaction);
        }

        if (deathInfo.source == "Killbox" && victimState.lastLaunchBy != ulong.MaxValue && now - victimState.lastLaunchAt <= launchFallKillWindow)
            UnlockServer(victimState.lastLaunchBy, SteamAchievementsCatalog.PhysicsExeStopped);

        victimState.lastDeathAt = now;
        victimState.tookDamageThisLife = false;
        victimState.noDamageKillStreak = 0;
        victimState.killStreak = 0;
    }

    public void ReportMatchStartedServer() {
        if (!IsServer) return;
        _matchStarted = true;
        _matchStartedAt = Time.time;
        foreach (var player in PlayerManager.Instance.Players()) {
            var state = GetAchievementState(player.ClientId);
            state.roundDamageDealt = 0f;
            state.tookDamageThisLife = false;
            state.noDamageKillStreak = 0;
            state.killStreak = 0;
            state.deathTimes.Clear();
        }
    }

    public void ReportMatchWinnerServer(ulong winnerClientId) {
        if (!IsServer) return;
        AwardWinnerAchievements(winnerClientId);
        _matchStarted = false;
    }

    public void ReportTeamWinnerServer(TeamManager.Team winnerTeam) {
        if (!IsServer) return;
        foreach (var player in PlayerManager.Instance.Players()) {
            if (TeamManager.Instance.GetTeam(player.ClientId) != winnerTeam)
                continue;
            AwardWinnerAchievements(player.ClientId);
        }

        _matchStarted = false;
    }

    private void AwardWinnerAchievements(ulong clientId) {
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var winnerClient))
            return;

        var playerObject = winnerClient.PlayerObject;
        var health = playerObject.GetComponent<Damageable>().CurrentHealth;
        if (health <= 3f)
            UnlockServer(clientId, SteamAchievementsCatalog.OneHpClutch);

        // if (playerObject.GetComponent<SpellCasterPlayer>().Mana.PrimalMana > 0f)
        //     UnlockServer(clientId, SteamAchievementsCatalog.NoManaNoProblem);

        if (GetAchievementState(clientId).roundDamageDealt <= 200f)
            UnlockServer(clientId, SteamAchievementsCatalog.PacifistRun);
    }
}