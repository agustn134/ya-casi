using System.Collections.Generic;
using UnityEngine;

/// <summary>Patrulla, visión o ruido, persecución, pérdida de rastro y aturdimiento.</summary>
public sealed class LevelFourPursuer : MonoBehaviour
{
    public bool isDog;
    public Texture2D idleSheet, runSheet;
    public Vector2 home;
    public bool Chasing { get; private set; }
    public bool IsStunned => stun > 0;
    SpriteRenderer visual;
    Rigidbody2D body;
    Sprite[][] idle, run;
    LineRenderer vision;
    Vector2 facing = Vector2.down;
    List<Vector2> path = new List<Vector2>();
    float stun, clock, repath, lost, patrol;
    int patrolCorner;
    Material visionMaterial;

    void Awake()
    {
        body = GetComponent<Rigidbody2D>(); visual = GetComponent<SpriteRenderer>();
        if (!isDog) { idle = Frames(idleSheet); run = Frames(runSheet); }
        var go = new GameObject(isDog ? "Alcance del perro" : "Cono de visión del guardia"); go.transform.SetParent(transform, false);
        vision = go.AddComponent<LineRenderer>(); vision.useWorldSpace = false; vision.positionCount = 12; vision.widthMultiplier = .025f; vision.sortingOrder = 80;
        visionMaterial = new Material(Shader.Find("Sprites/Default")); vision.sharedMaterial = visionMaterial;
    }

    Sprite[][] Frames(Texture2D sheet)
    {
        var array = new Sprite[4][];
        int perDirection = sheet.width / 16 / 4;
        for (int d = 0; d < 4; d++)
        {
            array[d] = new Sprite[perDirection];
            for (int i = 0; i < perDirection; i++) array[d][i] = Sprite.Create(sheet, new Rect((d * perDirection + i) * 16, 0, 16, sheet.height), new Vector2(.5f, .4f), 16);
        }
        return array;
    }

    public void Stun(float seconds)
    {
        stun = Mathf.Max(stun, seconds); path.Clear();
        LevelFourMission.Active?.Feedback(isDog ? "Perro detenido momentáneamente. ¡Corre!" : "Guardia aturdido. ¡Aprovecha para escapar!");
    }

    void FixedUpdate()
    {
        var mission = LevelFourMission.Active;
        if (mission == null || mission.Intro.Paused) return;
        clock += Time.fixedDeltaTime;
        if (stun > 0) { stun -= Time.fixedDeltaTime; visual.color = new Color(.65f, .75f, 1); vision.enabled = false; return; }
        visual.color = Color.white; vision.enabled = mission.CampusActive;
        var fighter = GetComponent<LevelFourFighter>();
        if (fighter != null && fighter.ControlsMotion) { vision.enabled = false; return; }
        if (!mission.CampusActive) { Chasing = false; return; }
        Vector2 player = mission.Intro.player.position;
        Vector2 toward = player - body.position;
        float distance = toward.magnitude;
        bool sees = Sees(player, mission);
        if (sees || isDog && mission.Running && distance < 11)
        {
            if (!Chasing) mission.Feedback(isDog ? "¡Un perro te detectó! Corre o detenlo con Espacio." : "¡Te vio un guardia! Busca cómo perderlo.");
            Chasing = true; lost = 0;
        }
        else if (Chasing) { lost += Time.fixedDeltaTime; if (lost > (isDog ? 8 : 6) || distance > 24) { Chasing = false; path.Clear(); } }
        Vector2 destination;
        if (Chasing)
        {
            destination = player;
            if (distance < (isDog ? .85f : 1.05f))
            {
                mission.Intro.player.GetComponent<LevelFourMotor>().Damage(toward);
                DrawVision(); Animate(Vector2.zero); return;
            }
        }
        else
        {
            patrol -= Time.fixedDeltaTime;
            if (patrol <= 0) { patrol = 4.5f; patrolCorner = (patrolCorner + 1) % 4; }
            Vector2[] offsets = { new Vector2(-3, 0), new Vector2(0, 3), new Vector2(3, 0), new Vector2(0, -3) };
            destination = home + offsets[patrolCorner];
            if (!mission.Walkable(destination)) destination = home;
        }
        repath -= Time.fixedDeltaTime;
        if (repath <= 0) { repath = Chasing ? .4f : 1.5f; path = LevelFourNavigation.Find(body.position, destination, mission); }
        Vector2 delta = Vector2.zero;
        if (path.Count > 0)
        {
            Vector2 target = path[0];
            if (Vector2.Distance(body.position, target) < .16f) path.RemoveAt(0);
            else
            {
                delta = Vector2.MoveTowards(body.position, target, (Chasing ? isDog ? 5.3f : 4f : 1.55f) * Time.fixedDeltaTime) - body.position;
                if (mission.Walkable(body.position + delta)) body.MovePosition(body.position + delta);
            }
        }
        if (delta.sqrMagnitude > .00001f) facing = delta.normalized;
        Animate(delta); DrawVision();
        visual.sortingOrder = Mathf.RoundToInt(1000 - (body.position.y - .5f) * 10);
    }

