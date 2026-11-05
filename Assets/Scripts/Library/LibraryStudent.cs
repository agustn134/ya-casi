using UnityEngine;

/// <summary>Alumnos ambientales: lectura sentada, paseo y pausas para buscar en estantes.</summary>
public sealed class LibraryStudent : MonoBehaviour
{
    public LibraryLevelManager manager;
    public bool seated;
    public Sprite[] readingFrames;
    public Vector2[] route;
    public bool[] searchStops;
    public float speed = 1.15f;
    public float searchSeconds = 3f;
    public int startWaypoint;
    public string facing = "Down";
    Animator animator;
    SpriteRenderer sprite;
    int next;
    float waiting, frameTime, blocked;
    string currentState;
    public bool IsSearching => !seated && waiting > 0;
    public int VisitedStops { get; private set; }
    public float DistanceTravelled { get; private set; }
    readonly RaycastHit2D[] hits = new RaycastHit2D[12];
    Collider2D footprint;
    void Awake()
    {
        animator = GetComponent<Animator>(); sprite = GetComponent<SpriteRenderer>();
        footprint = GetComponent<Collider2D>(); next = startWaypoint;
        if (seated && animator != null) animator.enabled = false;
    }
    void Update()
    {
        if (manager == null || !manager.IsPlaying) return;
        if (seated)
        {
            if (readingFrames != null && readingFrames.Length > 0)
            {
                frameTime += Time.deltaTime * 5;
                sprite.sprite = readingFrames[(int)frameTime % readingFrames.Length];
            }
            return;
        }
        if (route == null || route.Length < 2) return;
        if (waiting > 0) { waiting -= Time.deltaTime; Animate(Vector2.zero); return; }
        var delta = route[next] - (Vector2)transform.position;
        if (delta.magnitude < .09f)
        {
            if (searchStops != null && next < searchStops.Length && searchStops[next])
            {
                waiting = searchSeconds; facing = "Up"; VisitedStops++;
            }
            next = (next + 1) % route.Length;
            Animate(Vector2.zero);
            return;
        }
        var direction = delta.normalized;
        bool obstacle = false;
        if (footprint != null)
        {
            var filter = ContactFilter2D.noFilter; filter.useTriggers = false;
            int count = footprint.Cast(direction, filter, hits, .12f + speed * Time.deltaTime);
            for (int i = 0; i < count; i++)
                if (hits[i].collider != null && hits[i].collider.GetComponentInParent<PlayerMovement>() == null && hits[i].collider.GetComponentInParent<LibraryStudent>() == null)
                    obstacle = true;
        }
        if (obstacle)
        {
            blocked += Time.deltaTime; Animate(Vector2.zero);
            if (blocked > 1.5f) { next = (next + 1) % route.Length; blocked = 0; }
            return;
        }
        blocked = 0;
        Vector2 previous = transform.position;
        transform.position = Vector2.MoveTowards(previous, route[next], speed * Time.deltaTime);
        DistanceTravelled += Vector2.Distance(previous, transform.position);
        Animate(direction);
    }
    void Animate(Vector2 direction)
    {
        if (animator == null) return;
        if (direction.sqrMagnitude > .001f)
            facing = Mathf.Abs(direction.x) > Mathf.Abs(direction.y) ? (direction.x > 0 ? "Right" : "Left") : (direction.y > 0 ? "Up" : "Down");
        string state = (direction.sqrMagnitude > .001f ? "Walk" : "Idle") + facing;
        if (state == currentState) return;
        animator.Play("Base Layer." + state); currentState = state;
    }
}
