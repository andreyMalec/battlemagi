using Unity.Netcode;
using UnityEngine.SceneManagement;

public static class GameScene {
    private const string game = "Game";
    private const string test = "Test";
    
    public const string Name = game;

    public static void StartGame() {
        if (NetworkManager.Singleton.IsHost) {
            NetworkManager.Singleton.SceneManager.LoadScene(Name, LoadSceneMode.Single);
        }
    }
}