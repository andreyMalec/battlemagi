using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SpellBook : MonoBehaviour {
    private const int SHAPE_CLOSED = 0;
    private const int SHAPE_PAGE1 = 1;
    private const int SHAPE_PAGE2 = 2;

    [SerializeField] private int animScale = 200;
    [SerializeField] private int page = 50;
    [SerializeField] private SkinnedMeshRenderer book;
    [SerializeField] private Renderer image;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private GameObject helper;

    [Header("Audio")]
    [SerializeField] private AudioClip[] pageSounds;

    [SerializeField] private AudioSource audioSource;

    [Header("Keys")]
    [SerializeField] private KeyCode openKey = KeyCode.B;

    [SerializeField] private KeyCode nextKey = KeyCode.E;
    [SerializeField] private KeyCode prevKey = KeyCode.Q;

    private int index = 0;
    private List<SpellData> spells;
    private float timePaging;
    private float timeOpening = 100;
    private int pageDir = 0;
    private bool opened = false;
    private int openDir = -1;
    private bool paging = false;
    private bool visible = false;

    private void Start() {
        spells = SpellDatabase.Instance.spells;
        book.SetBlendShapeWeight(SHAPE_CLOSED, 100);
        book.SetBlendShapeWeight(SHAPE_PAGE1, 0);
        book.SetBlendShapeWeight(SHAPE_PAGE2, 0);
    }

    private void Update() {
        if (!opened && Input.GetKeyDown(openKey)) {
            openDir = 1;
            timeOpening = 100;
            visible = true;
        } else if (opened && Input.GetKeyDown(openKey)) {
            openDir = -1;
            timeOpening = 0;
        }

        if (openDir == 1) {
            timeOpening -= Time.deltaTime * animScale;
            if (timeOpening < 0) {
                opened = true;
                openDir = 0;
            }
        }

        if (openDir == -1) {
            opened = false;
            timeOpening += Time.deltaTime * animScale;
            if (timeOpening > 100) {
                openDir = 0;
                visible = false;
            }
        }

        book.SetBlendShapeWeight(SHAPE_CLOSED, timeOpening);
        book.enabled = visible;
        helper.SetActive(visible);
        image.enabled = opened;
        nameText.enabled = opened;
        descriptionText.enabled = opened;
        if (!opened) return;

        var i = index;
        if (Input.GetKeyDown(nextKey)) {
            pageDir = 1;
            timePaging = 0;
            paging = true;
            audioSource.Play(pageSounds);
        }

        if (Input.GetKeyDown(prevKey)) {
            pageDir = -1;
            timePaging = 200;
            paging = true;
            book.SetBlendShapeWeight(SHAPE_PAGE1, 100);
            book.SetBlendShapeWeight(SHAPE_PAGE2, 100);
            audioSource.Play(pageSounds);
        }

        index = Mathf.Clamp(index, 0, spells.Count - 1);

        nameText.text = spells[index].name;
        descriptionText.text = spells[index].description;
        image.material.mainTexture = spells[index].bookImage;

        if (pageDir == 1 && timePaging < 100) {
            timePaging += Time.deltaTime * animScale;
            book.SetBlendShapeWeight(SHAPE_PAGE1, timePaging);
            if (timePaging > page && paging) {
                paging = false;
                index += pageDir;
            }
        }

        if (pageDir == 1 && timePaging >= 100) {
            timePaging += Time.deltaTime * animScale;
            book.SetBlendShapeWeight(SHAPE_PAGE2, timePaging - 100);
        }

        if (pageDir == -1 && timePaging < 100) {
            timePaging -= Time.deltaTime * animScale;
            book.SetBlendShapeWeight(SHAPE_PAGE1, timePaging);
            if (timePaging > page && paging) {
                paging = false;
                index += pageDir;
            }
        }

        if (pageDir == -1 && timePaging >= 100) {
            timePaging -= Time.deltaTime * animScale;
            book.SetBlendShapeWeight(SHAPE_PAGE2, timePaging - 100);
        }


        if (pageDir == 1 && timePaging >= 200) {
            pageDir = 0;
            book.SetBlendShapeWeight(SHAPE_PAGE1, 0);
            book.SetBlendShapeWeight(SHAPE_PAGE2, 0);
        }

        if (pageDir == -1 && timePaging <= 0) {
            pageDir = 0;
            book.SetBlendShapeWeight(SHAPE_PAGE1, 0);
            book.SetBlendShapeWeight(SHAPE_PAGE2, 0);
        }
    }
}