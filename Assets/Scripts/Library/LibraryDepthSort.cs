using UnityEngine;

public sealed class LibraryDepthSort : MonoBehaviour
{
    public float feetOffset;
    SpriteRenderer[] renderers;
    void Awake() { renderers = GetComponentsInChildren<SpriteRenderer>(); }
    void LateUpdate()
    {
        foreach (var sprite in renderers)
        {
            sprite.sortingLayerName = "Default";
            sprite.sortingOrder = 100 - Mathf.RoundToInt((transform.position.y + feetOffset) * 20);
        }
    }
}
