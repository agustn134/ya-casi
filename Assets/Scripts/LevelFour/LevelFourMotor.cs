using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Movimiento con colisiones, carrera, golpe y empujones del jugador.</summary>
public sealed class LevelFourMotor : MonoBehaviour
{
    public int Health { get; private set; } = 5;
    public bool Sprinting { get; set; }
    public Vector2 Facing { get; private set; } = Vector2.up;
    public Vector2 LastResolvedTarget { get; private set; }
    public readonly LevelFourSocialRules Social = new LevelFourSocialRules();
    Rigidbody2D body;
    Collider2D feet;
    SpriteRenderer appearance;
    readonly RaycastHit2D[] hits = new RaycastHit2D[32];
    Vector2 impulse;
    float impulseTime, nextAttack, invulnerable;

    void Awake() { body = GetComponent<Rigidbody2D>(); feet = GetComponent<CapsuleCollider2D>(); appearance = GetComponent<SpriteRenderer>(); }
    void Update()
    {
        var intro = LevelFourIntro.Active;
        if (intro == null || intro.Paused) return;
        var mission = intro.GetComponent<LevelFourMission>();
        bool allowed = intro.CurrentBeat == LevelFourIntro.Beat.Alley || mission != null && mission.CanMove;
        if (!allowed) return;
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) Attack();
        if (Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame) Attack(true);
        appearance.color = Time.time < invulnerable && Mathf.FloorToInt(Time.time * 12) % 2 == 0 ? new Color(1, .6f, .5f) : Color.white;
    }

    public void Move(Vector2 delta, Vector2 min, Vector2 max)
    {
        if (delta.sqrMagnitude > .00001f) Facing = delta.normalized;
        if (impulseTime > 0) { delta += impulse * Time.fixedDeltaTime; impulseTime -= Time.fixedDeltaTime; }
        Vector2 start = body.position;
        Vector2 target = start;
        target += Sweep(new Vector2(delta.x, 0));
        body.position = target;
        target += Sweep(new Vector2(0, delta.y));
        target = new Vector2(Mathf.Clamp(target.x, min.x, max.x), Mathf.Clamp(target.y, min.y, max.y));
        LastResolvedTarget = target;
        body.position = start;
        body.MovePosition(target);
    }

    Vector2 Sweep(Vector2 delta)
    {
        if (delta.sqrMagnitude < .000001f) return Vector2.zero;
        var mission = LevelFourMission.Active;
        if (mission != null && mission.CampusActive && !mission.Walkable(body.position + delta)) return Vector2.zero;
        float distance = delta.magnitude;
        int count = feet.Cast(delta.normalized, new ContactFilter2D { useTriggers = false }, hits, distance + .035f);
        for (int i = 0; i < count; i++)
        {
            var hit = hits[i];
            if (hit.rigidbody == body || hit.collider == feet) continue;
            if (hit.distance == 0 && Vector2.Dot(delta, (Vector2)hit.collider.bounds.center - (body.position + new Vector2(0, -.5f))) <= 0) continue;
            var student = hit.collider.GetComponent<LevelFourStudent>();
            if (student != null) ContactStudent(student, false);
            distance = Mathf.Min(distance, Mathf.Max(0, hit.distance - .025f));
        }
        return delta.normalized * distance;
    }

    public int ContactStudent(LevelFourStudent student, bool intentional)
    {
        int reaction = Social.Contact(Time.time, intentional || Sprinting);
        var intro = LevelFourIntro.Active;
        if (reaction == 1) intro.Speak(student.transform, "ALUMNO", "¡Fíjate por dónde vas!", 2);
        if (reaction == 2)
        {
            Push((Vector2)transform.position - (Vector2)student.transform.position, 4);
            intro.Speak(student.transform, "ALUMNO", "¡Ya estuvo! Ten cuidado.", 2);
        }
        if (reaction == 3)
        {
            student.Provoke();
            foreach (var friend in student.transform.parent.GetComponentsInChildren<LevelFourStudent>())
                if (friend != student) { friend.Provoke(); break; }
            intro.Speak(student.transform, "ALUMNO", "¡Ahora sí, ven para acá!", 2);
        }
        return reaction;
    }

    public void Push(Vector2 away, float strength) { impulse = away.sqrMagnitude > .001f ? away.normalized * strength : Vector2.down * strength; impulseTime = .24f; }
    public void Damage(Vector2 away)
    {
        var intro = LevelFourIntro.Active;
        if (intro == null || intro.Paused || Time.time < invulnerable) return;
        invulnerable = Time.time + 1.25f; Health = Mathf.Max(0, Health - 1); Push(away, 5);
        if (Health == 0) intro.GetComponent<LevelFourMission>()?.Fail("Te alcanzaron. Necesitas escapar sin recibir tantos golpes.");
    }

    public void Attack(bool shove = false)
    {
        if (Time.time < nextAttack) return;
        nextAttack = Time.time + (shove ? 1.05f : .5f);
        var seen = new HashSet<GameObject>();
        foreach (var collider in Physics2D.OverlapCircleAll(body.position, 1.65f))
        {
            if (collider.attachedRigidbody == body || !seen.Add(collider.gameObject)) continue;
            Vector2 to = (Vector2)collider.bounds.center - (body.position + new Vector2(0, -.4f));
            if (Vector2.Dot(Facing, to.normalized) < -.15f) continue;
            var student = collider.GetComponent<LevelFourStudent>();
            var pursuer = collider.GetComponent<LevelFourPursuer>();
            var fighter = collider.GetComponent<LevelFourFighter>();
            if (student != null) ContactStudent(student, true);
            if (fighter != null) fighter.TakeHit(1, (Vector2)collider.transform.position - body.position, shove);
            else if (pursuer != null) pursuer.Stun(pursuer.isDog ? 2f : 2.7f);
        }
        var go = new GameObject(shove ? "Empujón · derribo" : "Golpe · alcance corto");
        var line = go.AddComponent<LineRenderer>(); line.useWorldSpace = true; line.positionCount = 11; line.widthMultiplier = .06f;
        line.sharedMaterial = new Material(Shader.Find("Sprites/Default")); line.startColor = line.endColor = new Color(1, .83f, .4f); line.sortingOrder = 6000;
        float heading = Mathf.Atan2(Facing.y, Facing.x);
        for (int i = 0; i < 11; i++) { float angle = heading - .8f + i * .16f; line.SetPosition(i, body.position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 1.2f); }
        Destroy(line.sharedMaterial, .18f); Destroy(go, .18f);
    }
}
