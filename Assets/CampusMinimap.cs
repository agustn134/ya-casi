using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

/// <summary>Vista completa del campus con cámara y geometría exclusiva del minimapa.</summary>
public sealed class CampusMinimap : MonoBehaviour
{
    public const string LayerName = "Minimap";
    static readonly Color Pending = new Color(.98f, .72f, .16f);
    static readonly Color Delivered = new Color(.2f, .85f, .35f);
    static readonly Color PlayerColor = new Color(.15f, .65f, 1);
    public Camera MapCamera { get; private set; }
    public RenderTexture Texture { get; private set; }
    public Rect MapWorldRect { get; private set; }
    public bool IsExpanded { get; private set; }
    public Vector2 PlayerMapPosition => playerMarker != null ? (Vector2)playerMarker.position : Vector2.zero;
    public Vector2 ProfessorMapPosition(int index) => professorMarkers[index].transform.position;
    public Color ProfessorMapColor(int index) => professorMarkers[index].color;
    CampusPathPlayer player;
    ProjectDeliverySession session;
    Transform[] professors;
    Transform playerMarker;
    SpriteRenderer[] professorMarkers;
    Camera mainCamera;
    int originalMask, layer;
    GameObject world, content;
    Image panel;
    TMP_Text caption;
    float panelHeight;
    Vector2 previousPlayer;
    Sprite circle, arrow, square;
    Texture2D circleTexture, arrowTexture, squareTexture;
    Material mapMaterial;
    readonly List<Mesh> meshes = new List<Mesh>();
    bool initialized;

    public void Configure(Transform hud, CampusPathPlayer actor, Transform[] targets, ProjectDeliverySession round)
    {
        if (initialized) return;
        layer = LayerMask.NameToLayer(LayerName);
        if (layer < 0 || hud == null || actor == null || actor.Path == null || actor.ViewCamera == null)
        {
            Debug.LogError("Minimapa: faltan la capa Minimap, el camino o la cámara principal.", this);
            enabled = false; return;
        }
        initialized = true; player = actor; professors = targets; session = round;
        mainCamera = actor.ViewCamera; originalMask = mainCamera.cullingMask;
        mainCamera.cullingMask &= ~(1 << layer);
        world = new GameObject("Campus · geometría del minimapa") { layer = layer };
        world.transform.SetParent(transform, false);
        mapMaterial = new Material(Shader.Find("Sprites/Default")) { name = "Minimapa sin iluminación" };
        var bounds = new Bounds(); bool hasBounds = false;
        BuildTiles(actor.Path, "Caminos simplificados", new Color(.82f, .83f, .69f), 1, ref bounds, ref hasBounds);
        foreach (Tilemap tilemap in actor.Path.transform.parent.GetComponentsInChildren<Tilemap>())
            if (tilemap.name == "Edificio_Tilemap")
                BuildTiles(tilemap, "Edificios simplificados", new Color(.25f, .35f, .44f), 2, ref bounds, ref hasBounds);
        if (!hasBounds) { Debug.LogError("Minimapa: el camino no contiene tiles.", this); enabled = false; return; }

        circleTexture = MakeTexture(false); arrowTexture = MakeTexture(true);
        circle = Sprite.Create(circleTexture, new Rect(0, 0, 32, 32), Vector2.one * .5f, 32);
        arrow = Sprite.Create(arrowTexture, new Rect(0, 0, 32, 32), Vector2.one * .5f, 32);
        squareTexture = new Texture2D(1, 1); squareTexture.SetPixel(0, 0, Color.white); squareTexture.Apply();
        square = Sprite.Create(squareTexture, new Rect(0, 0, 1, 1), Vector2.one * .5f, 1);
        float halfSize = Mathf.Max(bounds.extents.x, bounds.extents.y) + 3;
        Vector2 center = bounds.center;
        MapWorldRect = new Rect(center - Vector2.one * halfSize, Vector2.one * halfSize * 2);
        var ground = Marker("Suelo del campus", square, new Color(.13f, .3f, .24f), 0);
        ground.transform.position = center;
        ground.transform.localScale = new Vector3(halfSize * 2, halfSize * 2, 1);

        Texture = new RenderTexture(512, 512, 16, RenderTextureFormat.ARGB32)
        {
            name = "Campus Minimap 512", antiAliasing = 1, useMipMap = false,
            filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp
        };
        Texture.Create();
        var cameraObject = new GameObject("MinimapCamera", typeof(Camera)) { layer = layer };
        cameraObject.transform.SetParent(world.transform, false);
        cameraObject.transform.position = new Vector3(center.x, center.y, -20);
        MapCamera = cameraObject.GetComponent<Camera>();
        MapCamera.orthographic = true; MapCamera.orthographicSize = halfSize; MapCamera.aspect = 1;
        MapCamera.nearClipPlane = .1f; MapCamera.farClipPlane = 50;
        MapCamera.clearFlags = CameraClearFlags.SolidColor; MapCamera.backgroundColor = new Color(.13f, .3f, .24f);
        MapCamera.cullingMask = 1 << layer; MapCamera.targetTexture = Texture;
        MapCamera.allowHDR = false; MapCamera.allowMSAA = false; MapCamera.depth = mainCamera.depth - 1;
        var data = cameraObject.AddComponent<UniversalAdditionalCameraData>();
        data.renderType = CameraRenderType.Base; data.renderPostProcessing = false;
        data.requiresColorOption = CameraOverrideOption.Off; data.requiresDepthOption = CameraOverrideOption.Off;
        data.antialiasing = AntialiasingMode.None; data.SetRenderer(-1);

        float markerSize = halfSize * .105f;
        professorMarkers = new SpriteRenderer[professors.Length];
        for (int i = 0; i < professors.Length; i++)
        {
            var marker = Marker("Profesor " + (i + 1), circle, Pending, 4);
            marker.transform.localScale = Vector3.one * markerSize;
            professorMarkers[i] = marker;
            var number = new GameObject("Número de proyecto", typeof(TextMeshPro)) { layer = layer };
            number.transform.SetParent(marker.transform, false);
            var text = number.GetComponent<TextMeshPro>();
            if (CampusTypography.Font != null) text.font = CampusTypography.Font;
            text.text = (i + 1).ToString(); text.fontSize = 8; text.color = new Color(.02f, .06f, .08f);
            text.alignment = TextAlignmentOptions.Center; text.rectTransform.sizeDelta = Vector2.one;
            text.GetComponent<MeshRenderer>().sortingOrder = 5;
        }
        playerMarker = Marker("Galindo · flecha azul", arrow, PlayerColor, 7).transform;
        playerMarker.localScale = Vector3.one * markerSize * 1.2f;
        previousPlayer = player.FeetPosition;
        BuildPanel(hud);
        UpdateMarkers();
    }