    public bool Sees(Vector2 target, LevelFourMission mission)
    {
        Vector2 difference = target - body.position;
        float distance = difference.magnitude;
        if (distance > (isDog ? 8 : 9)) return false;
        if (!isDog && distance > 1.8f && Vector2.Dot(facing, difference.normalized) < .64f) return false;
        for (float d = .6f; d < distance; d += .4f)
            if (!mission.Walkable(body.position + difference.normalized * d)) return false;
        return true;
    }

    void DrawVision()
    {
        if (vision == null) return;
        float heading = Mathf.Atan2(facing.y, facing.x), radius = isDog ? 8 : 9;
        vision.startColor = vision.endColor = Chasing ? new Color(1, .2f, .1f, .45f) : new Color(1, .85f, .3f, .18f);
        vision.SetPosition(0, Vector3.zero);
        for (int i = 1; i < 11; i++) { float angle = heading - .85f + (i - 1) * 1.7f / 9; vision.SetPosition(i, new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius); }
        vision.SetPosition(11, Vector3.zero);
    }

    void Animate(Vector2 delta)
    {
        if (isDog) { transform.localScale = new Vector3(facing.x < 0 ? -1 : 1, 1 + Mathf.Sin(clock * 18) * (delta.sqrMagnitude > 0 ? .035f : 0), 1); return; }
        int d = Mathf.Abs(facing.x) > Mathf.Abs(facing.y) ? facing.x > 0 ? 2 : 0 : facing.y > 0 ? 1 : 3;
        var frames = delta.sqrMagnitude > .000001f ? run[d] : idle[d];
        visual.sprite = frames[Mathf.FloorToInt(clock * (Chasing ? 9 : 5)) % frames.Length];
    }

    void OnDestroy()
    {
        foreach (var array in new[] { idle, run }) if (array != null) foreach (var direction in array) foreach (var sprite in direction) if (sprite != null) Destroy(sprite);
        if (visionMaterial != null) Destroy(visionMaterial);
    }
}

public static class LevelFourNavigation
{
    public static List<Vector2> Find(Vector2 from, Vector2 to, LevelFourMission mission)
    {
        var start = Vector2Int.RoundToInt(from); var goal = Vector2Int.RoundToInt(to);
        var open = new List<Vector2Int> { start };
        var came = new Dictionary<Vector2Int, Vector2Int>(); var cost = new Dictionary<Vector2Int, float> { [start] = 0 };
        Vector2Int[] dirs = { Vector2Int.left, Vector2Int.right, Vector2Int.up, Vector2Int.down };
        var nearest = start; float best = Vector2Int.Distance(start, goal);
        for (int iterations = 0; open.Count > 0 && iterations < 1800; iterations++)
        {
            int chosen = 0; float score = float.MaxValue;
            for (int i = 0; i < open.Count; i++) { float candidate = cost[open[i]] + Vector2Int.Distance(open[i], goal); if (candidate < score) { score = candidate; chosen = i; } }
            var current = open[chosen]; open.RemoveAt(chosen);
            float distance = Vector2Int.Distance(current, goal);
            if (distance < best) { best = distance; nearest = current; }
            if (current == goal) { nearest = goal; break; }
            foreach (var direction in dirs)
            {
                var next = current + direction;
                if (Vector2Int.Distance(next, start) > 22 || !mission.Walkable(next)) continue;
                float nextCost = cost[current] + 1;
                if (cost.TryGetValue(next, out float previous) && previous <= nextCost) continue;
                cost[next] = nextCost; came[next] = current;
                if (!open.Contains(next)) open.Add(next);
            }
        }
        var result = new List<Vector2>();
        while (nearest != start && came.TryGetValue(nearest, out var previous)) { result.Add(nearest); nearest = previous; }
        result.Reverse(); return result;
    }
}

