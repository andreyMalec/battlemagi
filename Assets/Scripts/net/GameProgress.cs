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

    private void Awake() {
        if (Instance == null) Instance = this;
        DontDestroyOnLoad(gameObject);
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
        LobbyManager.Instance.CurrentLobby?.SetJoinable(false);
        NetworkManager.Singleton.SceneManager.LoadScene(SceneName, LoadSceneMode.Single);
        started = true;

        FirebaseAnalytic.Instance.SendEvent("MatchStarted", new Dictionary<string, object> {
            { "map", SceneName },
            { "mode", TeamManager.Instance.CurrentMode.Value.ToString() },
            { "playerCount", LobbyManager.Instance.CurrentLobby!.Value.MemberCount }
        });
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
            StartCoroutine(EndMatch());
        }

        if (blue >= target) {
            GameAnnouncer.Instance.TeamWin(TeamManager.Team.Blue);
            StartCoroutine(EndMatch());
        }
    }

    private void HandlePlayersListChanged(List<PlayerManager.PlayerData> players) {
        if (!IsServer) return;
        if (TeamManager.Instance.CurrentMode.Value == TeamManager.TeamMode.CaptureTheFlag) {
            // in CTF we rely on team scores, not individual kills
            return;
        }

        int endChoice = TeamManager.Instance.EndChoice.Value;
        int target = endChoice >= 0 && endChoice < killsTargets.Length ? killsTargets[endChoice] : killsTargets[0];

        if (TeamManager.Instance.CurrentMode.Value == TeamManager.TeamMode.TwoTeams) {
            var redKills = 0;
            var blueKills = 0;
            foreach (var p in players) {
                if (TeamManager.Instance.GetTeam(p.ClientId) == TeamManager.Team.Red)
                    redKills += p.Kills;
                else if (TeamManager.Instance.GetTeam(p.ClientId) == TeamManager.Team.Blue)
                    blueKills += p.Kills;
            }

            if (redKills >= target) {
                GameAnnouncer.Instance.TeamWin(TeamManager.Team.Red);
                StartCoroutine(EndMatch());
                return;
            }

            if (blueKills >= target) {
                GameAnnouncer.Instance.TeamWin(TeamManager.Team.Blue);
                StartCoroutine(EndMatch());
            }
        } else {
            foreach (var p in players) {
                if (p.Kills >= target) {
                    GameAnnouncer.Instance.PlayerWin(p.ClientId);
                    StartCoroutine(EndMatch());
                    return;
                }
            }
        }
    }

    private IEnumerator EndMatch() {
        if (ended) yield break;
        ended = true;
        started = false;
        Debug.Log("[GameProgressTracker] Match ended by reaching target");

        yield return new WaitForSeconds(7f);
        LobbyManager.Instance.CurrentLobby?.SetJoinable(true);
        LobbyManager.Instance.RestartLobby();
        var spawned = NetworkManager.Singleton.SpawnManager.SpawnedObjectsList.ToList();
        foreach (var networkObject in spawned) {
            if (networkObject.IsSceneObject == false && networkObject.DestroyWithScene)
                networkObject.Despawn(true);
        }

        yield return new WaitForSeconds(0.2f);
        foreach (var player in PlayerManager.Instance.Players()) {
            PlayerManager.Instance.ResetScore(player.ClientId);
        }

        SpellInstanceLimiter.Clear();
        NetworkManager.Singleton.SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
        TeamManager.Instance.Reset();
        ended = false;
    }
}