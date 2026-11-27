using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>Prepara seis alumnos con recortes explícitos; nunca conserva sprites de Galindo.</summary>
[InitializeOnLoad]
public static class U2StudentArt
{
    const int ArtVersion = 2;
    const string ArtFolder = "Assets/Art/LevelOneStudents";
    const string AnimationFolder = "Assets/Animations/Students";
    const string PlayerController = "Assets/Animations/Player/Controllers/PlayerAnimator.controller";
    const string ConfigPath = "Assets/Resources/U2GameConfig.asset";
    static readonly string[] Directions = { "Right", "Up", "Left", "Down" };

    static U2StudentArt() { EditorApplication.delayCall += EnsurePrepared; }

    static void EnsurePrepared()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var config = AssetDatabase.LoadAssetAtPath<U2GameConfig>(ConfigPath);
        if (config != null && !IsPrepared(config)) Generate();
    }

    [MenuItem("Ya Casi/U2/Preparar estudiantes")]
    public static void Generate()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var config = AssetDatabase.LoadAssetAtPath<U2GameConfig>(ConfigPath);
        var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(PlayerController);
        if (config == null || controller == null)
        {
            Debug.LogError("No se pueden preparar alumnos: falta U2GameConfig o el controlador base.");
            return;
        }
        EnsureFolder(ArtFolder);
        EnsureFolder(AnimationFolder);
        var variants = new List<RuntimeAnimatorController>();
        var idleSprites = new List<Sprite>();
        try
        {
            for (int number = 1; number <= 6; number++)
            {
                string sheetPath = SheetPath(number);
                if (!File.Exists(sheetPath))
                {
                    string original = $"Assets/ThirdParty/LimeZu/ModernInteriors/Characters/Premade/16x16/Premade_Character_{number:00}.png";
                    if (!File.Exists(original)) throw new FileNotFoundException("Falta el personaje LimeZu.", original);
                    File.Copy(original, sheetPath);
                    AssetDatabase.ImportAsset(sheetPath, ImportAssetOptions.ForceSynchronousImport);
                }
                var sprites = PrepareSheet(sheetPath);
                var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
                foreach (var originalClip in controller.animationClips.Distinct())
                {
                    bool walking = originalClip.name.StartsWith("Walk", StringComparison.Ordinal);
                    bool idle = originalClip.name.StartsWith("Idle", StringComparison.Ordinal);
                    string direction = walking ? originalClip.name.Substring(4) : idle ? originalClip.name.Substring(4) : "";
                    if (!Directions.Contains(direction))
                        throw new InvalidOperationException("Animación de personaje desconocida: " + originalClip.name);
                    var frames = new ObjectReferenceKeyframe[walking ? 6 : 1];
                    for (int frame = 0; frame < frames.Length; frame++)
                    {
                        string spriteName = walking ? $"Walk{direction}_{frame}" : "Idle" + direction;
                        frames[frame] = new ObjectReferenceKeyframe { time = frame / 8f, value = sprites[spriteName] };
                    }
                    var clip = new AnimationClip { name = $"Student{number}_{originalClip.name}", frameRate = 8f };
                    var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
                    AnimationUtility.SetObjectReferenceCurve(clip, binding, frames);
                    var settings = AnimationUtility.GetAnimationClipSettings(clip);
                    settings.loopTime = true;
                    settings.startTime = 0;
                    settings.stopTime = frames.Length / 8f;
                    AnimationUtility.SetAnimationClipSettings(clip, settings);
                    string clipPath = $"{AnimationFolder}/Student{number}_{originalClip.name}.anim";
                    var saved = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
                    if (saved == null) { AssetDatabase.CreateAsset(clip, clipPath); saved = clip; }
                    else { EditorUtility.CopySerialized(clip, saved); Object.DestroyImmediate(clip); }
                    EditorUtility.SetDirty(saved);
                    AssetDatabase.SaveAssetIfDirty(saved);
                    overrides.Add(new KeyValuePair<AnimationClip, AnimationClip>(originalClip, saved));
                }
                string variantPath = $"{AnimationFolder}/Student{number}.overrideController";
                var variant = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(variantPath);
                if (variant == null)
                {
                    variant = new AnimatorOverrideController(controller);
                    AssetDatabase.CreateAsset(variant, variantPath);
                }
                else variant.runtimeAnimatorController = controller;
                variant.ApplyOverrides(overrides);
                EditorUtility.SetDirty(variant);
                AssetDatabase.SaveAssetIfDirty(variant);
                variants.Add(variant);
                idleSprites.Add(sprites["IdleDown"]);
            }
            config.studentControllers = variants.ToArray();
            config.studentIdleSprites = idleSprites.ToArray();
            config.studentArtVersion = ArtVersion;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssetIfDirty(config);
            Debug.Log("U2_STUDENTS: seis personajes distintos de Galindo, con ocho animaciones propias y recortes fijos.");
        }
        catch (Exception error)
        {
            Debug.LogException(error);
        }
    }

    static Dictionary<string, Sprite> PrepareSheet(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            importer = AssetImporter.GetAtPath(path) as TextureImporter;
        }
        if (importer == null) throw new InvalidOperationException("No se pudo importar " + path);
        var expected = Slices();
