using System.Collections.Generic;
using UnityEngine;

/// <summary>Patrulla y busca rutas por el espacio libre antes de acercarse a una fuente de ruido.</summary>
public sealed class LibraryLibrarian : MonoBehaviour
{
    public LibraryLevelManager manager;
    public Animator animator;
    public Transform[] patrol;
    public float hearingRadius = 5f;
    readonly List<Vector2> route = new List<Vector2>();
    int patrolIndex;
    float nextWarning, nextPath;
    bool investigating;
    string animationState;
    public bool Investigating => investigating;

    void Update()
    {
        if (manager == null || !manager.IsPlaying || manager.Player == null) return;
        Vector2 player = manager.Player.transform.position;
        if (!investigating && Time.time >= nextWarning && manager.PlayerIsRunning && Vector2.Distance(transform.position, player) <= hearingRadius)
        {
            investigating = true;
            nextPath = 0;
            manager.Notify("La bibliotecaria escuchó tus pasos. ¡Camina!");
        }
        Vector2 destination;
        if (investigating)
        {
            destination = player;
            if (Vector2.Distance(transform.position, player) < 1.1f)
            {
                manager.Reprimand();
                nextWarning = Time.time + 8f; // Tres segundos detenidos y cinco de protección.
                investigating = false;
                route.Clear();
                Play(Vector2.zero);
                return;
            }
        }
        else
        {
            if (patrol == null || patrol.Length == 0) return;
            destination = patrol[patrolIndex].position;
            if (Vector2.Distance(transform.position, destination) < .25f)
            {
                patrolIndex = (patrolIndex + 1) % patrol.Length;
                destination = patrol[patrolIndex].position;
            }
        }
        if (Time.time >= nextPath)
        {
            FindPath(destination);
            nextPath = Time.time + .6f;
        }
        if (route.Count == 0) { Play(Vector2.zero); return; }
        Vector2 from = transform.position;
        Vector2 direction = route[0] - from;
        Vector2 next = Vector2.MoveTowards(from, route[0], (investigating ? 3.4f : 1.25f) * Time.deltaTime);
        transform.position = new Vector3(next.x, next.y, 0);
        Play(direction);
        if (Vector2.Distance(next, route[0]) < .08f) route.RemoveAt(0);
    }

    void Play(Vector2 direction)
    {
        if (animator == null) return;
        string facing = Mathf.Abs(direction.x) > Mathf.Abs(direction.y) ? (direction.x > 0 ? "Right" : "Left") : (direction.y > 0 ? "Up" : "Down");
        string state = (direction.sqrMagnitude > .001f ? "Walk" : "Idle") + facing;
        if (animationState == state) return;
        animator.Play("Base Layer." + state);
        animationState = state;
    }

    Vector2Int Cell(Vector2 p) => new Vector2Int(Mathf.RoundToInt(p.x * 2), Mathf.RoundToInt(p.y * 2));
    Vector2 World(Vector2Int p) => new Vector2(p.x * .5f, p.y * .5f);
    bool Free(Vector2Int cell)
    {
        if (Mathf.Abs(cell.x) > 28 || Mathf.Abs(cell.y) > 16) return false;
        foreach (var hit in Physics2D.OverlapBoxAll(World(cell), new Vector2(.48f, .48f), 0))
            if (!hit.isTrigger && hit.GetComponentInParent<PlayerMovement>() == null && hit.GetComponentInParent<LibraryLibrarian>() == null) return false;
        return true;
    }
    void FindPath(Vector2 destination)
    {
        route.Clear();
        var start = Cell(transform.position);
        var goal = Cell(destination);
        var queue = new Queue<Vector2Int>();
        var previous = new Dictionary<Vector2Int, Vector2Int>();
        queue.Enqueue(start); previous[start] = start;
        Vector2Int[] directions = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
        var closest = start;
        while (queue.Count > 0 && previous.Count < 5000)
        {
            var current = queue.Dequeue();
            if ((current - goal).sqrMagnitude < (closest - goal).sqrMagnitude) closest = current;
            if (current == goal) { closest = current; break; }
            foreach (var direction in directions)
            {
                var next = current + direction;
                if (previous.ContainsKey(next) || !Free(next)) continue;
                previous[next] = current;
                queue.Enqueue(next);
            }
        }
        var cursor = closest;
        while (cursor != start)
        {
            route.Add(World(cursor)); cursor = previous[cursor];
        }
        route.Reverse();
    }
}
