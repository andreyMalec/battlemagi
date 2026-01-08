using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ColorizeMesh : MonoBehaviour {
    public static readonly int Hue = Shader.PropertyToID("Hue");
    public static readonly int Saturation = Shader.PropertyToID("Saturation");
    public static readonly int Value = Shader.PropertyToID("Value");

    [SerializeField] private Slider sliderHue;
    [SerializeField] private Slider sliderSaturation;
    [SerializeField] private TMP_InputField fieldHue;
    [SerializeField] private TMP_InputField fieldSaturation;
    [SerializeField] private GameObject colorPicker;
    [SerializeField] private Transform avatarRoot;

    private Renderer _renderer;

    private new Renderer renderer {
        get {
            if (_renderer == null || _renderer.gameObject == null) {
                _renderer = avatarRoot.GetComponentInChildren<Renderer>(true);
            }

            return _renderer;
        }
    }

    [SerializeField] private RawImage preview;

    private bool _contrastColors = false;

    public bool contrastColors {
        get => _contrastColors;
        set {
            _contrastColors = value;
            renderer?.material?.SetFloat(Value, _value);
            preview?.material?.SetFloat(Value, _value);
        }
    }

    private float _value => _contrastColors ? 1f : 0.2f;

    private float _hue = 0;

    public float hue {
        get => _hue;
        set {
            _hue = value;
            renderer?.material?.SetFloat(Hue, value);
            preview?.material?.SetFloat(Hue, value);
            needUpdate = true;
        }
    }

    private float _saturation = 0;

    public float saturation {
        get => _saturation;
        set {
            _saturation = value;
            renderer?.material?.SetFloat(Saturation, value);
            preview?.material?.SetFloat(Saturation, value);
            needUpdate = true;
        }
    }

    private int frame = 0;
    private bool needUpdate = false;

    private void Update() {
        frame++;
        if (needUpdate && frame % 10 == 0) {
            LobbyExt.SetColor(new PlayerColor(hue, saturation));
            needUpdate = false;
        }
    }

    public void UpdateRenderer() {
        StartCoroutine(UpdateRendererCoroutine());
    }

    private IEnumerator UpdateRendererCoroutine() {
        yield return new WaitForEndOfFrame();
        renderer?.material?.SetFloat(Hue, hue);
        renderer?.material?.SetFloat(Saturation, saturation);
        renderer?.material?.SetFloat(Value, _value);
    }

    private void Awake() {
        hue = sliderHue.value;
        saturation = sliderSaturation.value;
        _contrastColors = PlayerPrefs.GetInt("ContrastColors", 0) == 1;

        sliderHue.onValueChanged.AddListener(h => {
            fieldHue.text = h.ToString("0");
            hue = h;
        });
        sliderSaturation.onValueChanged.AddListener(s => {
            fieldSaturation.text = s.ToString("0.00");
            saturation = s;
        });

        fieldHue.onValueChanged.AddListener(h => {
            var hh = float.Parse(h);
            sliderHue.value = hh;
            hue = hh;
        });
        fieldSaturation.onValueChanged.AddListener(s => {
            var ss = float.Parse(s);
            sliderSaturation.value = ss;
            saturation = ss;
        });
    }

    private void OnEnable() {
        TeamManager.Instance.MyTeam += MyTeam;
    }

    private void OnDisable() {
        TeamManager.Instance.MyTeam -= MyTeam;
    }

    private void MyTeam(TeamManager.Team team) {
        if (!TeamManager.Instance.isTeamMode) {
            colorPicker.gameObject.SetActive(true);
        } else {
            colorPicker.gameObject.SetActive(false);
            if (team == TeamManager.Team.Blue) {
                hue = sliderHue.value = 228;
                saturation = sliderSaturation.value = 0.85f;
            } else {
                hue = sliderHue.value = 0;
                saturation = sliderSaturation.value = 0.85f;
            }
        }
    }

    public static float CalculateValue() {
        return PlayerPrefs.GetInt("ContrastColors", 0) == 1 ? 1f : 0.2f;
    }
}