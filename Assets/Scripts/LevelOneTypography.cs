using TMPro;
using UnityEngine;

/// <summary>Fuente legible del nivel 1, compartiendo el atlas ya usado por el campus.</summary>
public static class LevelOneTypography
{
    static TMP_FontAsset cached;

    public static TMP_FontAsset Font
    {
        get
        {
            if (cached == null)
            {
                cached = Resources.Load<TMP_FontAsset>("CampusRedaction35");
                if (cached == null)
                {
                    var config = Resources.Load<U2GameConfig>("U2GameConfig");
                    if (config != null) cached = config.font;
                }
            }
            return cached;
        }
    }

    /// <summary>Aplica la tipografía sin alterar los tamaños ni la distribución del HUD.</summary>
    public static void Apply(GameObject root)
    {
        var font = Font;
        if (root == null || font == null) return;
        foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
        {
            text.font = font;
            text.fontSharedMaterial = font.material;
            text.characterSpacing = 0;
            text.wordSpacing = 0;
        }
    }
}
