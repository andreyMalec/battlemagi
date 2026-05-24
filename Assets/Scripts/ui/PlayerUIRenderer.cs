using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUIRenderer : MonoBehaviour {
    public Image hp;
    public TMP_Text hpText;
    public Image hpSpendPreview;
    public TMP_Text hpSpendText;
    public RectTransform armor;
    public Image mp;
    public Image primalMp;
    public TMP_Text mpText;
    public Image mpSpendPreview;
    public TMP_Text mpSpendText;
    public RectTransform[] uiContainers;
    public RectTransform effectsContainer;
    public RectTransform echoContainer;
    public RectTransform alternativeSpawn;
}