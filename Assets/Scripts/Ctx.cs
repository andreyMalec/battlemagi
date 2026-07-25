using System.Collections.Generic;
using System.Linq;
using Steamworks.Data;
using Unity.Netcode;
using UnityEngine.Localization.Settings;
using Voice;

public static class Ctx {
    public static NetworkManager NetManager => NetworkManager.Singleton;

    public static bool IsClient => NetworkManager.Singleton.IsClient;

    public static ulong LocalClientId => NetworkManager.Singleton.LocalClientId;

    public static ArchetypeDatabase Archetypes => ArchetypeDatabase.Instance;
    public static AudioManager Audio => AudioManager.Instance;
    public static BotLifecycleManager BotLifecycle => BotLifecycleManager.Instance;
    public static BotSpellVoice BotSpellVoice => BotSpellVoice.Instance;
    public static BotSpellWeights BotSpellWeights => BotSpellWeights.Instance;
    public static CTFAnnouncer CtfAnnouncer => CTFAnnouncer.Instance;
    public static DefaultSpells DefaultSpells => DefaultSpells.Instance;
    public static FirebaseAnalytic Analytics => FirebaseAnalytic.Instance;
    public static GameAnnouncer GameAnnouncer => GameAnnouncer.Instance;
    public static GameConfig GameConfig => GameConfig.Instance;
    public static GameMapDatabase GameMaps => GameMapDatabase.instance;
    public static GameProgress GameProgress => GameProgress.Instance;
    public static Killfeed Killfeed => Killfeed.Instance;
    public static LobbyManager Lobby => LobbyManager.Instance;
    public static LocalizationSettings Localization => LocalizationSettings.Instance;
    public static PlayerAchievementsManager PlayerAchievements => PlayerAchievementsManager.Instance;
    public static PlayerManager Players => PlayerManager.Instance;
    public static PlayerSpawner PlayerSpawner => PlayerSpawner.instance;
    public static SpeechToTextHolder SpeechToText => SpeechToTextHolder.Instance;
    public static SpellPrefab SpellPrefab => SpellPrefab.Instance;
    public static SpellPrefabDatabase SpellPrefabs => SpellPrefabDatabase.Instance;
    public static StatusEffectDatabase StatusEffects => StatusEffectDatabase.Instance;
    public static TeamManager Teams => TeamManager.Instance;

    public static IReadOnlyDictionary<ulong, NetworkObject> SpawnedObjects => NetManager.SpawnManager.SpawnedObjects;
    public static IReadOnlyCollection<NetworkObject> SpawnedObjectsList => NetManager.SpawnManager.SpawnedObjectsList;

    public static bool TryGetSpawnedObject(ulong objectId, out NetworkObject obj) {
        return SpawnedObjects.TryGetValue(objectId, out obj);
    }

    public static NetworkObject GetSpawnedObject(ulong objectId) {
        return SpawnedObjects[objectId];
    }

    public static bool IsTeamMode => Teams != null && Teams.isTeamMode;

    public static bool AreEnemies(ParticipantId a, ParticipantId b) {
        if (Teams == null)
            return a != b;
        return Teams.AreEnemies(a, b);
    }

    public static bool AreAllies(ParticipantId a, ParticipantId b) {
        return !AreEnemies(a, b);
    }

    public static TeamManager.Team GetTeam(ulong? clientId) {
        return Teams.GetTeam(clientId);
    }

    public static TeamManager.Team GetTeam(ParticipantId participantId) {
        return Teams.GetTeam(participantId);
    }

    public static bool HasTeam(ulong clientId) {
        return Teams.HasTeam(clientId);
    }

    public static PlayerManager.PlayerData? FindPlayerByClientId(ulong clientId) {
        return Players.FindByClientId(clientId);
    }

    public static PlayerManager.PlayerData? FindPlayerBySteamId(ulong steamId) {
        return Players.FindBySteamId(steamId);
    }

    public static bool TryGetPlayerData(ulong clientId, out PlayerManager.PlayerData data) {
        return Players.TryGetPlayerData(clientId, out data);
    }

    public static bool TryGetPlayerDataBySteamId(ulong steamId, out PlayerManager.PlayerData data) {
        return Players.TryGetPlayerDataBySteamId(steamId, out data);
    }

    public static bool TryGetParticipant(ParticipantId participantId, out MatchParticipantData data) {
        return Players.TryGetParticipant(participantId, out data);
    }

