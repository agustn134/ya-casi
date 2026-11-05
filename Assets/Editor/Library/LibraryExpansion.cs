using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>Amplía la escena existente conservando jugador, mobiliario y configuración.</summary>
[InitializeOnLoad]
public static class LibraryExpansion
{
    const string Marker = "LibraryExpansionV2.txt";
    const string Art = "Assets/Art/Library/";
    static LibraryExpansion() { EditorApplication.delayCall += TryUpgrade; }
    static void TryUpgrade()
    {
        if (File.Exists(Marker) || !File.Exists(LibraryLevelBuilder.Path)) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        Upgrade();
    }
    [MenuItem("Ya Casi/Nivel 2/Ampliar biblioteca y reparar salida")]
    public static void Upgrade()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogWarning("Sal de Play para ampliar la biblioteca."); return; }
        var scene = SceneManager.GetSceneByPath(LibraryLevelBuilder.Path);
        bool close = !scene.isLoaded;
        if (close) scene = EditorSceneManager.OpenScene(LibraryLevelBuilder.Path, OpenSceneMode.Additive);
        if (scene.isDirty) { Debug.LogWarning("Guarda la escena de biblioteca antes de ampliarla."); return; }
        string backup = "LibraryBeforeExpansion.unity.bak";
        if (!File.Exists(backup)) File.Copy(LibraryLevelBuilder.Path, backup);
        var previous = SceneManager.GetActiveScene(); SceneManager.SetActiveScene(scene);
        try
        {
            var manager = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<LibraryLevelManager>()).Single();
            if (manager.shelves.Length >= 30) return;
            var existing = manager.shelves.OrderBy(s => s.section).ToArray();
            var template = existing[0];
            var shelves = new LibraryShelf[18];
            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 6; col++)
                {
                    int index = row * 6 + col;
                    var shelf = index < existing.Length ? existing[index] : Object.Instantiate(template);
                    shelf.gameObject.name = "Estante " + (char)('A' + row) + (col + 1);
                    shelf.section = ((char)('A' + row)).ToString() + (col + 1);
                    shelf.transform.position = new Vector3(-12f + col * 2.6f, 5f - row * 4f, 0);
                    shelf.searched = false; shelf.bookIndex = -1;
                    shelf.GetComponent<SpriteRenderer>().sortingOrder = 100 - Mathf.RoundToInt((shelf.transform.position.y - .5f) * 20);
                    shelves[index] = shelf;
                }
            }
            manager.shelves = shelves; manager.roundSeconds = 300;
            var texts = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<TextMeshPro>()).ToArray();
            foreach (var text in texts)
            {
                // Los identificadores anteriores eran objetos sueltos; ahora siguen a cada estante.
                if (text.text.Length == 2 && text.text[0] >= 'A' && text.text[0] <= 'C' && char.IsDigit(text.text[1])) Object.DestroyImmediate(text.gameObject);
                else if (text.text.StartsWith("Sección "))
                {
                    int row = text.text.Contains("A") ? 0 : text.text.Contains("B") ? 1 : 2;
                    text.text = "SECCIÓN " + (char)('A' + row) + " · CONSULTA GENERAL";
                }
            }
            foreach (var shelf in shelves)
            {
                var go = new GameObject("Rótulo " + shelf.section);
                go.transform.SetParent(shelf.transform, false); go.transform.localPosition = new Vector3(0,-3.1f,-.2f);
                var label = go.AddComponent<TextMeshPro>(); label.font = Resources.Load<U2GameConfig>("U2GameConfig").font;
                label.text = shelf.section; label.fontSize = 4.6f; label.alignment = TextAlignmentOptions.Center;
                label.color = new Color(.15f,.2f,.22f); label.rectTransform.sizeDelta = new Vector2(3,.8f); label.renderer.sortingOrder = 400;
            }
            ConfigureExit(manager, scene);
            var studentsRoot = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "Alumnos Biblioteca");
            if (studentsRoot != null && studentsRoot.GetComponentsInChildren<LibraryStudent>().Length != 14)
            {
                Object.DestroyImmediate(studentsRoot);
                studentsRoot = null;
            }
            if (studentsRoot == null) AddStudents(manager, scene);
            EditorUtility.SetDirty(manager);
            EditorSceneManager.MarkSceneDirty(scene); AssetDatabase.SaveAssets(); EditorSceneManager.SaveScene(scene);
            File.WriteAllText(Marker, "LIBRARY_EXPANDED_V2: 18 estantes, 8 lectores, 6 alumnos que caminan y buscan; salida física; 300 segundos.\n");
            Debug.Log("LIBRARY_EXPANDED_V2: biblioteca ampliada y salida reparada.");
        }
        finally
        {
            if (previous.isLoaded) SceneManager.SetActiveScene(previous);
            if (close) EditorSceneManager.CloseScene(scene,true);
        }
    }
    static void ConfigureExit(LibraryLevelManager manager, Scene scene)
    {
        manager.exit.position = new Vector3(0,-7.3f,0);
        var zone = manager.exit.GetComponent<LibraryExitZone>();
        if (zone == null) zone = manager.exit.gameObject.AddComponent<LibraryExitZone>();
        var collider = manager.exit.GetComponent<BoxCollider2D>();
        if (collider == null) collider = manager.exit.gameObject.AddComponent<BoxCollider2D>();
        collider.isTrigger = true; collider.size = new Vector2(3.8f,1.8f);
        zone.manager = manager; zone.zone = collider; manager.exitZone = zone;
        var mat = scene.GetRootGameObjects().First(g => g.name == "Tapete de salida");
        mat.transform.position = new Vector3(0,-7.3f,0); mat.transform.localScale = new Vector3(3.8f,1.8f,1);
        mat.GetComponent<SpriteRenderer>().sortingOrder = -900;
        zone.floor = mat.GetComponent<SpriteRenderer>();
        var sign = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<TextMeshPro>()).First(t => t.text.StartsWith("SALIDA"));
        sign.transform.position = new Vector3(0,-7.1f,-.2f); sign.rectTransform.sizeDelta = new Vector2(4.8f,1.1f);
        sign.fontSize = 3.3f; sign.color = new Color(.07f,.14f,.12f); zone.sign = sign;
    }
    static void AddStudents(LibraryLevelManager manager, Scene scene)
    {
        var root = new GameObject("Alumnos Biblioteca");
        var config = Resources.Load<U2GameConfig>("U2GameConfig");
        var material = AssetDatabase.LoadAssetAtPath<Material>(Art + "Generated/BibliotecaUnlit.mat");
        string[] readers = { "Lucy", "Rob", "Molly", "Pier" };
        foreach (var reader in readers) Prepare(Art + reader + "_reading_16x16.png");
        for (int i = 0; i < 8; i++)
        {
            int table = i / 2; bool left = i % 2 == 0;
            float x = table / 2 == 0 ? 6.5f : 11f; float y = table % 2 == 0 ? 3.5f : -.5f;
            var go = new GameObject("Alumno leyendo sentado " + (i + 1)); go.transform.SetParent(root.transform);
            go.transform.position = new Vector3(x + (left ? -1.15f : 1.15f),y+.15f,0);
            go.transform.localScale = manager.player.transform.localScale;
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + readers[table] + "_reading_16x16.png");
            var frames = new Sprite[5];
            for (int frame = 0; frame < 5; frame++) frames[frame] = ReaderFrame(texture, readers[table], (left ? 12 : 6) + frame);
            var sprite = go.AddComponent<SpriteRenderer>(); sprite.sprite = frames[0]; sprite.sharedMaterial = material;
            go.AddComponent<LibraryDepthSort>().feetOffset = -.35f;
            var student = go.AddComponent<LibraryStudent>(); student.manager = manager; student.seated = true; student.readingFrames = frames;
        }
        for (int i = 0; i < 6; i++)
        {
            var go = new GameObject("Alumno buscando libros " + (i + 1)); go.transform.SetParent(root.transform);
            go.transform.localScale = manager.player.transform.localScale;
            float y = 5 - (i % 3) * 4;
            float lane = y - 2f;
            Vector2[] route = {
                new Vector2(3.6f,lane), new Vector2(1,lane), new Vector2(1,y-1.08f), new Vector2(1,lane),
                new Vector2(-6.8f,lane), new Vector2(-6.8f,y-1.08f), new Vector2(-6.8f,lane),
                new Vector2(-13.5f,lane), new Vector2(-13.5f,lane-1), new Vector2(3.6f,lane-1)
            };
            int start = i < 3 ? 0 : 4;
            go.transform.position = route[start];
            var sprite = go.AddComponent<SpriteRenderer>(); sprite.sprite = manager.player.GetComponentInChildren<SpriteRenderer>().sprite; sprite.sharedMaterial = material;
            var animator = go.AddComponent<Animator>(); animator.runtimeAnimatorController = config.studentControllers[i % config.studentControllers.Length];
            var body = go.AddComponent<Rigidbody2D>(); body.bodyType = RigidbodyType2D.Kinematic; body.gravityScale = 0;
            var footprint = go.AddComponent<CapsuleCollider2D>(); footprint.isTrigger = true; footprint.size = new Vector2(.5f,.35f); footprint.offset = new Vector2(0,-.45f);
            go.AddComponent<LibraryDepthSort>().feetOffset = -.4f;
            var student = go.AddComponent<LibraryStudent>(); student.manager = manager; student.route = route; student.startWaypoint = (start+1)%route.Length;
            student.searchStops = new[] { false,false,true,false,false,true,false,false,false,false };
            student.speed = 1.05f + i*.04f; student.searchSeconds = 3 + i%3;
        }
    }
    static void Prepare(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new System.InvalidOperationException("Falta " + path);
        importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 16; importer.filterMode = FilterMode.Point; importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
    }
    static Sprite ReaderFrame(Texture2D texture, string reader, int frame)
    {
        string path = Art + "Generated/" + reader + "Reading" + frame + ".asset";
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite != null) return sprite;
        sprite = Sprite.Create(texture, new Rect(frame*16,0,16,32), new Vector2(.5f,.5f),16,0,SpriteMeshType.FullRect);
        sprite.name = reader + "Reading" + frame; AssetDatabase.CreateAsset(sprite,path); return sprite;
    }
}
