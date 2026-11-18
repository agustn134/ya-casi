using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>Profesor que patrulla un área local del camino y se detiene para atender a Galindo.</summary>
[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer), typeof(CapsuleCollider2D))]
public sealed class CampusProfessorWaiting : MonoBehaviour
{
    [SerializeField] Transform player;
    [SerializeField] Sprite[] idleDown, idleLeft, idleRight, walkLeft, walkRight;
    [SerializeField] Sprite[] idleUp, walkDown, walkUp;
    [SerializeField, Min(1)] float patrolRadius = 3.6f;
    [SerializeField, Min(.1f)] float patrolSpeed = 1f;
    [SerializeField, Min(.5f)] float interactionStopDistance = 2.2f;
    [SerializeField] float phaseOffset;
    Rigidbody2D body;
    SpriteRenderer sprite;
    CapsuleCollider2D feet;
    Tilemap path, roofs;
    Vector2 originFeet, target, facing = Vector2.down;
    float elapsed, pause, blocked;
    System.Random random;
    readonly Queue<Vector2> route = new Queue<Vector2>();
    readonly HashSet<Vector3Int> area = new HashSet<Vector3Int>();
    readonly List<Vector3Int> destinations = new List<Vector3Int>();
    readonly RaycastHit2D[] hits = new RaycastHit2D[16];
    static readonly Vector3Int[] Neighbors = { Vector3Int.up, Vector3Int.right, Vector3Int.down, Vector3Int.left };

    public float PatrolRadius => patrolRadius;

    void Awake()
    {
        body = GetComponent<Rigidbody2D>(); sprite = GetComponent<SpriteRenderer>(); feet = GetComponent<CapsuleCollider2D>();
        elapsed = phaseOffset;
        random = new System.Random(Mathf.RoundToInt(phaseOffset * 103) + 7919);
    }

    void Start()
    {
        var actor = player != null ? player.GetComponent<CampusPathPlayer>() : FindFirstObjectByType<CampusPathPlayer>();
        path = actor != null ? actor.Path : null;
        if (path == null) path = GameObject.Find("Camino_Tilemap")?.GetComponent<Tilemap>();
        roofs = GameObject.Find("Edificio_Tilemap")?.GetComponent<Tilemap>();
        originFeet = body.position + feet.offset;
        target = body.position;
        pause = .25f + Mathf.Repeat(phaseOffset, .8f);
        if (path != null) BuildArea();
    }

