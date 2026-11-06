using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>Suelo provisional para comprobar las entregas sin cargar el campus final.</summary>
public sealed class CampusDeliveryPrototypeLayout : MonoBehaviour
{
    [SerializeField] CampusPathPlayer player;
    Tile tile;
    Sprite sprite;
    void Awake()
    {
        var grid = new GameObject("Grid prueba", typeof(Grid));
        grid.transform.SetParent(transform, false);
        var ground = new GameObject("Camino prueba", typeof(Tilemap), typeof(TilemapRenderer));
        ground.transform.SetParent(grid.transform, false);
        var map = ground.GetComponent<Tilemap>();
        sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height), Vector2.one * .5f, Texture2D.whiteTexture.width);
        tile = ScriptableObject.CreateInstance<Tile>(); tile.sprite = sprite;
        tile.color = new Color(.15f, .22f, .25f); tile.colliderType = Tile.ColliderType.None;
        for (int x = -11; x <= 11; x++) for (int y = -5; y <= 5; y++) map.SetTile(new Vector3Int(x, y), tile);
        ground.GetComponent<TilemapRenderer>().sortingOrder = -10;
        if (player != null) player.ConfigurePath(map);
    }
    void OnDestroy() { if (tile != null) Destroy(tile); if (sprite != null) Destroy(sprite); }
}