#pragma warning disable 0618
        var current = importer.spritesheet;
        bool sameSlices = current != null && current.Length == expected.Length && expected.All(slice =>
            current.Any(item => item.name == slice.name && item.rect == slice.rect &&
                item.alignment == (int)SpriteAlignment.Custom && item.pivot == slice.pivot));
#pragma warning restore 0618
        bool changed = importer.textureType != TextureImporterType.Sprite ||
            importer.spriteImportMode != SpriteImportMode.Multiple || importer.spritePixelsPerUnit != 16 ||
            importer.filterMode != FilterMode.Point || importer.textureCompression != TextureImporterCompression.Uncompressed ||
            importer.mipmapEnabled || !sameSlices;
        if (changed)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 16;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            // Conservar las definiciones correctas mantiene los IDs de sprite al reimportar.
            if (!sameSlices)
            {
#pragma warning disable 0618
                importer.spritesheet = expected;
#pragma warning restore 0618
            }
            importer.SaveAndReimport();
        }
        var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToDictionary(sprite => sprite.name);
        if (sprites.Count != 28 || expected.Any(slice => !sprites.ContainsKey(slice.name)))
            throw new InvalidOperationException("La hoja debe contener los 28 cuadros de personaje: " + path);
        foreach (var slice in expected)
            if (sprites[slice.name].rect != slice.rect || AssetDatabase.GetAssetPath(sprites[slice.name]) != path)
                throw new InvalidOperationException("Recorte de personaje inválido: " + path + "/" + slice.name);
        return sprites;
    }

    static SpriteMetaData[] Slices()
    {
        var slices = new List<SpriteMetaData>();
        for (int i = 0; i < Directions.Length; i++) slices.Add(Slice("Idle" + Directions[i], i, 0));
        for (int frame = 0; frame < 6; frame++)
        {
            slices.Add(Slice("WalkRight_" + frame, frame, 1));
            slices.Add(Slice("WalkLeft_" + frame, 12 + frame, 1));
            slices.Add(Slice("WalkUp_" + frame, 6 + frame, 2));
            slices.Add(Slice("WalkDown_" + frame, 18 + frame, 2));
        }
        return slices.ToArray();
    }

    static SpriteMetaData Slice(string name, int column, int row) => new SpriteMetaData
    {
        name = name,
        rect = new Rect(column * 16, 656 - (row + 1) * 32 - 2, 16, 32),
        alignment = (int)SpriteAlignment.Custom,
        pivot = new Vector2(.5f, .375f)
    };

    static bool IsPrepared(U2GameConfig config)
    {
        if (config.studentArtVersion != ArtVersion || config.studentControllers == null || config.studentControllers.Length != 6 ||
            config.studentIdleSprites == null || config.studentIdleSprites.Length != 6) return false;
        for (int i = 0; i < 6; i++)
        {
            string path = SheetPath(i + 1);
            if (config.studentIdleSprites[i] == null || AssetDatabase.GetAssetPath(config.studentIdleSprites[i]) != path) return false;
            var variant = config.studentControllers[i] as AnimatorOverrideController;
            if (variant == null) return false;
            var pairs = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            variant.GetOverrides(pairs);
            if (pairs.Count != 8) return false;
            foreach (var pair in pairs)
            {
                if (pair.Value == null) return false;
                var bindings = AnimationUtility.GetObjectReferenceCurveBindings(pair.Value);
                if (bindings.Length == 0) return false;
                foreach (var binding in bindings)
                {
                    var frames = AnimationUtility.GetObjectReferenceCurve(pair.Value, binding);
                    if (frames.Length == 0 || frames.Any(frame => !(frame.value is Sprite) || AssetDatabase.GetAssetPath(frame.value) != path)) return false;
                }
            }
        }
        return true;
    }

    static string SheetPath(int number) => $"{ArtFolder}/Student_{number:00}.png";

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        if (Directory.Exists(path)) { AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport); return; }
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
