using System;
using System.Collections.Generic;
using Steamworks.Data;
using UnityEngine;

public static class LobbyBotRosterData {
    public const string LobbyDataKey = "Bots";

    [Serializable]
    private class Wrapper {
        public List<Entry> bots = new();
    }

    [Serializable]
    public class Entry {
        public ulong id;
        public TeamManager.Team team = TeamManager.Team.None;
        public int archetype;
        public float hue;
        public float saturation;
    }

    public static List<Entry> LoadFromLobby(Lobby lobby) {
        var raw = lobby.GetData(LobbyDataKey);
        if (string.IsNullOrEmpty(raw))
            return new List<Entry>();

        var wrapper = JsonUtility.FromJson<Wrapper>(raw);
        if (wrapper == null || wrapper.bots == null)
            return new List<Entry>();

        return wrapper.bots;
    }

    public static void SaveToLobby(Lobby lobby, List<Entry> entries) {
        var wrapper = new Wrapper { bots = entries };
        var raw = JsonUtility.ToJson(wrapper);
        lobby.SetData(LobbyDataKey, raw);
    }

    public static ulong NextId(List<Entry> entries) {
        ulong max = 0;
        for (int i = 0; i < entries.Count; i++) {
            if (entries[i].id > max)
                max = entries[i].id;
        }

        return max + 1;
    }
}

