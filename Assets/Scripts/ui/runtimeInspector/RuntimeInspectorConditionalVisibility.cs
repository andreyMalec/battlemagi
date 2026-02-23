using NaughtyAttributes;
using UnityEngine;

public class RuntimeInspectorConditionalVisibility : MonoBehaviour {
    private object _owner;
    private ShowIfAttributeBase _a;

    public void Bind(object owner, ShowIfAttributeBase a) {
        _owner = owner;
        _a = a;
        Apply();
    }

    public void Apply() {
        gameObject.SetActive(RuntimeInspectorConditionEvaluator.Evaluate(_owner, _a));
    }
}

