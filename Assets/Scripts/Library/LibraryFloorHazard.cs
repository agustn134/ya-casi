using UnityEngine;

/// <summary>Zona de piso mojado señalizada. El gestor combina las zonas antes de aplicar la lentitud.</summary>
public sealed class LibraryFloorHazard : MonoBehaviour
{
    public Vector2 size = new Vector2(1.8f, 1.1f);
    [Range(.2f, 1f)] public float speedMultiplier = .55f;

    public bool Contains(Vector2 feet)
    {
        Vector2 delta = feet - (Vector2)transform.position;
        return Mathf.Abs(delta.x) <= size.x * .5f && Mathf.Abs(delta.y) <= size.y * .5f;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1, .7f, .2f, .35f);
        Gizmos.DrawCube(transform.position, new Vector3(size.x, size.y, .1f));
    }
}
