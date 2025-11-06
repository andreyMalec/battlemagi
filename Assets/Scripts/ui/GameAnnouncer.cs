using System;
using Unity.Netcode;
using UnityEngine;

public class GameAnnouncer : NetworkBehaviour {
    public static GameAnnouncer Instance { get; private set; }
    [SerializeField] private AudioClip gameEndClip;
    [SerializeField] private AudioSource sfx;

    public event Action<int> OnTeamWin;
    public event Action<ulong> OnPlayerWin;

    private void Awake() {
        if (Instance == null) Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void TeamWin(TeamManager.Team team) {
        if (!IsServer) return;
        PlayTeamWinClientRpc((int)team);
    }

    public void PlayerWin(ulong clientId) {
        if (!IsServer) return;
        PlayPlayerWinClientRpc(clientId);
    }

    [ClientRpc]
    private void PlayTeamWinClientRpc(int team) {
        Debug.Log($"Team {(TeamManager.Team)team} wins the game!");
        if (sfx != null && gameEndClip != null) sfx.PlayOneShot(gameEndClip);
        OnTeamWin?.Invoke(team);
    }

    [ClientRpc]
    private void PlayPlayerWinClientRpc(ulong clientId) {
        Debug.Log($"Player_{clientId} wins the game!");
        if (sfx != null && gameEndClip != null) sfx.PlayOneShot(gameEndClip);
        OnPlayerWin?.Invoke(clientId);
    }
}