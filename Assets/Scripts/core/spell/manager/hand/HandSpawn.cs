using UnityEngine;

public class HandSpawn : IHandAppearance {
    public void Show(ActiveSpell manager, SpellData spell) {
        if (spell.spellInHandPrefab == null) return;

        GameObject obj = Object.Instantiate(spell.spellInHandPrefab, manager.invocation);
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;

        Debug.Log($"[SpellManager] Проявляем {spell.name} в руке {manager.gameObject.name}");
    }

    public void Clear(ActiveSpell manager) {
        for (int i = 0; i < manager.invocation.childCount; i++) {
            Object.Destroy(manager.invocation.GetChild(i).gameObject);
        }
        Debug.Log($"[SpellManager] Убираем заклинание из руки {manager.gameObject.name}");
    }
}