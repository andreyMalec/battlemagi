using System;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using Steamworks;
using Unity.Netcode;

public class SteamVoiceChat : NetworkBehaviour 
{
    public float voiceChatRange = 15f;

    private MemoryStream inputStream = new MemoryStream();
    private MemoryStream outputStream = new MemoryStream();

    // Буферы для каждого игрока
    private Dictionary<SteamId, float[]> playerAudioBuffers = new Dictionary<SteamId, float[]>();
    private Dictionary<SteamId, int> playerBufferPositions = new Dictionary<SteamId, int>();
    private Dictionary<SteamId, AudioClip> playerAudioClips = new Dictionary<SteamId, AudioClip>();
    private Dictionary<SteamId, int> playerSamplesWritten = new Dictionary<SteamId, int>();

    private int optimalRate;
    private int bufferSize;
    private const int bufferSeconds = 2; // 2 секунды буфера

    public override void OnNetworkSpawn() 
    {
        base.OnNetworkSpawn();

        if (IsOwner) 
        {
            SteamUser.VoiceRecord = true;
        }

        if (!IsOwner)
            GetComponent<AudioListener>().enabled = false;
    }

    private void Start() 
    {
        optimalRate = (int)SteamUser.OptimalSampleRate;
        bufferSize = optimalRate * bufferSeconds;
    }

    void Update() 
    {
        if (!SteamClient.IsValid) return;

        // Отправка голоса (только владелец)
        if (IsOwner && SteamUser.HasVoiceData) 
        {
            SendVoiceData();
        }

        // Прием голоса
        ReceiveVoiceData();
    }

    void SendVoiceData() 
    {
        using (var tempStream = new MemoryStream())
        {
            int compressedWritten = SteamUser.ReadVoiceData(tempStream);
            if (compressedWritten > 0)
            {
                byte[] voiceData = tempStream.ToArray();
                SendToOtherPlayers(voiceData, compressedWritten);
            }
        }
    }

    void SendToOtherPlayers(byte[] voiceData, int size) 
    {
        SteamId localPlayer = SteamClient.SteamId;

        foreach (var member in LobbyHolder.instance.currentLobby?.Members) 
        {
            // if (member.Id == localPlayer) continue;

            SteamNetworking.SendP2PPacket(member.Id, voiceData, size, 0, P2PSend.Unreliable);
        }
    }

    void ReceiveVoiceData() 
    {
        while (SteamNetworking.IsP2PPacketAvailable()) 
        {
            var packet = SteamNetworking.ReadP2PPacket();
            if (packet.HasValue) 
            {
                ProcessIncomingVoice(packet.Value.SteamId, packet.Value.Data);
            }
        }
    }

    void ProcessIncomingVoice(SteamId steamId, byte[] compressedVoiceData) 
    {
        Debug.Log($"Received voice from {steamId}: {compressedVoiceData.Length} bytes");
        // Находим игрока по SteamId
        if (!LobbyHolder.instance.players.TryGetValue(steamId, out var player))
            return;
        if (player == null) return;

        // Получаем AudioSource игрока
        var playerAudioSource = player.GetComponentInChildren<AudioSource>();
        if (playerAudioSource == null) return;

        // Обрабатываем голосовые данные
        ProcessVoiceForPlayer(steamId, compressedVoiceData, playerAudioSource);
    }

    void ProcessVoiceForPlayer(SteamId steamId, byte[] compressedData, AudioSource audioSource) 
    {
        // Инициализируем буферы для игрока, если нужно
        if (!playerAudioBuffers.ContainsKey(steamId)) 
        {
            InitializePlayerAudio(steamId, audioSource);
        }

        // Декомпрессируем голосовые данные
        inputStream.SetLength(0);
        inputStream.Write(compressedData, 0, compressedData.Length);
        inputStream.Position = 0;

        int uncompressedSize = SteamUser.DecompressVoice(inputStream, compressedData.Length, outputStream);

        if (uncompressedSize > 0) 
        {
            // Конвертируем в float аудио
            byte[] pcmData = outputStream.ToArray();
            AddAudioToPlayerBuffer(steamId, pcmData, uncompressedSize);
        }

        inputStream.SetLength(0);
        outputStream.SetLength(0);
    }

