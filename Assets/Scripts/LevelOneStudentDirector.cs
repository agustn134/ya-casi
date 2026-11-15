using System.Collections.Generic;
using UnityEngine;

/// <summary>Vida del pasillo: recorridos locales, cruces, conversaciones y salida al cierre.</summary>
public sealed class LevelOneStudentDirector : MonoBehaviour
{
    sealed class StudentEntry
    {
        public CampusStudent student;
        public bool conversing;
        public bool leaving;
        public float nextConversation;
        public float baseSpeed;
    }

    readonly List<StudentEntry> students = new List<StudentEntry>();
    readonly Collider2D[] spawnHits = new Collider2D[32];
    Level01Manager manager;
    PlayerMovement player;
    U2GameConfig config;
    Transform crowdRoot;
    float minX, maxX, upper, lower, middle;
    bool returnRush;

    public void Configure(Level01Manager levelManager, PlayerMovement actor, U2GameConfig settings,
        LockerInteraction[] topRow, LockerInteraction[] bottomRow)
    {
        manager = levelManager; player = actor; config = settings;
        if (player == null || topRow == null || topRow.Length < 2) return;
        if (crowdRoot != null) return;

        minX = float.PositiveInfinity; maxX = float.NegativeInfinity;
        float topY = 0;
        foreach (var locker in topRow)
        {
            Vector2 p = locker.InteractionPosition;
            minX = Mathf.Min(minX, p.x); maxX = Mathf.Max(maxX, p.x); topY += p.y;
        }
        topY /= topRow.Length;
        float bottomY = topY - 3.2f;
        if (bottomRow != null && bottomRow.Length > 0)
        {
            bottomY = 0;
            foreach (var locker in bottomRow) bottomY += locker.InteractionPosition.y;
            bottomY /= bottomRow.Length;
        }
        // Se deja libre el centro y se alejan los pies de los casilleros.
        upper = Mathf.Max(topY, bottomY) - .85f;
        lower = Mathf.Min(topY, bottomY) + .50f;
        if (upper - lower < 1.1f) { middle = (upper + lower) * .5f; upper = middle + .55f; lower = middle - .55f; }
        middle = (upper + lower) * .5f;
        minX += .6f; maxX -= .6f;
        if (maxX - minX < 3) return;
        crowdRoot = new GameObject("Estudiantes del pasillo").transform;
        crowdRoot.SetParent(transform, false);
        Physics2D.SyncTransforms();

        // Cuatro recorridos distribuidos, sin enviar a todos al mismo extremo.
        for (int i = 0; i < 4; i++)
        {
            float center = Mathf.Lerp(minX, maxX, .10f + i * .25f);
            float a = Mathf.Max(minX, center - 2), b = Mathf.Min(maxX, center + 2);
            var route = Rectangle(a, b, lower, upper, i % 2 == 0);
            Spawn("Estudiante de recorrido " + (i + 1), route, 1.05f + i * .08f, false);
        }
        // Cruzan en lugares distintos y a una velocidad que permite rodearlos.
        for (int i = 0; i < 2; i++)
        {
            float x = Mathf.Lerp(minX, maxX, i == 0 ? .27f : .69f);
            Spawn("Estudiante que cruza " + (i + 1), Rectangle(x - .55f, x + .55f, lower, upper, i == 0), 1.12f, false);
        }
        // Grupos de tres cerca de los bordes: caminan un tramo y vuelven a conversar.
        for (int group = 0; group < 2; group++)
        {
            float center = Mathf.Lerp(minX, maxX, group == 0 ? .43f : .83f);
            float lane = group == 0 ? upper : lower;
            float inner = Mathf.MoveTowards(lane, middle, .38f);
            for (int member = 0; member < 3; member++)
            {
                float x = Mathf.Clamp(center + (member - 1) * .90f, minX + .5f, maxX - .5f);
                var route = Rectangle(x - .32f, x + .32f, Mathf.Min(lane, inner), Mathf.Max(lane, inner), member % 2 == 0);
                Spawn("Grupo " + (group + 1) + " · estudiante " + (member + 1), route, .80f + member * .06f, true);
            }
        }
        Spawn("Estudiante con prisa", Rectangle(minX, maxX, lower + .30f, upper - .30f, false), 1.85f, false);
    }

