using System;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>Campus ornamental detrás del pasillo. No añade colisiones ni espacio jugable.</summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class LevelOneCampusBackdrop : MonoBehaviour
{
    public Tilemap baseFloor;
    public TileBase grassTile;
    public TileBase pathTile;
    public TileBase roofTile;
    public Sprite[] trees;
    public Vector2 margin = new Vector2(24, 16);
    public int seed = 137;

    const HideFlags GeneratedFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
    GameObject generated;
    int signature;
    float nextCheck;

    void OnEnable() { Rebuild(); }

    void Update()
    {
        if (Time.realtimeSinceStartup < nextCheck) return;
        nextCheck = Time.realtimeSinceStartup + .5f;
        if (baseFloor != null && CalculateSignature() != signature) Rebuild();
    }

    void OnValidate()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.delayCall -= RebuildIfActive;
        UnityEditor.EditorApplication.delayCall += RebuildIfActive;
#endif
    }

    void RebuildIfActive()
    {
        if (this != null && isActiveAndEnabled && gameObject.scene.IsValid()) Rebuild();
    }

    int CalculateSignature()
    {
        var camera = Camera.main;
        unchecked
        {
            int hash = baseFloor != null ? baseFloor.cellBounds.GetHashCode() : 0;
            hash = hash * 397 ^ margin.GetHashCode();
            hash = hash * 397 ^ seed;
            hash = hash * 397 ^ (camera != null ? Mathf.RoundToInt(camera.aspect * 100) : 0);
            hash = hash * 397 ^ (camera != null ? Mathf.RoundToInt(camera.orthographicSize * 100) : 0);
            return hash;
        }
    }

    [ContextMenu("Reconstruir campus decorativo")]
    public void Rebuild()
    {
        ClearGenerated();
        if (!isActiveAndEnabled || !gameObject.scene.IsValid() || baseFloor == null || grassTile == null) return;
        BoundsInt interior = baseFloor.cellBounds;
        if (interior.size.x <= 0 || interior.size.y <= 0) return;
        signature = CalculateSignature();

        generated = NewObject("Campus decorativo · sin colisiones", transform);
        generated.transform.SetPositionAndRotation(baseFloor.transform.position, baseFloor.transform.rotation);
        generated.transform.localScale = baseFloor.transform.lossyScale;
        var grid = generated.AddComponent<Grid>();
        grid.cellSize = baseFloor.layoutGrid != null ? baseFloor.layoutGrid.cellSize : Vector3.one;
        if (baseFloor.layoutGrid != null) grid.cellGap = baseFloor.layoutGrid.cellGap;

        float cellWidth = baseFloor.transform.TransformVector(new Vector3(grid.cellSize.x, 0, 0)).magnitude;
        float cellHeight = baseFloor.transform.TransformVector(new Vector3(0, grid.cellSize.y, 0)).magnitude;
        var camera = Camera.main;
        float viewHeight = camera != null && camera.orthographic ? camera.orthographicSize : 5;
        float viewWidth = viewHeight * (camera != null ? camera.aspect : 16f / 9f);
        int padX = Mathf.CeilToInt(Mathf.Max(margin.x, viewWidth + 4) / Mathf.Max(.01f, cellWidth));
        int padY = Mathf.CeilToInt(Mathf.Max(margin.y, viewHeight + 4) / Mathf.Max(.01f, cellHeight));
        var ground = NewTilemap("Pasto exterior", grid.transform, -50);
        var paths = NewTilemap("Andadores exteriores", grid.transform, -49);
        var roofs = NewTilemap("Edificios de fondo", grid.transform, -48);
        var sourceRenderer = baseFloor.GetComponent<TilemapRenderer>();
        if (sourceRenderer != null && sourceRenderer.sharedMaterial != null)
            foreach (var map in new[] { ground, paths, roofs })
                map.GetComponent<TilemapRenderer>().sharedMaterial = sourceRenderer.sharedMaterial;
        var outer = new BoundsInt(interior.xMin - padX, interior.yMin - padY, 0,
            interior.size.x + padX * 2, interior.size.y + padY * 2, 1);
        var tiles = new TileBase[outer.size.x * outer.size.y];
        for (int i = 0; i < tiles.Length; i++) tiles[i] = grassTile;
        ground.SetTilesBlock(outer, tiles);

        if (pathTile != null)
        {
            for (int x = outer.xMin; x < outer.xMax; x++)
                for (int lane = 0; lane < 3; lane++)
                {
                    PaintOutside(paths, interior, x, interior.yMax + 2 + lane, pathTile);
                    PaintOutside(paths, interior, x, interior.yMin - 5 + lane, pathTile);
                }
            for (int y = outer.yMin; y < outer.yMax; y++)
                for (int lane = 0; lane < 3; lane++)
                {
                    PaintOutside(paths, interior, interior.xMin - 5 + lane, y, pathTile);
                    PaintOutside(paths, interior, interior.xMax + 2 + lane, y, pathTile);
                }
        }

        if (roofTile != null)
            for (int x = outer.xMin + 8; x + 10 < outer.xMax; x += 18)
            {
                AddRoof(roofs, interior, new Vector2Int(x, interior.yMax + 8), 10, 5);
                AddRoof(roofs, interior, new Vector2Int(x + 5, interior.yMin - 13), 9, 5);
            }

        // System.Random preserva la elección aleatoria de la USB y las decisiones de los alumnos.
        var random = new System.Random(seed);
        if (trees != null && trees.Length > 0)
        {
            for (int x = outer.xMin + 3; x < outer.xMax - 2; x += 5)
            {
                AddTree(grid, interior, new Vector2(x + .5f, interior.yMax + 1), random);
                AddTree(grid, interior, new Vector2(x + 2, interior.yMin - 1.8f), random);
            }
            for (int y = outer.yMin + 3; y < outer.yMax - 2; y += 5)
            {
                AddTree(grid, interior, new Vector2(interior.xMin - 8, y), random);
                AddTree(grid, interior, new Vector2(interior.xMax + 8, y + 1), random);
            }
        }
    }

    void AddRoof(Tilemap roof, BoundsInt interior, Vector2Int corner, int width, int height)
    {
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                var cell = new Vector3Int(corner.x + x, corner.y + y, 0);
                if (interior.Contains(cell)) continue;
                roof.SetTile(cell, roofTile);
                roof.SetTileFlags(cell, TileFlags.None);
                bool rim = x == 0 || y == 0 || x == width - 1 || y == height - 1;
                roof.SetColor(cell, rim ? new Color(.64f, .65f, .68f) : Color.white);
            }
    }

    void AddTree(Grid grid, BoundsInt interior, Vector2 position, System.Random random)
    {
        if (position.x > interior.xMin - 1 && position.x < interior.xMax + 1 &&
            position.y > interior.yMin - 1 && position.y < interior.yMax + 1) return;
        Sprite sprite = trees[random.Next(trees.Length)];
        if (sprite == null) return;
        var tree = NewObject("Árbol ornamental", grid.transform);
        tree.transform.localPosition = new Vector3(position.x * grid.cellSize.x, position.y * grid.cellSize.y, 0);
        var renderer = tree.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = -47;
        float scale = 2.8f / Mathf.Max(.1f, sprite.bounds.size.y);
        tree.transform.localScale = Vector3.one * scale;
    }

    static void PaintOutside(Tilemap map, BoundsInt interior, int x, int y, TileBase tile)
    {
        var position = new Vector3Int(x, y, 0);
        if (!interior.Contains(position)) map.SetTile(position, tile);
    }

    static GameObject NewObject(string name, Transform parent)
    {
        var child = new GameObject(name) { hideFlags = GeneratedFlags };
        child.transform.SetParent(parent, false);
        return child;
    }

    static Tilemap NewTilemap(string name, Transform parent, int order)
    {
        var go = NewObject(name, parent);
        var tilemap = go.AddComponent<Tilemap>();
        var renderer = go.AddComponent<TilemapRenderer>();
        renderer.sortingOrder = order;
        renderer.mode = TilemapRenderer.Mode.Chunk;
        return tilemap;
    }

    void ClearGenerated()
    {
        if (generated == null) return;
        generated.SetActive(false);
        if (Application.isPlaying) Destroy(generated); else DestroyImmediate(generated);
        generated = null;
    }

    void OnDisable()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.delayCall -= RebuildIfActive;
#endif
        ClearGenerated();
    }
}
