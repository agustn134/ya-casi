using UnityEngine;

/// <summary>Un estante del catálogo. Su contenido se asigna al comenzar cada ronda.</summary>
public sealed class LibraryShelf : MonoBehaviour
{
    public string section;
    public int bookIndex = -1;
    public bool searched;
    public SpriteRenderer highlight;
    public Transform interactionPoint;
    public Vector2 Position => interactionPoint != null ? (Vector2)interactionPoint.position : (Vector2)transform.position;
    public void Highlight(bool value)
    {
        if (highlight != null)
        {
            highlight.enabled = value || searched;
            highlight.color = searched ? new Color(.45f, 1f, .65f, .35f) : new Color(1f, .82f, .35f, .5f);
        }
    }
}
