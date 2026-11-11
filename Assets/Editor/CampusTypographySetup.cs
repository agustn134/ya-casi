using System;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

/// <summary>Prepara un atlas persistente para que Redaction 35 funcione también en WebGL.</summary>
[InitializeOnLoad]
public sealed class CampusTypographySetup : IPreprocessBuildWithReport
{
    public const string FontPath = "Assets/Resources/CampusRedaction35.asset";
    const string SourcePath = "Assets/Art/Fonts/Redaction_35/Redaction35-Regular.ttf";
    const string Required = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789ÁÉÍÓÚÜÑáéíóúüñ¿¡";
    public int callbackOrder => -100;

    static CampusTypographySetup()
    {
        EditorApplication.update += EnsureWhenReady;
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.EnteredEditMode) return;
            EditorApplication.update -= EnsureWhenReady;
            EditorApplication.update += EnsureWhenReady;
        };
    }

    static void EnsureWhenReady()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
            return;
        EditorApplication.update -= EnsureWhenReady;
        if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath) != null) return;
        if (AssetDatabase.LoadAssetAtPath<Font>(SourcePath) == null) return;
        try { EnsureFont(); }
        catch (Exception e) { Debug.LogError("No se pudo preparar Redaction 35: " + e.Message); }
    }

    [MenuItem("Ya Casi/Nivel 3/Preparar fuente Redaction 35")]
    public static void EnsureFont()
    {
        var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (existing != null)
        {
            Validate(existing);
            return;
        }
        var source = AssetDatabase.LoadAssetAtPath<Font>(SourcePath);
        if (source == null) throw new InvalidOperationException("No se encontró " + SourcePath);
        if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");

        TMP_FontAsset font = TMP_FontAsset.CreateFontAsset(source, 90, 9, GlyphRenderMode.SDFAA,
            2048, 2048, AtlasPopulationMode.Dynamic, false);
        if (font == null) throw new InvalidOperationException("TextMesh Pro no pudo importar la fuente.");
        font.name = CampusTypography.ResourceName;
        font.hideFlags = HideFlags.None;
        var characters = new StringBuilder();
        for (int c = 32; c <= 126; c++) characters.Append((char)c);
        for (int c = 160; c <= 255; c++) characters.Append((char)c);
        characters.Append("–—−…←↑→↓");
        font.TryAddCharacters(characters.ToString(), out string missing);
        if (!font.HasCharacters(Required))
        {
            UnityEngine.Object.DestroyImmediate(font);
            throw new InvalidOperationException("Faltan letras o acentos necesarios para los textos del campus.");
        }
        // Nunca dependemos de añadir glifos o de buscar fuentes del sistema al ejecutar WebGL.
        font.atlasPopulationMode = AtlasPopulationMode.Static;
        font.isMultiAtlasTexturesEnabled = false;
        AssetDatabase.CreateAsset(font, FontPath);
        for (int i = 0; i < font.atlasTextures.Length; i++)
        {
            Texture2D texture = font.atlasTextures[i];
            if (texture == null) continue;
            texture.name = "Redaction 35 Regular Atlas " + i;
            texture.hideFlags = HideFlags.None;
            texture.filterMode = FilterMode.Bilinear;
            AssetDatabase.AddObjectToAsset(texture, font);
        }
        font.material.name = "Redaction 35 Regular Material";
        font.material.hideFlags = HideFlags.None;
        AssetDatabase.AddObjectToAsset(font.material, font);
        EditorUtility.SetDirty(font);
        AssetDatabase.SaveAssets();
        CampusTypography.ResetCache();
        Validate(font);
        Debug.Log("CAMPUS_FONT_READY: Redaction 35 Regular, atlas SDF estático y caracteres españoles."
            + (string.IsNullOrEmpty(missing) ? "" : " Símbolos opcionales no incluidos: " + missing));
    }

    static void Validate(TMP_FontAsset font)
    {
        if (font.atlasPopulationMode != AtlasPopulationMode.Static || !font.HasCharacters(Required)
            || font.atlasTextures == null || font.atlasTextures.Length == 0 || font.atlasTextures[0] == null)
            throw new InvalidOperationException("El atlas Redaction 35 no está preparado para la compilación.");
    }

    public void OnPreprocessBuild(BuildReport report)
    {
        EnsureFont();
        Validate(AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath));
    }
}
