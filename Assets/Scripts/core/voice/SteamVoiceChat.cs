using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Steamworks;
using Unity.Netcode;

public class SteamVoiceChat : NetworkBehaviour {
    public float voiceChatRange = 15f;

    private Dictionary<SteamId, AudioSource> audioSources = new Dictionary<SteamId, AudioSource>();

    private int optimalRate;
    private int clipBufferSize;

    private void Start() {
        optimalRate = (int)SteamUser.OptimalSampleRate;
        clipBufferSize = optimalRate * 5;

        if (IsOwner) {
            SteamUser.VoiceRecord = true;
            Debug.Log("[SteamVoiceChat] Голосовая запись включена для локального игрока");
        }

        if (!IsOwner)
            GetComponent<AudioListener>().enabled = false;
    }

    private void Update() {
        if (!SteamClient.IsValid) return;

        if (IsOwner && SteamUser.HasVoiceData) {
            using var stream = new MemoryStream();
            int compressedWritten = SteamUser.ReadVoiceData(stream);
            if (compressedWritten > 0) {
                Debug.Log($"[SteamVoiceChat] Прочитано {compressedWritten} байт голоса, отправляем другим игрокам");
                SendVoice(stream.GetBuffer(), compressedWritten);
            }
        }

        ReceiveVoice();
    }

    private void SendVoice(byte[] data, int size) {
        SteamId localPlayer = SteamClient.SteamId;

        foreach (var friend in LobbyHolder.instance.currentLobby?.Members) {
            if (friend.Id == localPlayer) continue;

            SteamNetworking.SendP2PPacket(friend.Id, data, size, 0, P2PSend.Unreliable);
            Debug.Log($"[SteamVoiceChat] Отправлен голос игроку {friend.Id} ({size} байт)");
        }
    }

    private void ReceiveVoice() {
        while (SteamNetworking.IsP2PPacketAvailable()) {
            var packet = SteamNetworking.ReadP2PPacket();
            if (packet.HasValue) {
                Debug.Log(
                    $"[SteamVoiceChat] Получен пакет голоса от {packet.Value.SteamId}, размер {packet.Value.Data.Length} байт");
                ProcessVoicePacket(packet.Value.SteamId, packet.Value.Data);
            }
        }
    }

    private void ProcessVoicePacket(SteamId steamId, byte[] voiceData) {
        if (!audioSources.ContainsKey(steamId)) {
            Transform playerTransform = PlayerManager.Instance.GetPlayerTransform(steamId);
            if (playerTransform == null) {
                Debug.LogWarning($"[SteamVoiceChat] Нет Transform для {steamId}, голос не будет воспроизведён");
                return;
            }

            GameObject go = new GameObject($"Voice_{steamId}");
            go.transform.SetParent(playerTransform);
            go.transform.localPosition = Vector3.zero;

            AudioSource source = go.AddComponent<AudioSource>();
            source.spatialBlend = 1f;
            source.loop = true;
            source.playOnAwake = true;
            source.minDistance = 1f;
            source.maxDistance = voiceChatRange;

            source.clip = AudioClip.Create($"VoiceClip_{steamId}", clipBufferSize, 1, optimalRate, true);
            source.Play();

            audioSources[steamId] = source;

            Debug.Log($"[SteamVoiceChat] Создан новый AudioSource для {steamId}");
        }

        AudioSource audioSource = audioSources[steamId];

        using var input = new MemoryStream(voiceData);
        using var output = new MemoryStream();

        int uncompressedWritten = SteamUser.DecompressVoice(input, voiceData.Length, output);

        if (uncompressedWritten > 0) {
            byte[] buffer = output.GetBuffer();
            float[] samples = new float[uncompressedWritten / 2];

            for (int i = 0; i < uncompressedWritten; i += 2) {
                short sample = (short)(buffer[i] | buffer[i + 1] << 8);
                samples[i / 2] = sample / 32767.0f;
            }

            audioSource.clip.SetData(samples, 0);
            Debug.Log(
                $"[SteamVoiceChat] Декомпрессия успешна: {uncompressedWritten} байт → {samples.Length} сэмплов для {steamId}");
        } else {
            Debug.LogWarning($"[SteamVoiceChat] Не удалось декомпрессировать голосовые данные от {steamId}");
        }
    }
}