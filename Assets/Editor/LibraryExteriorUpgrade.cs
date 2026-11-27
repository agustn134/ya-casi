using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class LibraryExteriorUpgrade
{
    const string Library = "Assets/Scenes/Nivel02_MaratonEnLaBiblioteca.unity";
    const string Route = "Assets/Scenes/Nivel03_LaRutaDeEntrega.unity";
    const string RootName = "Exterior completo / Pasto y calle del nivel 3";
    sealed class Sample { public Sprite sprite; public int count; public Color average; public float coverage; }

    [InitializeOnLoadMethod]
    static void CompleteRequestedExterior()
    {
        EditorApplication.delayCall += () =>
        {
            const string request = "LibraryRedesignBackups/ExteriorPending.txt";
            if (!File.Exists(request) || EditorApplication.isPlayingOrWillChangePlaymode) return;
            try { Apply(); }
            catch (Exception error)
            {
                File.WriteAllText("LibraryRedesignBackups/ExteriorResult.txt", "No se pudo guardar: " + error);
                Debug.LogException(error);
            }
            finally { File.Delete(request); }
        };
    }

    [MenuItem("Ya Casi/Nivel 2/Completar pasto y calle exterior")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying) { Debug.LogWarning("Sal de Play para completar el exterior."); return; }
        var library = SceneManager.GetSceneByPath(Library);
        if (!library.isLoaded) library = EditorSceneManager.OpenScene(Library);
        string backup = "LibraryRedesignBackups/Exterior-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
        Directory.CreateDirectory(backup);
        File.Copy(Library, backup + "/Biblioteca.unity");
        var route = SceneManager.GetSceneByPath(Route);
        bool opened = !route.isLoaded;
        if (opened) route = EditorSceneManager.OpenScene(Route, OpenSceneMode.Additive);
        var readable = new Dictionary<Texture2D, Texture2D>();
        try
        {
            var samples = new Dictionary<Sprite, Sample>();
            foreach (var map in route.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Tilemap>())
                .Where(m => m.name == "Suelo_Tilemap" || m.name == "Camino_Tilemap"))
            {
                var counts = new Dictionary<TileBase, int>();
                foreach (var tile in map.GetTilesBlock(map.cellBounds))
                    if (tile != null) counts[tile] = counts.TryGetValue(tile, out int n) ? n + 1 : 1;
                foreach (var item in counts.OrderByDescending(k => k.Value).Take(80))
                {
                    var tile = item.Key as Tile;
                    if (tile == null || tile.sprite == null) continue;
                    Sprite sprite = tile.sprite;
                    if (samples.TryGetValue(sprite, out var previous)) { previous.count += item.Value; continue; }
                    Texture2D source = sprite.texture;
                    if (!readable.TryGetValue(source, out var copy))
                    {
                        var previousRT = RenderTexture.active;
                        var rt = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                        Graphics.Blit(source, rt); RenderTexture.active = rt;
                        copy = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
                        copy.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0); copy.Apply();
                        RenderTexture.active = previousRT; RenderTexture.ReleaseTemporary(rt);
                        readable[source] = copy;
                    }
                    Rect r = sprite.rect;
                    Color total = Color.clear; int opaque = 0, pixels = 0;
                    for (int y = (int)r.y; y < r.yMax; y++) for (int x = (int)r.x; x < r.xMax; x++)
                    {
                        Color c = copy.GetPixel(x, y); pixels++;
                        if (c.a > .95f) { total += c; opaque++; }
                    }
                    if (opaque > 0) samples[sprite] = new Sample { sprite = sprite, count = item.Value, average = total / opaque, coverage = opaque / (float)pixels };
                }
            }
            var solid = samples.Values.Where(s => s.coverage > .98f).ToArray();
            var grass = solid.Where(s => s.average.g > s.average.r * 1.15f && s.average.g > s.average.b * 1.1f)
                .OrderByDescending(s => s.count).First();
            var asphalt = solid.Where(s => { Color.RGBToHSV(s.average, out float h, out float sat, out float v); return sat < .22f && v < .55f && v > .12f; })
                .OrderByDescending(s => s.count).First();
            var sidewalk = solid.Where(s => { Color.RGBToHSV(s.average, out float h, out float sat, out float v); return sat < .23f && v > .6f; })
                .OrderByDescending(s => s.count).FirstOrDefault();
            SceneManager.SetActiveScene(library);
            var oldRoot = library.GetRootGameObjects().FirstOrDefault(g => g.name == RootName);
            if (oldRoot != null) UnityEngine.Object.DestroyImmediate(oldRoot);
            var oldExterior = library.GetRootGameObjects().FirstOrDefault(g => g.name == "Exterior de la biblioteca (Ruta de Entrega)");
            if (oldExterior != null) oldExterior.SetActive(false);
            var root = new GameObject(RootName);
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Library/Generated/BibliotecaUnlit.mat");
            var pixel = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Library/Generated/InteriorColorPlano.asset");
            void Surface(string name, Sprite sprite, Vector2 center, Vector2 size, int order, Color tint)
            {
                // Un renderer por bloque evita superar el límite de malla del tile repetido.
                const float maxWorldChunk = 16f;
                int columns = sprite == pixel ? 1 : Mathf.CeilToInt(size.x / maxWorldChunk);
                int rows = sprite == pixel ? 1 : Mathf.CeilToInt(size.y / maxWorldChunk);
                for (int row = 0; row < rows; row++) for (int column = 0; column < columns; column++)
                {
                    float width = sprite == pixel ? size.x : Mathf.Min(maxWorldChunk, size.x - column * maxWorldChunk);
                    float height = sprite == pixel ? size.y : Mathf.Min(maxWorldChunk, size.y - row * maxWorldChunk);
                    Vector2 location = center - size * .5f + new Vector2(column * maxWorldChunk + width * .5f, row * maxWorldChunk + height * .5f);
                    var go = new GameObject(name + " / " + column + "-" + row);
                    go.transform.SetParent(root.transform, false); go.transform.position = location;
                    var renderer = go.AddComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.sharedMaterial = material;
                    renderer.sortingOrder = order; renderer.color = tint;
                    if (sprite == pixel) go.transform.localScale = new Vector3(width, height, 1);
                    else { go.transform.localScale = new Vector3(.5f, .5f, 1); renderer.drawMode = SpriteDrawMode.Tiled; renderer.size = new Vector2(width, height) * 2; }
                }
            }
            // Base extensa bajo el edificio y todo el campo de visión del jugador.
            Surface("Césped continuo", grass.sprite, Vector2.zero, new Vector2(88, 60), -4000, Color.white);
            // Vialidad perimetral y banquetas: el mismo arte que usa Ruta de Entrega.
            foreach (float x in new[] { -18.7f, 18.7f })
            {
                Surface("Calle lateral", asphalt.sprite, new Vector2(x, 0), new Vector2(5.4f, 36), -3900, Color.white);
                Surface("Banqueta lateral", sidewalk != null ? sidewalk.sprite : pixel, new Vector2(Mathf.Sign(x) * 15.55f, 0), new Vector2(1.5f, 20), -3800, sidewalk != null ? Color.white : new Color(.65f,.66f,.61f));
                for (float y = -16; y <= 16; y += 3)
                    Surface("Línea discontinua lateral", pixel, new Vector2(x, y), new Vector2(.12f, 1.25f), -3700, new Color(1,.78f,.22f));
            }
            foreach (float y in new[] { -12.5f, 12.5f })
            {
                Surface("Calle frente y fondo", asphalt.sprite, new Vector2(0, y), new Vector2(42.8f, 5.4f), -3890, Color.white);
                Surface("Banqueta frente y fondo", sidewalk != null ? sidewalk.sprite : pixel, new Vector2(0, Mathf.Sign(y) * 9.45f), new Vector2(32.6f, 1.2f), -3790, sidewalk != null ? Color.white : new Color(.65f,.66f,.61f));
                for (float x = -20; x <= 20; x += 3)
                    Surface("Línea discontinua horizontal", pixel, new Vector2(x, y), new Vector2(1.25f, .12f), -3690, new Color(1,.78f,.22f));
            }
            for (float y = -14.7f; y < -10.2f; y += .7f)
                Surface("Paso peatonal frente a biblioteca", pixel, new Vector2(0,y), new Vector2(3.6f,.32f), -3600, new Color(.9f,.9f,.82f));
            // Las capas nuevas permanecen por debajo del interior y del acceso existente.
            EditorSceneManager.MarkSceneDirty(library); EditorSceneManager.SaveScene(library);
            File.WriteAllText("LibraryRedesignBackups/ExteriorResult.txt", "PASS: exterior guardado.\nGrass: " + grass.sprite.name + "\nAsphalt: " + asphalt.sprite.name + "\nSidewalk: " + sidewalk?.sprite.name + "\nNo se añadieron colliders al acceso.\n");
            Debug.Log("LIBRARY_EXTERIOR_READY: césped, calles y banquetas con sprites del nivel 3.");
        }
        finally
        {
            foreach (var copy in readable.Values) UnityEngine.Object.DestroyImmediate(copy);
            if (opened && route.isLoaded) EditorSceneManager.CloseScene(route, true);
            SceneManager.SetActiveScene(library);
        }
    }
}