    void BuildTiles(Tilemap source, string name, Color color, int order, ref Bounds bounds, ref bool hasBounds)
    {
        var vertices = new List<Vector3>(); var triangles = new List<int>(); var colors = new List<Color>();
        foreach (Vector3Int cell in source.cellBounds.allPositionsWithin)
        {
            if (!source.HasTile(cell)) continue;
            int first = vertices.Count;
            Vector3[] corners = { source.CellToWorld(cell), source.CellToWorld(cell + Vector3Int.right),
                source.CellToWorld(cell + Vector3Int.right + Vector3Int.up), source.CellToWorld(cell + Vector3Int.up) };
            foreach (Vector3 point in corners)
            {
                vertices.Add(point); colors.Add(color);
                if (!hasBounds) { bounds = new Bounds(point, Vector3.zero); hasBounds = true; }
                else bounds.Encapsulate(point);
            }
            triangles.AddRange(new[] { first, first + 1, first + 2, first, first + 2, first + 3 });
        }
        if (vertices.Count == 0) return;
        var mesh = new Mesh { name = name, indexFormat = vertices.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
        mesh.SetVertices(vertices); mesh.SetColors(colors); mesh.SetTriangles(triangles, 0); mesh.RecalculateBounds();
        meshes.Add(mesh);
        var geometry = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)) { layer = layer };
        geometry.transform.SetParent(world.transform, false);
        // Los vértices ya están en coordenadas mundiales, independientemente del Grid original.
        geometry.transform.position = Vector3.zero;
        geometry.GetComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = geometry.GetComponent<MeshRenderer>(); renderer.sharedMaterial = mapMaterial; renderer.sortingOrder = order;
    }

    SpriteRenderer Marker(string name, Sprite sprite, Color color, int order)
    {
        var go = new GameObject(name, typeof(SpriteRenderer)) { layer = layer };
        go.transform.SetParent(world.transform, false);
        var renderer = go.GetComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.color = color; renderer.sortingOrder = order;
        renderer.sharedMaterial = mapMaterial;
        return renderer;
    }
    static Texture2D MakeTexture(bool isArrow)
    {
        var texture = new Texture2D(32, 32) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++)
        {
            bool inside = isArrow ? (y >= 13 && y <= 29 && Mathf.Abs(x - 15.5f) <= (29 - y) * .65f + 1)
                || (y >= 3 && y < 16 && x >= 11 && x <= 20) : (new Vector2(x - 15.5f, y - 15.5f)).sqrMagnitude <= 225;
            texture.SetPixel(x, y, inside ? Color.white : Color.clear);
        }
        texture.Apply(); return texture;
    }

    void BuildPanel(Transform hud)
    {
        float mapSize = Application.isMobilePlatform ? 160 : 212;
        panelHeight = mapSize + 110;
        panel = U2Presentation.Image(hud, "Minimapa del campus", new Color(.025f, .045f, .065f, .97f));
        var rect = panel.rectTransform; rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one;
        rect.anchoredPosition = new Vector2(-16, -110); rect.sizeDelta = new Vector2(mapSize + 24, panelHeight);
        panel.raycastTarget = false;
        content = new GameObject("Mapa y leyenda", typeof(RectTransform)); content.transform.SetParent(panel.transform, false);
        U2Presentation.Stretch(content.GetComponent<RectTransform>());
        var imageObject = new GameObject("Render Texture del campus", typeof(RawImage)); imageObject.transform.SetParent(content.transform, false);
        var image = imageObject.GetComponent<RawImage>(); image.texture = Texture; image.raycastTarget = false;
        Top(image.rectTransform, new Vector2(0, -40), Vector2.one * mapSize, new Vector2(.5f, 1));
        string[] labels = { "Jugador", "Pendiente 1–5", "Entregado" };
        Color[] legendColors = { PlayerColor, Pending, Delivered };
        for (int i = 0; i < labels.Length; i++)
        {
            float y = -(40 + mapSize + 13 + i * 20);
            var swatch = U2Presentation.Image(content.transform, "Color " + labels[i], legendColors[i]); swatch.raycastTarget = false;
            Top(swatch.rectTransform, new Vector2(-mapSize / 2 + 8, y), new Vector2(10, 10), Vector2.one * .5f);
            var text = U2Presentation.Text(content.transform, "Leyenda " + labels[i], labels[i], Vector2.zero, new Vector2(mapSize - 36, 20), 19, Color.white);
            Top(text.rectTransform, new Vector2(13, y), new Vector2(mapSize - 36, 20), Vector2.one * .5f);
            text.alignment = TextAlignmentOptions.MidlineLeft;
        }
        var toggle = U2Presentation.Button(panel.transform, "Campus [M]", Vector2.zero, Toggle);
        Top(toggle.GetComponent<RectTransform>(), new Vector2(0, -18), new Vector2(mapSize, 30), Vector2.one * .5f);
        caption = toggle.GetComponentInChildren<TMP_Text>(); caption.fontSize = 22;
        caption.rectTransform.sizeDelta = new Vector2(mapSize - 12, 29);
        IsExpanded = !Application.isMobilePlatform;
        SetPanelState(); CampusTypography.Apply(panel.transform, true);
    }
    static void Top(RectTransform rect, Vector2 position, Vector2 size, Vector2 pivot)
    { rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1); rect.pivot = pivot; rect.anchoredPosition = position; rect.sizeDelta = size; }
    public void Toggle() { if (!initialized || panel == null) return; IsExpanded = !IsExpanded; SetPanelState(); }
    void SetPanelState()
    {
        content.SetActive(IsExpanded); MapCamera.enabled = IsExpanded;
        panel.rectTransform.sizeDelta = new Vector2(panel.rectTransform.sizeDelta.x, IsExpanded ? panelHeight : 38);
        caption.text = IsExpanded ? "Campus [M] −" : "Mapa [M] +";
    }
    void LateUpdate()
    {
        if (!initialized || playerMarker == null) return;
        if (Keyboard.current != null && Keyboard.current.mKey.wasPressedThisFrame) Toggle();
        UpdateMarkers();
    }
    void UpdateMarkers()
    {
        Vector2 position = player.FeetPosition, movement = position - previousPlayer;
        playerMarker.position = position;
        if (movement.sqrMagnitude > .00001f) playerMarker.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(movement.y, movement.x) * Mathf.Rad2Deg - 90);
        previousPlayer = position;
        for (int i = 0; i < professors.Length; i++)
        {
            if (professors[i] == null) continue;
            professorMarkers[i].transform.position = professors[i].position;
            professorMarkers[i].color = session.IsDelivered("profesor-" + i) ? Delivered : Pending;
        }
    }
    void OnDestroy()
    {
        if (mainCamera != null) mainCamera.cullingMask = originalMask;
        if (MapCamera != null) { MapCamera.enabled = false; MapCamera.targetTexture = null; }
        if (Texture != null) { Texture.Release(); Destroy(Texture); }
        if (world != null) Destroy(world); if (panel != null) Destroy(panel.gameObject);
        foreach (Mesh mesh in meshes) Destroy(mesh);
        if (mapMaterial != null) Destroy(mapMaterial);
        if (circle != null) Destroy(circle); if (arrow != null) Destroy(arrow); if (square != null) Destroy(square);
        if (circleTexture != null) Destroy(circleTexture); if (arrowTexture != null) Destroy(arrowTexture); if (squareTexture != null) Destroy(squareTexture);
    }
}
