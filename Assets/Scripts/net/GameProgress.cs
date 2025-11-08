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

    private bool ended = false;

    private const string game0 = "Game";
    private const string game1 = "Game 3";
    private const string game2 = "Game 2";
    private const string test = "Test";

    public string SceneName = game0;
    public NetworkVariable<int> SelectedMap = new();

    private void Awake() {
        if (Instance == null) Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SelectMap(int mapIndex) {
        if (!IsServer) return;
        var map = game0;
        if (mapIndex == 1)
            map = game1;
        if (mapIndex == 2)
            map = game2;
        SceneName = map;
        SelectedMap.Value = mapIndex;
        Debug.Log($"[GameProgress] Selected map: {SceneName} mapIndex={mapIndex}");
    }

    public void StartMatch() {
        if (!IsServer) return;
        LobbyManager.Instance.CurrentLobby?.SetJoinable(false);
        NetworkManager.Singleton.SceneManager.LoadScene(SceneName, LoadSceneMode.Single);
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

        foreach (var p in players) {
            if (p.Kills >= target) {
                GameAnnouncer.Instance.PlayerWin(p.ClientId);
                StartCoroutine(EndMatch());
                return;
            }
        }
    }

    private IEnumerator EndMatch() {
        if (ended) yield break;
        ended = true;
        Debug.Log("[GameProgressTracker] Match ended by reaching target");

        yield return new WaitForSeconds(10f);
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

        NetworkManager.Singleton.SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
        TeamManager.Instance.Reset();
        ended = false;
    }
}