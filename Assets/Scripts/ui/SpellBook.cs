using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using Voice;

public class SpellBook : MonoBehaviour {
    private const int SHAPE_CLOSED = 0;
    private const int SHAPE_PAGE1 = 1;
    private const int SHAPE_PAGE2 = 2;

    [Header("Animation settings")]
    [Tooltip("Time to open/close book in seconds")] [SerializeField]
    private float openDuration = 0.35f;

    [Tooltip("Total time of one page flip (both phases)")] [SerializeField]
    private float pageFlipDuration = 0.6f;

    [Tooltip("Normalized point in first phase when page index actually changes (0..1)")]
    [Range(0.0f, 1.0f)]
    [SerializeField]
    private float pageFlipMidpoint = 0.45f;

    [Header("References")]
    [SerializeField] private SkinnedMeshRenderer book;

    [SerializeField] private Renderer spellImage;
    [SerializeField] private TMP_Text spellNameText;
    [SerializeField] private TMP_Text spellDescriptionText;
    [SerializeField] private TMP_Text spellManaCostText;
    [SerializeField] private GameObject helperUI;

    [Header("Audio")]
    [SerializeField] private AudioClip[] pageSounds;

    [SerializeField] private AudioSource audioSource;

    [Header("Keys (optional)")]
    [SerializeField] private bool useKeyboardInput = true;

    [SerializeField] private KeyCode openKey = KeyCode.B;
    [SerializeField] private KeyCode nextKey = KeyCode.E;
    [SerializeField] private KeyCode prevKey = KeyCode.Q;

    // data
    private List<SpellData> spells;
    private int currentIndex = 0;
    private SpellData pendingSpell;

    // state
    private bool isVisible = false;
    private bool isOpened = false;
    private bool isPaging = false;

    // coroutines
    private Coroutine openCoroutine;
    private Coroutine pageCoroutine;

    private void Start() {
        // initialize visuals
        ResetBlendShapes();
        UpdateSpellUI();
        SetUIVisibility(false);
        book.enabled = false;
        helperUI.SetActive(false);
    }

    private List<SpellData> GetSpells() {
        List<SpellData> list;
        if (NetworkManager.Singleton.IsClient) {
            var arch = PlayerManager.Instance.FindByClientId(NetworkManager.Singleton.LocalClientId);
            if (arch != null) {
                list = ArchetypeDatabase.Instance.GetArchetype(arch.Value.Archetype).spells.ToList();
                currentIndex = Math.Clamp(currentIndex, 0, list.Count - 1);
                return list;
            }
        }

        list = SpellDatabase.Instance.spells;
        currentIndex = Math.Clamp(currentIndex, 0, list.Count - 1);
        return list;
    }

    private void Update() {
        if (!useKeyboardInput) return;

        if (Input.GetKeyDown(openKey)) {
            if (isOpened) Close();
            else Open();
        }

        if (!isOpened) return;

        if (!isPaging) {
            if (Input.GetKeyDown(nextKey)) Next();
            else if (Input.GetKeyDown(prevKey)) Prev();
        }
    }

    #region Public API (call from UI / other systems)

    public void Open() {
        if (isVisible) return;
        spells = GetSpells();
        UpdateSpellUI();
        if (openCoroutine != null) StopCoroutine(openCoroutine);
        openCoroutine = StartCoroutine(OpenRoutine());
    }

    public void Close() {
        if (!isVisible) return;
        if (openCoroutine != null) StopCoroutine(openCoroutine);
        openCoroutine = StartCoroutine(CloseRoutine());
    }

    public void Next() {
        if (isPaging || spells == null || spells.Count == 0) return;
        if (currentIndex >= spells.Count - 1) return;
        if (pageCoroutine != null) StopCoroutine(pageCoroutine);
        pageCoroutine = StartCoroutine(FlipPageRoutine(1));
    }

    public void Prev() {
        if (isPaging || spells == null || spells.Count == 0) return;
        if (currentIndex <= 0) return;
        if (pageCoroutine != null) StopCoroutine(pageCoroutine);
        pageCoroutine = StartCoroutine(FlipPageRoutine(-1));
    }

    #endregion

    #region Open/Close coroutines

    private IEnumerator OpenRoutine() {
        isVisible = true;
        // enable renderer/UI immediately for opening animation
        book.enabled = true;
        helperUI?.SetActive(true);

        float elapsed = 0f;
        float from = 100f;
        float to = 0f;

        while (elapsed < openDuration) {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / openDuration);
            float blend = Mathf.Lerp(from, to, t);
            SetClosedBlend(blend);
            yield return null;
        }

