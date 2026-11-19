using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

/// <summary>Mapa del campus tomado de sus tilemaps, con Galindo, el F y ruta caminable.</summary>
public sealed class LevelFourMinimap : MonoBehaviour
{
    public bool Expanded { get; private set; }
    public float RouteDistance { get; private set; }
    public bool HasRoute { get; private set; }
    public Vector2 TrackedPlayer { get; private set; }
    public bool Visible => canvas != null && canvas.activeSelf;
    public Vector2 PlayerMapPosition => playerDot == null ? Vector2.zero : playerDot.anchoredPosition;
    GameObject canvas;
    RectTransform panel, map, playerDot, destination;
    TMP_Text title, footer;
    Texture2D texture;
    readonly Rect world = new Rect(-55, -52, 110, 104);
    readonly List<Image> lines = new List<Image>();
    readonly List<RectTransform> threats = new List<RectTransform>();
    readonly Dictionary<Vector2Int, bool> clear = new Dictionary<Vector2Int, bool>();
    List<Vector2> route = new List<Vector2>();
    LevelFourMission mission;
    Vector2 previous = new Vector2(999, 999);
    float refresh;

    void Start()
    {
        mission = GetComponent<LevelFourMission>();
        canvas = U2Presentation.NewCanvas("Nivel 4 · minimapa del campus", 170); canvas.GetComponent<CanvasScaler>().matchWidthOrHeight = 1;
        var background = U2Presentation.Image(canvas.transform, "Mapa del campus", new Color32(13, 25, 33, 250)); panel = background.rectTransform;
        var mapObject = new GameObject("Calles y edificios", typeof(RectTransform), typeof(RawImage), typeof(RectMask2D)); mapObject.transform.SetParent(panel, false); map = mapObject.GetComponent<RectTransform>();
        texture = BakeMap(); mapObject.GetComponent<RawImage>().texture = texture;
        var routeRoot = new GameObject("Ruta al destino", typeof(RectTransform)); routeRoot.transform.SetParent(map, false); U2Presentation.Stretch(routeRoot.GetComponent<RectTransform>());
        for (int i = 0; i < 100; i++)
        {
            var image = U2Presentation.Image(routeRoot.transform, "Ruta", new Color(1, .82f, .25f)); image.raycastTarget = false; image.gameObject.SetActive(false); lines.Add(image);
        }
        destination = Dot("F", new Color(1, .72f, .15f), 25);
        Text(destination, "F", Vector2.zero, new Vector2(24, 24), 23, new Color(.1f, .12f, .16f));
        foreach (var guard in mission.guards) threats.Add(Dot("Guardia o perro", guard.isDog ? new Color(1, .4f, .7f) : new Color(1, .25f, .2f), 7));
        playerDot = Dot("Galindo", new Color(.2f, .85f, 1), 13);
        Text(playerDot, "TÚ", new Vector2(0, -15), new Vector2(55, 25), 17, Color.white);
        title = Text(panel, "CAMPUS · F", Vector2.zero, new Vector2(270, 45), 25, Color.white);
        footer = Text(panel, "", Vector2.zero, new Vector2(320, 65), 19, new Color(.85f, .9f, .83f));
        var toggle = U2Presentation.Image(panel, "Ampliar mapa", new Color(.18f, .3f, .35f)); toggle.rectTransform.anchoredPosition = new Vector2(135, 160); toggle.rectTransform.sizeDelta = new Vector2(38, 35);
        toggle.gameObject.AddComponent<Button>().onClick.AddListener(Toggle); Text(toggle.transform, "M", Vector2.zero, new Vector2(35, 35), 22, Color.white);
        Layout(); canvas.SetActive(false);
    }

