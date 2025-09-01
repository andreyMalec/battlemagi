using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using FullOpaqueVFX;
using UnityEngine.Serialization;
using UnityEngine;
using UnityEngine.Events;
using Debug = UnityEngine.Debug;

public class PlayerSpellCaster : MonoBehaviour {
    [Header("Available Spells")] public List<SpellData> spells = new();

    public SpellManager spellManager;
    public Mouth mouth;
    public PlayerAnimator playerAnimator;

    public KeyCode spellCastKey = KeyCode.Mouse0;

    private float currentChargeTime;
    private float maxChargeTime = 3;

    public SpellData CurrentSpell { get; private set; }

    public bool IsCasting { get; private set; }

    private void Start() {
        IsCasting = false;
        CurrentSpell = spells.First();
        mouth.OnMouthClose += OnMouthClose;
    }

    private void OnMouthClose(string lastWords) {
        if (currentChargeTime >= CurrentSpell.castTime)
            CastSpell();

        Debug.Log("resultSpell: " + lastWords);

        IsCasting = false;
    }

    private void Update() {
        HandleSpellCasting();
        playerAnimator.Casting(IsCasting, currentChargeTime);
    }

    private void HandleSpellCasting() {
        if (Input.GetKeyDown(spellCastKey) && !IsCasting) {
            IsCasting = true;
            mouth.Open();
        }
        else if (Input.GetKeyUp(spellCastKey) && IsCasting)
            mouth.Close();

        if (IsCasting)
            currentChargeTime += Time.deltaTime;
    }

    private void CastSpell() {
        currentChargeTime = 0;
        StartCoroutine(playerAnimator.CastSpell(CurrentSpell));
        StartCoroutine(spellManager.CastSpell(CurrentSpell));
    }
}