using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

public class TestBotController : MonoBehaviour {
    [SerializeField] private GameObject botPrefab;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private int archetype;
    [SerializeField] private float hue = 78f;
    [SerializeField] private float saturation = 0.5f;
    [SerializeField] private int spawnCount = 1;

    private ulong _nextBotId = 1;
    private readonly List<GameObject> _spawnedByController = new();

    [Button("Spawn Bots")]
    private void SpawnBots() {
        if (spawnCount <= 0 || botPrefab == null)
            return;

        for (int i = 0; i < spawnCount; i++) {
            var spawn = GetSpawnPoint(i);
            var bot = Instantiate(botPrefab, spawn.position, spawn.rotation);
            var participant = bot.GetComponent<ParticipantIdentity>();
            if (participant == null)
                participant = bot.AddComponent<ParticipantIdentity>();

            var botId = _nextBotId++;
            participant.SetParticipantId(ParticipantId.Bot(botId));

            var player = bot.GetComponent<Player>();
            if (player != null)
                player.ApplyPlayerState(0, archetype, hue, saturation);

            bot.name = $"Bot_{botId}";
            _spawnedByController.Add(bot);
        }
    }

    [Button("Despawn Last Spawned")]
    private void DespawnLastSpawned() {
        if (_spawnedByController.Count == 0)
            return;

        var idx = _spawnedByController.Count - 1;
        var bot = _spawnedByController[idx];
        _spawnedByController.RemoveAt(idx);
        if (bot != null)
            Destroy(bot);
    }

    [Button("Despawn Spawned By Controller")]
    private void DespawnSpawnedByController() {
        if (_spawnedByController.Count == 0)
            return;

        for (int i = _spawnedByController.Count - 1; i >= 0; i--) {
            var bot = _spawnedByController[i];
            if (bot != null)
                Destroy(bot);
        }

        _spawnedByController.Clear();
    }

    [Button("Despawn All Bots")]
    private void DespawnAllBots() {
        var allBots = FindObjectsByType<ParticipantIdentity>(FindObjectsSortMode.None);
        foreach (var participant in allBots) {
            if (!participant.Id.IsBot) continue;
            Destroy(participant.gameObject);
        }

        _spawnedByController.Clear();
    }

    private Transform GetSpawnPoint(int index) {
        if (spawnPoints != null && spawnPoints.Length > 0)
            return spawnPoints[index % spawnPoints.Length];
        return transform;
    }
}


