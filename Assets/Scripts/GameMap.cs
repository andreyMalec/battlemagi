using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "New Map", menuName = "Game/Map")]
public class GameMap : ScriptableObject {
    public string mapName;
    public string sceneName;

#if UNITY_EDITOR
    [SerializeField] SceneAsset sceneAsset;
    private void OnValidate() {
        if (sceneAsset)
            sceneName = sceneAsset.name;
    }
#endif
}