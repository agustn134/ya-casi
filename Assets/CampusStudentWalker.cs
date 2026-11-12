using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>Compañero con colisión, rutas por el camino y pausas para conversar.</summary>
[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer), typeof(CapsuleCollider2D))]
public sealed class CampusStudentWalker : MonoBehaviour
{
    [SerializeField] Tilemap path;
    [SerializeField] Sprite[] idleDown, idleUp, idleLeft, idleRight;
    [SerializeField] Sprite[] walkDown, walkUp, walkLeft, walkRight;
    [SerializeField] float speed = 1.1f;
    [SerializeField] int groupId, formationSlot;
    [SerializeField] Vector2 patrolCenter;
    [SerializeField, Min(2)] int patrolRange = 14;
    [SerializeField] int seed = 1;
    Rigidbody2D body;
    CapsuleCollider2D feet;
    SpriteRenderer sprite;
    System.Random random;
    Vector2 target;
    Vector2 lastDirection = Vector2.down;
    Vector3Int previousCell;
    float avoidUntil;
    Vector3Int goal;
    readonly Queue<Vector2> route = new Queue<Vector2>();
    static readonly Dictionary<Tilemap, HashSet<Vector3Int>> Networks = new Dictionary<Tilemap, HashSet<Vector3Int>>();
    static readonly Dictionary<int, Vector3Int> GroupGoals = new Dictionary<int, Vector3Int>();
    static readonly HashSet<CampusStudentWalker> Actors = new HashSet<CampusStudentWalker>();
    HashSet<Vector3Int> network;
    List<Vector3Int> destinations;
    float pause, blocked, animationTime;
    readonly RaycastHit2D[] hits = new RaycastHit2D[16];
    readonly Vector3Int[] neighbors = { Vector3Int.up, Vector3Int.right, Vector3Int.down, Vector3Int.left };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetCaches() { Networks.Clear(); GroupGoals.Clear(); Actors.Clear(); }

    void OnEnable() { Actors.Add(this); }
    void OnDisable() { Actors.Remove(this); }

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        feet = GetComponent<CapsuleCollider2D>();
        sprite = GetComponent<SpriteRenderer>();
        random = new System.Random(seed);
        target = body.position;
        pause = (float)random.NextDouble() * .25f;
        if (path == null)
        {
            Debug.LogError("Estudiante sin Camino_Tilemap asignado.", this);
            enabled = false;
            return;
        }
        if (!Networks.TryGetValue(path, out network))
        {
            network = new HashSet<Vector3Int>();
            var roofs = GameObject.Find("Edificio_Tilemap")?.GetComponent<Tilemap>();
            foreach (var cell in path.cellBounds.allPositionsWithin)
                if (path.HasTile(cell) && (roofs == null || !roofs.HasTile(roofs.WorldToCell(path.GetCellCenterWorld(cell)))))
                    network.Add(cell);
            Networks.Add(path, network);
        }
        // Cada grupo o estudiante cubre un sector; el conjunto cubre toda la universidad.
        // La distancia se mide por el camino, sin atravesar jardines ni edificios.
        var home = path.WorldToCell(patrolCenter);
        if (!network.Contains(home)) home = path.WorldToCell(body.position + feet.offset);
        var frontier = new Queue<Vector3Int>();
        var distance = new Dictionary<Vector3Int, int>();
        frontier.Enqueue(home); distance[home] = 0;
        while (frontier.Count > 0)
        {
            var cell = frontier.Dequeue();
            if (distance[cell] >= patrolRange) continue;
            foreach (var direction in neighbors)
            {
                var next = cell + direction;
                if (!network.Contains(next) || distance.ContainsKey(next)) continue;
                distance[next] = distance[cell] + 1; frontier.Enqueue(next);
            }
        }
        destinations = new List<Vector3Int>(distance.Keys);
        destinations.Sort((a, b) => a.y == b.y ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
    }

    void FixedUpdate()
    {
        animationTime += Time.fixedDeltaTime;
        if (pause > 0)
        {
            pause -= Time.fixedDeltaTime;
            Animate(false);
            return;
        }
        if (Vector2.Distance(body.position, target) < .025f)
        {
            ChooseTarget();
            Animate(false);
            return;
        }
        Vector2 delta = Vector2.MoveTowards(body.position, target, speed * Time.fixedDeltaTime) - body.position;
        if (Clear(delta))
        {
            body.MovePosition(body.position + delta);
            lastDirection = delta.normalized;
            blocked = 0;
            Animate(true);
        }
        else
        {
            Animate(false);
            blocked += Time.fixedDeltaTime;
            if (blocked < .5f) return;
            // Cambia de rumbo para evitar quedarse empujando al jugador o a otro compañero.
            previousCell = path.WorldToCell(target + feet.offset);
            avoidUntil = Time.time + 3.5f;
            target = body.position;
            route.Clear();
            Detour();
            pause = .08f + (float)random.NextDouble() * .2f;
            blocked = 0;
        }
    }

