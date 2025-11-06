using System;
using Unity.Netcode;
using UnityEngine;

public class CTFAnnouncer : NetworkBehaviour {
    public static CTFAnnouncer Instance { get; private set; }
    [SerializeField] private AudioClip takeFlagClip;
    [SerializeField] private AudioClip dropFlagClip;
    [SerializeField] private AudioClip returnFlagClip;
    [SerializeField] private AudioClip captureClip;
    [SerializeField] private AudioSource sfx;
    
    public event Action<int> OnFlagTaken;
    public event Action<int> OnFlagDropped;
    public event Action<int> OnFlagReturned;
    public event Action<int> OnFlagCaptured;

    private void Awake() {
        if (Instance == null) Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void TakeFlag(TeamManager.Team takerTeam, TeamManager.Team flagTeam) {
        if (!IsServer) return;
        PlayTakeFlagClientRpc((int)takerTeam, (int)flagTeam);
    }

    public void DropFlag(TeamManager.Team flagTeam) {
        if (!IsServer) return;
        PlayDropFlagClientRpc((int)flagTeam);
    }

    public void ReturnFlag(TeamManager.Team flagTeam) {
        if (!IsServer) return;
        PlayReturnFlagClientRpc((int)flagTeam);
    }

    public void CaptureFlag(TeamManager.Team scoringTeam) {
        if (!IsServer) return;
        PlayCaptureClientRpc((int)scoringTeam);
    }

    [ClientRpc]
    private void PlayTakeFlagClientRpc(int takerTeam, int flagTeam) {
        Debug.Log($"Team {(TeamManager.Team)takerTeam} took the flag of team {(TeamManager.Team)flagTeam}");
        if (sfx != null && takeFlagClip != null) sfx.PlayOneShot(takeFlagClip);
        OnFlagTaken?.Invoke(flagTeam);
    }

    [ClientRpc]
    private void PlayDropFlagClientRpc(int flagTeam) {
        Debug.Log($"Flag of team {(TeamManager.Team)flagTeam} was dropped");
        if (sfx != null && dropFlagClip != null) sfx.PlayOneShot(dropFlagClip);
        OnFlagDropped?.Invoke(flagTeam);
    }

    [ClientRpc]
    private void PlayReturnFlagClientRpc(int flagTeam) {
        Debug.Log($"Flag of team {(TeamManager.Team)flagTeam} was returned to base");
        if (sfx != null && returnFlagClip != null) sfx.PlayOneShot(returnFlagClip);
        OnFlagReturned?.Invoke(flagTeam);
    }

    [ClientRpc]
    private void PlayCaptureClientRpc(int scoringTeam) {
        Debug.Log($"Team {(TeamManager.Team)scoringTeam} captured the flag!");
        if (sfx != null && captureClip != null) sfx.PlayOneShot(captureClip);
        OnFlagCaptured?.Invoke(scoringTeam);
    }
}