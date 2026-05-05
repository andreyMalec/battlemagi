using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BotLifecycleManager : MonoBehaviour {
    [SerializeField] private int initialBotCount;
    [SerializeField] private float respawnDelay = 5f;
    [SerializeField] private bool autoSpawnOffline = true;

    public static BotLifecycleManager Instance { get; private set; }

    private readonly Dictionary<ulong, GameObject> _activeBots = new();
    private readonly HashSet<ulong> _pendingRespawn = new();
    private readonly Dictionary<ulong, Coroutine> _respawnCoroutines = new();

    private ulong _nextBotId = 1;
    private bool _matchBotsEnabled;
    private bool _spawnQueued;
    private bool _networkSceneSubscribed;
    private NetworkManager _subscribedNetworkManager;

    private void Awake() {
        if (Instance == null) Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start() {
        SceneManager.sceneLoaded += OnSceneLoaded;
        StartCoroutine(SubscribeNetworkSceneEventsWhenReady());
    }

    private void OnDestroy() {
        if (Instance == this)
            Instance = null;

        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (_networkSceneSubscribed && _subscribedNetworkManager != null && _subscribedNetworkManager.SceneManager != null)
            _subscribedNetworkManager.SceneManager.OnLoadEventCompleted -= OnNetworkSceneLoaded;
    }

    private IEnumerator SubscribeNetworkSceneEventsWhenReady() {
        while (!_networkSceneSubscribed) {
            var networkManager = NetworkManager.Singleton;
            if (networkManager != null && networkManager.SceneManager != null) {
                networkManager.SceneManager.OnLoadEventCompleted += OnNetworkSceneLoaded;
                _subscribedNetworkManager = networkManager;
                _networkSceneSubscribed = true;
                yield break;
            }

            yield return null;
        }
    }

    public void BeginMatch() {
        _matchBotsEnabled = true;
    }

    public void EndMatch() {
        _matchBotsEnabled = false;
        DespawnAllBots();
    }

    public void SpawnInitialBotsNow() {
        if (initialBotCount <= 0) return;
        if (PlayerSpawner.instance == null || PlayerManager.Instance == null || TeamManager.Instance == null) {
            QueueSpawnWhenReady();
            return;
        }

        for (int i = 0; i < initialBotCount; i++) {
            SpawnNewBot();
        }
    }

    public ParticipantId SpawnNewBot(int archetype = 0, float hue = 78f, float saturation = 0.5f) {
        var botId = _nextBotId++;
        var participantId = ParticipantId.Bot(botId);

        var participantData = new MatchParticipantData(participantId, 0) {
            Archetype = archetype,
            Hue = hue,
            Saturation = saturation
        };

        PlayerManager.Instance.RegisterParticipant(participantData);
        TeamManager.Instance.RegisterBot(botId);

        var bot = PlayerSpawner.instance.SpawnBotObject(botId);
        if (bot != null)
            _activeBots[botId] = bot;

        return participantId;
    }

    public void HandleBotDeath(ParticipantId participantId, GameObject botObject) {
        if (!participantId.IsBot) return;
        var botId = participantId.Value;

        if (botObject != null)
            DespawnBotObject(botObject);

        _activeBots.Remove(botId);

        if (_pendingRespawn.Contains(botId))
            return;

        _pendingRespawn.Add(botId);
        _respawnCoroutines[botId] = StartCoroutine(RespawnBot(botId));
    }

    public bool DespawnBot(ulong botId, bool removeFromRegistry = true) {
        var despawned = false;

        if (_respawnCoroutines.TryGetValue(botId, out var coroutine) && coroutine != null)
            StopCoroutine(coroutine);

        _respawnCoroutines.Remove(botId);
        _pendingRespawn.Remove(botId);

        if (_activeBots.TryGetValue(botId, out var botObject) && botObject != null) {
            DespawnBotObject(botObject);
            despawned = true;
        }

        _activeBots.Remove(botId);

        if (removeFromRegistry) {
            var participantId = ParticipantId.Bot(botId);
            PlayerManager.Instance.RemoveParticipant(participantId);
            TeamManager.Instance.RemoveBot(botId);
        }

        return despawned;
    }

    public void DespawnAllBots() {
        foreach (var coroutine in _respawnCoroutines.Values) {
            if (coroutine != null)
                StopCoroutine(coroutine);
        }

        _respawnCoroutines.Clear();
        _pendingRespawn.Clear();

        var botIds = new List<ulong>(_activeBots.Keys);
        foreach (var botId in botIds) {
            DespawnBot(botId, false);
        }

        _activeBots.Clear();

        foreach (var participant in PlayerManager.Instance.Participants) {
            if (!participant.Id.IsBot) continue;
            PlayerManager.Instance.RemoveParticipant(participant.Id);
            TeamManager.Instance.RemoveBot(participant.Id.Value);
        }
    }

    private IEnumerator RespawnBot(ulong botId) {
        yield return new WaitForSeconds(respawnDelay);
        _pendingRespawn.Remove(botId);
        _respawnCoroutines.Remove(botId);

        if (!_matchBotsEnabled)
            yield break;

        if (!PlayerManager.Instance.TryGetBotData(botId, out _))
            yield break;

        var bot = PlayerSpawner.instance.SpawnBotObject(botId);
        if (bot != null)
            _activeBots[botId] = bot;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
        if (!autoSpawnOffline) return;

        var hasNetcodeSession = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
        if (hasNetcodeSession)
            return;

        if (scene.name == "MainMenu") {
            EndMatch();
            return;
        }

        if (_matchBotsEnabled || initialBotCount <= 0)
            return;

        BeginMatch();
        QueueSpawnWhenReady();
    }

    private void OnNetworkSceneLoaded(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut) {
        if (!_matchBotsEnabled) return;
        if (sceneName == "MainMenu") {
            EndMatch();
            return;
        }

        if (GameProgress.Instance != null && sceneName != GameProgress.Instance.SceneName)
            return;

        if (_activeBots.Count > 0)
            return;

        QueueSpawnWhenReady();
    }

    private void QueueSpawnWhenReady() {
        if (_spawnQueued)
            return;
        _spawnQueued = true;
        StartCoroutine(WaitForSpawnDependencies());
    }

    private IEnumerator WaitForSpawnDependencies() {
        while (_matchBotsEnabled && (PlayerSpawner.instance == null || PlayerManager.Instance == null || TeamManager.Instance == null)) {
            yield return null;
        }

        _spawnQueued = false;
        if (!_matchBotsEnabled)
            yield break;

        if (_activeBots.Count == 0)
            SpawnInitialBotsNow();
    }

    private static void DespawnBotObject(GameObject botObject) {
        if (botObject.TryGetComponent<NetworkObject>(out var networkObject) && networkObject.IsSpawned) {
            networkObject.Despawn(true);
            return;
        }

        Destroy(botObject);
    }
}