    void ChooseTarget()
    {
        if (route.Count > 0) { target = route.Dequeue(); return; }
        Vector3Int cell = path.WorldToCell(body.position + feet.offset);
        if (!network.Contains(cell) || destinations.Count == 0) return;
        if (groupId > 0 && GroupGoals.TryGetValue(groupId, out Vector3Int common)
            && destinations.Contains(common) && Vector3Int.Distance(cell, common) > 2)
            goal = common;
        else
        {
            float best = float.NegativeInfinity;
            for (int attempt = 0; attempt < 24; attempt++)
            {
                var candidate = destinations[random.Next(destinations.Count)];
                float travel = Vector3Int.Distance(cell, candidate);
                if (travel < 3) continue;
                float score = (float)random.NextDouble() * 5f - Traffic(candidate) * 4f;
                if (score <= best) continue;
                best = score; goal = candidate;
            }
            if (float.IsNegativeInfinity(best)) goal = destinations[random.Next(destinations.Count)];
            if (groupId > 0) GroupGoals[groupId] = goal;
        }
        if (groupId > 0)
            for (int i = 0; i < 4; i++)
            {
                var slot = goal + neighbors[(formationSlot + i) % 4];
                if (network.Contains(slot)) { goal = slot; break; }
            }
        // A* con coste de tráfico: una ruta algo más larga evita las filas del cruce central.
        var frontier = new List<Vector3Int>();
        var parents = new Dictionary<Vector3Int, Vector3Int>();
        var costs = new Dictionary<Vector3Int, float>();
        var traffic = new Dictionary<Vector3Int, float>();
        frontier.Add(cell); parents[cell] = cell; costs[cell] = 0;
        while (frontier.Count > 0)
        {
            int selected = 0;
            for (int i = 1; i < frontier.Count; i++)
                if (costs[frontier[i]] + Manhattan(frontier[i], goal)
                    < costs[frontier[selected]] + Manhattan(frontier[selected], goal)) selected = i;
            var current = frontier[selected]; frontier.RemoveAt(selected);
            if (current == goal) break;
            for (int i = 0; i < 4; i++)
            {
                var next = current + neighbors[(i + seed % 4) % 4];
                if (!network.Contains(next)) continue;
                if (Time.time < avoidUntil && next == previousCell) continue;
                if (!traffic.TryGetValue(next, out float congestion))
                {
                    congestion = Traffic(next); traffic[next] = congestion;
                }
                float cost = costs[current] + 1 + congestion * 3;
                if (costs.TryGetValue(next, out float old) && cost >= old) continue;
                costs[next] = cost; parents[next] = current;
                if (!frontier.Contains(next)) frontier.Add(next);
            }
        }
        if (!parents.ContainsKey(goal))
        {
            // Las canchas desconectadas del camino no pueden ser destinos del grupo.
            var reachable = new List<Vector3Int>(parents.Keys);
            if (reachable.Count < 2) { Detour(); return; }
            goal = reachable[random.Next(reachable.Count)];
            if (groupId > 0) GroupGoals[groupId] = goal;
        }
        var reverse = new Stack<Vector3Int>();
        for (var current = goal; current != cell; current = parents[current]) reverse.Push(current);
        Vector2 center = (Vector2)path.GetCellCenterWorld(cell) - feet.offset;
        if (Vector2.Distance(center, body.position) > .05f) route.Enqueue(center);
        while (reverse.Count > 0) route.Enqueue((Vector2)path.GetCellCenterWorld(reverse.Pop()) - feet.offset);
        if (route.Count > 0) target = route.Dequeue();
    }

    static int Manhattan(Vector3Int a, Vector3Int b) { return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y); }

    float Traffic(Vector3Int cell)
    {
        Vector2 center = path.GetCellCenterWorld(cell);
        float cost = 0;
        foreach (var actor in Actors)
        {
            if (actor == this || actor.body == null || actor.path != path) continue;
            if (Vector2.Distance(actor.body.position + actor.feet.offset, center) < 1.25f) cost += 1;
            if (Vector2.Distance(actor.target + actor.feet.offset, center) < .5f) cost += .5f;
        }
        return cost;
    }

    void Detour()
    {
        var cell = path.WorldToCell(body.position + feet.offset);
        int first = random.Next(4);
        for (int i = 0; i < 4; i++)
        {
            var next = cell + neighbors[(first + i) % 4];
            if (!network.Contains(next) || next == previousCell) continue;
            Vector2 candidate = (Vector2)path.GetCellCenterWorld(next) - feet.offset;
            if (!Clear(Vector2.ClampMagnitude(candidate - body.position, speed * Time.fixedDeltaTime))) continue;
            target = candidate;
            return;
        }
        target = body.position;
    }

    bool OnPath(Vector2 position)
    {
        Vector2 center = position + feet.offset;
        Vector2 half = feet.size * .5f;
        for (int x = -1; x <= 1; x++)
            for (int y = -1; y <= 1; y++)
                if (!network.Contains(path.WorldToCell(center + new Vector2(x * half.x, y * half.y)))) return false;
        return true;
    }

    bool Clear(Vector2 delta)
    {
        // Muestras intermedias también protegen frente a cambios de velocidad en Inspector.
        int steps = Mathf.Max(1, Mathf.CeilToInt(delta.magnitude / .05f));
        for (int i = 1; i <= steps; i++)
            if (!OnPath(body.position + delta * ((float)i / steps))) return false;
        if (delta.sqrMagnitude < .0000001f) return true;
        var filter = new ContactFilter2D();
        filter.useTriggers = false;
        int count = Physics2D.CapsuleCast(body.position + feet.offset, feet.size,
            feet.direction, 0f, delta.normalized, filter, hits, delta.magnitude + .025f);
        for (int i = 0; i < count; i++)
            if (hits[i].collider != feet && hits[i].rigidbody != body) return false;
        return true;
    }

    void Animate(bool walking)
    {
        Sprite[] frames;
        if (Mathf.Abs(lastDirection.x) > Mathf.Abs(lastDirection.y))
            frames = lastDirection.x > 0 ? (walking ? walkRight : idleRight) : (walking ? walkLeft : idleLeft);
        else frames = lastDirection.y > 0 ? (walking ? walkUp : idleUp) : (walking ? walkDown : idleDown);
        if (frames != null && frames.Length > 0)
            sprite.sprite = frames[Mathf.FloorToInt(animationTime * (walking ? 8 : 4)) % frames.Length];
    }
}
