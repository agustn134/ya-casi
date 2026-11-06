using UnityEngine;

/// <summary>Personajes ordenados por los pies, delante de los techos y detrás de las copas.</summary>
[RequireComponent(typeof(SpriteRenderer))]
public sealed class CampusActorSorting : MonoBehaviour
{
    SpriteRenderer sprite;
    void Awake() { sprite = GetComponent<SpriteRenderer>(); }
    void LateUpdate()
    {
        sprite.sortingOrder = Mathf.Clamp(1000 - Mathf.RoundToInt((transform.position.y - .5f) * 10f), 100, 2000);
    }
}
