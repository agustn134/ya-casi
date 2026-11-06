/* Ya Casi — Agustín López Parra y Carlos Manuel Palma Muñoz.
 * Herramienta de editor para configurar el HUD sin sustituir textos ni referencias. */
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class ConfigureCampusHud
{
    [MenuItem("Ya Casi/Configurar textos del Canvas")]
    public static void Configure()
    {
        if (Application.isPlaying) { Debug.LogWarning("Sal de Play antes de configurar el Canvas."); return; }
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null) { Debug.LogError("No se encontró Canvas."); return; }
        string fontPath = "Assets/Art/Fonts/PixelifySans/PixelifySans SDF.asset";
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);
        if (font == null)
        {
            Font source = AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/Fonts/PixelifySans/PixelifySans.ttf");
            if (source == null) { Debug.LogError("Espera a que Unity importe PixelifySans.ttf."); return; }
            font = TMP_FontAsset.CreateFontAsset(source);
            if (font == null) { Debug.LogError("No se pudo crear la fuente TMP."); return; }
            font.name = "PixelifySans SDF";
            font.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            font.TryAddCharacters("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789áéíóúÁÉÍÓÚüÜñÑ¡!¿?.,:;−-+()[] /");
            AssetDatabase.CreateAsset(font, fontPath);
            if (font.material != null) AssetDatabase.AddObjectToAsset(font.material, font);
            foreach (Texture2D atlas in font.atlasTextures)
                if (atlas != null && !AssetDatabase.Contains(atlas)) AssetDatabase.AddObjectToAsset(atlas, font);
            EditorUtility.SetDirty(font);
        }
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            Undo.RecordObject(scaler, "Configurar HUD");
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920,1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }
        Apply(canvas, font, "TextoObjetivo", new Vector2(0,1), new Vector2(0,1), new Vector2(32,-32), new Vector2(1080,88), 38, false);
        Apply(canvas, font, "TextoTiempo", new Vector2(1,1), new Vector2(1,1), new Vector2(-32,-32), new Vector2(360,88), 44, true);
        Apply(canvas, font, "TextoInteraccion", new Vector2(.5f,0), new Vector2(.5f,0), new Vector2(0,32), new Vector2(480,80), 36, false);
        Apply(canvas, font, "TextoMensaje", new Vector2(.5f,0), new Vector2(.5f,0), new Vector2(0,132), new Vector2(1120,104), 38, false);
        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        AssetDatabase.SaveAssets();
        Debug.Log("HUD configurado: revisa el Canvas y guarda la escena con Ctrl+S.", canvas);
    }

    private static void Apply(Canvas canvas, TMP_FontAsset font, string name, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size, int fontSize, bool timer)
    {
        TMP_Text text = null;
        foreach (TMP_Text candidate in canvas.GetComponentsInChildren<TMP_Text>(true))
            if (candidate.name == name) { text = candidate; break; }
        if (text == null) { Debug.LogWarning("No se encontró " + name); return; }
        string panelName = "Panel_" + name;
        Transform existing = canvas.transform.Find(panelName);
        GameObject panel;
        if (existing == null)
        {
            panel = new GameObject(panelName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline), typeof(HudPanelVisibility));
            Undo.RegisterCreatedObjectUndo(panel, "Crear fondo HUD");
            panel.transform.SetParent(canvas.transform, false);
        }
        else panel = existing.gameObject;
        Undo.RecordObject(panel.GetComponent<RectTransform>(), "Posicionar HUD");
        RectTransform box = panel.GetComponent<RectTransform>();
        box.anchorMin = box.anchorMax = anchor;
        box.pivot = pivot; box.anchoredPosition = position; box.sizeDelta = size;
        box.localScale = Vector3.one;
        Image background = panel.GetComponent<Image>();
        Undo.RecordObject(background, "Color HUD");
        background.color = new Color32(23,33,51,238);
        background.raycastTarget = false;
        Outline border = panel.GetComponent<Outline>();
        border.effectColor = new Color32(103,155,178,255);
        border.effectDistance = new Vector2(2,-2);
        Undo.SetTransformParent(text.transform, panel.transform, "Agrupar texto HUD");
        Undo.RecordObject(text.rectTransform, "Ajustar texto HUD");
        text.rectTransform.anchorMin = Vector2.zero; text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.pivot = new Vector2(.5f,.5f);
        text.rectTransform.offsetMin = Vector2.zero; text.rectTransform.offsetMax = Vector2.zero;
        text.rectTransform.localScale = Vector3.one;
        text.rectTransform.localRotation = Quaternion.identity;
        Undo.RecordObject(text, "Estilo texto HUD");
        text.font = font; text.fontSharedMaterial = font.material;
        text.fontSize = fontSize; text.enableAutoSizing = false;
        text.fontStyle = FontStyles.Normal;
        text.color = timer ? new Color32(255,216,116,255) : new Color32(247,245,232,255);
        text.alignment = name == "TextoObjetivo" ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.Center;
        text.margin = new Vector4(24,12,24,12);
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;
        panel.GetComponent<HudPanelVisibility>().target = text;
        EditorUtility.SetDirty(panel.GetComponent<HudPanelVisibility>());
        EditorUtility.SetDirty(text);
    }
}