    void Update()
    {
        if (manager == null || !manager.IsPlaying) return;
        foreach (var entry in students)
        {
            if (entry.student == null || !entry.conversing || entry.leaving || Time.time < entry.nextConversation) continue;
            entry.student.PauseFor(1.6f + (entry.student.GetInstanceID() & 3) * .25f);
            entry.nextConversation = Time.time + 5.5f + (entry.student.GetInstanceID() & 3) * .60f;
        }
    }

    public void BeginReturnRush()
    {
        if (returnRush || manager == null || !manager.HasUSB) return;
        returnRush = true;
        var exit = FindFirstObjectByType<LevelExit>();
        var exitCollider = exit != null ? exit.GetComponent<Collider2D>() : null;
        if (exitCollider == null) return;
        Vector2 door = exitCollider.bounds.center;
        float doorLane = Mathf.Min(.18f, exitCollider.bounds.extents.y * .25f);
        int departing = 0;
        foreach (var entry in students)
        {
            if (entry.conversing || entry.student == null) continue;
            var student = entry.student;
            Vector2 start = student.transform.position;
            // Las posiciones finales quedan separadas y fuera de la boca de la puerta.
            float parkingX = door.x - 2.0f - departing * .85f;
            float parkingY = door.y + (departing % 2 == 0 ? -.20f : .20f);
            Vector2[] path =
            {
                start,
                new Vector2(start.x, middle),
                new Vector2(Mathf.Max(minX, door.x + 1.1f), middle),
                new Vector2(door.x + .9f, door.y + doorLane),
                new Vector2(door.x - .9f, door.y + doorLane),
                new Vector2(parkingX, parkingY)
            };
            student.speed = Mathf.Clamp(entry.baseSpeed * 1.15f, 1.2f, 2.05f);
            student.SetRoute(path, true);
            student.PauseFor(.5f + departing * 1.7f);
            entry.leaving = true;
            if (++departing == 3) break;
        }
    }

    void Spawn(string caption, Vector2[] route, float speed, bool conversation)
    {
        if (!TryFindSpawn(route, out Vector2 spawn)) return;
        var go = new GameObject(caption);
        go.transform.SetParent(crowdRoot, false); go.transform.position = spawn;
        var actorAnimator = player.GetComponent<Animator>();
        var actorSprite = player.GetComponentInChildren<SpriteRenderer>();
        RuntimeAnimatorController controller = null;
        if (config != null && config.studentControllers != null && config.studentControllers.Length > 0)
            controller = config.studentControllers[students.Count % config.studentControllers.Length];
        if (controller == null && actorAnimator != null) controller = actorAnimator.runtimeAnimatorController;
        var student = go.AddComponent<CampusStudent>();
        student.speed = speed;
        student.Initialize(controller, actorSprite != null ? actorSprite.sprite : null, Color.white);
        var points = new List<Vector2> { spawn };
        points.AddRange(route);
        student.SetRoute(points.ToArray());
        var gateObject = GameObject.Find("SalidaNivel01");
        var gate = gateObject != null ? gateObject.GetComponent<Collider2D>() : null;
        if (gate != null) Physics2D.IgnoreCollision(student.GetComponent<Collider2D>(), gate);
        students.Add(new StudentEntry
        {
            student = student, conversing = conversation, baseSpeed = speed,
            nextConversation = Time.time + 1.5f + (students.Count % 3) * 1.1f
        });
        Physics2D.SyncTransforms();
    }

    bool TryFindSpawn(Vector2[] route, out Vector2 spawn)
    {
        var filter = ContactFilter2D.noFilter; filter.useTriggers = false;
        for (int segment = 0; segment < route.Length; segment++)
        {
            for (int step = 0; step < 4; step++)
            {
                Vector2 point = Vector2.Lerp(route[segment], route[(segment + 1) % route.Length], step / 4f);
                int count = Physics2D.OverlapCapsule(point + Vector2.down * .45f,
                    new Vector2(.70f, .50f), CapsuleDirection2D.Vertical, 0, filter, spawnHits);
                if (count == 0) { spawn = point; return true; }
            }
        }
        spawn = Vector2.zero;
        return false;
    }

    static Vector2[] Rectangle(float left, float right, float bottom, float top, bool clockwise)
    {
        var points = new[] { new Vector2(left, top), new Vector2(right, top), new Vector2(right, bottom), new Vector2(left, bottom) };
        if (!clockwise) System.Array.Reverse(points);
        return points;
    }
}