    void BuildArea()
    {
        var candidates = new HashSet<Vector3Int>();
        Vector3Int home = default;
        float nearest = float.PositiveInfinity;
        foreach (Vector3Int cell in path.cellBounds.allPositionsWithin)
        {
            if (!path.HasTile(cell)) continue;
            Vector2 center = path.GetCellCenterWorld(cell);
            float fromHome = (center - originFeet).sqrMagnitude;
            if (fromHome > patrolRadius * patrolRadius || !OnPath(center - feet.offset)) continue;
            candidates.Add(cell);
            if (fromHome < nearest) { nearest = fromHome; home = cell; }
        }
        if (candidates.Count == 0) return;
        var frontier = new Queue<Vector3Int>(); frontier.Enqueue(home); area.Add(home);
        while (frontier.Count > 0)
        {
            Vector3Int current = frontier.Dequeue();
            foreach (Vector3Int offset in Neighbors)
            {
                var next = current + offset;
                if (candidates.Contains(next) && area.Add(next)) frontier.Enqueue(next);
            }
        }
        destinations.AddRange(area);
        destinations.Sort((a, b) => a.y == b.y ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
        if (!OnPath(body.position))
        {
            body.position = (Vector2)path.GetCellCenterWorld(home) - feet.offset;
            target = body.position;
        }
    }

    void FixedUpdate()
    {
        elapsed += Time.fixedDeltaTime;
        if (player != null && Vector2.Distance(player.position, body.position) < interactionStopDistance)
        {
            facing = ((Vector2)player.position - body.position).normalized;
            Animate(false); return;
        }
        if (pause > 0) { pause -= Time.fixedDeltaTime; Animate(false); return; }
        if (Vector2.Distance(body.position, target) < .025f)
        {
            if (route.Count > 0) target = route.Dequeue();
            else { ChooseRoute(); Animate(false); return; }
        }
        Vector2 delta = Vector2.MoveTowards(body.position, target, patrolSpeed * Time.fixedDeltaTime) - body.position;
        if (CanStep(delta))
        {
            body.MovePosition(body.position + delta); facing = delta.normalized; blocked = 0; Animate(true);
        }
        else
        {
            Animate(false); blocked += Time.fixedDeltaTime;
            if (blocked < .65f) return;
            // No empuja al grupo que bloquea el pasillo; toma otro destino de su área.
            route.Clear(); target = body.position; pause = .15f + (float)random.NextDouble() * .45f; blocked = 0;
        }
    }

    void ChooseRoute()
    {
        if (path == null || destinations.Count < 2) { pause = 1; return; }
        Vector3Int start = path.WorldToCell(body.position + feet.offset);
        if (!area.Contains(start)) { pause = .5f; return; }
        Vector3Int goal = destinations[random.Next(destinations.Count)];
        for (int attempt = 0; attempt < 12 && Vector3Int.Distance(start, goal) < 1.5f; attempt++)
            goal = destinations[random.Next(destinations.Count)];
        var parents = new Dictionary<Vector3Int, Vector3Int>();
        var frontier = new Queue<Vector3Int>(); frontier.Enqueue(start); parents[start] = start;
        while (frontier.Count > 0 && !parents.ContainsKey(goal))
        {
            var current = frontier.Dequeue();
            foreach (Vector3Int offset in Neighbors)
            {
                var next = current + offset;
                if (!area.Contains(next) || parents.ContainsKey(next)) continue;
                parents[next] = current; frontier.Enqueue(next);
            }
        }
        if (goal == start || !parents.ContainsKey(goal)) { pause = .4f; return; }
        var reverse = new Stack<Vector3Int>();
        for (var current = goal; current != start; current = parents[current]) reverse.Push(current);
        Vector2 center = (Vector2)path.GetCellCenterWorld(start) - feet.offset;
        if (Vector2.Distance(center, body.position) > .05f) route.Enqueue(center);
        while (reverse.Count > 0) route.Enqueue((Vector2)path.GetCellCenterWorld(reverse.Pop()) - feet.offset);
        target = route.Dequeue();
        pause = .35f + (float)random.NextDouble() * .9f;
    }

    bool OnPath(Vector2 position)
    {
        Vector2 center = position + feet.offset, half = feet.size * .5f;
        for (int x = -1; x <= 1; x++) for (int y = -1; y <= 1; y++)
        {
            Vector2 point = center + new Vector2(x * half.x, y * half.y);
            if (!path.HasTile(path.WorldToCell(point)) || (roofs != null && roofs.HasTile(roofs.WorldToCell(point)))) return false;
        }
        return true;
    }

    bool CanStep(Vector2 delta)
    {
        if (path == null || delta.sqrMagnitude < .0000001f) return false;
        int steps = Mathf.Max(1, Mathf.CeilToInt(delta.magnitude / .05f));
        for (int i = 1; i <= steps; i++) if (!OnPath(body.position + delta * (i / (float)steps))) return false;
        var filter = new ContactFilter2D { useTriggers = false };
        int count = Physics2D.CapsuleCast(body.position + feet.offset, feet.size, feet.direction,
            0, delta.normalized, filter, hits, delta.magnitude + .02f);
        for (int i = 0; i < count; i++) if (hits[i].rigidbody != body && hits[i].collider != feet) return false;
        return true;
    }

    void Animate(bool walking)
    {
        Sprite[] frames;
        if (Mathf.Abs(facing.x) > Mathf.Abs(facing.y)) frames = facing.x < 0 ? (walking ? walkLeft : idleLeft) : (walking ? walkRight : idleRight);
        else frames = facing.y > 0 ? (walking ? walkUp : idleUp) : (walking ? walkDown : idleDown);
        if (frames == null || frames.Length == 0) frames = idleDown;
        if (frames == null || frames.Length == 0) return;
        Sprite frame = frames[Mathf.FloorToInt(elapsed * 8f) % frames.Length];
        if (frame != null) sprite.sprite = frame;
    }
}
