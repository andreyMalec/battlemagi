using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ArchetypeSelector : MonoBehaviour {
    [SerializeField] private ArchetypeAvatar avatar;
    [SerializeField] private Button buttonPrev;
    [SerializeField] private Button buttonNext;
    [SerializeField] private TMP_Text label;

    private void Awake() {
        if (buttonPrev != null) buttonPrev.onClick.AddListener(OnPrev);
        if (buttonNext != null) buttonNext.onClick.AddListener(OnNext);
    }

    private void OnEnable() {
        UpdateLabel();
    }

    private void OnPrev() {
        if (avatar == null) return;
        avatar.PrevArchetype();
        UpdateLabel();
    }

    private void OnNext() {
        if (avatar == null) return;
        avatar.NextArchetype();
        UpdateLabel();
    }

    private void UpdateLabel() {
        if (label == null || avatar == null) return;
        var data = avatar.CurrentArchetype;
        label.text = data != null ? data.archetypeName : "—";

        GetComponentInParent<ColorizeMesh>().UpdateRenderer();
        if (PlayerManager.Instance != null)
            PlayerManager.Instance.SetArchetypeServerRpc(data.id);
    }
}