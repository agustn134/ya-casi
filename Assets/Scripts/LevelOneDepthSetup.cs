using UnityEngine;
using UnityEngine.Tilemaps;
using TMPro;

/// <summary>Separa las filas de pared para que compartan la profundidad de los objetos.</summary>
public sealed class LevelOneDepthSetup : MonoBehaviour
{
    void Start()
    {
        var world=LevelOneNightSchool.Active;
        if(world==null||world.authoredLayout==null)return;
        foreach(var image in world.authoredLayout.GetComponentsInChildren<SpriteRenderer>())
            if(image.GetComponent<LevelOneDepthSort>()==null)image.gameObject.AddComponent<LevelOneDepthSort>();
        var player=FindFirstObjectByType<PlayerMovement>();
        if(player!=null)foreach(var image in player.GetComponentsInChildren<SpriteRenderer>())
            if(image.GetComponent<LevelOneDepthSort>()==null)image.gameObject.AddComponent<LevelOneDepthSort>();
        foreach(var label in FindObjectsByType<TextMeshPro>(FindObjectsSortMode.None))label.renderer.sortingOrder=3000;
        foreach(var source in world.authoredLayout.GetComponentsInChildren<Tilemap>())
        {
            if(!source.name.Contains("Muros")&&!source.name.Contains("Columnas")&&!source.name.Contains("Ventanas"))continue;
            var old=source.GetComponent<TilemapRenderer>();old.enabled=false;
            BoundsInt bounds=source.cellBounds;
            for(int y=bounds.yMin;y<bounds.yMax;y++)
            {
                Tilemap row=null;
                for(int x=bounds.xMin;x<bounds.xMax;x++)
                {
                    var cell=new Vector3Int(x,y,0);var tile=source.GetTile(cell);if(tile==null)continue;
                    if(row==null)
                    {
                        var go=new GameObject("Profundidad · fila "+y);go.transform.SetParent(source.transform,false);
                        row=go.AddComponent<Tilemap>();var renderer=go.AddComponent<TilemapRenderer>();renderer.sharedMaterial=old.sharedMaterial;
                        int local=((y%32)+32)%32;int baseRow=(local==7||local==17)?y-1:y;
                        renderer.sortingOrder=1000-Mathf.RoundToInt((source.CellToWorld(new Vector3Int(0,baseRow,0)).y)*10);
                        if(source.name.Contains("Ventanas")||source.name.Contains("Columnas"))renderer.sortingOrder++;
                    }
                    row.SetTile(cell,tile);row.SetTileFlags(cell,TileFlags.None);row.SetColor(cell,source.GetColor(cell));row.SetTransformMatrix(cell,source.GetTransformMatrix(cell));
                }
            }
        }
    }
}