    public static bool TryGetParticipantBySteamId(ulong steamId, out MatchParticipantData data) {
        return Players.TryGetParticipantBySteamId(steamId, out data);
    }

    public static List<PlayerManager.PlayerData> GetPlayersList() {
        return Players.Players();
    }

    public static Steamworks.SteamId? GetPlayerSteamId(ulong clientId) {
        return Players.GetSteamId(clientId);
    }

    public static PlayerColor? GetPlayerColor(ulong steamId) {
        return Players.GetColor(steamId);
    }

    public static Lobby? CurrentLobby => Lobby.CurrentLobby;

    public static LobbyManager.PlayerState LobbyState => Lobby.State;

    public static bool IsHost() {
        return Lobby.IsHost();
    }

    public static bool ToggleReady() {
        return Lobby.ToggleReady();
    }

    public static void CreateLobby(int maxPlayers) {
        Lobby.CreateLobby(maxPlayers);
    }

    public static void JoinLobby(ulong lobbyId) {
        Lobby.JoinLobby(lobbyId);
    }

    public static void RefreshLobbyList() {
        Lobby.RefreshLobbyList();
    }

    public static void LeaveLobby() {
        Lobby.LeaveLobby();
    }

    public static void RestartLobby() {
        Lobby.RestartLobby();
    }

    public static void GameStarted() {
        Lobby.GameStarted();
    }

    public static void InviteFriends() {
        Lobby.InviteFriends();
    }

    public static void SetLobbyVisibility(LobbyVisibility visibility) {
        Lobby.SetVisibility(visibility);
    }

    public static LobbyVisibility GetLobbyVisibility() {
        return Lobby.GetVisibility();
    }

    public static void UpdateLobbyMeta(int mapIndex, TeamManager.TeamMode mode) {
        Lobby.UpdateLobbyMeta(mapIndex, mode);
    }

    public static void KickPlayer(ulong steamId) {
        Lobby.KickPlayer(steamId);
    }

    public static ArchetypeData GetArchetype(int id) {
        return Archetypes.GetArchetype(id);
    }

    public static UnityEngine.Sprite GetArchetypeIcon(int id) {
        return Archetypes.GetArchetypeIcon(id);
    }

    public static int ArchetypeCount => Archetypes.archetypes.Count;

    public static UnityEngine.GameObject GetSpellPrefab(bool useNetwork) {
        return SpellPrefab.GetPrefab(useNetwork);
    }

    public static void DespawnSpellObject(UnityEngine.GameObject go) {
        SpellPrefab.Despawn(go);
    }

    public static UnityEngine.GameObject GetSpellPrefab(SpellDefinition def) {
        return SpellPrefabs.Get(def);
    }

    public static UnityEngine.GameObject GetSpellPrefab(SpellProjectilePrefabId id) {
        return SpellPrefabs.Get(id);
    }

    public static UnityEngine.GameObject GetSpellPrefab(SpellZonePrefabId id) {
        return SpellPrefabs.Get(id);
    }

    public static UnityEngine.GameObject GetSpellPrefab(SpellBeamPrefabId id) {
        return SpellPrefabs.Get(id);
    }

    public static UnityEngine.GameObject GetSpellPrefab(SpellSummonPrefabId id) {
        return SpellPrefabs.Get(id);
    }

    public static UnityEngine.GameObject GetSpellPrefab(SpellSelfPrefabId id) {
        return SpellPrefabs.Get(id);
    }

    public static DamageKind GetSpellSound(SpellDefinition def) {
        return SpellPrefabs.Sound(def);
    }

    public static UnityEngine.GameObject GetSpellHandPrefab(SpellDefinition def) {
        return SpellPrefabs.Hand(def);
    }

    public static UnityEngine.GameObject GetSpellHandPrefab(int core, int prefab) {
        return SpellPrefabs.Hand(core, prefab);
    }

    public static List<DefaultSpell> GetAllDefaultSpells() {
        return DefaultSpells.list.ToList();
    }

    public static bool TryGetLocalArchetypeSpells(out List<DefaultSpell> spells) {
        var arch = Players.FindByClientId(LocalClientId);
        if (arch != null) {
            var all = GetAllDefaultSpells();
            var typed = Archetypes.GetArchetype(arch.Value.Archetype).spells;
            spells = typed.Map(s => all.Find(sp => sp.spell.spellName == s.spellName)).ToList();
            return true;
        }

        spells = null;
        return false;
    }
}


