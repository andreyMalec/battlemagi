using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Unity.Netcode;
using Steamworks;

public class SteamVoiceChat : NetworkBehaviour {
    private static bool LOG = false;

    [SerializeField] private AudioSource playerVoice;

    [Header("Voice settings")] public float voiceChatRange = 25f;
    public float bufferSeconds = 1.0f; // длина внутреннего буфера клипа

    private int sampleRate;
    private int clipLengthSamples;

    // Потоковые данные на игрока
    private class VoiceStream {
        public AudioSource source;
        public AudioClip clip;
        public Queue<float> queue = new Queue<float>(8192);
        public AudioClip.PCMReaderCallback reader; // держим ссылку, чтобы GC не собрал
        public AudioClip.PCMSetPositionCallback setPosition; // опционально
        public object lockObj = new object();
    }

    // SteamId → VoiceStream
    private readonly Dictionary<ulong, VoiceStream> streams = new Dictionary<ulong, VoiceStream>();

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        if (!IsOwner) return;

        sampleRate = (int)SteamUser.OptimalSampleRate;
        clipLengthSamples = Mathf.Max(1, Mathf.RoundToInt(sampleRate * bufferSeconds));

        PlayerManager.Instance.OnPlayerAdded += HandlePlayerAdded;
        PlayerManager.Instance.OnPlayerRemoved += HandlePlayerRemoved;

