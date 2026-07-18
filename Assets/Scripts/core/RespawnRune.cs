using NaughtyAttributes;
using UnityEngine;

public class RespawnRune : MonoBehaviour {
#if UNITY_EDITOR
    [Button("Random Rune")]
    public void RandomRune() {
        GetComponent<RunePlatform>().RandomRune();
    }

    [Button("Quad Rune")]
    public void QuadRune() {
        GetComponent<RunePlatform>().QuadRune();
    }

    [Button("Haste Rune")]
    public void HasteRune() {
        GetComponent<RunePlatform>().HasteRune();
    }

    [Button("Proj Rune")]
    public void ProjRune() {
        GetComponent<RunePlatform>().ProjRune();
    }

    [Button("Regen Rune")]
    public void RegenRune() {
        GetComponent<RunePlatform>().RegenRune();
    }

    [Button("Resist Rune")]
    public void ResistRune() {
        GetComponent<RunePlatform>().ResistRune();
    }

    [Button("Stasis Rune")]
    public void StasisRune() {
        GetComponent<RunePlatform>().StasisRune();
    }
#endif
}