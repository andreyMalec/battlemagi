using System.Collections;
using TMPro;
using UnityEngine;

public class PlayerEffectUI : MonoBehaviour {
    [SerializeField] private TMP_Text title;
    [SerializeField] private TMP_Text description;
    [SerializeField] private float duration = 5f;

    public void Show(string effectName, string _description, Color _color) {
        title.text = effectName;
        title.color = _color;
        description.text = _description;
        StartCoroutine(Show());
    }

    private IEnumerator Show() {
        title.gameObject.SetActive(true);
        description.gameObject.SetActive(true);
        yield return new WaitForSeconds(duration);
        title.gameObject.SetActive(false);
        description.gameObject.SetActive(false);
    }
}