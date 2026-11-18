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
    Rigidbody2D body;
    BoxCollider2D solidBody;
    readonly RaycastHit2D[] contacts = new RaycastHit2D[16];
    float heardUntil;

    void Awake()
    {
        body = gameObject.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        solidBody = gameObject.AddComponent<BoxCollider2D>();
        solidBody.size = new Vector2(.8f, .85f);
        solidBody.offset = new Vector2(0, -.25f);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (manager != null && manager.IsPlaying && manager.Player != null &&
            collision.rigidbody == manager.Player.GetComponent<Rigidbody2D>() &&
            (investigating || manager.PlayerIsRunning) && Time.time >= nextWarning)
            WarnPlayer();
    }

    void WarnPlayer()
    {
        manager.Reprimand();
        nextWarning = Time.time + 8f;
        investigating = false;
        nextPath = 0;
        route.Clear();
        Play(Vector2.zero);
    }
    string animationState;
    public bool Investigating => investigating;

    void FixedUpdate()
    {
        if (manager == null || !manager.IsPlaying || manager.Player == null) return;
        Vector2 player = manager.Player.transform.position;
        bool hearsPlayer = manager.PlayerIsRunning && Vector2.Distance(body.position, player) <= hearingRadius;
        if (hearsPlayer) heardUntil = Time.time + 3f;
        if (investigating && Time.time >= heardUntil)
        {
            investigating = false;
            nextPath = 0;
        }
        if (!investigating && Time.time >= nextWarning && hearsPlayer)
        {
            investigating = true;
            nextPath = 0;
            manager.Notify("El guardia de seguridad escuchó tus pasos. ¡Camina!");
        }
        Vector2 destination;
        if (investigating)
        {
            destination = player;
            if (Vector2.Distance(transform.position, player) < 1.1f)
            {
                WarnPlayer();
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
        Vector2 from = body.position;
        Vector2 direction = route[0] - from;
        Vector2 next = Vector2.MoveTowards(from, route[0], (investigating ? 3.4f : 1.25f) * Time.fixedDeltaTime);
        Vector2 step = next - from;
        float distance = step.magnitude;
        if (distance > 0f)
        {
            const float clearance = .02f;
            Vector2 heading = step / distance;
            var filter = new ContactFilter2D { useTriggers = false };
            int hits = solidBody.Cast(heading, filter, contacts, distance + clearance);
            for (int i = 0; i < hits; i++)
            {
                if (contacts[i].collider.GetComponentInParent<PlayerMovement>() != manager.Player)
                    continue;
                if (Vector2.Dot(heading, player - from) <= 0f)
                    continue;

                if (Time.time >= nextWarning && (investigating || manager.PlayerIsRunning))
                {
                    WarnPlayer();
                    return;
                }

                distance = Mathf.Min(distance, Mathf.Max(0f, contacts[i].distance - clearance));
            }
            next = from + heading * distance;
        }
        body.MovePosition(next);
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