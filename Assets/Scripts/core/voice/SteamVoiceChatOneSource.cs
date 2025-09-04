// using System;
// using UnityEngine;
// using System.Collections.Generic;
// using System.IO;
// using System.Linq;
// using System.Net.Sockets;
// using Steamworks;
// using Unity.Netcode;
//
// public class SteamVoiceChatOld : NetworkBehaviour {
//     [SerializeField] AudioSource audioSource;
//
//     public float voiceChatRange = 15f;
//
//     private MemoryStream input = new MemoryStream();
//     private MemoryStream stream = new MemoryStream();
//     private MemoryStream output = new MemoryStream();
//
//     private int optimalRate;
//     private int clipBufferSize;
//     private float[] clipBuffer;
//
//     private int playbackBuffer;
//     private int dataPosition;
//     private int dataReceived;
//
//     public override void OnNetworkSpawn() {
//         base.OnNetworkSpawn();
//
//         if (!IsOwner)
//             GetComponent<AudioListener>().enabled = false;
//
//         SteamUser.VoiceRecord = true;
//     }
//
//     private void Start() {
//         optimalRate = (int)SteamUser.OptimalSampleRate;
//
//         clipBufferSize = optimalRate * 5;
//         clipBuffer = new float[clipBufferSize];
//
//
//         audioSource.clip = AudioClip.Create("VoiceData", (int)256, 1, (int)optimalRate, true, OnAudioRead, null);
//         audioSource.loop = true;
//         audioSource.Play();
//     }
//
//     private void OnAudioRead(float[] data) {
//         for (int i = 0; i < data.Length; ++i) {
//             // start with silence
//             data[i] = 0;
//
//             // do I  have anything to play?
//             if (playbackBuffer > 0) {
//                 // current data position playing
//                 dataPosition = (dataPosition + 1) % clipBufferSize;
//
//                 data[i] = clipBuffer[dataPosition];
//
//                 playbackBuffer--;
//             }
//         }
//     }
//
//
//     void Update() {
//         if (!SteamClient.IsValid) return;
//
//         if (SteamUser.HasVoiceData) {
//             int compressedWritten = SteamUser.ReadVoiceData(stream);
//             stream.Position = 0;
//             SendVoice(stream.GetBuffer(), compressedWritten);
//         }
//         
//         ReceiveVoice();
//     }
//
//     void SendVoice(byte[] data, int iSize) {
//         SteamId localPlayer = SteamClient.SteamId;
//
//         // Отправляем друзьям в игре
//         foreach (var friend in LobbyHolder.instance.currentLobby?.Members) {
//             if (friend.Id == localPlayer)
//                 continue;
//
//             SteamNetworking.SendP2PPacket(friend.Id, data, iSize, 0, P2PSend.Unreliable);
//         }
//     }
//
//     void ReceiveVoice() {
//         // Читаем входящие P2P пакеты
//         while (SteamNetworking.IsP2PPacketAvailable()) {
//             var packet = SteamNetworking.ReadP2PPacket();
//             if (packet.HasValue) {
//                 ProcessVoicePacket(packet.Value.SteamId, packet.Value.Data);
//             }
//         }
//     }
//
//     void ProcessVoicePacket(SteamId steamId, byte[] voiceData) {
//         // Обработка голосовых данных
//         Debug.Log($"Received voice from {steamId}: {voiceData.Length} bytes");
//
//         foreach (var player in LobbyHolder.instance.players) {
//             if (player.steamId == steamId)
//                 VoiceData(voiceData, voiceData.Length);
//         }
//     }
//
//     private void VoiceData(byte[] compressed, int bytesWritten) {
//         input.Write(compressed, 0, bytesWritten);
//         input.Position = 0;
//
//         int uncompressedWritten = SteamUser.DecompressVoice(input, bytesWritten, output);
//         input.Position = 0;
//
//         byte[] outputBuffer = output.GetBuffer();
//         for (int i = 0; i < uncompressedWritten; i += 2) {
//             // insert converted float to buffer
//             float converted = (short)(outputBuffer[i] | outputBuffer[i + 1] << 8) / 32767.0f;
//             clipBuffer[dataReceived] = converted;
//
//             // buffer loop
//             dataReceived = (dataReceived + 1) % clipBufferSize;
//
//             playbackBuffer++;
//         }
//         output.Position = 0;
//     }
// }