using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Duplica el campus y guarda el callejón como objetos editables.</summary>
[InitializeOnLoad]
public static class LevelFourBuilder
{
    public const string ScenePath = "Assets/Scenes/Nivel04_ElCallejon.unity";
    const string Art = "Assets/Art/LevelFour";
    const string Work = "Level4Work";
    static double nextPoll;
    static LevelFourBuilder() { EditorApplication.update += Poll; }

    static void Poll()
    {
        if (EditorApplication.timeSinceStartup < nextPoll || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        nextPoll = EditorApplication.timeSinceStartup + 1;
        string command = Work + "/command.txt";
        if (!File.Exists(command)) return;
        string request = File.ReadAllText(command).Trim();
        File.Delete(command); File.WriteAllText(Work + "/EstadoEditor.txt", request + " playing=" + EditorApplication.isPlaying + " time=" + DateTime.Now);
        try
        {
            if (request == "build" && !EditorApplication.isPlaying) Build();
            if (request == "test" && !EditorApplication.isPlaying) BeginTest();
            if (request == "refine" && !EditorApplication.isPlaying) Refine();
            if (request == "upgrade" && !EditorApplication.isPlaying) LevelFourUpgrade.Apply();
            if (request == "mission" && !EditorApplication.isPlaying) LevelFourMissionBuilder.Apply();
            if (request == "mission-test" && !EditorApplication.isPlaying) LevelFourMissionVerification.Begin();
            if (request == "mission-polish" && !EditorApplication.isPlaying) LevelFourMissionBuilder.Polish();
            if (request == "details" && !EditorApplication.isPlaying) LevelFourDetailBuilder.Apply();
            if (request == "campus" && !EditorApplication.isPlaying) LevelFourCampusUpgrade.Apply();
        }
        catch (Exception e) { File.WriteAllText(Work + "/error.txt", e.ToString()); Debug.LogException(e); }
    }

    [MenuItem("Ya Casi/Nivel 4/Crear apertura del callejón")]
    public static void Build()
    {
        Directory.CreateDirectory(Work);
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            throw new InvalidOperationException("La escena ya existe; no se sobrescribe el diseño editado.");
        Directory.CreateDirectory(Art);
        AssetDatabase.Refresh();
        var pixel = Texture("Pixel", 1, 1, (x, y) => Color.white, 1);
        var mesh = Texture("Malla", 64, 32, (x, y) =>
        {
            if (y < 2 || y > 29) return new Color32(100, 126, 125, 255);
            return (x + y) % 12 == 0 || (x - y + 96) % 12 == 0 ? new Color32(133, 155, 153, 255) : Color.clear;
        }, 16);
        var asphalt = Texture("Asfalto", 64, 64, (x, y) =>
        {
            int n = (x * 17 + y * 41 + x * y) % 23;
            byte c = (byte)(n == 0 ? 63 : n < 3 ? 54 : 47);
            return new Color32(c, (byte)(c + 3), (byte)(c + 8), 255);
        }, 16);
        var beer = Texture("Cerveza", 8, 16, (x, y) =>
        {
            if (y > 10 && (x < 3 || x > 4) || y <= 10 && (x < 1 || x > 6)) return Color.clear;
            if (y == 15) return new Color32(219, 186, 107, 255);
            if (y > 5 && y < 10) return new Color32(235, 207, 136, 255);
            return new Color32(107, 62, 30, 255);
        }, 16);
        var rounded = Texture("PanelRedondeado", 32, 32, (x, y) =>
        {
            float dx = Mathf.Max(0, Mathf.Abs(x - 15.5f) - 7.5f);
            float dy = Mathf.Max(0, Mathf.Abs(y - 15.5f) - 7.5f);
            return dx * dx + dy * dy <= 64 ? Color.white : Color.clear;
        }, 32, true);

        if (!AssetDatabase.CopyAsset("Assets/Scenes/Nivel03_LaRutaDeEntrega.unity", ScenePath))
            throw new InvalidOperationException("No se pudo copiar el campus.");
        Scene previous = SceneManager.GetActiveScene();
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        try
        {
            var behaviours = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<MonoBehaviour>(true)).ToArray();
            var oldPlayer = behaviours.OfType<CampusPathPlayer>().Single();
            var player = oldPlayer.transform;
            var oldStudents = behaviours.OfType<CampusStudentWalker>().Select(s => s.GetComponent<SpriteRenderer>()).Where(s => s != null && s.sprite != null).Take(3).ToArray();
            var friendSprites = oldStudents.Select(s => s.sprite).ToArray();
            foreach (var b in behaviours)
            {
                if (b is CampusPathPlayer || b is CampusDeliveryObjectives || b is CampusProfessorWaiting || b is CampusStudentWalker || b is CampusMinimap || b is CampusFinalDeliveryChallenge || b is CampusCourtesyRequest || b is CampusActorSorting)
                    UnityEngine.Object.DestroyImmediate(b);
            }
            foreach (var root in scene.GetRootGameObjects())
                if (root.name.StartsWith("Profesor_") || root.name.StartsWith("Profesora_")) root.SetActive(false);
            player.name = "Galindo_Nivel04";
            player.position = new Vector3(-8, -47, 0);
            player.GetComponent<SpriteRenderer>().sortingOrder = 1455;
            player.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            var camera = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Camera>(true)).First(c => c.orthographic);
            camera.transform.position = player.position + new Vector3(0, 1.1f, -10);
            camera.orthographicSize = 6.8f;
            var rootAlley = new GameObject("NIVEL 4 · Callejón Politécnico Nacional");
            Transform parent = rootAlley.transform;
            var story = rootAlley.AddComponent<LevelFourIntro>();
            story.player = player;
            story.view = camera;
            story.roundedPanel = rounded;
            Quad("Asfalto del callejón", parent, asphalt, new Vector2(-2, -47), new Vector2(32, 8), Color.white, -30);
            Quad("Banqueta junto a la universidad", parent, pixel, new Vector2(-2, -43.6f), new Vector2(32, .7f), new Color32(126, 121, 109, 255), -20);
            Quad("Guarnición", parent, pixel, new Vector2(-2, -44), new Vector2(32, .16f), new Color32(186, 175, 137, 255), -19);
            for (int i = 0; i < 8; i++)
                Quad("Marca vial " + i, parent, pixel, new Vector2(-15 + i * 4, -49.7f), new Vector2(1.5f, .12f), new Color32(183, 160, 91, 255), -18);
            for (int i = 0; i < 8; i++)
            {
                float x = -16 + i * 4;
                Quad("Malla ciclónica " + i, parent, mesh, new Vector2(x + 2, -43), new Vector2(4, 2), Color.white, 1425);
                var post = Quad("Poste " + i, parent, pixel, new Vector2(x, -43), new Vector2(.13f, 2.3f), new Color32(107, 124, 120, 255), 1426);
                var collider = post.gameObject.AddComponent<BoxCollider2D>();
                collider.size = new Vector2(.13f, .35f);
            }
            var fence = new GameObject("Barrera física de la malla"); fence.transform.SetParent(parent);
            fence.transform.position = new Vector3(-2, -43.8f, 0);
            fence.AddComponent<BoxCollider2D>().size = new Vector2(32, .3f);
            story.climbStart = Marker("Punto para trepar", parent, new Vector2(-1.5f, -44.6f));
            story.landing = Marker("Aterrizaje dentro de UTNG", parent, new Vector2(-1.5f, -41.2f));
            Quad("Tramo señalado de la malla", parent, pixel, new Vector2(-1.5f, -44.15f), new Vector2(1.5f, .09f), new Color32(245, 194, 99, 255), 1427);
            WorldLabel("Señal del salto", parent, "MALLA", new Vector2(-1.5f, -45), .25f, new Color32(245, 194, 99, 255));
            WorldLabel("Calle", parent, "POLITÉCNICO NACIONAL", new Vector2(5, -49.1f), .22f, new Color32(179, 178, 167, 255));
            for (int i = 0; i < friendSprites.Length; i++)
            {
                var friend = new GameObject("Amigo de Galindo " + (i + 1)); friend.transform.SetParent(parent);
                friend.transform.position = new Vector3(-10.3f + i * 1.8f, i == 1 ? -48.2f : -46.7f, 0);
                var renderer = friend.AddComponent<SpriteRenderer>(); renderer.sprite = friendSprites[i]; renderer.sortingOrder = 1450;
                var drink = Quad("Cerveza del amigo", friend.transform, beer, new Vector2(0, 0), new Vector2(.25f, .5f), Color.white, 1460);
                drink.transform.localPosition = new Vector3(.4f, -.05f);
            }
            var bottle = Quad("Cerveza de Galindo", player, beer, Vector2.zero, new Vector2(.25f, .5f), Color.white, 1600);
            bottle.transform.localPosition = new Vector3(.4f, -.05f, 0);
            story.beer = bottle.transform;
            WorldLabel("Lugar de reunión", parent, "EL CALLEJÓN", new Vector2(-8, -45.4f), .28f, Color.white);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            if (!EditorBuildSettings.scenes.Any(s => s.path == ScenePath))
                EditorBuildSettings.scenes = EditorBuildSettings.scenes.Concat(new[] { new EditorBuildSettingsScene(ScenePath, true) }).ToArray();
            CaptureCamera(camera, Work + "/Callejon.png");
            File.WriteAllText(Work + "/Creacion.txt", "OK\nEscena: " + ScenePath + "\nCampus copiado del nivel 3. Escena original intacta.\nMalla con collider, 3 amigos, cerveza, mensaje en celular y secuencia hasta el salto.\n");
            Debug.Log("LEVEL4_BUILD_OK: " + ScenePath);
        }
        finally
        {
            if (previous.IsValid()) SceneManager.SetActiveScene(previous);
            EditorSceneManager.CloseScene(scene, true);
        }
        AssetDatabase.SaveAssets();
        Refine();
    }

    static Transform Marker(string name, Transform parent, Vector2 position)
    {
        var go = new GameObject(name); go.transform.SetParent(parent); go.transform.position = position; return go.transform;
    }

    static SpriteRenderer Quad(string name, Transform parent, Sprite sprite, Vector2 position, Vector2 size, Color color, int order)
    {
        var go = new GameObject(name); go.transform.SetParent(parent); go.transform.position = position;
        var renderer = go.AddComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.color = color; renderer.sortingOrder = order;
        if (name.StartsWith("Asfalto")) { renderer.drawMode = SpriteDrawMode.Tiled; renderer.size = size; }
        else go.transform.localScale = new Vector3(size.x / sprite.bounds.size.x, size.y / sprite.bounds.size.y, 1);
        return renderer;
    }

    static void WorldLabel(string name, Transform parent, string text, Vector2 position, float size, Color color)
    {
        var go = new GameObject(name); go.transform.SetParent(parent); go.transform.position = position;
        var label = go.AddComponent<TMPro.TextMeshPro>();
        label.text = text; label.fontSize = 3; label.color = color; label.alignment = TMPro.TextAlignmentOptions.Center;
        label.rectTransform.sizeDelta = new Vector2(40, 4); go.transform.localScale = Vector3.one * size;
        label.GetComponent<MeshRenderer>().sortingOrder = 1600;
    }

    static Sprite Texture(string name, int w, int h, Func<int, int, Color> pixel, int ppu, bool rounded = false)
    {
        var texture = new Texture2D(w, h, TextureFormat.RGBA32, false);
        var pixels = new Color[w * h];
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) pixels[y * w + x] = pixel(x, y);
        texture.SetPixels(pixels); texture.Apply();
        string path = Art + "/" + name + ".png";
        File.WriteAllBytes(path, texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite; importer.spritePixelsPerUnit = ppu;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.filterMode = FilterMode.Point; importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        if (rounded) importer.spriteBorder = new Vector4(12, 12, 12, 12);
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static void CaptureCamera(Camera camera, string file)
    {
        var target = new RenderTexture(1600, 900, 24);
        var old = camera.targetTexture; var active = RenderTexture.active;
        camera.targetTexture = target;
        camera.Render(); RenderTexture.active = target;
        var image = new Texture2D(1600, 900, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); image.Apply();
        File.WriteAllBytes(file, image.EncodeToPNG());
        camera.targetTexture = old; RenderTexture.active = active;
        target.Release(); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(image);
    }

    [MenuItem("Ya Casi/Nivel 4/Abrir escena del callejón")]
    public static void Open()
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(ScenePath);
    }

    static void BeginTest()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Hay una escena sin guardar; se conserva antes de probar.");
        EditorSceneManager.OpenScene(ScenePath);
        var intro = UnityEngine.Object.FindFirstObjectByType<LevelFourIntro>();
        intro.player.GetComponent<SpriteRenderer>().sortingOrder = 1455;
        if (!EditorBuildSettings.scenes.Any(s => s.path == ScenePath))
            EditorBuildSettings.scenes = EditorBuildSettings.scenes.Concat(new[] { new EditorBuildSettingsScene(ScenePath, true) }).ToArray();
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        SessionState.SetBool("Level4.RunTest", true);
        EditorApplication.isPlaying = true;
    }

    static void Refine()
    {
        var scene = SceneManager.GetSceneByPath(ScenePath);
        bool wasOpen = scene.IsValid() && scene.isLoaded;
        if (!wasOpen) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        var intro = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<LevelFourIntro>(true)).Single();
        var panelImporter = (TextureImporter)AssetImporter.GetAtPath(Art + "/PanelRedondeado.png");
        panelImporter.spriteImportMode = SpriteImportMode.Single;
        panelImporter.spriteBorder = new Vector4(12, 12, 12, 12);
        panelImporter.SaveAndReimport();
        intro.roundedPanel = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "/PanelRedondeado.png");
        bool relocate = intro.transform.position.y == 0;
        if (relocate)
        {
            intro.transform.position += Vector3.down * 7;
            intro.player.position += Vector3.down * 7;
            intro.alleyMin += Vector2.down * 7;
            intro.alleyMax += Vector2.down * 7;
        }
        intro.player.GetComponent<SpriteRenderer>().sortingOrder = 1455;
        foreach (var renderer in intro.GetComponentsInChildren<SpriteRenderer>())
        {
            if (renderer.name.StartsWith("Malla ciclónica") || renderer.name.StartsWith("Poste ")) renderer.enabled = false;
            if (renderer.name.StartsWith("Asfalto"))
            {
                renderer.sortingOrder = 10;
                renderer.size = new Vector2(100, 20);
                renderer.transform.position = new Vector3(-2, -61, 0);
            }
            if (renderer.name.StartsWith("Banqueta")) renderer.sortingOrder = 11;
            if (renderer.name.StartsWith("Guarnición")) renderer.sortingOrder = 12;
            if (renderer.name.StartsWith("Marca vial")) renderer.sortingOrder = 13;
        }
        foreach (var text in intro.GetComponentsInChildren<TMPro.TextMeshPro>())
        {
            if (relocate) text.transform.localScale *= 2.8f;
            if (relocate && text.name == "Lugar de reunión") text.transform.position += Vector3.down * .6f;
            if (text.name == "Calle") text.transform.position = new Vector3(-8, -57.2f, 0);
        }
        intro.view.transform.position = intro.player.position + new Vector3(0, 1.1f, -10);
        if (!EditorBuildSettings.scenes.Any(s => s.path == ScenePath))
            EditorBuildSettings.scenes = EditorBuildSettings.scenes.Concat(new[] { new EditorBuildSettingsScene(ScenePath, true) }).ToArray();
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        CaptureCamera(intro.view, Work + "/Callejon.png");
        if (!wasOpen) EditorSceneManager.CloseScene(scene, true);
        File.WriteAllText(Work + "/Ubicacion.txt", "OK: callejón al exterior de la malla sur existente.\n");
    }
}

