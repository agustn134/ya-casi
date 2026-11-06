using UnityEngine;

/// <summary>Letrero de edificio visible tanto al editar como durante la partida.</summary>
[ExecuteAlways]
public sealed class CampusBuildingLabel : MonoBehaviour
{
    [SerializeField] string buildingName = "Edificio A";
    public string DisplayName => buildingName?.Trim() ?? "";
    [SerializeField] float textSize = .11f;
    GameObject visual;
    static Sprite background;

    void OnEnable() { Build(); }
    void OnValidate()
    {
        // Actualiza el contenido sin crear objetos durante la deserialización.
        if (visual != null)
        {
            var text = visual.GetComponentInChildren<TextMesh>();
            if (text != null) text.text = buildingName;
        }
    }

    void Build()
    {
        if (visual != null) return;
        visual = new GameObject("Letrero", typeof(TextMesh));
        visual.hideFlags = HideFlags.HideAndDontSave;
        visual.transform.SetParent(transform, false);
        var text = visual.GetComponent<TextMesh>();
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.font = font;
        text.text = buildingName;
        text.fontSize = 64;
        text.characterSize = textSize;
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.color = Color.white;
        var renderer = text.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = font.material;
        renderer.sortingOrder = 31;
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
        // El ancho se calcula con las métricas de la fuente.
        font.RequestCharactersInTexture(buildingName, 64);
        float width = 0;
        foreach (char c in buildingName)
            if (font.GetCharacterInfo(c, out CharacterInfo info, 64)) width += info.advance;
        panel.transform.localScale = new Vector3(width * textSize / 10f + .5f, .95f, 1);
        var sr = panel.GetComponent<SpriteRenderer>();
        sr.sprite = background;
        sr.color = new Color(.05f, .1f, .12f, .88f);
        sr.sortingOrder = 30;
    }

    void OnDisable()
    {
        if (visual == null) return;
        if (Application.isPlaying) Destroy(visual);
        else DestroyImmediate(visual);
        visual = null;
    }
}
