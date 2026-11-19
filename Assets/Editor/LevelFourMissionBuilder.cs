using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class LevelFourMissionBuilder
{
    const string Art = "Assets/Art/LevelFour";
    static Sprite pixel;
    static Material lit, unlit;

    public static void Polish()
    {
        var scene = SceneManager.GetSceneByPath(LevelFourBuilder.ScenePath);
        if (!scene.IsValid() || !scene.isLoaded || scene.isDirty) throw new InvalidOperationException("Abre y guarda la escena del nivel 4 antes de ajustar los letreros.");
        foreach (var label in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<TMP_Text>(true)))
            label.text = label.text.Replace("◀", "<").Replace("▶", ">").Replace("↑", "^");
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
    }

    [MenuItem("Ya Casi/Nivel 4/Construir misión del edificio F")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Detén Play para construir la misión.");
        var scene = SceneManager.GetSceneByPath(LevelFourBuilder.ScenePath);
        bool open = scene.IsValid() && scene.isLoaded;
        if (open)
        {
            var partial = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<LevelFourMission>(true)).FirstOrDefault();
            if (partial != null && partial.groundSpawn == null)
            {
                string preserved = AssetDatabase.GenerateUniqueAssetPath("Assets/Scenes/Nivel04_RespaldoConstruccion.unity");
                EditorSceneManager.SaveScene(scene, preserved, true);
                scene = EditorSceneManager.OpenScene(LevelFourBuilder.ScenePath);
            }
            else if (scene.isDirty) throw new InvalidOperationException("Hay cambios sin guardar en la escena; se conservan antes de construir.");
        }
        if (!File.Exists("Level4Work/AntesDeMisionF.unity.bak")) File.Copy(LevelFourBuilder.ScenePath, "Level4Work/AntesDeMisionF.unity.bak");
        Directory.CreateDirectory(Art + "/Security");
        string source = "C:/Users/elmane/Desktop/assets/moderninteriors-win/2_Characters/Old/Single_Characters_Legacy/16x16/";
        foreach (string action in new[] { "idle_anim", "run" })
        {
            string path = Art + "/Security/Guard_" + action + ".png";
            if (!File.Exists(path)) File.Copy(source + "Bouncer_" + action + "_16x16.png", path);
            AssetDatabase.ImportAsset(path); var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default; importer.isReadable = true; importer.filterMode = FilterMode.Point; importer.mipmapEnabled = false; importer.textureCompression = TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
        }
        pixel = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "/Pixel.png");
        lit = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Lit-Default.mat");
        unlit = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat");
        var idle = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "/Security/Guard_idle_anim.png");
        var run = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "/Security/Guard_run.png");
        var guardPose = new Texture2D(16, 32, TextureFormat.RGBA32, false);
        guardPose.SetPixels(idle.GetPixels(idle.width * 3 / 4, 0, 16, 32)); guardPose.Apply();
        var guardSprite = SaveSprite("Guardia", guardPose);
        var dogSprite = Dog();
        if (!open) scene = EditorSceneManager.OpenScene(LevelFourBuilder.ScenePath, OpenSceneMode.Additive);
        var previous = SceneManager.GetActiveScene(); SceneManager.SetActiveScene(scene);
        try
        {
            var intro = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<LevelFourIntro>(true)).Single();
            if (intro.GetComponent<LevelFourMission>() != null) throw new InvalidOperationException("La misión ya existe. No se reemplaza automáticamente.");
            var mission = intro.gameObject.AddComponent<LevelFourMission>();
            if (intro.player.GetComponent<LevelFourMotor>() == null) intro.player.gameObject.AddComponent<LevelFourMotor>();
            var root = new GameObject("MISIÓN · Edificio F, cubículos y vigilancia"); root.transform.SetParent(intro.transform, false); root.transform.position = Vector3.zero;
            var campus = new GameObject("Vigilancia nocturna del campus"); campus.transform.SetParent(root.transform, false);
            mission.roofs = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Tilemap>()).FirstOrDefault(m => m.name == "Edificio_Tilemap");
            var path = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Tilemap>()).First(m => m.name == "Camino_Tilemap");
            var f = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<CampusBuildingLabel>()).First(label => label.DisplayName.Trim() == "EDIFICIO F");
            Vector2 entry = Nearest(path, mission.roofs, (Vector2)f.transform.position + Vector2.down * 8);
            mission.entranceF = Marker("Acceso y panel de alarma del F", root.transform, entry);
            Quad("Panel de alarma del F", root.transform, pixel, entry + new Vector2(.9f, .5f), new Vector2(.5f, .7f), new Color32(58, 70, 74, 255), 2400);
            Quad("Indicador del panel", root.transform, pixel, entry + new Vector2(.9f, .65f), new Vector2(.12f, .12f), Color.red, 2401, true);
            Text("ENTRADA F · PANEL", root.transform, entry + Vector2.up * 1.5f, .26f, new Color(1, .8f, .35f));
            var pursuers = new System.Collections.Generic.List<LevelFourPursuer>();
            Vector2[] locations = { new Vector2(-10, -35), new Vector2(8, -34), new Vector2(22, -23), new Vector2(30, -13), new Vector2(0, -10), new Vector2(28, 10), new Vector2(10, 20), new Vector2(-20, -8) };
            for (int i = 0; i < locations.Length; i++)
            {
                Vector2 location = Nearest(path, mission.roofs, locations[i]);
                pursuers.Add(Pursuer("Guardia " + (i + 1), campus.transform, location, false, guardSprite, idle, run));
                if (i % 2 == 0) pursuers.Add(Pursuer("Perro del guardia " + (i + 1), campus.transform, Nearest(path, mission.roofs, location + Vector2.right * 1.4f), true, dogSprite, null, null));
            }
            mission.guards = pursuers.ToArray();
            foreach (var go in scene.GetRootGameObjects())
                if (go.name.Contains("Estudiantes") || go.name.StartsWith("Profesor_") || go.name.StartsWith("Profesora_") || go.name.StartsWith("Entregas_")) go.SetActive(false);
            string[] teams = { "Software", "Redes", "Entornos", "Mecatrónicos" };
            var groups = intro.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("Grupo de alumnos ")).OrderBy(t => t.name).ToArray();
            for (int i = 0; i < groups.Length; i++)
            {
                var students = groups[i].GetComponentsInChildren<LevelFourStudent>();
                foreach (var student in students)
                {
                    student.team = teams[i % 4];
                    var body = student.GetComponent<Rigidbody2D>(); if (body == null) body = student.gameObject.AddComponent<Rigidbody2D>(); body.bodyType = RigidbodyType2D.Kinematic; body.useFullKinematicContacts = true;
                    var collider = student.GetComponent<CapsuleCollider2D>(); if (collider == null) collider = student.gameObject.AddComponent<CapsuleCollider2D>(); collider.isTrigger = false; collider.size = new Vector2(.6f, .55f); collider.offset = new Vector2(0, -.55f);
                }
                Vector2 center = students.Aggregate(Vector2.zero, (sum, s) => sum + (Vector2)s.transform.position) / students.Length;
                Text("TEAM " + teams[i % 4].ToUpperInvariant(), groups[i], center + Vector2.down * 1.2f, .23f, new Color(.6f, .83f, 1));
                if (i == 0) mission.softwareGroup = Marker("Reunión final con Software", root.transform, center);
            }
            var interiors = new GameObject("INTERIORES · F de noche"); interiors.transform.SetParent(root.transform, false);
            Corridor(interiors.transform, 106, 36, "PLANTA BAJA · EDIFICIO F");
            Corridor(interiors.transform, 131, 31, "PLANTA ALTA · CUBÍCULOS");
            mission.groundSpawn = Marker("Entrada interior F", interiors.transform, new Vector2(106, 2.2f));
            mission.groundExit = Marker("Salir del F", interiors.transform, new Vector2(106, 1.3f));
            mission.groundStairs = Marker("Escalera al fondo", interiors.transform, new Vector2(106, 32));
            mission.upperSpawn = Marker("Llegada a planta alta", interiors.transform, new Vector2(131, 3.5f));
            mission.upperBack = Marker("Bajar escalera", interiors.transform, new Vector2(131, 1.5f));
            mission.torresDoor = Marker("Puerta de Torres · izquierda", interiors.transform, new Vector2(128.4f, 24));
            Text("< TORRES", interiors.transform, new Vector2(128.8f, 25), .3f, new Color(1, .85f, .4f));
            Text("OTROS CUBÍCULOS >", interiors.transform, new Vector2(134.5f, 25), .24f, Color.white);
            Text("BAÑOS", interiors.transform, new Vector2(101.5f, 9), .27f, Color.white);
            Text("CUBÍCULOS PB · TORRES ARRIBA", interiors.transform, new Vector2(110.5f, 24), .19f, Color.white);
            Stairs(interiors.transform, new Vector2(106, 32)); Stairs(interiors.transform, new Vector2(131, 1.2f));
            Sensor(interiors.transform, new Vector2(106, 11), 0); Sensor(interiors.transform, new Vector2(106, 21), 2); Sensor(interiors.transform, new Vector2(106, 27), 4);
            Sensor(interiors.transform, new Vector2(131, 10), 1); Sensor(interiors.transform, new Vector2(131, 18), 3);
            Office(interiors.transform);
            mission.officeSpawn = Marker("Entrar al cubículo de Torres", interiors.transform, new Vector2(153, 1.8f));
            mission.officeExit = Marker("Salir del cubículo", interiors.transform, new Vector2(153, 1));
            mission.laptop = Marker("Laptop de Torres", interiors.transform, new Vector2(153, 6.1f));
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            File.WriteAllText("Level4Work/MisionF.txt", "OK: misión completa instalada. Alarma: 180 s. 8 guardias/4 perros. 5 sensores. F y cubículo de Torres, laptop local y regreso a Software.\nAcceso F: " + entry + "\n");
        }
        finally { SceneManager.SetActiveScene(previous); if (!open) EditorSceneManager.CloseScene(scene, true); }
        AssetDatabase.SaveAssets();
    }

    static Vector2 Nearest(Tilemap path, Tilemap roof, Vector2 desired)
    {
        Vector2 result = desired; float best = float.PositiveInfinity;
        foreach (var cell in path.cellBounds.allPositionsWithin)
        {
            if (!path.HasTile(cell)) continue;
            Vector2 point = path.GetCellCenterWorld(cell);
            if (roof != null && roof.HasTile(roof.WorldToCell(point))) continue;
            float distance = (point - desired).sqrMagnitude;
            if (distance < best) { best = distance; result = point + Vector2.up * .5f; }
        }
        return result;
    }

    static LevelFourPursuer Pursuer(string name, Transform parent, Vector2 position, bool dog, Sprite sprite, Texture2D idle, Texture2D run)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.position = position;
        var renderer = go.AddComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.sharedMaterial = lit; renderer.sortingOrder = Mathf.RoundToInt(1000 - (position.y - .5f) * 10);
        var body = go.AddComponent<Rigidbody2D>(); body.bodyType = RigidbodyType2D.Kinematic; body.useFullKinematicContacts = true;
        var collider = go.AddComponent<CapsuleCollider2D>(); collider.size = dog ? new Vector2(.95f, .45f) : new Vector2(.6f, .55f); collider.offset = new Vector2(0, dog ? -.3f : -.55f);
        var script = go.AddComponent<LevelFourPursuer>(); script.home = position; script.isDog = dog; script.idleSheet = idle; script.runSheet = run;
        if (!dog)
        {
            var cap = Quad("Gorra de vigilancia", go.transform, pixel, Vector2.zero, new Vector2(.55f, .14f), new Color32(34, 56, 88, 255), renderer.sortingOrder + 1);
            cap.transform.localPosition = new Vector3(0, .52f);
        }
        return script;
    }

    static void Corridor(Transform parent, float x, int length, string name)
    {
        var room = new GameObject(name); room.transform.SetParent(parent, false); var p = room.transform;
        for (int y = 0; y <= length; y++) for (int col = -7; col <= 7; col++)
        {
            bool band = Mathf.Abs(col) == 2;
            Quad("Loseta blanca y gris", p, pixel, new Vector2(x + col, y), new Vector2(.98f, .98f), band ? new Color32(93, 114, 130, 255) : new Color32(203, 204, 189, 255), -40);
        }
        foreach (int side in new[] { -1, 1 })
        {
            Quad("Muro amarillo del pasillo", p, pixel, new Vector2(x + side * 3.4f, length * .5f), new Vector2(.4f, length + 1), new Color32(188, 171, 100, 255), 2000);
            for (int y = 4; y < length; y += 7)
            {
                Quad("Columna roja", p, pixel, new Vector2(x + side * 3.35f, y), new Vector2(.55f, 1), new Color32(137, 58, 43, 255), 2010);
                Quad("Aula cerrada", p, pixel, new Vector2(x + side * 5.3f, y + 2), new Vector2(3.1f, 4.6f), new Color32(49, 57, 66, 255), -30);
                Quad("Puerta cerrada", p, pixel, new Vector2(x + side * 3.35f, y + 2.1f), new Vector2(.48f, 1.5f), new Color32(57, 64, 63, 255), 2011);
                Quad("Ventana de aula", p, pixel, new Vector2(x + side * 3.35f, y + 4.2f), new Vector2(.42f, 1.8f), new Color32(57, 78, 89, 255), 2011);
                Quad("Mesa de aula", p, pixel, new Vector2(x + side * 5.2f, y + 1.8f), new Vector2(1.8f, .75f), new Color32(137, 115, 76, 255), 10);
            }
        }
        for (int y = 5; y < length - 3; y += 9)
        {
            Quad("Casillero del pasillo", p, pixel, new Vector2(x + 2.65f, y), new Vector2(.55f, 1.2f), new Color32(93, 104, 98, 255), 2012);
            Quad("Banca del pasillo", p, pixel, new Vector2(x - 2.6f, y), new Vector2(.48f, 1.8f), new Color32(105, 68, 47, 255), 2012);
            Text("^", p, new Vector2(x + 2, y + 1), .23f, new Color(.3f, .5f, .7f));
        }
        foreach (float y in new[] { 6f, 18f, 29f }) PointLight(p, new Vector2(x, y), .7f, 6.5f);
        Text(name, p, new Vector2(x, length + 1), .27f, Color.white);
    }

    static void Office(Transform parent)
    {
        for (int y = 0; y < 10; y++) for (int x = 150; x <= 156; x++)
            Quad("Piso del cubículo", parent, pixel, new Vector2(x, y), new Vector2(.98f, .98f), new Color32(156, 145, 125, 255), -40);
        Quad("Pared izquierda del cubículo", parent, pixel, new Vector2(150, 4.5f), new Vector2(.3f, 10), new Color32(191, 174, 112, 255), 2000);
        Quad("Pared derecha del cubículo", parent, pixel, new Vector2(156, 4.5f), new Vector2(.3f, 10), new Color32(191, 174, 112, 255), 2000);
        Quad("Fondo del cubículo", parent, pixel, new Vector2(153, 9), new Vector2(6, .3f), new Color32(182, 159, 108, 255), 2000);
        Quad("Escritorio de Torres", parent, pixel, new Vector2(153, 6.7f), new Vector2(3, 1.3f), new Color32(128, 81, 49, 255), 2450);
        Quad("Laptop abierta", parent, pixel, new Vector2(153, 7), new Vector2(1, .6f), new Color32(31, 42, 56, 255), 2451);
        Quad("Pantalla de laptop", parent, pixel, new Vector2(153, 7.05f), new Vector2(.78f, .4f), new Color32(44, 103, 135, 255), 2452, true);
        Quad("Silla del profesor", parent, pixel, new Vector2(153, 5.3f), new Vector2(.85f, .8f), new Color32(43, 52, 67, 255), 2400);
        for (int i = 0; i < 6; i++) Quad("Libro del cubículo", parent, pixel, new Vector2(155.25f, 4 + i * .38f), new Vector2(.6f, .28f), i % 2 == 0 ? new Color32(64, 111, 104, 255) : new Color32(154, 78, 67, 255), 2451);
        for (int i = 0; i < 3; i++) Quad("Hojas sobre el escritorio", parent, pixel, new Vector2(152 + i * .12f, 6.7f + i * .08f), new Vector2(.55f, .4f), new Color32(221, 215, 185, 255), 2452 + i);
        PointLight(parent, new Vector2(153, 5), .65f, 6);
        Text("PROFESOR TORRES", parent, new Vector2(153, 8.5f), .25f, new Color(1, .82f, .45f));
    }

    static void Sensor(Transform parent, Vector2 position, float phase)
    {
        var beam = Quad("Sensor de movimiento · esperar luz apagada", parent, pixel, position, new Vector2(6, .35f), new Color(1, .12f, .1f, .5f), 2800, true);
        var sensor = beam.gameObject.AddComponent<LevelFourSensor>(); sensor.phase = phase;
        Quad("Dispositivo del sensor", parent, pixel, position + Vector2.right * 2.95f, new Vector2(.25f, .38f), new Color32(58, 66, 70, 255), 2801);
    }
    static void Stairs(Transform parent, Vector2 position)
    {
        for (int i = 0; i < 7; i++) Quad("Escalón", parent, pixel, position + Vector2.up * (i * .3f - .9f), new Vector2(2.8f, .25f), new Color32((byte)(75 + i * 9), (byte)(80 + i * 9), (byte)(85 + i * 9), 255), 40);
        Text("ESCALERAS · E", parent, position + Vector2.up * 1.4f, .24f, new Color(1, .85f, .4f));
    }
    static void PointLight(Transform parent, Vector2 position, float intensity, float radius)
    {
        var go = new GameObject("Luz tenue de emergencia"); go.transform.SetParent(parent); go.transform.position = position;
        var light = go.AddComponent<Light2D>(); light.lightType = Light2D.LightType.Point; light.color = new Color(.65f, .76f, 1); light.intensity = intensity; light.pointLightOuterRadius = radius; light.pointLightInnerRadius = 1;
    }
    static Transform Marker(string name, Transform parent, Vector2 position) { var go = new GameObject(name); go.transform.SetParent(parent); go.transform.position = position; return go.transform; }
    static SpriteRenderer Quad(string name, Transform parent, Sprite sprite, Vector2 position, Vector2 size, Color color, int order, bool emissive = false)
    {
        var go = new GameObject(name); go.transform.SetParent(parent); go.transform.position = position; go.transform.localScale = new Vector3(size.x / sprite.bounds.size.x, size.y / sprite.bounds.size.y, 1);
        var render = go.AddComponent<SpriteRenderer>(); render.sprite = sprite; render.color = color; render.sortingOrder = order; render.sharedMaterial = emissive ? unlit : lit; return render;
    }
    static void Text(string text, Transform parent, Vector2 position, float size, Color color)
    {
        var go = new GameObject(text); go.transform.SetParent(parent); go.transform.position = position; go.transform.localScale = Vector3.one * size;
        var label = go.AddComponent<TextMeshPro>(); label.text = text; label.fontSize = 3; label.color = color; label.alignment = TextAlignmentOptions.Center; label.rectTransform.sizeDelta = new Vector2(40, 3); label.GetComponent<MeshRenderer>().sortingOrder = 3100;
    }
    static Sprite SaveSprite(string name, Texture2D texture)
    {
        string path = Art + "/Security/" + name + ".png"; File.WriteAllBytes(path, texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path); var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single; importer.spritePixelsPerUnit = 16; importer.filterMode = FilterMode.Point; importer.mipmapEnabled = false; importer.textureCompression = TextureImporterCompression.Uncompressed; importer.SaveAndReimport(); return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    static Sprite Dog()
    {
        var texture = new Texture2D(32, 24, TextureFormat.RGBA32, false); var pixels = new Color[32 * 24];
        for (int y = 0; y < 24; y++) for (int x = 0; x < 32; x++)
        {
            bool shape = x >= 8 && x <= 24 && y >= 8 && y <= 16 || x >= 22 && x <= 29 && y >= 13 && y <= 21 || x >= 27 && y >= 13 && y <= 17 || (x >= 9 && x <= 11 || x >= 20 && x <= 22) && y >= 2 && y <= 9 || x >= 3 && x <= 8 && y >= 14 && y <= 16 || x >= 2 && x <= 4 && y >= 15 && y <= 20;
            if (!shape) continue;
            Color color = new Color32(161, 111, 63, 255);
            if (y > 13 && x < 23 || x == 23 && y > 19 || x > 29 || x == 28 && y == 19) color = new Color32(48, 45, 41, 255);
            pixels[y * 32 + x] = color;
        }
        texture.SetPixels(pixels); texture.Apply(); return SaveSprite("PerroGuardian", texture);
    }
}
