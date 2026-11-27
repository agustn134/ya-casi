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

/// <summary>Pasto libre, troncos sólidos y un vestíbulo del F más amplio y amueblado.</summary>
public static class LevelFourCampusUpgrade
{
    static Sprite pixel;
    static Material lit, unlit;
    static readonly Dictionary<Texture2D, Texture2D> readable = new Dictionary<Texture2D, Texture2D>();
    static readonly Dictionary<Sprite, Vector2> stats = new Dictionary<Sprite, Vector2>();

    [MenuItem("Ya Casi/Nivel 4/Actualizar pasto y nuevo diseño F")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Detén Play antes de actualizar.");
        if (!File.Exists("Level4Work/AntesDePastoYVestibulo.unity.bak")) File.Copy(LevelFourBuilder.ScenePath, "Level4Work/AntesDePastoYVestibulo.unity.bak");
        LevelFourDetailBuilder.Apply();
        var scene = SceneManager.GetSceneByPath(LevelFourBuilder.ScenePath);
        bool opened = scene.IsValid() && scene.isLoaded;
        if (!opened) scene = EditorSceneManager.OpenScene(LevelFourBuilder.ScenePath, OpenSceneMode.Additive);
        var previous = SceneManager.GetActiveScene(); SceneManager.SetActiveScene(scene);
        try
        {
            var mission = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<LevelFourMission>()).Single();
            pixel = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/LevelFour/Pixel.png");
            lit = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Lit-Default.mat");
            unlit = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat");
            BuildTrunks(mission);
            var detail = mission.GetComponentsInChildren<Transform>(true).First(t => t.name == "DISEÑO F · Referencia fotográfica");
            var old = detail.Find("F · Vestíbulo renovado"); if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
            var root = new GameObject("F · Vestíbulo renovado"); root.transform.SetParent(detail, false);
            var ground = detail.Find("F · Pasillo de aulas"); var upper = detail.Find("F · Cubículos de planta alta");
            Lobby(root.transform, ground, 106, 36, false); Lobby(root.transform, upper, 131, 31, true);
            ground.gameObject.SetActive(false); upper.gameObject.SetActive(false);
            mission.torresDoor.position = new Vector3(125.8f, 24, 0);
            foreach (var sensor in mission.GetComponentsInChildren<LevelFourSensor>(true))
            {
                sensor.size = new Vector2(12, .35f);
                sensor.transform.localScale = new Vector3(12, sensor.transform.localScale.y, 1);
            }
            foreach (var device in mission.GetComponentsInChildren<Transform>(true).Where(t => t.name == "Dispositivo del sensor"))
                device.position = new Vector3(device.position.x < 120 ? 111.9f : 136.9f, device.position.y, 0);
            var office = detail.Find("Cubículo de Torres · detallado");
            mission.officeExit.position = new Vector3(153, 1.05f, 0);
            mission.officeSpawn.position = new Vector3(153, 2.35f, 0);
            foreach (int side in new[] { -1, 1 })
            {
                Quad("Muro frontal junto a la puerta", office, new Vector2(153 + side * 2.1f, .35f), new Vector2(2, .35f), new Color32(211, 196, 146, 255), 2000);
                Quad("Jamba de entrada al cubículo", office, new Vector2(153 + side * 1.05f, .65f), new Vector2(.15f, .8f), new Color32(45, 55, 59, 255), 2050);
            }
            Quad("Umbral de salida al pasillo", office, new Vector2(153, .48f), new Vector2(2, .18f), new Color32(181, 186, 173, 255), 2050);
            Quad("Puerta abierta hacia el interior", office, new Vector2(154.05f, 1.35f), new Vector2(.18f, 1.4f), new Color32(150, 105, 60, 255), 2051);
            Quad("Canto iluminado de puerta", office, new Vector2(153.96f, 1.35f), new Vector2(.04f, 1.35f), new Color32(204, 161, 99, 255), 2052);
            Quad("Tapete frente a la salida", office, new Vector2(153, 1.1f), new Vector2(1.6f, .65f), new Color32(40, 76, 81, 255), -18);
            Label("SALIDA AL PASILLO", office, new Vector2(153, .18f), .17f);
            Label("v", office, new Vector2(153, 1.1f), .32f);
            Quad("Alfombra azul de cubículo", office, new Vector2(153, 5.5f), new Vector2(4.1f, 5), new Color32(43, 69, 86, 255), -23);
            for (int side = -1; side <= 1; side += 2)
                Quad("Filete de alfombra", office, new Vector2(153 + side * 1.95f, 5.5f), new Vector2(.06f, 4.8f), new Color32(156, 139, 91, 255), -22);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            File.AppendAllText("Level4Work/PastoYVestibulo.txt", "\nVestíbulo amplio, revestimientos, ventanas, asientos, casilleros y señales renovados.\n");
        }
        finally
        {
            foreach (var pair in readable) if (pair.Value != pair.Key) UnityEngine.Object.DestroyImmediate(pair.Value);
            readable.Clear(); stats.Clear();
            SceneManager.SetActiveScene(previous); if (!opened) EditorSceneManager.CloseScene(scene, true);
        }
        AssetDatabase.SaveAssets();
    }

    static void BuildTrunks(LevelFourMission mission)
    {
        var old = mission.transform.Find("Troncos sólidos · pasto transitable"); if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
        var root = new GameObject("Troncos sólidos · pasto transitable"); root.transform.SetParent(mission.transform); root.transform.position = Vector3.zero;
        var map = mission.gardens;
        var remaining = new HashSet<Vector3Int>();
        foreach (var cell in map.cellBounds.allPositionsWithin) if (map.HasTile(cell)) remaining.Add(cell);
        var rectangles = new List<Rect>(); var report = new System.Text.StringBuilder();
        var templates = new List<List<KeyValuePair<Vector3Int, Sprite>>>();
        Vector3Int[] dirs = { Vector3Int.left, Vector3Int.right, Vector3Int.up, Vector3Int.down };
        while (remaining.Count > 0)
        {
            var first = remaining.First(); remaining.Remove(first);
            var queue = new Queue<Vector3Int>(); var cells = new List<Vector3Int>(); queue.Enqueue(first);
            var bounds = new Bounds(map.GetCellCenterWorld(first), Vector3.zero);
            bool namedTree = false; float alpha = 0, brown = 0;
            while (queue.Count > 0)
            {
                var cell = queue.Dequeue(); cells.Add(cell);
                bounds.Encapsulate(map.CellToWorld(cell)); bounds.Encapsulate(map.CellToWorld(cell + Vector3Int.one));
                var sprite = map.GetSprite(cell);
                string name = map.GetTile(cell).name.ToLowerInvariant();
                namedTree |= name.Contains("tree") || name.Contains("stump") || name.Contains("trunk") || name.Contains("arbol");
                if (sprite != null) { var value = PixelStats(sprite); alpha += value.x; brown += value.y; }
                foreach (var d in dirs) if (remaining.Remove(cell + d)) queue.Enqueue(cell + d);
            }
            float width = bounds.size.x, height = bounds.size.y;
            alpha /= cells.Count; brown /= cells.Count;
            bool tree = width >= 1.5f && width <= 8 && height >= 3 && height <= 8 && height >= width * .45f && (namedTree || alpha < .94f && alpha > .08f && brown > .006f);
            report.AppendLine($"Grupo {bounds.center}: {width:F2}x{height:F2}, alpha={alpha:F2}, marrón={brown:F2}, árbol={tree}");
            if (!tree) continue;
            var anchor = cells.OrderBy(c => c.y).ThenBy(c => c.x).First();
            templates.Add(cells.OrderBy(c => c.y).ThenBy(c => c.x).Select(c => new KeyValuePair<Vector3Int, Sprite>(c - anchor, map.GetSprite(c))).ToList());
            var trunk = new Rect(bounds.center.x - .24f, bounds.min.y + .12f, .48f, .44f);
            rectangles.Add(trunk);
        }
        // Reconoce también árboles iguales que se tocan con otros objetos del jardín.
        foreach (var template in templates)
        foreach (var cell in map.cellBounds.allPositionsWithin)
        {
            if (map.GetSprite(cell) != template[0].Value || !template.All(p => map.GetSprite(cell + p.Key) == p.Value)) continue;
            var bounds = new Bounds(map.GetCellCenterWorld(cell), Vector3.zero);
            foreach (var p in template) { bounds.Encapsulate(map.CellToWorld(cell + p.Key)); bounds.Encapsulate(map.CellToWorld(cell + p.Key + Vector3Int.one)); }
            var trunk = new Rect(bounds.center.x - .24f, bounds.min.y + .12f, .48f, .44f);
            if (!rectangles.Any(r => Vector2.Distance(r.center, trunk.center) < .6f)) rectangles.Add(trunk);
        }
        foreach (var trunk in rectangles)
        {
            var go = new GameObject("Tronco sólido"); go.transform.SetParent(root.transform);
            go.transform.position = trunk.center; var collider = go.AddComponent<BoxCollider2D>(); collider.size = trunk.size;
        }
        mission.trunkAreas = rectangles.ToArray();
        File.WriteAllText("Level4Work/PastoYVestibulo.txt", "Pasto y jardines transitables. Troncos detectados: " + rectangles.Count + "\n" + report);
    }

    static Vector2 PixelStats(Sprite sprite)
    {
        if (stats.TryGetValue(sprite, out var value)) return value;
        Texture2D source = sprite.texture;
        if (!readable.TryGetValue(source, out var texture))
        {
            if (source.isReadable) texture = source;
            else
            {
                var rt = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32);
                var active = RenderTexture.active; Graphics.Blit(source, rt); RenderTexture.active = rt;
                texture = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
                texture.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0); texture.Apply(); RenderTexture.active = active; RenderTexture.ReleaseTemporary(rt);
            }
            readable[source] = texture;
        }
        int total = 0, opaque = 0, brown = 0;
        for (int y = 0; y < sprite.rect.height; y += 2) for (int x = 0; x < sprite.rect.width; x += 2)
        {
            var c = texture.GetPixel((int)sprite.rect.x + x, (int)sprite.rect.y + y); total++;
            if (c.a < .3f) continue; opaque++;
            if (c.r > .12f && c.r > c.g * 1.06f && c.g > c.b * 1.08f) brown++;
        }
        value = new Vector2((float)opaque / total, (float)brown / total); stats[sprite] = value; return value;
    }

    static void Lobby(Transform parent, Transform source, float x, int length, bool upper)
    {
        var root = new GameObject(upper ? "Galería superior · cubículos" : "Vestíbulo y galería del F"); root.transform.SetParent(parent, false); var p = root.transform;
        var originalFloor = source.GetComponentsInChildren<SpriteRenderer>().First(r => r.name == "Piso blanco con juntas finas");
        var floor = Quad("Piso cerámico del vestíbulo", p, new Vector2(x, length * .5f), Vector2.one, new Color32(229, 225, 207, 255), -25, originalFloor.sprite);
        floor.transform.localScale = Vector3.one; floor.drawMode = SpriteDrawMode.Tiled; floor.size = new Vector2(13, length + 1);
        Quad("Franja central de cerámica azul", p, new Vector2(x, length * .5f), new Vector2(2.8f, length + 1), new Color32(120, 142, 152, 255), -24, originalFloor.sprite, true);
        foreach (int side in new[] { -1, 1 })
        {
            Quad("Filete del pavimento", p, new Vector2(x + side * 5.6f, length * .5f), new Vector2(.18f, length + 1), new Color32(69, 89, 96, 255), -23);
            Quad("Revestimiento interior marfil", p, new Vector2(x + side * 6.15f, length * .5f), new Vector2(.9f, length + 1), new Color32(208, 196, 156, 255), 2000);
            Quad("Zoclo oscuro", p, new Vector2(x + side * 5.67f, length * .5f), new Vector2(.10f, length + 1), new Color32(41, 57, 65, 255), 2001);
            for (int y = 5; y < length; y += 8)
            {
                var at = new Vector2(x + side * 6.15f, y);
                Quad("Ventanal con marco de acero", p, at, new Vector2(.78f, 3.1f), new Color32(35, 49, 57, 255), 2020);
                Quad("Vidrio nocturno", p, at, new Vector2(.52f, 2.85f), new Color32(60, 95, 114, 255), 2021);
                Quad("Reflejo del cristal", p, at + new Vector2(-side * .12f, 0), new Vector2(.06f, 2.65f), new Color32(128, 159, 170, 255), 2022);
                for (int j = -1; j <= 1; j++) Quad("División de ventana", p, at + Vector2.up * j * .85f, new Vector2(.75f, .07f), new Color32(38, 49, 55, 255), 2023);
                Quad("Pilar rojo del F", p, at + Vector2.up * 2.5f, new Vector2(1.05f, .7f), new Color32(165, 66, 48, 255), 2030);
                Quad("Canto del pilar", p, at + new Vector2(-side * .36f, 2.5f), new Vector2(.16f, .7f), new Color32(205, 94, 62, 255), 2031);
                Quad("Aplique de pared", p, at + new Vector2(-side * .5f, -1), new Vector2(.14f, .55f), new Color32(244, 222, 160, 255), 2032, null, false, true);
            }
        }
        foreach (var render in source.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (!(render.name.Contains("Maceta") || render.name.Contains("Papelera"))) continue;
            var copy = UnityEngine.Object.Instantiate(render.gameObject, p); copy.SetActive(true);
            Vector3 position = render.transform.position;
            float sign = position.x < x ? -1 : 1; position.x += sign * 2.9f;
            copy.transform.position = position; copy.name = render.name + " · galería";
        }
        for (int side = -1; side <= 1; side += 2)
        {
            var at = new Vector2(x + side * 4.7f, 8.3f);
            Quad("Panel de señalización F", p, at, new Vector2(1.8f, 1.8f), new Color32(28, 55, 70, 255), 2200);
            Label(upper ? "2" : "F", p, at + Vector2.up * .12f, .85f);
            Label(upper ? "CUBÍCULOS" : "EDIFICIO F", p, at + Vector2.down * .58f, .16f);
            if (!upper) for (int y = 5; y < length - 2; y += 8) Classroom(p, x, y, side, originalFloor.sprite);
            else
            {
                var atRoom = new Vector2(x + side * 8.5f, 24);
                Quad("Piso del área interior de cubículos", p, atRoom, new Vector2(4.8f, 8), new Color32(208, 208, 191, 255), -25, originalFloor.sprite, true);
                RoomWalls(p, atRoom, new Vector2(4.8f, 8));
                CorridorCubicle(p, atRoom + Vector2.up * 2, side, true, side < 0 ? 0 : 8);
                CorridorCubicle(p, atRoom - Vector2.up * 2, side, true, side < 0 ? 16 : 24);
                Entrance(p, new Vector2(x + side * 6.15f, 24), side, side < 0 ? "CUBÍCULOS · TORRES" : "CUBÍCULOS · DOCENTES");
            }
        }
        Quad("Dintel de acceso", p, new Vector2(x, -.25f), new Vector2(13.2f, .4f), new Color32(35, 52, 63, 255), 2002);
        Label(upper ? "GALERÍA DE CUBÍCULOS" : "F · ACCESO PRINCIPAL", p, new Vector2(x, .3f), .24f);
        Label(upper ? "< TORRES                       OTROS CUBÍCULOS >" : "BAÑOS <                           AULAS >", p, new Vector2(x, 24.8f), .23f);
        Label("ESCALERAS AL FONDO", p, new Vector2(x, length - 1.8f), .23f);
        if (upper)
        {
            Quad("Directorio de cubículos", p, new Vector2(131, 26.3f), new Vector2(6.5f, 1.25f), new Color32(25, 53, 66, 255), 2200);
            Label("CUBÍCULOS · PLANTA ALTA", p, new Vector2(131, 26.55f), .24f);
            Label("< PROF. TORRES     |     DOCENTES >", p, new Vector2(131, 26.05f), .18f);
        }
    }

    static void RoomWalls(Transform p, Vector2 at, Vector2 size)
    {
        foreach (int side in new[] { -1, 1 })
        {
            Quad("Muro del aula u oficina", p, at + Vector2.up * side * size.y * .5f, new Vector2(size.x + .3f, .3f), new Color32(205, 192, 150, 255), 2000);
            Quad("Muro lateral del recinto", p, at + Vector2.right * side * size.x * .5f, new Vector2(.22f, size.y), new Color32(205, 192, 150, 255), 2000);
        }
    }

    static void Entrance(Transform p, Vector2 at, int side, string title)
    {
        Quad("Marco de entrada", p, at, new Vector2(.9f, 2.25f), new Color32(28, 38, 42, 255), 2050);
        Quad("Puerta de madera", p, at, new Vector2(.63f, 1.97f), new Color32(144, 104, 61, 255), 2051);
        Quad("Ventana de puerta", p, at + Vector2.up * .45f, new Vector2(.43f, .65f), new Color32(65, 101, 114, 255), 2052);
        Quad("Manija de entrada", p, at + new Vector2(-side * .22f, -.35f), new Vector2(.07f, .24f), new Color32(222, 215, 181, 255), 2053);
        Quad("Placa de entrada", p, at + new Vector2(-side * 1.1f, 1.6f), new Vector2(2.8f, .7f), new Color32(30, 59, 70, 255), 2200);
        Label(title, p, at + new Vector2(-side * 1.1f, 1.6f), .14f);
    }

    static void Classroom(Transform p, float hallX, float y, int side, Sprite floorSprite)
    {
        var at = new Vector2(hallX + side * 8.5f, y);
        Quad("Piso del salón", p, at, new Vector2(4.8f, 6.3f), new Color32(218, 218, 202, 255), -25, floorSprite, true);
        RoomWalls(p, at, new Vector2(4.8f, 6.3f));
        Entrance(p, new Vector2(hallX + side * 6.15f, y), side, "AULA TI-" + (12 + (int)y / 8 + (side > 0 ? 4 : 0)));
        Quad("Marco de pizarrón", p, at + Vector2.up * 2.6f, new Vector2(3.7f, .6f), new Color32(184, 188, 172, 255), 2310);
        Quad("Pizarrón del aula", p, at + Vector2.up * 2.6f, new Vector2(3.5f, .42f), new Color32(42, 78, 66, 255), 2311);
        var desk = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/LevelFour/FInterior/Classroom_and_Library_Singles_Shadowless_25.png");
        var chair = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/LevelFour/FInterior/Conference_Hall_Singles_Shadowless_37.png");
        for (int row = 0; row < 3; row++) for (int col = 0; col < 2; col++)
        {
            var pos = at + new Vector2(-1.05f + col * 2.1f, 1.35f - row * 1.55f);
            Quad("Pupitre dentro del aula", p, pos, new Vector2(1.45f, .8f), Color.white, 2350, desk);
            Quad("Silla del alumno", p, pos + Vector2.down * .58f, new Vector2(.55f, .7f), new Color(.6f, .64f, .68f), 2351, chair);
        }
    }

    static void CorridorCubicle(Transform parent, Vector2 at, int side, bool upper, int number)
    {
        var root = new GameObject("Cubículo visible · " + (side < 0 ? "izquierda " : "derecha ") + number);
        root.transform.SetParent(parent, false); var p = root.transform;
        Quad("Sombra del módulo", p, at + Vector2.down * .15f, new Vector2(2.65f, 3.55f), new Color32(25, 33, 33, 100), -20);
        Quad("Alfombra de oficina", p, at, new Vector2(2.5f, 3.35f), new Color32(51, 72, 78, 255), -19);
        foreach (int edge in new[] { -1, 1 })
        {
            Quad("Marco horizontal de mampara", p, at + Vector2.up * edge * 1.7f, new Vector2(2.65f, .17f), new Color32(190, 195, 179, 255), 2410);
            Quad("División verde de cubículo", p, at + Vector2.up * edge * 1.7f, new Vector2(2.45f, .09f), new Color32(143, 161, 70, 255), 2411);
        }
        Quad("Mampara exterior", p, at + Vector2.right * side * 1.25f, new Vector2(.18f, 3.4f), new Color32(181, 190, 169, 255), 2410);
        Quad("Panel verde exterior", p, at + Vector2.right * side * 1.25f, new Vector2(.10f, 3.2f), new Color32(143, 161, 70, 255), 2411);
        Quad("Canto del escritorio de roble", p, at + Vector2.up * .52f, new Vector2(2.25f, .22f), new Color32(115, 80, 43, 255), 2420);
        Quad("Tablero de escritorio", p, at + Vector2.up * .95f, new Vector2(2.25f, .82f), new Color32(193, 152, 96, 255), 2421);
        Quad("Retorno en L", p, at + new Vector2(side * .85f, .23f), new Vector2(.55f, 1.45f), new Color32(193, 152, 96, 255), 2421);
        for (int j = 0; j < 3; j++)
        {
            Quad("Cajón del escritorio", p, at + new Vector2(side * .85f, -.05f - j * .19f), new Vector2(.48f, .16f), new Color32(166, 127, 75, 255), 2422);
            Quad("Tirador del cajón", p, at + new Vector2(side * .85f, -.02f - j * .19f), new Vector2(.18f, .025f), new Color32(217, 216, 196, 255), 2423);
        }
        Quad("Pantalla de computadora", p, at + new Vector2(-side * .25f, 1.04f), new Vector2(.68f, .45f), new Color32(29, 37, 45, 255), 2440);
        Quad("Monitor apagado con reflejo", p, at + new Vector2(-side * .25f, 1.07f), new Vector2(.55f, .32f), new Color32(66, 98, 110, 255), 2441);
        Quad("Base y teclado de laptop", p, at + new Vector2(-side * .25f, .70f), new Vector2(.76f, .22f), new Color32(106, 116, 120, 255), 2440);
        for (int j = 0; j < 5; j++) Quad("Teclas", p, at + new Vector2(-side * .25f - .25f + j * .12f, .73f), new Vector2(.07f, .04f), new Color32(38, 46, 50, 255), 2441);
        var chair = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/LevelFour/FInterior/Conference_Hall_Singles_Shadowless_37.png");
        Quad("Silla del cubículo", p, at + new Vector2(-side * .2f, -.5f), new Vector2(.7f, .95f), new Color(.6f, .63f, .65f), 2450, chair);
        var shelf = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/LevelFour/FInterior/Classroom_and_Library_Singles_Shadowless_43.png");
        Quad("Librero con libros", p, at + new Vector2(side * .85f, -1.05f), new Vector2(.6f, .85f), Color.white, 2450, shelf);
        for (int j = 0; j < 3; j++) Quad("Hojas sobre el escritorio", p, at + new Vector2(side * .55f + j * .04f, 1.02f + j * .04f), new Vector2(.3f, .38f), new Color32(234, 230, 202, 255), 2443 + j);
        Label(upper ? "CUBÍCULO " + (number / 8 + 1) : "ÁREA DOCENTE", p, at + Vector2.down * 1.48f, .11f);
        var body = new GameObject("Mobiliario sólido del módulo"); body.transform.SetParent(p); body.transform.position = at + Vector2.up * .25f;
        body.AddComponent<BoxCollider2D>().size = new Vector2(2.5f, 2.7f);
    }

    static SpriteRenderer Quad(string name, Transform parent, Vector2 pos, Vector2 size, Color color, int order, Sprite sprite = null, bool tiled = false, bool emissive = false)
    {
        sprite = sprite != null ? sprite : pixel; var go = new GameObject(name); go.transform.SetParent(parent); go.transform.position = pos;
        var renderer = go.AddComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.color = color; renderer.sortingOrder = order; renderer.sharedMaterial = emissive ? unlit : lit;
        if (tiled) { renderer.drawMode = SpriteDrawMode.Tiled; renderer.size = size; }
        else go.transform.localScale = new Vector3(size.x / sprite.bounds.size.x, size.y / sprite.bounds.size.y, 1);
        return renderer;
    }
    static void Label(string text, Transform parent, Vector2 pos, float scale)
    {
        var go = new GameObject(text); go.transform.SetParent(parent); go.transform.position = pos; go.transform.localScale = Vector3.one * scale;
        var label = go.AddComponent<TextMeshPro>(); label.text = text; label.fontSize = 18; label.rectTransform.sizeDelta = new Vector2(35, 4); label.alignment = TextAlignmentOptions.Center; label.color = new Color(1, .92f, .72f); label.GetComponent<MeshRenderer>().sortingOrder = 3100;
    }
}