    void InitializePlayerAudio(SteamId steamId, AudioSource audioSource) 
    {
        // Создаем буфер для аудио данных
        playerAudioBuffers[steamId] = new float[bufferSize];
        playerBufferPositions[steamId] = 0;
        playerSamplesWritten[steamId] = 0;

        // Создаем AudioClip для этого игрока
        AudioClip clip = AudioClip.Create($"Voice_{steamId}", bufferSize, 1, optimalRate, false);
        playerAudioClips[steamId] = clip;

        // Настраиваем AudioSource
        audioSource.clip = clip;
        audioSource.loop = false; // Не зацикливаем
        audioSource.spatialBlend = 1.0f;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.maxDistance = voiceChatRange;
        audioSource.Play();
    }

    void AddAudioToPlayerBuffer(SteamId steamId, byte[] pcmData, int dataSize) 
    {
        float[] buffer = playerAudioBuffers[steamId];
        int position = playerBufferPositions[steamId];
        int samplesWritten = playerSamplesWritten[steamId];

        // Конвертируем 16-bit PCM в float
        int samplesToProcess = Mathf.Min(dataSize / 2, bufferSize - samplesWritten);
        
        for (int i = 0; i < samplesToProcess * 2; i += 2) 
        {
            if (i + 1 < pcmData.Length) 
            {
                short sample = (short)((pcmData[i + 1] << 8) | pcmData[i]);
                float sampleFloat = sample / 32768.0f;

                buffer[position] = sampleFloat;
                position = (position + 1) % bufferSize;
            }
        }

        playerBufferPositions[steamId] = position;
        playerSamplesWritten[steamId] += samplesToProcess;

        // Если буфер заполнен, воспроизводим и очищаем
        if (playerSamplesWritten[steamId] >= bufferSize)
        {
            PlayAndResetBuffer(steamId);
        }
        else
        {
            // Частичное обновление AudioClip
            UpdatePlayerAudioClip(steamId, 0, playerSamplesWritten[steamId]);
        }
    }

    void PlayAndResetBuffer(SteamId steamId)
    {
        if (playerAudioClips.TryGetValue(steamId, out AudioClip clip) &&
            playerAudioBuffers.TryGetValue(steamId, out float[] buffer))
        {
            // Обновляем весь AudioClip
            clip.SetData(buffer, 0);
            
            // Воспроизводим
            if (LobbyHolder.instance.players.TryGetValue(steamId, out var player))
            {
                var audioSource = player.GetComponentInChildren<AudioSource>();
                if (audioSource != null)
                {
                    audioSource.Stop();
                    audioSource.Play();
                }
            }

            // Очищаем буфер
            Array.Clear(buffer, 0, buffer.Length);
            playerBufferPositions[steamId] = 0;
            playerSamplesWritten[steamId] = 0;
        }
    }

    void UpdatePlayerAudioClip(SteamId steamId, int offset, int samples)
    {
        if (playerAudioClips.TryGetValue(steamId, out AudioClip clip) &&
            playerAudioBuffers.TryGetValue(steamId, out float[] buffer) &&
            samples > 0)
        {
            // Обновляем только часть AudioClip
            float[] tempBuffer = new float[samples];
            Array.Copy(buffer, offset, tempBuffer, 0, samples);
            clip.SetData(tempBuffer, offset);
        }
    }

    private void OnDestroy() 
    {
        // Очистка ресурсов
        foreach (var clip in playerAudioClips.Values) 
        {
            if (clip != null)
                Destroy(clip);
        }

        playerAudioBuffers.Clear();
        playerBufferPositions.Clear();
        playerAudioClips.Clear();
        playerSamplesWritten.Clear();

        if (inputStream != null)
            inputStream.Dispose();
        if (outputStream != null)
            outputStream.Dispose();
    }
}