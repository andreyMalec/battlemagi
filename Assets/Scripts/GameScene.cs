using Unity.Netcode;
using UnityEngine.SceneManagement;

public static class GameScene {
    private const string game0 = "Game";
    private const string game1 = "Game 3";
    private const string game2 = "Game 2";
    private const string test = "Test";

    public static string Name;

    public static void StartGame(int mapIndex) {
        if (NetworkManager.Singleton.IsHost) {
            LobbyManager.Instance.CurrentLobby?.SetJoinable(false);
            var map = game0;
            if (mapIndex == 1)
                map = game1;
            if (mapIndex == 2)
                map = game2;
            Name = map;
            NetworkManager.Singleton.SceneManager.LoadScene(map, LoadSceneMode.Single);
        }
    }
}