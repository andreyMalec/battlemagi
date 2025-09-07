using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEngine;
using Unity.Netcode;
using Steamworks;

public class SteamVoiceChat : NetworkBehaviour {
    [Header("Voice settings")] public float voiceChatRange = 15f;
    public float bufferSeconds = 2.0f; // длина внутреннего буфера клипа

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
    private readonly Dictionary<SteamId, VoiceStream> streams = new Dictionary<SteamId, VoiceStream>();

    private void OnEnable() {
        sampleRate = (int)SteamUser.OptimalSampleRate;
        clipLengthSamples = Mathf.Max(1, Mathf.RoundToInt(sampleRate * bufferSeconds));

        if (PlayerManager.Instance != null) {
            PlayerManager.Instance.OnPlayerAdded += HandlePlayerAdded;
            PlayerManager.Instance.OnPlayerRemoved += HandlePlayerRemoved;

            // На случай, если кто-то уже был в момент включения
            foreach (var kv in PlayerManager.Instance.GetAllPlayers())
                HandlePlayerAdded(kv.Key, kv.Value);
        } else {
            Debug.LogWarning("[SteamVoiceChat] PlayerManager.Instance is null");
        }
    }

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();

        if (IsOwner) {
            if (LobbyHolder.instance.currentLobby?.MemberCount == 1) return;
            SteamUser.VoiceRecord = true;
            Debug.Log("[SteamVoiceChat] Local voice recording enabled");
        }
    }

    private void OnDisable() {
        if (PlayerManager.Instance != null) {
            PlayerManager.Instance.OnPlayerAdded -= HandlePlayerAdded;
            PlayerManager.Instance.OnPlayerRemoved -= HandlePlayerRemoved;
        }

        foreach (var vs in streams.Values) {
            if (vs.source) vs.source.Stop();
            if (vs.source) Destroy(vs.source.gameObject);
        }

        streams.Clear();
    }

    private void Update() {
        if (!SteamClient.IsValid) return;

        // Считываем локальный микрофон и рассылаем всем
        if (IsOwner && SteamUser.HasVoiceData) {
            using (var stream = new MemoryStream()) {
                int compressedBytes = SteamUser.ReadVoiceData(stream);
                if (compressedBytes > 0) {
                    var buf = stream.GetBuffer();
                    SteamId me = SteamClient.SteamId;

                    foreach (var member in LobbyHolder.instance.currentLobby?.Members) {
                        if (member.Id == me) continue;
                        SteamNetworking.SendP2PPacket(member.Id, buf, compressedBytes, 0, P2PSend.Unreliable);
                    }

                    Debug.Log($"[SteamVoiceChat] Sent {compressedBytes} bytes to peers");
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
    private void HandlePlayerAdded(SteamId sid, Transform playerTransform) {
        if (streams.ContainsKey(sid)) return;

        // Создаём объект-источник звука у игрока
        var go = new GameObject($"Voice_{sid}");
        go.transform.SetParent(playerTransform);
        go.transform.localPosition = Vector3.zero;

        var src = go.AddComponent<AudioSource>();
        src.playOnAwake = true;
        src.loop = true;
        src.spatialBlend = 1f;
        src.minDistance = 1f;
        src.maxDistance = voiceChatRange;
        src.rolloffMode = AudioRolloffMode.Linear;
        src.dopplerLevel = 0f;

        var vs = new VoiceStream();
        vs.source = src;

        // Создаём стриминговый клип с PCM-callback (никаких SetData!)
        vs.reader = data => PcmRead(sid, data);
        vs.setPosition = pos => {
            /* можно логать, но не обязательно */
        };

        vs.clip = AudioClip.Create($"VoiceClip {sid}", clipLengthSamples, 1, sampleRate, true, vs.reader,
            vs.setPosition);
        src.clip = vs.clip;
        src.Play();

        streams[sid] = vs;

        Debug.Log(
            $"[SteamVoiceChat] Created streaming AudioSource for {sid} (rate={sampleRate}, len={clipLengthSamples})");
    }

    private void HandlePlayerRemoved(SteamId sid) {
        if (streams.TryGetValue(sid, out var vs)) {
            if (vs.source) vs.source.Stop();
            if (vs.source) Destroy(vs.source.gameObject);
            streams.Remove(sid);
            Debug.Log($"[SteamVoiceChat] Destroyed AudioSource for {sid}");
        }
    }

    // ===== Audio streaming =====
    private void PcmRead(SteamId sid, float[] data) {
        // Этот коллбэк вызывается аудио-потоком Unity.
        if (!streams.TryGetValue(sid, out var vs)) {
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

    private void EnqueueSamples(SteamId sid, float[] samples) {
        if (!streams.TryGetValue(sid, out var vs)) {
            // Ещё не создан источник (новичок?) — просто игнорируем пакет или можно накопить в temp-буфере
            Debug.LogWarning($"[SteamVoiceChat] Voice from {sid} but stream not ready yet");
            return;
        }

        lock (vs.lockObj) {
            // Ограничим размер очереди, чтобы не росла бесконечно (например, x4 длины клипа)
            int maxQueue = clipLengthSamples * 4;
            if (vs.queue.Count > maxQueue) {
                // если очередь переполнена — подчистим (дропнем лишнее)
                int drop = vs.queue.Count - maxQueue;
                for (int i = 0; i < drop; i++) vs.queue.Dequeue();
                Debug.LogWarning($"[SteamVoiceChat] Queue overflow for {sid}, dropping {drop} samples");
            }

            for (int i = 0; i < samples.Length; i++)
                vs.queue.Enqueue(samples[i]);
        }
    }

    private void ProcessIncomingVoice(SteamId from, byte[] voiceData) {
        // Распаковка из Steam (16-бит PCM little-endian)
        using (var input = new MemoryStream(voiceData))
        using (var output = new MemoryStream()) {
            int uncompressedBytes = SteamUser.DecompressVoice(input, voiceData.Length, output);

            if (uncompressedBytes <= 0) {
                Debug.LogWarning($"[SteamVoiceChat] Decompress failed from {from}");
                return;
            }

            byte[] buf = output.GetBuffer();
            int sampleCount = uncompressedBytes / 2;

            // Конвертация short → float [-1..1]
            var samples = new float[sampleCount];
            int bi = 0;
            for (int si = 0; si < sampleCount; si++, bi += 2) {
                short s = (short)(buf[bi] | (buf[bi + 1] << 8));
                samples[si] = s / 32768f;
            }

            EnqueueSamples(from, samples);
            Debug.Log($"[SteamVoiceChat] From {from}: {uncompressedBytes} bytes → {sampleCount} samples (enqueued)");
        }
    }
}