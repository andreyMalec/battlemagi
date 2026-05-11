using System.Collections.Generic;
using UnityEngine;

public class BotSpellWeights : MonoBehaviour {
    public static BotSpellWeights Instance { get; private set; }

    [SerializeField] public List<SpellWeights> weights = new();

    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}