using TMPro;
using UnityEngine;

/// <summary>Salida con un área física amplia y prioridad sobre los estantes.</summary>
public sealed class LibraryExitZone : MonoBehaviour
{
    public LibraryLevelManager manager;
    public BoxCollider2D zone;
    public SpriteRenderer floor;
    public TMP_Text sign;
    public SpriteRenderer leftDoor, rightDoor, statusLight;
    public Sprite[] doorFrames;
    float opening;
    public bool DoorIsOpen => opening > .95f;
    public bool Contains(PlayerMovement player)
    {
        if (player == null || zone == null) return false;
        var feet = player.GetComponent<Collider2D>();
        if (feet != null) return zone.bounds.Intersects(feet.bounds);
        return zone.bounds.Contains(player.transform.position);
    }
    void Update()
    {
        if (manager == null) return;
        bool open = manager.CollectedCount == 3;
        if (floor != null) floor.color = open ? Color.white : new Color(.8f,.84f,.85f);
        if (sign != null) sign.text = open ? "SALIDA ABIERTA\nCRUZA LA PUERTA" : "SALIDA\nREÚNE LOS 3 LIBROS";
        if (statusLight != null) statusLight.color = open ? new Color(.3f,1f,.6f) : new Color(1f,.72f,.3f);
        opening = Mathf.MoveTowards(opening, open ? 1f : 0f, Time.unscaledDeltaTime * 2.5f);
        if (doorFrames != null && doorFrames.Length > 0)
        {
            var frame = doorFrames[Mathf.RoundToInt(opening * (doorFrames.Length-1))];
            if (leftDoor != null) leftDoor.sprite = frame;
            if (rightDoor != null) rightDoor.sprite = frame;
        }
    }
    void OnTriggerEnter2D(Collider2D other) { Attempt(other); }
    void OnTriggerStay2D(Collider2D other) { Attempt(other); }
    void Attempt(Collider2D other)
    {
        if (manager != null && other.GetComponentInParent<PlayerMovement>() == manager.Player) manager.TryExit(false);
    }
}
