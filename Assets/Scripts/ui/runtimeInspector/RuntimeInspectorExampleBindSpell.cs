using UnityEngine;

public class RuntimeInspectorExampleBindSpell : MonoBehaviour {
    [SerializeField] private RuntimeInspectorView inspector;
    [SerializeField] private SpellDefinition spell;

    private void Start() {
        if (inspector == null) return;
        inspector.Bind(spell);
    }
}

