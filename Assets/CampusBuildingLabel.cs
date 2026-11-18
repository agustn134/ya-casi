using UnityEngine;
using TMPro;

/// <summary>Letrero de edificio visible tanto al editar como durante la partida.</summary>
[ExecuteAlways]
public sealed class CampusBuildingLabel : MonoBehaviour
{
    [SerializeField] string buildingName = "Edificio A";
    public string DisplayName => buildingName?.Trim() ?? "";
    [SerializeField] float textSize = .11f;
    [SerializeField, Min(1)] float professorVisibilityRadius = 5f;
    GameObject visual;
    string shownName;
    float shownSize;
    static Sprite background;
    Transform nearbyPlayer;
    TextMeshPro labelText;
    SpriteRenderer panelRenderer;
    float visibility = 1f;
    bool IsProfessor => GetComponentInParent<CampusProfessorWaiting>() != null;
    float FontSize => IsProfessor ? Mathf.Clamp(textSize * 60f, 4.5f, 5f) : Mathf.Clamp(textSize * 60f, .8f, 12f);

    void OnEnable() { Build(); }
    void OnValidate()
    {
        // Actualiza el contenido sin crear objetos durante la deserialización.
        if (visual != null)
        {
            var text = visual.GetComponentInChildren<TMP_Text>();
            if (text != null) text.text = buildingName;
        }
    }

    void Build()
    {
        if (visual != null) return;
        visual = new GameObject("Letrero", typeof(TextMeshPro));
        visual.hideFlags = HideFlags.HideAndDontSave;
        visual.transform.SetParent(transform, false);
        var text = visual.GetComponent<TextMeshPro>();
        labelText = text;
        if (CampusTypography.Font != null) text.font = CampusTypography.Font;
        text.text = buildingName;
        text.fontSize = FontSize;
        text.rectTransform.sizeDelta = new Vector2(100, 1.5f);
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.color = Color.white;
        var renderer = text.GetComponent<MeshRenderer>();
        // Los actores llegan hasta orden 2000; el nombre queda legible encima.
        renderer.sortingOrder = IsProfessor ? 3001 : 31;
        visual.transform.localPosition = IsProfessor ? new Vector3(0, .45f, 0) : Vector3.zero;
        if (background == null)
        {
            var texture = new Texture2D(1, 1);
            texture.hideFlags = HideFlags.HideAndDontSave;
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            background = Sprite.Create(texture, new Rect(0, 0, 1, 1), Vector2.one * .5f, 1);
            background.hideFlags = HideFlags.HideAndDontSave;
        }
        var panel = new GameObject("Fondo", typeof(SpriteRenderer));
        panel.hideFlags = HideFlags.HideAndDontSave;
        panel.transform.SetParent(visual.transform, false);
        Vector2 preferred = text.GetPreferredValues(buildingName);
        panel.transform.localScale = new Vector3(preferred.x + .3f, Mathf.Max(.55f, preferred.y + .12f), 1);
        var sr = panel.GetComponent<SpriteRenderer>();
        panelRenderer = sr;
        sr.sprite = background;
        sr.color = new Color(.05f, .1f, .12f, .88f);
        sr.sortingOrder = IsProfessor ? 3000 : 30;
        shownName = buildingName; shownSize = textSize;
        visibility = ShouldShow() ? 1f : 0f;
        ApplyVisibility();
    }

    void Update()
    {
        if (visual == null) { Build(); return; }
        float targetVisibility = ShouldShow() ? 1f : 0f;
        visibility = Application.isPlaying ? Mathf.MoveTowards(visibility, targetVisibility, Time.unscaledDeltaTime * 6f) : targetVisibility;
        ApplyVisibility();
        var text = visual.GetComponent<TextMeshPro>();
        if (text == null || CampusTypography.Font == null || (text.font == CampusTypography.Font && shownName == buildingName && shownSize == textSize)) return;
        text.font = CampusTypography.Font; text.text = buildingName; text.fontSize = FontSize;
        Vector2 preferred = text.GetPreferredValues(buildingName);
        visual.transform.Find("Fondo").localScale = new Vector3(preferred.x + .3f, Mathf.Max(.55f, preferred.y + .12f), 1);
        shownName = buildingName; shownSize = textSize;
    }

    bool ShouldShow()
    {
        var professor = GetComponentInParent<CampusProfessorWaiting>();
        if (!Application.isPlaying || professor == null) return true;
        if (nearbyPlayer == null) nearbyPlayer = FindFirstObjectByType<CampusPathPlayer>()?.transform;
        return nearbyPlayer == null || ((Vector2)nearbyPlayer.position - (Vector2)professor.transform.position).sqrMagnitude
            <= professorVisibilityRadius * professorVisibilityRadius;
    }

    void ApplyVisibility()
    {
        if (visual.activeSelf != (visibility > .005f)) visual.SetActive(visibility > .005f);
        if (labelText != null) labelText.color = new Color(1, 1, 1, visibility);
        if (panelRenderer != null) panelRenderer.color = new Color(.05f, .1f, .12f, .88f * visibility);
    }

    void OnDisable()
    {
        if (visual == null) return;
        if (Application.isPlaying) Destroy(visual);
        else DestroyImmediate(visual);
        visual = null;
        labelText = null; panelRenderer = null;
    }
}
