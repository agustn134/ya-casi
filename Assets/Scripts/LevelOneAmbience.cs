using UnityEngine;

[CreateAssetMenu(menuName="Ya Casi/Ambientación del edificio F")]
public sealed class LevelOneAmbience : ScriptableObject
{
    public Sprite backpack,bin,paper;
    public void Populate(Transform parent,LevelOneNightPalette palette,Material material)
    {
        for(int level=0;level<2;level++)
        {
            float y=level*32;
            for(int room=0;room<4;room++)
            {
                float x=10+18*room;
                Add(parent,"Mochila",backpack,new Vector2(x-4,y+10.5f),.55f,material,false);
                Add(parent,"Papel en el suelo",paper,new Vector2(x+2,y+8.5f),.35f,material,true);
                Add(parent,"Silla apartada",palette.chair,new Vector2(x+4,y+13.5f),.65f,material,false);
                Add(parent,"Papelera del aula",bin,new Vector2(x-4,y+14),.65f,material,false);
            }
            for(int i=0;i<6;i++)
            {
                Add(parent,"Papelera del pasillo",bin,new Vector2(3+i*12,y-4.3f),.7f,material,false);
                Add(parent,"Mochila junto al muro",backpack,new Vector2(7+i*12,y-4.3f),.55f,material,false);
            }
        }
    }
    static void Add(Transform parent,string name,Sprite sprite,Vector2 at,float width,Material material,bool flat)
    {
        if(sprite==null)return;
        var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.position=at;
        go.transform.localScale=Vector3.one*(width/sprite.bounds.size.x);
        var image=go.AddComponent<SpriteRenderer>();image.sprite=sprite;image.sharedMaterial=material;
        if(flat)image.sortingOrder=-7;else go.AddComponent<LevelOneDepthSort>();
        if(name.StartsWith("Papelera")||name.StartsWith("Silla"))
        {
            var box=go.AddComponent<BoxCollider2D>();float scale=go.transform.localScale.x;
            box.size=new Vector2(width*.7f,.16f)/scale;
            box.offset=(Vector2)go.transform.InverseTransformPoint(new Vector3(at.x,image.bounds.min.y+.08f,0));
        }
    }
}
