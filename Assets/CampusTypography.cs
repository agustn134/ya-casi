using TMPro;
using UnityEngine;

/// <summary>Tipografía del campus; no modifica la configuración ni las fuentes del nivel 1.</summary>
public static class CampusTypography
{
    public const string ResourceName = "CampusRedaction35";
    static TMP_FontAsset cached;

    /// <summary>Atlas SDF estático generado desde Redaction35-Regular.ttf en el editor.</summary>
    public static TMP_FontAsset Font
    {
        get
        {
            if (cached == null) cached = Resources.Load<TMP_FontAsset>(ResourceName);
            return cached;
        }
    }

    /// <summary>Aplica la fuente a un HUD, panel o menú del nivel 3, incluidos hijos inactivos.</summary>
    public static void Apply(Transform root, bool compact = false)
    {
        TMP_FontAsset font = Font;
        if (root == null || font == null) return;
        foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
        {
            text.font = font;
            // Corrige las dos etiquetas heredadas del panel compartido al mostrarlas aquí.
            text.text = text.text.Replace("MÃºsica", "Música").Replace("Calidad grÃ¡fica", "Calidad gráfica");
            text.characterSpacing = 0;
            text.wordSpacing = 0;
            if (text is TextMeshProUGUI)
            {
                if (compact) text.fontSize = Mathf.Max(16, text.fontSize);
                else
                {
                    var sizing = text.GetComponent<CampusTextSizing>();
                    if (sizing == null) sizing = text.gameObject.AddComponent<CampusTextSizing>();
                    sizing.Apply(text);
                }
            }
        }
    }

#if UNITY_EDITOR
    public static void ResetCache() { cached = null; }
#endif
}
