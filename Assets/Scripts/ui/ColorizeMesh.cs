using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ColorizeMesh : MonoBehaviour {
    public static readonly int Hue = Shader.PropertyToID("Hue");
    public static readonly int Saturation = Shader.PropertyToID("Saturation");

    [SerializeField] private Slider sliderHue;
    [SerializeField] private Slider sliderSaturation;
    [SerializeField] private TMP_InputField fieldHue;
    [SerializeField] private TMP_InputField fieldSaturation;
    [SerializeField] private GameObject colorPicker;

    [SerializeField] private Renderer _renderer;
    [SerializeField] private RawImage preview;

    private float _hue = 0;

    public float hue {
        get => _hue;
        set {
            _hue = value;
            _renderer.material.SetFloat(Hue, value);
            preview?.material?.SetFloat(Hue, value);
            needUpdate = true;
        }
    }

    private float _saturation = 0;

    public float saturation {
        get => _saturation;
        set {
            _saturation = value;
            _renderer.material.SetFloat(Saturation, value);
            preview?.material?.SetFloat(Saturation, value);
            needUpdate = true;
        }
    }

    private int frame = 0;
    private bool needUpdate = false;

    private void Update() {
        frame++;
        if (needUpdate && frame % 60 == 0) {
            LobbyExt.SetColor(new PlayerColor(hue, saturation));
            needUpdate = false;
        }
    }

    private void Awake() {
        hue = sliderHue.value;
        saturation = sliderSaturation.value;

        sliderHue.onValueChanged.AddListener(h => {
            fieldHue.text = h.ToString("0.0");
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
        if (TeamManager.Instance.CurrentMode.Value == TeamManager.TeamMode.FreeForAll) {
            colorPicker.gameObject.SetActive(true);
        } else {
            colorPicker.gameObject.SetActive(false);
            if (team == TeamManager.Team.Blue) {
                sliderHue.value = 228;
                sliderSaturation.value = 1;
            } else {
                sliderHue.value = 0;
                sliderSaturation.value = 1;
            }
        }
    }
}