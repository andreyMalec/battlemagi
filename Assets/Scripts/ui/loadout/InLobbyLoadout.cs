using System;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;

public class InLobbyLoadout : MonoBehaviour {
    [Header("State")]
    [SerializeField] private GameObject stateLoadout;

    [SerializeField] private RuneSpellSaveListView listView;
    [SerializeField] private Button buttonOpenLoadout;

    [SerializeField] private TMP_InputField searchInputField;
    [SerializeField] private Button buttonSubmitSpellLoadout;
    [SerializeField] private TMP_Text spellLoadoutStatus;

    private readonly RuneSpellLocalStorage _localStorage = new RuneSpellLocalStorage();
    private const int MaxSelectedSavedSpells = 3;

    private State _state;

    private enum State {
        Hidden,
        Loadout,
    }

    private void Awake() {
        RefreshSavedSpells();
        buttonSubmitSpellLoadout.onClick.AddListener(SubmitSpellLoadout);
        buttonOpenLoadout.onClick.AddListener(OpenLoadout);
        searchInputField.onValueChanged.AddListener(_ => RefreshSavedSpells());
    }

    public void RefreshSavedSpells() {
        var saves = _localStorage.List();
        var search = searchInputField.text;
        if (!string.IsNullOrWhiteSpace(search)) {
            var filtered = new List<RuneSpellSaveInfo>();
            for (var i = 0; i < saves.Count; i++) {
                var info = saves[i];
                if (info.DisplayName != null && info.DisplayName.ToLowerInvariant().Contains(search.ToLowerInvariant()))
                    filtered.Add(info);
            }

            saves = filtered;
        }

        listView.Bind(saves, null, HandleSavedSpellLoadoutToggle);
    }

    private void OnEnable() {
        RefreshSavedSpells();
    }

    private void OnBackClick() {
        ToggleMenu();
    }

    private void ToggleMenu() {
        if (_state == State.Loadout) {
            _state = State.Hidden;
            return;
        }

        var active = !stateLoadout.activeSelf;
        stateLoadout.SetActive(active);
    }

    private void Update() {
        if (Input.GetKeyDown(KeyCode.Escape)) {
            _state = State.Hidden;
            stateLoadout.SetActive(false);
        }
    }

    private void FixedUpdate() {
        UpdateSpellLoadoutStatus();
    }

    private void OpenLoadout() {
        _state = State.Loadout;
        stateLoadout.SetActive(true);
    }

    private void SubmitSpellLoadout() {
        var saves = _localStorage.List();
        var selected = new List<RuneSpellSaveInfo>(MaxSelectedSavedSpells);
        for (var i = 0; i < saves.Count; i++) {
            if (!saves[i].IsSelectedForLoadout)
                continue;
            selected.Add(saves[i]);
            if (selected.Count == MaxSelectedSavedSpells)
                break;
        }

        if (selected.Count == 0) {
            spellLoadoutStatus.text = "Select at least one saved draft";
            return;
        }

        var payload0 = new FixedString4096Bytes(string.Empty);
        var payload1 = new FixedString4096Bytes(string.Empty);
        var payload2 = new FixedString4096Bytes(string.Empty);

        for (var i = 0; i < selected.Count; i++) {
            if (!_localStorage.TryLoad(selected[i].FileName, out var draft, out var error)) {
                spellLoadoutStatus.text = "Load draft failed: " + error;
                return;
            }

            var json = SpellJsonSerializer.ToJson(draft, false);
            if (json.Length > SpellNetworkCodec.MaxChunkChars) {
                spellLoadoutStatus.text = "Draft is too large for lobby submit: " + selected[i].DisplayName;
                return;
            }

            var payload = new FixedString4096Bytes(json);
            if (i == 0) payload0 = payload;
            if (i == 1) payload1 = payload;
            if (i == 2) payload2 = payload;
        }

        Ctx.Players.RequestRuneSpellLoadoutServerRpc(payload0, payload1, payload2, selected.Count);
        spellLoadoutStatus.text = "Loadout sent";
    }

    private void HandleSavedSpellLoadoutToggle(string fileName, bool isSelectedForLoadout) {
        var saves = _localStorage.List();
        if (isSelectedForLoadout) {
            var selectedCount = 0;
            for (var i = 0; i < saves.Count; i++) {
                if (saves[i].IsSelectedForLoadout)
                    selectedCount++;
            }

            var alreadySelected = false;
            for (var i = 0; i < saves.Count; i++) {
                if (saves[i].FileName == fileName) {
                    alreadySelected = saves[i].IsSelectedForLoadout;
                    break;
                }
            }

            if (!alreadySelected && selectedCount >= MaxSelectedSavedSpells) {
                spellLoadoutStatus.text = "Loadout: max 3 saved spells";
                RefreshSavedSpells();
                return;
            }
        }

        if (!_localStorage.SetSelectedForLoadout(fileName, isSelectedForLoadout, out var error)) {
            spellLoadoutStatus.text = "Loadout save toggle failed: " + error;
            RefreshSavedSpells();
            return;
        }

        spellLoadoutStatus.text = isSelectedForLoadout ? "Saved spell selected" : "Saved spell unselected";
        RefreshSavedSpells();
    }

    private void UpdateSpellLoadoutStatus() {
        if (!Ctx.TryGetPlayerData(Ctx.LocalClientId, out var data)) {
            spellLoadoutStatus.text = "Loadout: no player";
            return;
        }

        var state = (SpellLoadoutApprovalState)data.SpellLoadoutState;
        switch (state) {
            case SpellLoadoutApprovalState.Pending:
                spellLoadoutStatus.text = "Loadout: pending";
                break;
            case SpellLoadoutApprovalState.Approved:
                spellLoadoutStatus.text = "Loadout: approved";
                break;
            case SpellLoadoutApprovalState.Rejected:
                spellLoadoutStatus.text = "Loadout: rejected";
                break;
            default:
                spellLoadoutStatus.text = "Loadout: not submitted";
                break;
        }
    }
}