    void Update()
    {
        if (canvas == null || mission == null) return;
        canvas.SetActive(mission.CampusActive);
        if (!mission.CampusActive || mission.Intro.Paused) return;
        if (Keyboard.current != null && Keyboard.current.mKey.wasPressedThisFrame) Toggle();
        Vector2 p = mission.Intro.player.position;
        TrackedPlayer = p;
        Vector2 goal = mission.CurrentStage == LevelFourMission.Stage.Escape ? new Vector2(Mathf.Clamp(p.x, mission.Intro.alleyMin.x + 1, mission.Intro.alleyMax.x - 1), -48.2f) : (Vector2)mission.entranceF.position;
        destination.anchoredPosition = MapPosition(goal); playerDot.anchoredPosition = MapPosition(p);
        title.text = mission.CurrentStage == LevelFourMission.Stage.Escape ? "CAMPUS · SALIDA" : "CAMPUS · EDIFICIO F";
        destination.GetComponentInChildren<TMP_Text>().text = mission.CurrentStage == LevelFourMission.Stage.Escape ? "S" : "F";
        for (int i = 0; i < threats.Count; i++)
        {
            var guard = mission.guards[i];
            bool nearby = Vector2.Distance(p, guard.transform.position) < 13;
            threats[i].gameObject.SetActive(nearby);
            if (nearby) threats[i].anchoredPosition = MapPosition(guard.transform.position);
        }
        refresh -= Time.deltaTime;
        if (refresh <= 0 && (Vector2.Distance(p, previous) > 1.2f || route.Count == 0))
        {
            previous = p; refresh = .9f; route = FindRoute(p, goal); HasRoute = route.Count > 1;
            RouteDistance = 0;
            for (int i = 1; i < route.Count; i++) RouteDistance += Vector2.Distance(route[i - 1], route[i]);
            DrawRoute();
        }
        footer.text = "AZUL: TÚ  ·  AMARILLO: DESTINO\n" + (HasRoute ? "Ruta: " + Mathf.CeilToInt(RouteDistance) + " m" : "Destino: " + Mathf.CeilToInt(Vector2.Distance(p, goal)) + " m") + "  ·  M: " + (Expanded ? "reducir" : "ampliar");
    }

    public void Toggle() { Expanded = !Expanded; Layout(); previous = new Vector2(999, 999); refresh = 0; }
    void Layout()
    {
        panel.anchoredPosition = Expanded ? new Vector2(0, 0) : new Vector2(750, -230);
        panel.sizeDelta = Expanded ? new Vector2(760, 840) : new Vector2(340, 380);
        map.sizeDelta = Expanded ? new Vector2(700, 690) : new Vector2(310, 290); map.anchoredPosition = new Vector2(0, 10);
        if (title != null) title.rectTransform.anchoredPosition = new Vector2(0, Expanded ? 385 : 160);
        if (footer != null) { footer.rectTransform.anchoredPosition = new Vector2(0, Expanded ? -375 : -151); footer.rectTransform.sizeDelta = new Vector2(Expanded ? 710 : 325, 65); }
        var toggle = panel.Find("Ampliar mapa") as RectTransform;
        if (toggle != null) toggle.anchoredPosition = Expanded ? new Vector2(345, 385) : new Vector2(135, 160);
        DrawRoute();
    }

