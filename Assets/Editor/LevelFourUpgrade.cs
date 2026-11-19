using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>Mejoras locales del nivel 4: calle nocturna, alumnos y malla continua.</summary>
public static class LevelFourUpgrade
{
    const string Art = "Assets/Art/LevelFour";
    const string Characters = Art + "/Students";
    static Sprite pixel, bottle;
    static Material lit, unlit;

    [MenuItem("Ya Casi/Nivel 4/Mejorar calle, alumnos y noche")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Detén Play antes de editar la escena.");
        var scene = SceneManager.GetSceneByPath(LevelFourBuilder.ScenePath);
        bool wasOpen = scene.IsValid() && scene.isLoaded;
        if (wasOpen && scene.isDirty) throw new InvalidOperationException("La escena tiene cambios sin guardar; se conservan antes de aplicar la mejora.");
        string backup = "Level4Work/Nivel04_AntesDeNoche.unity.bak";
        if (!File.Exists(backup)) File.Copy(LevelFourBuilder.ScenePath, backup);
        Directory.CreateDirectory(Characters);
        string source = "C:/Users/elmane/Desktop/assets/moderninteriors-win/2_Characters/Old/Single_Characters_Legacy/16x16/";
        string[] names = { "Amelia", "Lucy", "Molly", "Rob", "Pier", "Samuel", "Ash", "Adam", "Alex", "Roki" };
        foreach (string name in names)
        {
            string output = Characters + "/" + name + ".png";
            if (!File.Exists(output)) File.Copy(source + name + "_idle_anim_16x16.png", output);
            AssetDatabase.ImportAsset(output);
            var importer = (TextureImporter)AssetImporter.GetAtPath(output);
            importer.textureType = TextureImporterType.Default; importer.isReadable = true;
            importer.filterMode = FilterMode.Point; importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
        File.Copy("C:/Users/elmane/Desktop/assets/moderninteriors-win/LICENSE.txt", Characters + "/LICENSE.txt", true);
        pixel = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "/Pixel.png");
        bottle = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "/Cerveza.png");
        lit = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Lit-Default.mat");
        unlit = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat");
        if (lit == null) throw new InvalidOperationException("No se encontró el material Sprite-Lit de URP.");
        var lamp = MakeLamp();
        var asphalt = MakeAsphalt();
        if (!wasOpen) scene = EditorSceneManager.OpenScene(LevelFourBuilder.ScenePath, OpenSceneMode.Additive);
        Scene previous = SceneManager.GetActiveScene(); SceneManager.SetActiveScene(scene);
        try
        {
            var intro = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<LevelFourIntro>(true)).Single();
            // Reaplicar solo reemplaza esta nueva capa; conserva el campus y el controlador de historia.
            var old = intro.transform.Find("Calle nocturna y grupos");
            if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
            var obsolete = intro.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("Amigo de Galindo ") || t.name == "Tramo señalado de la malla" || t.name == "Señal del salto" || t.name == "Lugar de reunión" || t.name.StartsWith("Marca vial ") || t.name == "Calle").ToArray();
            foreach (var t in obsolete) if (t != null) UnityEngine.Object.DestroyImmediate(t.gameObject);
            var root = new GameObject("Calle nocturna y grupos"); root.transform.SetParent(intro.transform, false); root.transform.position = Vector3.zero;
            Transform parent = root.transform;
            intro.alleyMin = new Vector2(-31, -59.2f);
            intro.alleyMax = new Vector2(27, -51.5f);
            intro.climbStart.position = new Vector3(-8, -51.5f, 0);
            intro.landing.position = new Vector3(-8, -48.2f, 0);
            intro.climbBack = AssetDatabase.LoadAllAssetsAtPath("Assets/ThirdParty/LimeZu/ModernInteriors/Characters/Premade/16x16/Premade_Character_11.png").OfType<Sprite>().First(s => s.name == "Premade_Character_11_1");
            intro.view.orthographicSize = 6.4f;
            var road = intro.GetComponentsInChildren<SpriteRenderer>().First(s => s.name == "Asfalto del callejón");
            road.sprite = asphalt; road.size = new Vector2(120, 24); road.transform.position = new Vector3(0, -63, 0);
            Quad("Banqueta exterior continua", parent, pixel, new Vector2(0, -52.15f), new Vector2(110, 2.2f), new Color32(126, 126, 133, 255), 15);
            Quad("Guarnición exterior", parent, pixel, new Vector2(0, -53.25f), new Vector2(110, .16f), new Color32(205, 193, 151, 255), 16);
            for (int x = -54; x < 55; x++)
                Quad("Junta de banqueta", parent, pixel, new Vector2(x, -52.15f), new Vector2(.025f, 2.05f), new Color32(86, 91, 104, 255), 17);
            for (int x = -54; x < 55; x += 5)
                Quad("Línea central de calle", parent, pixel, new Vector2(x, -57.4f), new Vector2(2.4f, .10f), new Color32(209, 182, 103, 255), 14);
            for (int i = 0; i < 7; i++)
                Quad("Paso peatonal", parent, pixel, new Vector2(-2.5f, -54.1f - i * .75f), new Vector2(3.4f, .35f), new Color32(207, 208, 199, 255), 14);
            for (int x = -30; x <= 30; x += 12)
            {
                Quad("Cajón de estacionamiento", parent, pixel, new Vector2(x, -59.8f), new Vector2(.09f, 2.4f), new Color32(188, 188, 175, 255), 14);
                Quad("Tope de rueda", parent, pixel, new Vector2(x + 2.7f, -60.7f), new Vector2(1.3f, .23f), new Color32(166, 153, 107, 255), 18);
            }
            // Lado opuesto: fachadas, puertas cerradas y ventanas cálidas.
            for (int i = 0; i < 9; i++)
            {
                float x = -36 + i * 9;
                Quad("Local del otro lado de la calle", parent, pixel, new Vector2(x, -63.3f), new Vector2(8.6f, 3.3f), i % 2 == 0 ? new Color32(100, 82, 76, 255) : new Color32(76, 89, 94, 255), 20);
                Quad("Cornisa del local", parent, pixel, new Vector2(x, -61.6f), new Vector2(8.8f, .2f), new Color32(48, 55, 66, 255), 21);
                Quad("Cortina cerrada", parent, pixel, new Vector2(x, -62.7f), new Vector2(2.8f, 2), new Color32(64, 70, 81, 255), 22);
                for (int j = 0; j < 5; j++) Quad("Lámina de cortina", parent, pixel, new Vector2(x, -61.9f - j * .35f), new Vector2(2.75f, .04f), new Color32(93, 97, 105, 255), 23);
                Quad("Ventana iluminada", parent, pixel, new Vector2(x + 2.8f, -62.7f), new Vector2(1.2f, 1.3f), new Color32(237, 174, 88, 255), 24, true);
            }
            // Diferentes grupos con poses mirando al centro de su conversación.
            Vector2[] centers = { new Vector2(-8.2f, -54.1f), new Vector2(-17, -54.1f), new Vector2(1.5f, -54.1f), new Vector2(12, -54.1f), new Vector2(23, -55.1f), new Vector2(-26, -55.1f), new Vector2(-16, -57), new Vector2(10, -57) };
            var closeFriends = new Transform[3];
            for (int group = 0; group < centers.Length; group++)
            {
                var groupRoot = new GameObject("Grupo de alumnos " + (group + 1)); groupRoot.transform.SetParent(parent, false);
                for (int j = 0; j < 3; j++)
                {
                    Vector2 offset = j == 0 ? new Vector2(-1.6f, .35f) : j == 1 ? new Vector2(1.5f, .25f) : new Vector2(-.3f, -1.1f);
                    int facing = j == 0 ? 2 : j == 1 ? 0 : 1;
                    string name = names[(group * 3 + j) % names.Length];
                    var student = Student(name, groupRoot.transform, centers[group] + offset, facing, group * 1.9f + j);
                    if (group == 0) closeFriends[j] = student;
                }
                // Caja enfriadora para cada reunión; tapa, cuerpo y asa.
                Vector2 cooler = centers[group] + new Vector2(.1f, -.5f);
                Quad("Hielera", groupRoot.transform, pixel, cooler, new Vector2(.7f, .43f), new Color32(113, 47, 43, 255), 1600);
                Quad("Tapa de hielera", groupRoot.transform, pixel, cooler + Vector2.up * .2f, new Vector2(.77f, .11f), new Color32(207, 210, 198, 255), 1601);
            }
            intro.friends = closeFriends;
            // Lámparas con luz real 2D, sin tocar los renderizadores de otros niveles.
            foreach (var light in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Light2D>(true)).ToArray())
                UnityEngine.Object.DestroyImmediate(light);
            var moon = new GameObject("Noche · iluminación ambiente"); moon.transform.SetParent(parent);
            var ambient = moon.AddComponent<Light2D>(); ambient.lightType = Light2D.LightType.Global;
            ambient.color = new Color(.44f, .58f, .92f); ambient.intensity = .48f;
            for (int i = 0; i < 7; i++)
            {
                float x = -34 + i * 11;
                var post = Quad("Lámpara de calle " + (i + 1), parent, lamp, new Vector2(x, -51.9f), new Vector2(1.6f, 4.8f), Color.white, 1550);
                post.transform.position += Vector3.up * 2.15f;
                var lightObject = new GameObject("Luz cálida " + (i + 1)); lightObject.transform.SetParent(parent);
                lightObject.transform.position = new Vector3(x - .6f, -53.3f, 0);
                var point = lightObject.AddComponent<Light2D>(); point.lightType = Light2D.LightType.Point;
                point.color = new Color(1, .73f, .38f); point.intensity = 1.3f;
                point.pointLightInnerRadius = 1.2f; point.pointLightOuterRadius = 5.5f;
                point.falloffIntensity = .6f;
                Quad("Bombilla encendida", parent, pixel, new Vector2(x - .45f, -48.35f), new Vector2(.29f, .11f), new Color32(255, 236, 165, 255), 1552, true);
                // Bancas y botes junto a la banqueta.
                float bx = x + 2.8f;
                Quad("Asiento de banca", parent, pixel, new Vector2(bx, -52.45f), new Vector2(1.55f, .22f), new Color32(141, 95, 59, 255), 1510);
                Quad("Respaldo de banca", parent, pixel, new Vector2(bx, -52.1f), new Vector2(1.55f, .25f), new Color32(156, 108, 63, 255), 1511);
                Quad("Pata de banca", parent, pixel, new Vector2(bx - .55f, -52.75f), new Vector2(.1f, .5f), new Color32(46, 53, 63, 255), 1512);
                Quad("Pata de banca", parent, pixel, new Vector2(bx + .55f, -52.75f), new Vector2(.1f, .5f), new Color32(46, 53, 63, 255), 1512);
                Quad("Bote de basura", parent, pixel, new Vector2(x - 1.8f, -52.3f), new Vector2(.48f, .72f), new Color32(49, 85, 77, 255), 1512);
                Quad("Tapa del bote", parent, pixel, new Vector2(x - 1.8f, -51.93f), new Vector2(.55f, .09f), new Color32(42, 57, 60, 255), 1513);
            }
            foreach (var renderer in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Renderer>(true)))
                if ((renderer is SpriteRenderer || renderer is TilemapRenderer) && !renderer.name.StartsWith("Bombilla") && renderer.name != "Ventana iluminada") renderer.sharedMaterial = lit;
            intro.view.backgroundColor = new Color32(15, 22, 39, 255);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            File.WriteAllText("Level4Work/MejorasV2.txt", "OK: 24 alumnos, 10 personajes, 7 lámparas 2D, ambiente nocturno, calle extendida, sin entrada marcada y escalada continua.\n");
        }
        finally
        {
            SceneManager.SetActiveScene(previous);
            if (!wasOpen) EditorSceneManager.CloseScene(scene, true);
        }
        AssetDatabase.SaveAssets();
    }

    static Transform Student(string name, Transform parent, Vector2 position, int facing, float phase)
    {
        string file = Characters + "/" + name + ".png";
        var sheet = AssetDatabase.LoadAssetAtPath<Texture2D>(file);
        string posePath = Characters + "/" + name + "_" + facing + ".png";
        if (!File.Exists(posePath))
        {
            var pose = new Texture2D(16, 32, TextureFormat.RGBA32, false);
            pose.SetPixels(sheet.GetPixels(facing * 6 * 16, 0, 16, 32)); pose.Apply();
            File.WriteAllBytes(posePath, pose.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(pose);
        }
        AssetDatabase.ImportAsset(posePath);
        var importer = (TextureImporter)AssetImporter.GetAtPath(posePath);
        importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 16; importer.spritePivot = new Vector2(.5f, .4f);
        var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.Custom; settings.spritePivot = new Vector2(.5f, .4f); importer.SetTextureSettings(settings);
        importer.filterMode = FilterMode.Point; importer.mipmapEnabled = false; importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        var go = new GameObject("Alumno · " + name); go.transform.SetParent(parent); go.transform.position = position;
        var renderer = go.AddComponent<SpriteRenderer>(); renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(posePath);
        renderer.sortingOrder = Mathf.RoundToInt(1000 - (position.y - .5f) * 10); renderer.sharedMaterial = lit;
        var animation = go.AddComponent<LevelFourStudent>(); animation.sheet = sheet; animation.facing = facing; animation.phase = phase;
        var drink = Quad("Cerveza del alumno", go.transform, bottle, Vector2.zero, new Vector2(.23f, .46f), Color.white, renderer.sortingOrder + 2);
        drink.transform.localPosition = new Vector3(facing == 0 ? -.3f : .3f, -.1f);
        animation.bottle = drink.transform;
        return go.transform;
    }

    static SpriteRenderer Quad(string name, Transform parent, Sprite sprite, Vector2 pos, Vector2 size, Color color, int order, bool emissive = false)
    {
        var go = new GameObject(name); go.transform.SetParent(parent); go.transform.position = pos;
        go.transform.localScale = new Vector3(size.x / sprite.bounds.size.x, size.y / sprite.bounds.size.y, 1);
        var renderer = go.AddComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.color = color; renderer.sortingOrder = order; renderer.sharedMaterial = emissive ? unlit : lit;
        return renderer;
    }

    static Sprite SaveSprite(string name, Texture2D image, int ppu)
    {
        string path = Art + "/" + name + ".png"; File.WriteAllBytes(path, image.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(image);
        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path); importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single; importer.spritePixelsPerUnit = ppu; importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false; importer.textureCompression = TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static Sprite MakeLamp()
    {
        var texture = new Texture2D(32, 96, TextureFormat.RGBA32, false);
        var pixels = new Color[32 * 96];
        for (int y = 0; y < 96; y++) for (int x = 0; x < 32; x++)
        {
            Color color = Color.clear;
            if (x >= 18 && x <= 21 && y >= 5 && y <= 87) color = x == 18 ? new Color32(140, 148, 155, 255) : new Color32(62, 77, 91, 255);
            if (x >= 14 && x <= 25 && y < 7) color = new Color32(46, 58, 72, 255);
            if (x >= 7 && x <= 20 && y >= 84 && y <= 87) color = new Color32(71, 84, 97, 255);
            if (x >= 6 && x <= 9 && y >= 77 && y <= 85) color = new Color32(75, 88, 101, 255);
            if (x >= 2 && x <= 14 && y >= 74 && y <= 77) color = new Color32(54, 67, 79, 255);
            if (x >= 4 && x <= 12 && y >= 71 && y <= 73) color = new Color32(255, 225, 147, 255);
            pixels[y * 32 + x] = color;
        }
        texture.SetPixels(pixels); texture.Apply(); return SaveSprite("Farola", texture, 16);
    }

    static Sprite MakeAsphalt()
    {
        var texture = new Texture2D(128, 128, TextureFormat.RGBA32, false);
        var pixels = new Color[128 * 128];
        for (int y = 0; y < 128; y++) for (int x = 0; x < 128; x++)
        {
            int noise = (x * 37 + y * 71 + x * y * 3) % 41;
            byte value = (byte)(noise < 2 ? 70 : noise < 5 ? 59 : 49);
            if (Mathf.Abs(x - (24 + y / 3)) < 1 && y > 50 || y % 59 == 0 && x > 65) value = 36;
            pixels[y * 128 + x] = new Color32(value, (byte)(value + 4), (byte)(value + 11), 255);
        }
        texture.SetPixels(pixels); texture.Apply(); return SaveSprite("AsfaltoNocturno", texture, 16);
    }
}