        SteamUser.VoiceRecord = true;
        if (LOG) Debug.Log("[SteamVoiceChat] Local voice recording enabled");
    }

    public override void OnNetworkDespawn() {
        base.OnNetworkDespawn();
        if (!IsOwner) return;

        if (PlayerManager.Instance != null) {
            PlayerManager.Instance.OnPlayerAdded -= HandlePlayerAdded;
            PlayerManager.Instance.OnPlayerRemoved -= HandlePlayerRemoved;
        }

        foreach (var vs in streams.Values) {
            if (vs.source) vs.source.Stop();
        }

        streams.Clear();
    }

    public void DisableMicrophone() {
        SteamUser.VoiceRecord = false;
    }

    private void Update() {
        if (!SteamClient.IsValid) return;
        if (!IsOwner) return;

        // Считываем локальный микрофон и рассылаем всем
        if (SteamUser.HasVoiceData) {
            using (var stream = new MemoryStream()) {
                int compressedBytes = SteamUser.ReadVoiceData(stream);
                if (compressedBytes > 0) {
                    var buf = stream.GetBuffer();
                    SteamId me = SteamClient.SteamId;

                    var i = new List<ulong>();
                    var lobby = LobbyManager.Instance.CurrentLobby;
                    if (lobby == null) return;
                    foreach (var member in lobby.Value.Members) {
                        if (member.Id == me) continue;
                        SteamNetworking.SendP2PPacket(member.Id, buf, compressedBytes, 0, P2PSend.Unreliable);
                        i.Add(member.Id);
                    }

                    if (LOG)
                        Debug.Log(
                            $"[SteamVoiceChat] Sent {compressedBytes} bytes to {i.Count} peers ({string.Join(", ", i)})");
                }
            }
        }

        // Принимаем пакеты
        while (SteamNetworking.IsP2PPacketAvailable()) {
            var pkt = SteamNetworking.ReadP2PPacket();
            if (pkt.HasValue) {
                var from = pkt.Value.SteamId;
                var data = pkt.Value.Data;
                // Декодируем и ставим в очередь
                ProcessIncomingVoice(from, data);
            }
        }
    }

    // ===== PlayerManager events =====
    private void HandlePlayerAdded(PlayerManager.PlayerData player) {
        if (LOG) Debug.Log($"[SteamVoiceChat] HandlePlayerAdded {player.SteamId}");
        if (streams.TryGetValue(player.SteamId, out var oldVs)) {
            if (oldVs.source != null) {
                oldVs.source.Stop();
            }
        }

        var playerObject = player.PlayerObject();
        if (playerObject == null) return;

        var src = playerObject.GetComponent<SteamVoiceChat>().playerVoice;

        var vs = new VoiceStream();
        vs.source = src;
        vs.reader = data => PcmRead(player.SteamId, data);
        vs.clip = AudioClip.Create($"VoiceClip {player.SteamId}", clipLengthSamples, 1, sampleRate, true, vs.reader,
            vs.setPosition);
        src.clip = vs.clip;
        src.Play();

        streams[player.SteamId] = vs;
        if (LOG)
            Debug.Log(
                $"[SteamVoiceChat] Created streaming AudioSource for {player.SteamId} (rate={sampleRate}, len={clipLengthSamples})");
    }

    private void HandlePlayerRemoved(PlayerManager.PlayerData player) {
        if (streams.TryGetValue(player.SteamId, out var vs)) {
            if (vs.source) vs.source.Stop();
            streams.Remove(player.SteamId);
            if (LOG) Debug.Log($"[SteamVoiceChat] Destroyed AudioSource for {player.SteamId}");
        }
    }

    // ===== Audio streaming =====
    private void PcmRead(ulong steamId, float[] data) {
        // Этот коллбэк вызывается аудио-потоком Unity.
        if (!streams.TryGetValue(steamId, out var vs)) {
            // Кто-то уже удалился — заполняем тишиной
            Array.Clear(data, 0, data.Length);
            return;
        }

        int i = 0;
        lock (vs.lockObj) {
            // Снимаем сэмплы из очереди
            while (i < data.Length && vs.queue.Count > 0) {
                data[i++] = vs.queue.Dequeue();
            }
        }

        // Если не хватило, добиваем тишиной
        for (; i < data.Length; i++)
            data[i] = 0f;
    }

    private void EnqueueSamples(ulong steamId, float[] samples) {
        VoiceStream vs;
        if (!streams.TryGetValue(steamId, out vs) || vs.source == null || vs.source.gameObject == null) {
            // Ещё не создан источник (новичок?) — просто игнорируем пакет или можно накопить в temp-буфере
            if (LOG) Debug.LogWarning($"[SteamVoiceChat] Voice from {steamId} but stream not ready yet");
            var data = PlayerManager.Instance.FindBySteamId(steamId);
            if (data.HasValue) {
                HandlePlayerAdded(data.Value);
            }

            streams.TryGetValue(steamId, out vs);

            if (vs == null)
                return;
        }

        lock (vs.lockObj) {
            // Ограничим размер очереди, чтобы не росла бесконечно (например, x4 длины клипа)
            int maxQueue = clipLengthSamples * 4;
            if (vs.queue.Count > maxQueue) {
                // если очередь переполнена — подчистим
                int drop = vs.queue.Count - maxQueue;
                for (int i = 0; i < drop; i++) vs.queue.Dequeue();
                if (LOG) Debug.LogWarning($"[SteamVoiceChat] Queue overflow for {steamId}, dropping {drop} samples");
            }

            for (int i = 0; i < samples.Length; i++)
                vs.queue.Enqueue(samples[i]);
        }
    }

    private void ProcessIncomingVoice(ulong fromSteamId, byte[] voiceData) {
        // Распаковка из Steam (16-бит PCM little-endian)
        using (var input = new MemoryStream(voiceData))
        using (var output = new MemoryStream()) {
            int uncompressedBytes = SteamUser.DecompressVoice(input, voiceData.Length, output);

            if (uncompressedBytes <= 0) {
                if (LOG) Debug.LogWarning($"[SteamVoiceChat] Decompress failed from {fromSteamId}");
                return;
            }

            byte[] buf = output.GetBuffer();
            int sampleCount = uncompressedBytes / 2;

            // Конвертация short → float [-1..1]
            var samples = new float[sampleCount];
            int bi = 0;
            float vol = PlayersVoiceSettings.Volume(fromSteamId);
            for (int si = 0; si < sampleCount; si++, bi += 2) {
                short s = (short)(buf[bi] | (buf[bi + 1] << 8));
                float f = s / 32768f;
                // Apply gain
                float scaled = f * vol;
                // Soft saturation to avoid hard clipping when vol > 1: y = x / (1 + |x|)
                samples[si] = scaled / (1f + Mathf.Abs(scaled));
            }

            EnqueueSamples(fromSteamId, samples);
            if (LOG)
                Debug.Log(
                    $"[SteamVoiceChat] From {fromSteamId}: {uncompressedBytes} bytes → {sampleCount} samples (enqueued)");
        }
    }
}