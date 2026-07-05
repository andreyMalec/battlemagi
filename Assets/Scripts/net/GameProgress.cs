using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class GameProgress : NetworkBehaviour {
    // EndChoice mapping (as set from lobby):
    // 0 - first option (3 flags / 15 kills)
    // 1 - second option (5 flags / 30 kills)
    // 2 - third option (7 flags / 45 kills)

    public static readonly int[] ctfTargets = new[] { 3, 5, 7 };
    public static readonly int[] killsTargets = new[] { 15, 30, 45 };

    public static GameProgress Instance { get; private set; }

    private bool started = false;
    private bool ended = false;

    public string SceneName;
    public NetworkVariable<int> SelectedMap = new();
    public NetworkVariable<int> LobbyVisibility = new();

    private void Awake() {
        if (Instance == null) Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetKeyCast(bool allowed) {
        SetKeyCastClientRpc(allowed);
    }

    [ClientRpc]
    private void SetKeyCastClientRpc(bool allowed) {
        GameConfig.Instance.allowKeySpells = allowed;
    }

    public void SelectMap(int mapIndex) {
        if (!IsServer) return;
        started = false;
        SceneName = GameMapDatabase.instance.gameMaps[mapIndex].sceneName;
        SelectedMap.Value = mapIndex;
        Debug.Log($"[GameProgress] Selected map: {SceneName} mapIndex={mapIndex}");
    }

    public void StartMatch() {
        if (!IsServer || started) return;
        PlayerAchievementsManager.Instance?.ReportMatchStartedServer();
        BotLifecycleManager.Instance?.BeginMatch();
        NetworkManager.Singleton.SceneManager.LoadScene(SceneName, LoadSceneMode.Single);
        LobbyManager.Instance.GameStarted();
        started = true;

        var lobby = LobbyManager.Instance.CurrentLobby!.Value;
        var param = new Dictionary<string, object> {
            { "map", SceneName },
            { "mode", TeamManager.Instance.CurrentMode.Value.ToString() },
            { "playerCount", lobby.MemberCount }
        };
        foreach (var player in PlayerManager.Instance.Participants) {
            var arch = ArchetypeDatabase.Instance.GetArchetype(player.Archetype).archetypeName;
            if (player.Id.IsHuman) {
                var prev = (int)(param.GetValueOrDefault($"playerArchetype_{arch}", 0));
                param[$"playerArchetype_{arch}"] = prev + 1;
            }
        }

        var bots = LobbyBotRosterData.LoadFromLobby(lobby);
        foreach (var bot in bots) {
            var arch = ArchetypeDatabase.Instance.GetArchetype(bot.archetype).archetypeName;
            var prev = (int)(param.GetValueOrDefault($"botArchetype_{arch}", 0));
            param[$"botArchetype_{arch}"] = prev + 1;
        }

        FirebaseAnalytic.Instance.SendEvent("MatchStarted", param);
    }

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        if (!IsServer) return;
        SelectMap(0);
        TeamManager.Instance.OnScoreChanged += HandleCTFScoreChanged;
        PlayerManager.Instance.OnListChanged += HandlePlayersListChanged;
    }

    public override void OnDestroy() {
        base.OnDestroy();
        if (!IsServer) return;
        if (TeamManager.Instance != null) TeamManager.Instance.OnScoreChanged -= HandleCTFScoreChanged;
        if (PlayerManager.Instance != null) PlayerManager.Instance.OnListChanged -= HandlePlayersListChanged;
    }

    private void HandleCTFScoreChanged(int red, int blue) {
        if (!IsServer) return;
        if (TeamManager.Instance.CurrentMode.Value != TeamManager.TeamMode.CaptureTheFlag) return;

        int endChoice = TeamManager.Instance.EndChoice.Value;
        int target = endChoice >= 0 && endChoice < ctfTargets.Length ? ctfTargets[endChoice] : ctfTargets[0];

        if (red >= target) {
            GameAnnouncer.Instance.TeamWin(TeamManager.Team.Red);
            PlayerAchievementsManager.Instance?.ReportTeamWinnerServer(TeamManager.Team.Red);
            SendEndMatchEvent();
            StartCoroutine(EndMatch());
        }

        if (blue >= target) {
            GameAnnouncer.Instance.TeamWin(TeamManager.Team.Blue);
            PlayerAchievementsManager.Instance?.ReportTeamWinnerServer(TeamManager.Team.Blue);
            SendEndMatchEvent();
            StartCoroutine(EndMatch());
        }
    }

    private void HandlePlayersListChanged(NetworkList<MatchParticipantData> participantsList) {
        _ = participantsList;
        if (!IsServer) return;
        if (ended) return;
        if (TeamManager.Instance.CurrentMode.Value == TeamManager.TeamMode.CaptureTheFlag) {
            // in CTF we rely on team scores, not individual kills
            return;
        }

        int endChoice = TeamManager.Instance.EndChoice.Value;
        int target = endChoice >= 0 && endChoice < killsTargets.Length ? killsTargets[endChoice] : killsTargets[0];
        var participants = PlayerManager.Instance.Participants;

        if (TeamManager.Instance.CurrentMode.Value == TeamManager.TeamMode.TwoTeams) {
            var redKills = 0;
            var blueKills = 0;
            foreach (var p in participants) {
                if (TeamManager.Instance.GetTeam(p.Id) == TeamManager.Team.Red)
                    redKills += p.Kills;
                else if (TeamManager.Instance.GetTeam(p.Id) == TeamManager.Team.Blue)
                    blueKills += p.Kills;
            }

            if (redKills >= target) {
                GameAnnouncer.Instance.TeamWin(TeamManager.Team.Red);
                PlayerAchievementsManager.Instance?.ReportTeamWinnerServer(TeamManager.Team.Red);
                SendEndMatchEvent();
                StartCoroutine(EndMatch());
                return;
            }

            if (blueKills >= target) {
                GameAnnouncer.Instance.TeamWin(TeamManager.Team.Blue);
                PlayerAchievementsManager.Instance?.ReportTeamWinnerServer(TeamManager.Team.Blue);
                SendEndMatchEvent();
                StartCoroutine(EndMatch());
            }
        } else {
            foreach (var p in participants) {
                if (p.Kills >= target) {
                    if (p.Id.IsHuman) {
                        PlayerAchievementsManager.Instance?.ReportMatchWinnerServer(p.Id.Value);
                    }

                    GameAnnouncer.Instance.PlayerWin(ParticipantIdentityCodec.Encode(p.Id));
                    SendEndMatchEvent(p.Id);
                    StartCoroutine(EndMatch());
                    return;
                }
            }
        }
    }

    private void SendEndMatchEvent(ParticipantId? playerWinner = null) {
        if (playerWinner == null) {
            FirebaseAnalytic.Instance.SendEvent("MatchEnded", new Dictionary<string, object> {
                { "map", SceneName },
                { "mode", TeamManager.Instance.CurrentMode.Value.ToString() },
                { "playerCount", LobbyManager.Instance.CurrentLobby!.Value.MemberCount }
            });
        } else {
            var winner = PlayerManager.Instance.Participants.First(p => p.Id == playerWinner);
            var winnerArchetype = ArchetypeDatabase.Instance.GetArchetype(winner.Archetype).archetypeName;
            if (playerWinner.Value.IsBot) {
                winnerArchetype += " (Bot)";
            }

            FirebaseAnalytic.Instance.SendEvent("MatchEnded", new Dictionary<string, object> {
                { "map", SceneName },
                { "mode", TeamManager.Instance.CurrentMode.Value.ToString() },
                { "playerCount", LobbyManager.Instance.CurrentLobby!.Value.MemberCount },
                { "winnerArchetype", winnerArchetype }
            });
        }
    }

    private IEnumerator EndMatch() {
        if (ended) yield break;
        ended = true;
        started = false;
        Debug.Log("[GameProgressTracker] Match ended by reaching target");

        yield return new WaitForSeconds(7f);
        BotLifecycleManager.Instance?.EndMatch();
        LobbyManager.Instance.CurrentLobby?.SetJoinable(true);
        LobbyManager.Instance.RestartLobby();
        var spawned = NetworkManager.Singleton.SpawnManager.SpawnedObjectsList.ToList();
        foreach (var networkObject in spawned) {
            if (networkObject != null
                && networkObject.IsSpawned
                && networkObject.IsSceneObject == false
                && networkObject.DestroyWithScene)
                networkObject.Despawn(true);
        }

        yield return new WaitForSeconds(0.2f);
        foreach (var player in PlayerManager.Instance.Players()) {
            PlayerManager.Instance.ResetScore(player.ClientId);
        }

        SpellInstanceLimiter.Clear();
        SceneLoader.LoadMenu();
        TeamManager.Instance.Reset();
        ended = false;
    }
}