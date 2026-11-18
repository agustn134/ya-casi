using UnityEngine;

/// <summary>Compara los pies, no la cabeza, para la profundidad de personajes y muebles.</summary>
public sealed class LevelOneDepthSort : MonoBehaviour
{
    SpriteRenderer image;Collider2D feet;
    void Start(){image=GetComponent<SpriteRenderer>();feet=GetComponent<Collider2D>();}
    void LateUpdate()
    {
        if(image==null)return;
        float y=feet!=null?feet.bounds.min.y:image.bounds.min.y;
        image.sortingOrder=1000-Mathf.RoundToInt(y*10);
    }
}
