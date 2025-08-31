using UnityEngine;

public class ModelPartHider : MonoBehaviour {
    [Header("Model Parts")] public SkinnedMeshRenderer fullBodyRenderer;
    public GameObject headModel; // Голова (скрываем)
    public GameObject[] bodyPartsToHide; // Части тела для скрытия

    [Header("Arms & Weapons")] public GameObject fpsArms; // Отдельная модель рук для FPS
    public GameObject fpsWeapon;

    [Header("Settings")] public float hideHeadDistance = 0.8f;
    public LayerMask wallLayers;

    private bool headHidden = false;
    private bool armsVisible = false;

    void Update() {
        HandleHeadVisibility();
        HandleArmsVisibility();
        HandleOcclusion();
    }

    void HandleHeadVisibility() {
        if (headModel == null) return;

        // Скрываем голову если камера близко
        float distance = Vector3.Distance(Camera.main.transform.position, headModel.transform.position);
        bool shouldHideHead = distance < hideHeadDistance;

        if (shouldHideHead != headHidden) {
            headHidden = shouldHideHead;
            headModel.SetActive(!headHidden);

            // Также скрываем другие части тела при необходимости
            foreach (var part in bodyPartsToHide) {
                if (part != null)
                    part.SetActive(!headHidden);
            }
        }
    }

    void HandleArmsVisibility() {
        if (fpsArms == null) return;

        // Показываем FPS руки когда голова скрыта
        bool shouldShowArms = headHidden;

        if (shouldShowArms != armsVisible) {
            armsVisible = shouldShowArms;
            fpsArms.SetActive(armsVisible);
            if (fpsWeapon != null) fpsWeapon.SetActive(armsVisible);
        }
    }

    void HandleOcclusion() {
        if (fullBodyRenderer == null) return;

        // Проверяем occlusion между камерой и телом
        RaycastHit hit;
        Vector3 cameraPos = Camera.main.transform.position;
        Vector3 bodyDirection = (transform.position - cameraPos).normalized;

        if (Physics.Raycast(cameraPos, bodyDirection, out hit, 2f, wallLayers)) {
            // Если луч попадает в стену, делаем тело полупрозрачным
            SetBodyAlpha(0.3f);
        } else {
            SetBodyAlpha(1f);
        }
    }

    void SetBodyAlpha(float alpha) {
        foreach (var material in fullBodyRenderer.materials) {
            Color color = material.color;
            color.a = alpha;
            material.color = color;

            if (alpha < 0.9f) {
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.EnableKeyword("_ALPHABLEND_ON");
            }
        }
    }
}