        SetClosedBlend(to);
        isOpened = true;
        SetUIVisibility(true);
        openCoroutine = null;
    }

    private IEnumerator CloseRoutine() {
        // hide interactive UI immediately
        SetUIVisibility(false);

        float elapsed = 0f;
        float from = 0f;
        float to = 100f;

        while (elapsed < openDuration) {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / openDuration);
            float blend = Mathf.Lerp(from, to, t);
            SetClosedBlend(blend);
            yield return null;
        }

        SetClosedBlend(to);
        isOpened = false;
        isVisible = false;

        // disable renderer after animation
        book.enabled = false;
        helperUI?.SetActive(false);

        openCoroutine = null;
    }

    #endregion

    #region Page flip coroutine

    private IEnumerator FlipPageRoutine(int dir) {
        if (dir != 1 && dir != -1) yield break;

        isPaging = true;
        PlayPageSound();

        float half = pageFlipDuration * 0.5f;
        bool pageChanged = false;

        if (dir == 1) {
            // ➡️ Листаем вперёд
            float elapsed = 0f;
            while (elapsed < half) {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / half);
                SetPage1Blend(Mathf.Lerp(0f, 100f, t));

                if (!pageChanged && t >= pageFlipMidpoint) {
                    currentIndex = Mathf.Clamp(currentIndex + 1, 0, spells.Count - 1);
                    pendingSpell = spells[currentIndex];

                    // Вперёд: обновляем ПРАВУЮ страницу (новая)
                    UpdateRightPageText(pendingSpell);
                    pageChanged = true;
                }

                yield return null;
            }

            // Вторая половина
            elapsed = 0f;
            while (elapsed < half) {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / half);
                SetPage2Blend(Mathf.Lerp(0f, 100f, t));
                yield return null;
            }

            // После полного переворота — обновляем ЛЕВУЮ страницу
            if (pendingSpell != null) {
                UpdateLeftPage(pendingSpell);
                pendingSpell = null;
            }
        } else {
            // ⬅️ Листаем назад
            SetPage1Blend(100f);
            SetPage2Blend(100f);

            float elapsed = 0f;
            while (elapsed < half) {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / half);
                SetPage2Blend(Mathf.Lerp(100f, 0f, t));

                if (!pageChanged && t >= pageFlipMidpoint) {
                    currentIndex = Mathf.Clamp(currentIndex - 1, 0, spells.Count - 1);
                    pendingSpell = spells[currentIndex];

                    // Назад: обновляем ЛЕВУЮ страницу (новая)
                    UpdateLeftPage(pendingSpell);
                    pageChanged = true;
                }

                yield return null;
            }

            elapsed = 0f;
            while (elapsed < half) {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / half);
                SetPage1Blend(Mathf.Lerp(100f, 0f, t));
                yield return null;
            }

            // После полного переворота — обновляем ПРАВУЮ страницу
            if (pendingSpell != null) {
                UpdateRightPageText(pendingSpell);
                pendingSpell = null;
            }
        }

        ResetPageBlend();
        isPaging = false;
        pageCoroutine = null;
    }

    private void UpdateRightPageText(SpellData spell) {
        if (spell == null) return;
        spellNameText.text = SpeechToTextHolder.Instance.Language == Language.Ru ? spell.nameRu : spell.name;
        spellDescriptionText.text = spell.description;
    }

    private void UpdateLeftPage(SpellData spell) {
        if (spell == null || spell.bookImage == null) return;
        spellImage.material.mainTexture = spell.bookImage;
        spellManaCostText.text = spell.isChanneling ? $"{spell.manaCost:0}/s" : $"{spell.manaCost:0}";
    }

    #endregion

    #region Helpers: visuals/audio/UI

    private void SetUIVisibility(bool show) {
        if (spellImage != null) spellImage.enabled = show;
        if (spellManaCostText != null) spellManaCostText.enabled = show;
        if (spellNameText != null) spellNameText.enabled = show;
        if (spellDescriptionText != null) spellDescriptionText.enabled = show;
    }

    private void UpdateSpellUI(SpellData spell = null) {
        if (spells == null || spells.Count == 0) return;

        var s = spell ?? spells[currentIndex];
        spellNameText.text = SpeechToTextHolder.Instance.Language == Language.Ru ? s.nameRu : s.name;
        spellDescriptionText.text = s.description;
        spellManaCostText.text = s.isChanneling ? $"{s.manaCost:0}/s" : $"{s.manaCost:0}";
        if (s.bookImage != null)
            spellImage.material.mainTexture = s.bookImage;
    }

    private void PlayPageSound() {
        if (audioSource == null || pageSounds == null || pageSounds.Length == 0) return;
        audioSource.Play(pageSounds);
    }

    private void ResetBlendShapes() {
        SetClosedBlend(100f);
        SetPage1Blend(0f);
        SetPage2Blend(0f);
    }

    private void SetClosedBlend(float v) {
        if (book != null) book.SetBlendShapeWeight(SHAPE_CLOSED, Mathf.Clamp(v, 0f, 100f));
    }

    private void SetPage1Blend(float v) {
        if (book != null) book.SetBlendShapeWeight(SHAPE_PAGE1, Mathf.Clamp(v, 0f, 100f));
    }

    private void SetPage2Blend(float v) {
        if (book != null) book.SetBlendShapeWeight(SHAPE_PAGE2, Mathf.Clamp(v, 0f, 100f));
    }

    private void ResetPageBlend() {
        SetPage1Blend(0f);
        SetPage2Blend(0f);
    }

    #endregion
}