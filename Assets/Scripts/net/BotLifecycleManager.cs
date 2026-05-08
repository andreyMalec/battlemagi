using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    private Coroutine _netSubscribeCoroutine;

    private void Awake() {
        if (Instance == null) Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start() {
        SceneManager.sceneLoaded += OnSceneLoaded;
        _netSubscribeCoroutine = StartCoroutine(SubscribeNetworkSceneEventsWhenReady());
    }

    private void OnDestroy() {
        if (Instance == this)
            Instance = null;

        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (_networkSceneSubscribed && _subscribedNetworkManager != null &&
            _subscribedNetworkManager.SceneManager != null)
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
        DespawnAllBots();
        _matchBotsEnabled = true;
    }

    public void EndMatch() {
        _matchBotsEnabled = false;
        DespawnAllBots();
    }

    public void SpawnInitialBotsNow() {
        if (PlayerSpawner.instance == null || PlayerManager.Instance == null || TeamManager.Instance == null) {
            QueueSpawnWhenReady();
            return;
        }

        var hasNetcodeSession = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
        if (hasNetcodeSession && LobbyManager.Instance != null && LobbyManager.Instance.CurrentLobby.HasValue) {
            var lobbyBots = LobbyBotRosterData.LoadFromLobby(LobbyManager.Instance.CurrentLobby.Value);
            for (int i = 0; i < lobbyBots.Count; i++) {
                var bot = lobbyBots[i];
                SpawnNewBot(bot.archetype, bot.hue, bot.saturation, bot.team);
            }

            return;
        }

        if (initialBotCount <= 0) return;

        for (int i = 0; i < initialBotCount; i++) {
            SpawnNewBot(Random.Range(1, 4), Random.Range(0f, 360f), Random.Range(0f, 1f));
        }
    }

    public ParticipantId SpawnNewBot(
        int archetype = 0,
        float hue = 78f,
        float saturation = 0.5f,
        TeamManager.Team requestedTeam = TeamManager.Team.None
    ) {
        var botId = _nextBotId++;
        var participantId = ParticipantId.Bot(botId);

        TeamManager.Instance.RegisterBot(botId, requestedTeam);
        var assignedTeam = TeamManager.Instance.GetTeam(participantId);
        if (TeamManager.Instance.isTeamMode) {
            if (assignedTeam == TeamManager.Team.Blue) {
                hue = 228f;
                saturation = 0.85f;
            } else if (assignedTeam == TeamManager.Team.Red) {
                hue = 0f;
                saturation = 0.85f;
            }
        }

        var participantData = new MatchParticipantData(participantId, 0) {
            Archetype = archetype,
            Hue = hue,
            Saturation = saturation
        };

        PlayerManager.Instance.RegisterParticipant(participantData);

        var bot = PlayerSpawner.instance.SpawnBotObject(botId);
        if (bot != null)
            _activeBots[botId] = bot;

        return participantId;
    }

    public void HandleBotDeath(ParticipantId participantId, GameObject botObject) {
        if (!participantId.IsBot) return;
        var botId = participantId.Value;

        if (botObject != null) {
            botObject.GetComponentInChildren<MeshController>().SetRagdoll(true);
            var bot = botObject.GetComponent<Bot>();
            if (bot != null && bot.IsSpawned)
                bot.SetRagdollClientRpc(true);
            botObject.GetComponent<BotMovement>().enabled = false;
            botObject.GetComponent<BotMovementController>().enabled = false;
            botObject.GetComponent<SpellCasterPlayer>().enabled = false;
            botObject.GetComponent<BotCombatController>().enabled = false;
        }

        _activeBots.Remove(botId);

        if (_pendingRespawn.Contains(botId))
            return;

        _pendingRespawn.Add(botId);
        _respawnCoroutines[botId] = StartCoroutine(RespawnBot(botId, botObject));
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

        foreach (var participant in PlayerManager.Instance.Participants.ToList()) {
            if (!participant.Id.IsBot) continue;
            PlayerManager.Instance.RemoveParticipant(participant.Id);
            TeamManager.Instance.RemoveBot(participant.Id.Value);
        }
    }

    private IEnumerator RespawnBot(ulong botId, GameObject oldBotObject) {
        yield return new WaitForSeconds(respawnDelay);
        DespawnBotObject(oldBotObject);
        yield return new WaitForEndOfFrame();
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
            _networkSceneSubscribed = false;
            _subscribedNetworkManager = null;
            if (_netSubscribeCoroutine != null)
                StopCoroutine(_netSubscribeCoroutine);
            _netSubscribeCoroutine = StartCoroutine(SubscribeNetworkSceneEventsWhenReady());
            EndMatch();
            return;
        }

        if (_matchBotsEnabled || initialBotCount <= 0)
            return;

        BeginMatch();
        QueueSpawnWhenReady();
    }

    private void OnNetworkSceneLoaded(
        string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut
    ) {
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
        while (_matchBotsEnabled && (PlayerSpawner.instance == null || PlayerManager.Instance == null ||
                                     TeamManager.Instance == null)) {
            yield return null;
        }

        _spawnQueued = false;
        if (!_matchBotsEnabled)
            yield break;

        if (_activeBots.Count == 0)
            SpawnInitialBotsNow();
    }

    private static void DespawnBotObject(GameObject botObject) {
        if (botObject == null) return;
        if (botObject.TryGetComponent<NetworkObject>(out var networkObject) && networkObject.IsSpawned) {
            networkObject.Despawn(true);
            return;
        }

        Destroy(botObject);
    }
}