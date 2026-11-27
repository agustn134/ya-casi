using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>Interior basado en las fotos y preparación de combate/minimapa.</summary>
public static class LevelFourDetailBuilder
{
    const string Art = "Assets/Art/LevelFour/FInterior";
    const string Sources = "C:/Users/elmane/Desktop/assets/moderninteriors-win/1_Interiors/16x16/Theme_Sorter_Shadowless_Singles/";
    static Sprite pixel, floor, arrow, chair, locker, desk, shelf, plant, extinguisher, bin;
    static Material lit, unlit;

    [MenuItem("Ya Casi/Nivel 4/Aplicar diseño F de las fotografías")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Detén Play para actualizar el diseño.");
        var scene = SceneManager.GetSceneByPath(LevelFourBuilder.ScenePath);
        bool wasOpen = scene.IsValid() && scene.isLoaded;
        if (wasOpen && scene.isDirty) throw new InvalidOperationException("Guarda los cambios de la escena antes de aplicar el diseño.");
        if (!File.Exists("Level4Work/AntesDeDisenoFotografico.unity.bak")) File.Copy(LevelFourBuilder.ScenePath, "Level4Work/AntesDeDisenoFotografico.unity.bak");
        Directory.CreateDirectory(Art);
        lit = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Lit-Default.mat");
        unlit = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat");
        pixel = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/LevelFour/Pixel.png");
        chair = Import("13_Conference_Hall_Singles_Shadowless", "Conference_Hall_Singles_Shadowless", 37);
        locker = Import("5_Classroom_and_Library_Singles_Shadowless", "Classroom_and_Library_Singles_Shadowless", 40);
        desk = Import("5_Classroom_and_Library_Singles_Shadowless", "Classroom_and_Library_Singles_Shadowless", 25);
        shelf = Import("5_Classroom_and_Library_Singles_Shadowless", "Classroom_and_Library_Singles_Shadowless", 43);
        plant = Import("19_Hospital_SIngles_Shadowless", "Hospital_Singles_Shadowless", 69);
        bin = Import("19_Hospital_SIngles_Shadowless", "Hospital_Singles_Shadowless", 253);
        extinguisher = Import("13_Conference_Hall_Singles_Shadowless", "Conference_Hall_Singles_Shadowless", 59);
        floor = FloorTile(); arrow = Arrow();
        if (!wasOpen) scene = EditorSceneManager.OpenScene(LevelFourBuilder.ScenePath, OpenSceneMode.Additive);
        var previous = SceneManager.GetActiveScene(); SceneManager.SetActiveScene(scene);
        try
        {
            var mission = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<LevelFourMission>(true)).Single();
            var intro = mission.GetComponent<LevelFourIntro>();
            var maps = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<UnityEngine.Tilemaps.Tilemap>()).ToArray();
            mission.gardens = maps.FirstOrDefault(m => m.name == "Jardines_Tilemap");
            mission.paths = maps.FirstOrDefault(m => m.name == "Camino_Tilemap");
            if (mission.gardens != null) mission.gardens.GetComponent<UnityEngine.Tilemaps.TilemapRenderer>().sortingOrder = 30;
            foreach (var student in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<LevelFourStudent>(true)))
            {
                var fighter = student.GetComponent<LevelFourFighter>(); if (fighter == null) fighter = student.gameObject.AddComponent<LevelFourFighter>(); fighter.maximumHealth = 3;
            }
            foreach (var guard in mission.guards)
            {
                var fighter = guard.GetComponent<LevelFourFighter>(); if (fighter == null) fighter = guard.gameObject.AddComponent<LevelFourFighter>(); fighter.maximumHealth = guard.isDog ? 3 : 4;
            }
            if (mission.GetComponent<LevelFourMinimap>() == null) mission.gameObject.AddComponent<LevelFourMinimap>();
            Vector2 oldEntry = mission.entranceF.position, newEntry = mission.NearestClear(oldEntry);
            mission.entranceF.position = newEntry;
            foreach (var t in mission.GetComponentsInChildren<Transform>(true))
                if (t.name == "Panel de alarma del F" || t.name == "Indicador del panel" || t.name == "ENTRADA F · PANEL") t.position += (Vector3)(newEntry - oldEntry);
            var interiors = mission.GetComponentsInChildren<Transform>(true).First(t => t.name == "INTERIORES · F de noche");
            var old = interiors.Find("DISEÑO F · Referencia fotográfica"); if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
            foreach (var renderer in interiors.GetComponentsInChildren<Renderer>(true))
                if (renderer.GetComponent<LevelFourSensor>() == null && renderer.name != "Dispositivo del sensor") renderer.enabled = false;
            foreach (var light in interiors.GetComponentsInChildren<Light2D>(true)) { light.color = new Color(1, .96f, .84f); light.intensity = .9f; light.pointLightOuterRadius = 8; }
            var root = new GameObject("DISEÑO F · Referencia fotográfica"); root.transform.SetParent(interiors, false); root.transform.position = Vector3.zero;
            Hall(root.transform, 106, 36, false); Hall(root.transform, 131, 31, true); Office(root.transform);
            Spiral(root.transform, mission.groundStairs.position); Spiral(root.transform, mission.upperBack.position);
            intro.view.orthographicSize = 5.9f;
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            File.WriteAllText("Level4Work/DisenoF.txt", "OK: piso de loseta fina, franjas grises, flechas azules, muros amarillos, columnas rojas, cristales, puertas, bancas, asientos negros, casilleros y cubículo con sprites LimeZu. Combate con daño/derribo y minimapa preparados.\n");
        }
        finally { SceneManager.SetActiveScene(previous); if (!wasOpen) EditorSceneManager.CloseScene(scene, true); }
        AssetDatabase.SaveAssets();
    }

    static void Hall(Transform parent, float x, int length, bool upper)
    {
        var root = new GameObject(upper ? "F · Cubículos de planta alta" : "F · Pasillo de aulas"); root.transform.SetParent(parent, false);
        var p = root.transform;
        Tiled("Piso blanco con juntas finas", p, new Vector2(x, length * .5f), new Vector2(14, length + 1), new Color32(238, 236, 220, 255), -30);
        foreach (int side in new[] { -1, 1 })
        {
            Tiled("Franja gris del piso", p, new Vector2(x + side * 2, length * .5f), new Vector2(1, length + 1), new Color32(133, 150, 159, 255), -29);
            Quad("Pared amarilla continua", p, new Vector2(x + side * 3.45f, length * .5f), new Vector2(.76f, length + 1), new Color32(237, 211, 124, 255), 2000);
            Quad("Zoclo gris oscuro", p, new Vector2(x + side * 3.02f, length * .5f), new Vector2(.12f, length + 1), new Color32(75, 80, 79, 255), 2001);
            Quad("Sombra lateral del muro", p, new Vector2(x + side * 3.88f, length * .5f), new Vector2(.12f, length + 1), new Color32(131, 119, 86, 255), 2001);
            for (int y = 4; y < length - 1; y += 7)
            {
                float wallX = x + side * 3.43f;
                Quad("Columna roja de concreto", p, new Vector2(wallX, y), new Vector2(.87f, .8f), new Color32(172, 62, 42, 255), 2030);
                Quad("Canto iluminado de columna", p, new Vector2(wallX - side * .31f, y), new Vector2(.16f, .8f), new Color32(209, 88, 58, 255), 2031);
                Tiled("Aula oscura", p, new Vector2(x + side * 5.45f, y + 2.35f), new Vector2(3.0f, 5.8f), new Color32(142, 145, 133, 255), -28);
                Door(p, new Vector2(wallX, y + 1.65f), side, upper ? "CUBÍCULOS" : "AULA TI-" + (12 + y / 7));
                Glass(p, new Vector2(wallX, y + 4.4f), side);
                for (int row = 0; row < 2; row++) Prop("Pupitre de aula", p, desk, new Vector2(x + side * 5.3f, y + .7f + row * 2), new Vector2(1.7f, 1.05f), Color.white, false);
                Prop("Extintor en la columna", p, extinguisher, new Vector2(wallX - side * .38f, y + .15f), new Vector2(.21f, .52f), Color.white, false);
            }
            for (int y = 6; y < length; y += 5)
            {
                var mark = Quad("Flecha azul de circulación", p, new Vector2(x + side * 2, y), new Vector2(.28f, .56f), new Color32(62, 111, 164, 255), -27, arrow);
                if (side < 0) mark.transform.localRotation = Quaternion.Euler(0, 0, 180);
            }
        }
        Tiled("Unión de franjas en el acceso", p, new Vector2(x, .8f), new Vector2(5, .5f), new Color32(133, 150, 159, 255), -28);
        // Tres asientos negros conectados, como en las fotografías.
        for (int i = 0; i < 3; i++) Prop("Asiento negro de espera", p, chair, new Vector2(x - 2.62f, 2.8f + i * .9f), new Vector2(.68f, 1.05f), new Color(.54f, .56f, .58f), true);
        Quad("Bastidor metálico de asientos", p, new Vector2(x - 2.58f, 3.75f), new Vector2(.07f, 2.8f), new Color32(85, 91, 93, 255), 2450);
        Bench(p, new Vector2(x - 2.61f, 7.3f)); Bench(p, new Vector2(x - 2.61f, 19.5f));
        for (int y = 12; y < length - 3; y += 7)
            for (int j = 0; j < 2; j++) Prop("Casillero gris con puerta y ventilación", p, locker, new Vector2(x + 2.58f, y + j * 1.35f), new Vector2(.77f, 1.38f), new Color(.85f, .89f, .83f), true);
        Prop("Maceta del recibidor", p, plant, new Vector2(x + 2.55f, 2.4f), new Vector2(.65f, 1.15f), Color.white, true);
        Prop("Papelera de pasillo", p, bin, new Vector2(x - 2.6f, 15), new Vector2(.44f, .57f), Color.white, true);
        for (int y = 6; y < length; y += 6)
        {
            Quad("Luminaria rectangular", p, new Vector2(x + 3.0f, y + .6f), new Vector2(.13f, .8f), new Color32(235, 228, 181, 255), 2050, null, true);
            Quad("Marco de luminaria", p, new Vector2(x + 3.0f, y + .6f), new Vector2(.2f, .92f), new Color32(74, 80, 80, 255), 2049);
        }
        if (!upper)
        {
            Quad("Dintel de acceso al F", p, new Vector2(x, -.2f), new Vector2(7.3f, .35f), new Color32(41, 45, 46, 255), 2030);
            Label("EDIFICIO F · ACCESO", p, new Vector2(x, .1f), .19f);
            Label("BAÑOS", p, new Vector2(x - 3.4f, 10.7f), .17f);
            Label("CUBÍCULOS: PLANTA ALTA", p, new Vector2(x, length - 2.3f), .20f);
        }
        else
        {
            Door(p, new Vector2(127.6f, 24), -1, "PROF. TORRES");
            Label("< PROFESOR TORRES", p, new Vector2(129.5f, 25.5f), .18f);
            Label("OTROS CUBÍCULOS >", p, new Vector2(133.2f, 25.5f), .16f);
        }
    }

    static void Door(Transform p, Vector2 position, int side, string caption)
    {
        Quad("Marco negro de puerta", p, position, new Vector2(.78f, 2.1f), new Color32(31, 36, 37, 255), 2040);
        Quad("Hoja de puerta de aula", p, position, new Vector2(.51f, 1.84f), new Color32(133, 107, 70, 255), 2041);
        Quad("Ventana alta de puerta", p, position + Vector2.up * .4f, new Vector2(.37f, .55f), new Color32(75, 104, 113, 255), 2042);
        Quad("Manija metálica", p, position + new Vector2(-side * .2f, -.3f), new Vector2(.09f, .24f), new Color32(191, 194, 183, 255), 2043);
        Quad("Umbral de puerta", p, position + Vector2.down * 1.03f, new Vector2(.8f, .09f), new Color32(176, 179, 168, 255), 2043);
        Label(caption, p, position + Vector2.up * 1.25f, .14f);
    }
    static void Glass(Transform p, Vector2 position, int side)
    {
        Quad("Marco negro de ventanal", p, position, new Vector2(.68f, 2.2f), new Color32(30, 38, 39, 255), 2035);
        Quad("Vidrio azul oscuro", p, position, new Vector2(.45f, 2.0f), new Color32(62, 95, 107, 255), 2036);
        for (int i = -1; i <= 1; i++) Quad("División del cristal", p, position + Vector2.up * i * .65f, new Vector2(.64f, .055f), new Color32(31, 39, 40, 255), 2037);
        Quad("Reflejo en vidrio", p, position + new Vector2(side * .12f, .15f), new Vector2(.05f, 1.6f), new Color32(104, 139, 143, 255), 2038);
    }
    static void Bench(Transform p, Vector2 position)
    {
        int order = 2500 - Mathf.RoundToInt(position.y * 10);
        Quad("Banca de madera · bastidor", p, position, new Vector2(.57f, 2.8f), new Color32(38, 46, 48, 255), order);
        for (int i = 0; i < 6; i++) Quad("Listón de madera de la banca", p, position + Vector2.right * (i - 2.5f) * .075f, new Vector2(.055f, 2.65f), i % 2 == 0 ? new Color32(147, 88, 49, 255) : new Color32(184, 116, 66, 255), order + 1);
        for (int i = -1; i <= 1; i += 2) Quad("Pie metálico de banca", p, position + Vector2.up * i * 1.05f, new Vector2(.72f, .09f), new Color32(66, 73, 74, 255), order + 2);
    }
    static void Spiral(Transform p, Vector2 position)
    {
        for (int i = 0; i < 14; i++)
        {
            float a = i * 360f / 14;
            var step = Quad("Peldaño de escalera de caracol", p, position + new Vector2(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad)) * .65f, new Vector2(1.25f, .14f), new Color32((byte)(85 + i * 4), (byte)(94 + i * 4), (byte)(99 + i * 4), 255), 2100);
            step.transform.localRotation = Quaternion.Euler(0, 0, a);
        }
        Quad("Eje central de escalera", p, position, new Vector2(.2f, .2f), new Color32(49, 58, 62, 255), 2102);
        var railing = new GameObject("Barandal circular de escalera"); railing.transform.SetParent(p); railing.transform.position = position;
        var line = railing.AddComponent<LineRenderer>(); line.useWorldSpace = false; line.loop = true; line.positionCount = 48; line.widthMultiplier = .065f; line.sharedMaterial = unlit; line.startColor = line.endColor = new Color(.2f, .25f, .27f); line.sortingOrder = 2103;
        for (int i = 0; i < 48; i++) line.SetPosition(i, new Vector3(Mathf.Cos(i * Mathf.PI * 2 / 48), Mathf.Sin(i * Mathf.PI * 2 / 48)) * 1.3f);
        Label("ESCALERAS · E", p, position + Vector2.up * 1.65f, .2f);
    }

    static void Office(Transform parent)
    {
        var root = new GameObject("Cubículo de Torres · detallado"); root.transform.SetParent(parent, false); var p = root.transform;
        Tiled("Loseta del cubículo", p, new Vector2(153, 4.5f), new Vector2(6.2f, 9.5f), new Color32(222, 217, 194, 255), -25);
        foreach (int side in new[] { -1, 1 })
        {
            Quad("Muro amarillo de cubículo", p, new Vector2(153 + side * 3.2f, 4.5f), new Vector2(.5f, 9.5f), new Color32(232, 213, 146, 255), 2000);
            Quad("Zoclo gris de cubículo", p, new Vector2(153 + side * 2.91f, 4.5f), new Vector2(.08f, 9.5f), new Color32(76, 80, 77, 255), 2001);
        }
        Quad("Fondo amarillo", p, new Vector2(153, 9.1f), new Vector2(6.7f, .5f), new Color32(232, 213, 146, 255), 2000);
        // Módulo de oficina en L: mamparas verdes, estructura metálica y madera.
        foreach (int side in new[] { -1, 1 })
        {
            Quad("Sombra del módulo", p, new Vector2(153 + side * 2.1f, 6.5f), new Vector2(.95f, 3.8f), new Color32(34, 37, 35, 120), -15);
            Quad("Marco aluminio de mampara", p, new Vector2(153 + side * 2.55f, 7.1f), new Vector2(.16f, 3.1f), new Color32(183, 189, 176, 255), 2400);
            Quad("Mampara verde lateral", p, new Vector2(153 + side * 2.55f, 7.25f), new Vector2(.09f, 2.7f), new Color32(141, 156, 65, 255), 2401);
        }
        Quad("Marco superior de mampara", p, new Vector2(153, 8.5f), new Vector2(5.25f, .85f), new Color32(181, 190, 168, 255), 2400);
        Quad("Panel verde de privacidad", p, new Vector2(153, 8.53f), new Vector2(5.02f, .66f), new Color32(145, 163, 67, 255), 2401);
        Quad("Panel traslúcido central", p, new Vector2(152.5f, 8.53f), new Vector2(1.25f, .66f), new Color32(205, 207, 176, 255), 2402);
        Quad("Frente del escritorio", p, new Vector2(153, 6.48f), new Vector2(4.85f, .36f), new Color32(123, 85, 47, 255), 2420);
        Quad("Tablero roble del escritorio", p, new Vector2(153, 7.05f), new Vector2(4.85f, 1.12f), new Color32(192, 153, 96, 255), 2430);
        Quad("Frente del retorno en L", p, new Vector2(151.12f, 5.75f), new Vector2(1.06f, .3f), new Color32(123, 85, 47, 255), 2420);
        Quad("Retorno de madera en L", p, new Vector2(151.12f, 6.25f), new Vector2(1.06f, 2.15f), new Color32(192, 153, 96, 255), 2430);
        var deskBody = new GameObject("Colisión del escritorio en L"); deskBody.transform.SetParent(p); deskBody.transform.position = new Vector3(153, 6.55f, 0);
        deskBody.AddComponent<BoxCollider2D>().size = new Vector2(4.8f, .28f);
        var returnBody = new GameObject("Colisión del retorno"); returnBody.transform.SetParent(p); returnBody.transform.position = new Vector3(151.12f, 5.75f, 0);
        returnBody.AddComponent<BoxCollider2D>().size = new Vector2(.9f, 1.5f);
        for (int i = 0; i < 16; i++)
            Quad("Veta fina del roble", p, new Vector2(150.65f + i * .3f, 7.07f + (i % 3 - 1) * .22f), new Vector2(.19f, .015f), new Color32(164, 125, 77, 255), 2431);
        Quad("Cajonera bajo el retorno", p, new Vector2(151.1f, 5.7f), new Vector2(.85f, 1.25f), new Color32(157, 116, 65, 255), 2432);
        for (int i = 0; i < 3; i++)
        {
            Quad("Frente de cajón", p, new Vector2(151.1f, 5.25f + i * .38f), new Vector2(.76f, .32f), new Color32(187, 146, 89, 255), 2433);
            Quad("Tirador de aluminio", p, new Vector2(151.1f, 5.3f + i * .38f), new Vector2(.25f, .045f), new Color32(212, 215, 204, 255), 2434);
        }
        foreach (float legX in new[] { 150.68f, 155.25f })
            Quad("Pata metálica del escritorio", p, new Vector2(legX, 6.2f), new Vector2(.09f, .8f), new Color32(186, 192, 182, 255), 2440);
        Prop("Silla negra de Torres", p, chair, new Vector2(153, 5.1f), new Vector2(.95f, 1.3f), new Color(.5f, .53f, .55f), true);
        Prop("Librero de Torres", p, shelf, new Vector2(155.1f, 6), new Vector2(1.2f, 1.5f), Color.white, true);
        Prop("Planta del cubículo", p, plant, new Vector2(155, 2.3f), new Vector2(.8f, 1.3f), Color.white, true);
        Prop("Papelera de Torres", p, bin, new Vector2(150.9f, 3.5f), new Vector2(.5f, .6f), Color.white, true);
        Quad("Pantalla de la laptop · marco", p, new Vector2(153, 7.2f), new Vector2(.97f, .62f), new Color32(28, 34, 42, 255), 2470);
        Quad("Pantalla de laptop encendida", p, new Vector2(153, 7.23f), new Vector2(.78f, .44f), new Color32(57, 115, 143, 255), 2471, null, true);
        Quad("Base de laptop", p, new Vector2(153, 6.75f), new Vector2(1.07f, .35f), new Color32(86, 93, 99, 255), 2470);
        for (int row = 0; row < 3; row++) for (int col = 0; col < 8; col++) Quad("Tecla de laptop", p, new Vector2(152.64f + col * .1f, 6.67f + row * .08f), new Vector2(.07f, .04f), new Color32(33, 39, 45, 255), 2471);
        Quad("Trackpad de laptop", p, new Vector2(153, 6.59f), new Vector2(.25f, .07f), new Color32(155, 165, 170, 255), 2472);
        Quad("Barra de ventana SIGA", p, new Vector2(153, 7.39f), new Vector2(.70f, .06f), new Color32(210, 110, 39, 255), 2472, null, true);
        for (int i = 0; i < 3; i++) Quad("Filas visibles en pantalla", p, new Vector2(153, 7.28f - i * .08f), new Vector2(.53f, .025f), new Color32(177, 219, 216, 255), 2472, null, true);
        Quad("Mouse sobre escritorio", p, new Vector2(153.85f, 6.84f), new Vector2(.17f, .26f), new Color32(43, 49, 53, 255), 2472);
        Quad("Taza de café", p, new Vector2(154.5f, 7.25f), new Vector2(.23f, .3f), new Color32(224, 220, 191, 255), 2472);
        Quad("Café en taza", p, new Vector2(154.5f, 7.35f), new Vector2(.17f, .08f), new Color32(66, 43, 28, 255), 2473);
        for (int i = 0; i < 4; i++)
        {
            var paper = Quad("Hoja con apuntes", p, new Vector2(151.9f + i * .07f, 6.9f + i * .07f), new Vector2(.55f, .65f), new Color32(245, 239, 210, 255), 2460 + i);
            paper.transform.localRotation = Quaternion.Euler(0, 0, 9 - i * 3);
        }
        Label("PROFESOR TORRES", p, new Vector2(153, 8.8f), .23f);
    }

    static Sprite Import(string folder, string prefix, int index)
    {
        string filename = prefix + "_" + index + ".png", output = Art + "/" + filename;
        if (!File.Exists(output)) File.Copy(Sources + folder + "/" + filename, output);
        return ImportSprite(output, 16);
    }
    static Sprite ImportSprite(string path, int ppu)
    {
        AssetDatabase.ImportAsset(path); var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single; importer.spritePixelsPerUnit = ppu; importer.filterMode = FilterMode.Point; importer.mipmapEnabled = false; importer.textureCompression = TextureImporterCompression.Uncompressed;
        var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings); settings.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(settings); importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    static Sprite FloorTile()
    {
        var texture = new Texture2D(32, 32, TextureFormat.RGBA32, false); var pixels = new Color[1024];
        for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++)
        {
            byte c = (byte)(x == 0 || y == 0 ? 154 : x == 1 || y == 1 ? 214 : 250);
            pixels[y * 32 + x] = new Color32(c, c, c, 255);
        }
        texture.SetPixels(pixels); texture.Apply(); string path = Art + "/LosetaBlanca.png"; File.WriteAllBytes(path, texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture); return ImportSprite(path, 64);
    }
    static Sprite Arrow()
    {
        var texture = new Texture2D(16, 24, TextureFormat.RGBA32, false); var pixels = new Color[16 * 24];
        for (int y = 0; y < 24; y++) for (int x = 0; x < 16; x++) pixels[y * 16 + x] = y < 15 && x >= 6 && x <= 9 || y >= 14 && y <= 22 && Mathf.Abs(x - 7.5f) <= (22 - y) * .85f ? Color.white : Color.clear;
        texture.SetPixels(pixels); texture.Apply(); string path = Art + "/FlechaCirculacion.png"; File.WriteAllBytes(path, texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture); return ImportSprite(path, 16);
    }
    static SpriteRenderer Quad(string name, Transform parent, Vector2 position, Vector2 size, Color color, int order, Sprite sprite = null, bool emissive = false)
    {
        sprite = sprite != null ? sprite : pixel;
        var go = new GameObject(name); go.transform.SetParent(parent); go.transform.position = position; go.transform.localScale = new Vector3(size.x / sprite.bounds.size.x, size.y / sprite.bounds.size.y, 1);
        var renderer = go.AddComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.color = color; renderer.sortingOrder = order; renderer.sharedMaterial = emissive ? unlit : lit; return renderer;
    }
    static void Tiled(string name, Transform parent, Vector2 position, Vector2 size, Color color, int order)
    {
        var render = Quad(name, parent, position, Vector2.one * .5f, color, order, floor); render.transform.localScale = Vector3.one;
        render.drawMode = SpriteDrawMode.Tiled; render.size = size;
    }
    static void Prop(string name, Transform parent, Sprite sprite, Vector2 position, Vector2 size, Color color, bool solid)
    {
        var render = Quad(name, parent, position, size, color, 2500 - Mathf.RoundToInt(position.y * 10), sprite);
        if (!solid) return;
        var collider = render.gameObject.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(size.x * .8f / render.transform.localScale.x, .24f / render.transform.localScale.y);
        collider.offset = new Vector2(0, -size.y * .34f / render.transform.localScale.y);
    }
    static void Label(string content, Transform parent, Vector2 position, float scale)
    {
        var go = new GameObject(content); go.transform.SetParent(parent); go.transform.position = position; go.transform.localScale = Vector3.one * scale;
        var text = go.AddComponent<TextMeshPro>(); text.text = content; text.fontSize = 3; text.rectTransform.sizeDelta = new Vector2(20, 3); text.alignment = TextAlignmentOptions.Center; text.color = new Color(1, .92f, .68f); text.GetComponent<MeshRenderer>().sortingOrder = 3100;
    }
}