    Texture2D BakeMap()
    {
        var image = new Texture2D(320, 320, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        Tilemap path = null; foreach (var tilemap in FindObjectsByType<Tilemap>(FindObjectsSortMode.None)) if (tilemap.name == "Camino_Tilemap") path = tilemap;
        var colors = new Color[320 * 320];
        for (int y = 0; y < 320; y++) for (int x = 0; x < 320; x++)
        {
            Vector2 p = new Vector2(world.xMin + (x + .5f) / 320 * world.width, world.yMin + (y + .5f) / 320 * world.height);
            Color color = new Color32(36, 64, 52, 255);
            if (path != null && path.HasTile(path.WorldToCell(p))) color = new Color32(145, 151, 129, 255);
            foreach (var trunk in mission.trunkAreas) if (trunk.Contains(p)) { color = new Color32(23, 43, 34, 255); break; }
            if (mission.roofs != null && mission.roofs.HasTile(mission.roofs.WorldToCell(p))) color = new Color32(62, 81, 101, 255);
            if (p.y < -49) color = new Color32(58, 60, 70, 255);
            colors[y * 320 + x] = color;
        }
        image.SetPixels(colors); image.Apply(); return image;
    }
    Vector2 MapPosition(Vector2 position) => new Vector2((position.x - world.center.x) / world.width * map.sizeDelta.x, (position.y - world.center.y) / world.height * map.sizeDelta.y);
    RectTransform Dot(string name, Color color, float size)
    {
        var dot = U2Presentation.Image(map, name, color); dot.rectTransform.sizeDelta = Vector2.one * size; dot.raycastTarget = false;
        dot.sprite = mission.Intro.roundedPanel; dot.type = Image.Type.Sliced; return dot.rectTransform;
    }
    TMP_Text Text(Transform parent, string content, Vector2 pos, Vector2 size, float font, Color color) => U2Presentation.Text(parent, content, content, pos, size, font, color);

    void DrawRoute()
    {
        var corners = new List<Vector2>();
        for (int i = 0; i < route.Count; i++)
            if (i == 0 || i == route.Count - 1 || Vector2.Dot((route[i] - route[i - 1]).normalized, (route[i + 1] - route[i]).normalized) < .99f) corners.Add(route[i]);
        for (int i = 0; i < lines.Count; i++)
        {
            lines[i].gameObject.SetActive(i + 1 < corners.Count);
            if (i + 1 >= corners.Count) continue;
            Vector2 a = MapPosition(corners[i]), b = MapPosition(corners[i + 1]), difference = b - a;
            lines[i].rectTransform.anchoredPosition = (a + b) * .5f; lines[i].rectTransform.sizeDelta = new Vector2(difference.magnitude, Expanded ? 3 : 2);
            lines[i].rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(difference.y, difference.x) * Mathf.Rad2Deg);
        }
    }

    sealed class Node { public Vector2Int cell; public float cost, priority; public int id; }
    bool Clear(Vector2Int cell)
    {
        if (!clear.TryGetValue(cell, out bool value)) { value = mission.Walkable((Vector2)cell * .5f); clear[cell] = value; }
        return value;
    }
    List<Vector2> FindRoute(Vector2 from, Vector2 to)
    {
        var start = Vector2Int.RoundToInt(from * 2); var goal = Vector2Int.RoundToInt(mission.NearestClear(to) * 2);
        if (!Clear(goal))
        {
            bool snapped = false;
            for (int r = 1; r < 8 && !snapped; r++) for (int x = -r; x <= r && !snapped; x++) for (int y = -r; y <= r; y++)
                if (Clear(goal + new Vector2Int(x, y))) { goal += new Vector2Int(x, y); snapped = true; break; }
        }
        var comparer = Comparer<Node>.Create((a, b) => a.priority == b.priority ? a.id.CompareTo(b.id) : a.priority.CompareTo(b.priority));
        var open = new SortedSet<Node>(comparer); var parent = new Dictionary<Vector2Int, Vector2Int>(); var costs = new Dictionary<Vector2Int, float> { [start] = 0 };
        int id = 0; open.Add(new Node { cell = start, cost = 0, priority = 0, id = id++ });
        var directions = new[] { Vector2Int.left, Vector2Int.right, Vector2Int.up, Vector2Int.down };
        bool found = false;
        for (int count = 0; open.Count > 0 && count < 50000; count++)
        {
            Node node = open.Min; open.Remove(node);
            if (node.cost > costs[node.cell]) continue;
            if (node.cell == goal) { found = true; break; }
            foreach (var direction in directions)
            {
                var next = node.cell + direction;
                if (!Clear(next)) continue;
                float cost = node.cost + 1;
                if (costs.TryGetValue(next, out float old) && old <= cost) continue;
                costs[next] = cost; parent[next] = node.cell;
                float estimate = Mathf.Abs(next.x - goal.x) + Mathf.Abs(next.y - goal.y);
                open.Add(new Node { cell = next, cost = cost, priority = cost + estimate, id = id++ });
            }
        }
        var result = new List<Vector2>(); if (!found) return result;
        var cursor = goal; result.Add(to);
        while (cursor != start && parent.TryGetValue(cursor, out var before)) { result.Add((Vector2)cursor * .5f); cursor = before; }
        result.Add(from); result.Reverse(); return result;
    }
    void OnDestroy() { if (canvas != null) Destroy(canvas); if (texture != null) Destroy(texture); }
}
