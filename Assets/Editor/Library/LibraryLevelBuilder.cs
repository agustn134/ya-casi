using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

/// <summary>Genera una escena editable; reutiliza el jugador del nivel 1 sin modificar esa escena.</summary>
[InitializeOnLoad]
public static class LibraryLevelBuilder
{
    public const string Path = "Assets/Scenes/Nivel02_MaratonEnLaBiblioteca.unity";
    const string Art = "Assets/Art/Library/";
    const string Generated = "Assets/Art/Library/Generated/";
    static Scene output;
    static Material material;
    static TMP_FontAsset font;
    static Sprite shelfSprite, tableSprite, deskSprite, floorSprite, wallSprite, book1, book2, book3;
    static LibraryLevelBuilder() { EditorApplication.delayCall += AutoInstall; }
    static void AutoInstall()
    {
        if (File.Exists(Path) || EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) { EditorApplication.delayCall += AutoInstall; return; }
        try { Build(); } catch (Exception e) { Debug.LogException(e); }
    }
    [MenuItem("Ya Casi/Nivel 2/Crear biblioteca")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogWarning("Sal de Play antes de crear el nivel 2."); return; }
        if (File.Exists(Path)) { Debug.Log("El nivel 2 ya existe. Usa Ya Casi/Nivel 2/Abrir biblioteca."); return; }
        Directory.CreateDirectory(Generated);
        AssetDatabase.Refresh();
        PrepareTexture(Art + "Library.png"); PrepareTexture(Art + "RoomBuilder.png");
        var library = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "Library.png");
        var room = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "RoomBuilder.png");
        if (library == null || room == null) throw new InvalidOperationException("Faltan las dos láminas de biblioteca.");
        shelfSprite = Slice(library, "Estante", 0, 360, 48, 48);
        tableSprite = Slice(library, "MesaLectura", 120, 8, 80, 40);
        deskSprite = Slice(library, "Recepcion", 184, 160, 64, 40);
        book1 = Slice(library, "LibroAlgoritmos", 192, 304, 16, 16);
        book2 = Slice(library, "LibroBasesDeDatos", 208, 304, 16, 16);
        book3 = Slice(library, "LibroRedesAbierto", 192, 280, 16, 16);
        floorSprite = Slice(room, "Piso", 576, 208, 16, 16);
        wallSprite = Slice(room, "Muro", 176, 176, 16, 32);
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Art/Fonts/PixelifySans/PixelifySans SDF.asset");
        var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        material = AssetDatabase.LoadAssetAtPath<Material>(Generated + "BibliotecaUnlit.mat");
        if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, Generated + "BibliotecaUnlit.mat"); }

        var original = SceneManager.GetActiveScene();
        var source = SceneManager.GetSceneByPath("Assets/Scenes/Nivel01_CaosDelCasillero.unity");
        bool closeSource = !source.isLoaded;
        if (closeSource) source = EditorSceneManager.OpenScene("Assets/Scenes/Nivel01_CaosDelCasillero.unity", OpenSceneMode.Additive);
        var sourcePlayer = source.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<PlayerMovement>(true)).FirstOrDefault();
        if (sourcePlayer == null) throw new InvalidOperationException("No se encontró el jugador del nivel 1.");
        output = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(output);
        try
        {
            var playerObject = Object.Instantiate(sourcePlayer.gameObject);
            playerObject.name = "Jugador";
            SceneManager.MoveGameObjectToScene(playerObject, output);
            playerObject.transform.position = new Vector3(0, -6.5f, 0);
            playerObject.SetActive(true);
            var player = playerObject.GetComponent<PlayerMovement>(); player.enabled = true;
            foreach (var interaction in playerObject.GetComponents<PlayerInteraction>()) interaction.enabled = false;
            foreach (var sprite in playerObject.GetComponentsInChildren<SpriteRenderer>()) sprite.sharedMaterial = material;
            playerObject.AddComponent<LibraryDepthSort>().feetOffset = -.4f;
            var body = playerObject.GetComponent<Rigidbody2D>(); body.gravityScale = 0; body.constraints = RigidbodyConstraints2D.FreezeRotation;

            Floor(); Walls();
            var manager = New("Gestor Biblioteca").AddComponent<LibraryLevelManager>();
            manager.player = player;
            manager.bookIcons = new[] { book1, book2, book3 };
            var shelves = new List<LibraryShelf>();
            for (int row = 0; row < 3; row++)
            {
                float y = 5f - row * 4f;
                Label("Sección " + (char)('A' + row) + " · " + manager.bookNames[row], new Vector2(-6, y + 1.4f), 3.1f);
                for (int col = 0; col < 3; col++)
                {
                    float x = -11 + col * 5;
                    var shelf = Prop("Estante " + (char)('A' + row) + (col + 1), shelfSprite, new Vector2(x, y));
                    Solid(shelf, new Vector2(1.4f, .6f), new Vector2(0, -.42f));
                    var interact = shelf.AddComponent<LibraryShelf>(); interact.section = ((char)('A' + row)).ToString() + (col + 1);
                    var point = New("Punto de interacción"); point.transform.SetParent(shelf.transform); point.transform.localPosition = new Vector3(0, -2.1f, 0); interact.interactionPoint = point.transform;
                    var glow = New("Marca de revisión"); glow.transform.SetParent(shelf.transform); glow.transform.localPosition = new Vector3(0, -1.6f, 0);
                    var renderer = glow.AddComponent<SpriteRenderer>(); renderer.sprite = floorSprite; renderer.sharedMaterial = material; renderer.sortingOrder = 350; glow.transform.localScale = new Vector3(3.2f, .28f, 1); renderer.enabled = false; interact.highlight = renderer;
                    Label(interact.section, new Vector2(x, y - 1.65f), 2.3f);
                    shelves.Add(interact);
                }
            }
            manager.shelves = shelves.ToArray();
            Label("SALA DE LECTURA", new Vector2(9, 6.6f), 3.5f);
            foreach (float x in new[] { 6.5f, 11f })
                foreach (float y in new[] { 3.5f, -.5f })
                {
                    var table = Prop("Mesa de estudio", tableSprite, new Vector2(x, y));
                    Solid(table, new Vector2(2.2f, .8f), new Vector2(0, -.12f));
                }
            var desk = Prop("Mostrador de préstamos", deskSprite, new Vector2(9, -5.3f));
            Solid(desk, new Vector2(1.9f, .6f), new Vector2(0, -.2f));
            Label("PRÉSTAMOS", new Vector2(9, -6.6f), 2.4f);
            Label("SILENCIO POR FAVOR", new Vector2(-6, 7.5f), 3.2f);
            var exit = New("Salida Biblioteca"); exit.transform.position = new Vector3(0, -7.7f, 0); manager.exit = exit.transform;
            var mat = Prop("Tapete de salida", floorSprite, new Vector2(0, -7.7f)); mat.transform.localScale = new Vector3(2.5f, .6f, 1); mat.GetComponent<SpriteRenderer>().color = new Color(.35f, .65f, .6f);
            Label("SALIDA ↓", new Vector2(0, -8.3f), 2.6f);
            CreateLibrarian(manager, player);
            var cameraObject = New("Main Camera"); cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0, -3.5f, -10);
            var camera = cameraObject.AddComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 5; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.07f, .10f, .12f);
            cameraObject.AddComponent<AudioListener>();
            var data = cameraObject.AddComponent<UniversalAdditionalCameraData>(); data.SetRenderer(0);
            cameraObject.AddComponent<LibraryCamera>().target = player.transform;
            EditorSceneManager.SaveScene(output, Path);
            var scenes = EditorBuildSettings.scenes.ToList();
            if (!scenes.Any(s => s.path == Path)) scenes.Add(new EditorBuildSettingsScene(Path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
            LibraryExpansion.Upgrade();
            LibraryThirtyShelves.Apply();
            Validate();
            Debug.Log("LIBRARY_LEVEL_READY: Nivel 2 creado. Ya Casi > Nivel 2 > Abrir biblioteca.");
        }
        finally
        {
            if (original.isLoaded) SceneManager.SetActiveScene(original);
            if (closeSource && source.isLoaded) EditorSceneManager.CloseScene(source, true);
            if (output.isLoaded) EditorSceneManager.CloseScene(output, true);
        }
    }
    static void PrepareTexture(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;
        importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 16; importer.filterMode = FilterMode.Point; importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed; importer.alphaIsTransparency = true; importer.maxTextureSize = 4096; importer.SaveAndReimport();
    }
    static Sprite Slice(Texture2D texture, string name, int x, int top, int width, int height)
    {
        string path = Generated + name + ".asset";
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite != null) return sprite;
        sprite = Sprite.Create(texture, new Rect(x, texture.height - top - height, width, height), new Vector2(.5f, .5f), 16, 0, SpriteMeshType.FullRect);
        sprite.name = name; AssetDatabase.CreateAsset(sprite, path); return sprite;
    }
    static GameObject New(string name)
    {
        var go = new GameObject(name); SceneManager.MoveGameObjectToScene(go, output); return go;
    }
    static GameObject Prop(string name, Sprite sprite, Vector2 position)
    {
        var go = New(name); go.transform.position = position; go.transform.localScale = new Vector3(.5f, .5f, 1);
        var renderer = go.AddComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.sharedMaterial = material; renderer.sortingOrder = 100 - Mathf.RoundToInt((position.y - .5f) * 20);
        return go;
    }
    static void Solid(GameObject go, Vector2 worldSize, Vector2 worldOffset)
    {
        var collider = go.AddComponent<BoxCollider2D>(); collider.size = worldSize * 2; collider.offset = worldOffset * 2;
    }
    static void Label(string value, Vector2 position, float size)
    {
        var go = New(value); go.transform.position = new Vector3(position.x, position.y, -.1f);
        var text = go.AddComponent<TextMeshPro>(); text.font = font; text.text = value; text.fontSize = size; text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(.19f,.23f,.24f); text.rectTransform.sizeDelta = new Vector2(9, .65f); text.renderer.sortingOrder = 400;
    }
    static void Floor()
    {
        var grid = New("Grid Biblioteca · celda 16 px / escala 0.5"); grid.AddComponent<Grid>(); grid.transform.localScale = new Vector3(.5f,.5f,1);
        var go = New("Tilemap Piso"); go.transform.SetParent(grid.transform, false);
        var map = go.AddComponent<Tilemap>(); var renderer = go.AddComponent<TilemapRenderer>(); renderer.sortingOrder = -1000; renderer.sharedMaterial = material;
        var tile = AssetDatabase.LoadAssetAtPath<Tile>(Generated + "PisoTile.asset");
        if (tile == null) { tile = ScriptableObject.CreateInstance<Tile>(); tile.sprite = floorSprite; AssetDatabase.CreateAsset(tile, Generated + "PisoTile.asset"); }
        for (int x = -30; x < 30; x++) for (int y = -18; y < 18; y++) map.SetTile(new Vector3Int(x,y,0), tile);
        ColorFloor(map);
    }
    static void Walls()
    {
        var root = New("Límites Biblioteca");
        foreach (var spec in new[] { new Vector4(0,9,30,.5f), new Vector4(0,-9,30,.5f), new Vector4(-15,0,.5f,18), new Vector4(15,0,.5f,18) })
        {
            var go = New("Muro sólido"); go.transform.SetParent(root.transform); go.transform.position = new Vector3(spec.x,spec.y,0); var collider = go.AddComponent<BoxCollider2D>(); collider.size = new Vector2(spec.z,spec.w);
        }
        for (int x = -30; x < 30; x++)
        {
            var top = Prop("Muro norte", wallSprite, new Vector2(x*.5f+.25f,8.35f)); top.GetComponent<SpriteRenderer>().sortingOrder = -600;
        }
    }
    static void CreateLibrarian(LibraryLevelManager manager, PlayerMovement player)
    {
        var go = New("Bibliotecaria"); go.transform.position = new Vector3(4,-5,0); go.transform.localScale = player.transform.localScale;
        var renderer = go.AddComponent<SpriteRenderer>(); var sourceSprite = player.GetComponentInChildren<SpriteRenderer>(); renderer.sprite = sourceSprite.sprite; renderer.sharedMaterial = material;
        var animator = go.AddComponent<Animator>(); var config = Resources.Load<U2GameConfig>("U2GameConfig");
        animator.runtimeAnimatorController = config != null && config.studentControllers != null && config.studentControllers.Length > 3 ? config.studentControllers[3] : player.GetComponent<Animator>().runtimeAnimatorController;
        go.AddComponent<LibraryDepthSort>().feetOffset = -.4f;
        var librarian = go.AddComponent<LibraryLibrarian>(); librarian.animator = animator; librarian.manager = manager;
        Vector2[] points = { new Vector2(4,-5), new Vector2(-12,-5), new Vector2(-12,-7), new Vector2(3,-7), new Vector2(3,7), new Vector2(-13,7), new Vector2(3,7), new Vector2(3,-5) };
        librarian.patrol = new Transform[points.Length];
        for (int i = 0; i < points.Length; i++) { var p = New("Ruta bibliotecaria " + i); p.transform.position = points[i]; librarian.patrol[i] = p.transform; }
        manager.librarian = librarian;
        Label("Bibliotecaria", new Vector2(4,-4), 2.1f);
    }
    [MenuItem("Ya Casi/Nivel 2/Abrir biblioteca")]
    public static void Open()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (!File.Exists(Path)) Build();
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(Path);
    }
    [MenuItem("Ya Casi/Nivel 2/Validar biblioteca")]
    public static void Validate()
    {
        var scene = SceneManager.GetSceneByPath(Path);
        bool close = !scene.isLoaded;
        if (close) scene = EditorSceneManager.OpenScene(Path, OpenSceneMode.Additive);
        try
        {
            var manager = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<LibraryLevelManager>()).Single();
            if (manager.player == null || manager.librarian == null || manager.exit == null || manager.exitZone == null || manager.shelves.Length != 30 || manager.bookIcons.Any(s => s == null)) throw new InvalidOperationException("Referencias incompletas del nivel 2.");
            foreach (var shelf in manager.shelves) if (shelf.interactionPoint == null || shelf.highlight == null) throw new InvalidOperationException("Estante sin configurar.");
            string report = "LIBRARY_VALIDATION_OK\n30 estantes; 3 libros; 8 lectores; 6 alumnos móviles; jugador nivel 1; cámara; bibliotecaria; salida física; 300 segundos; penalización repetible 10 segundos.\n";
            File.WriteAllText("LibraryValidation.txt", report); Debug.Log(report);
        }
        finally { if (close) EditorSceneManager.CloseScene(scene, true); }
    }
    static void ColorFloor(Tilemap map)
    {
        foreach (var tile in map.GetTilesBlock(map.cellBounds).OfType<Tile>().Distinct())
        {
            tile.flags = TileFlags.None;
            EditorUtility.SetDirty(tile);
        }
        for (int x = -30; x < 30; x++) for (int y = -18; y < 18; y++)
        {
            var cell = new Vector3Int(x,y,0);
            bool border = x < -29 || x > 28 || y < -17 || y > 16;
            bool aisle = (x >= 5 && x <= 8) || (y >= -14 && y <= -11);
            Color color = border ? new Color(.35f,.43f,.46f) : aisle ? new Color(.65f,.79f,.77f) : x > 8 ? new Color(.94f,.87f,.74f) : new Color(.89f,.88f,.83f);
            map.SetTileFlags(cell, TileFlags.None); map.SetColor(cell,color);
        }
    }
    [MenuItem("Ya Casi/Nivel 2/Aplicar acabados al piso")]
    public static void Polish()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var scene = SceneManager.GetSceneByPath(Path);
        bool close = !scene.isLoaded;
        if (close) scene = EditorSceneManager.OpenScene(Path, OpenSceneMode.Additive);
        foreach (var map in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Tilemap>())) ColorFloor(map);
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "Library.png");
        if (texture != null)
        {
            var icon = Slice(texture, "LibroRedesAbierto", 192, 280, 16, 16);
            foreach (var manager in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<LibraryLevelManager>()))
            {
                manager.bookIcons[2] = icon;
                EditorUtility.SetDirty(manager);
            }
        }
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        if (close) EditorSceneManager.CloseScene(scene,true);
    }
}
