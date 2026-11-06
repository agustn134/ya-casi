using UnityEngine;

/// <summary>Pequeños pasos y cambios de orientación alrededor del punto de entrega.</summary>
[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer), typeof(CapsuleCollider2D))]
public sealed class CampusProfessorWaiting : MonoBehaviour
{
    [SerializeField] Transform player;
    [SerializeField] Sprite[] idleDown, idleLeft, idleRight, walkLeft, walkRight;
    [SerializeField, Min(0)] float paceDistance = .3f;
    [SerializeField, Min(.1f)] float paceSpeed = .35f;
    [SerializeField] float phaseOffset;
    Rigidbody2D body;
    SpriteRenderer sprite;
    CapsuleCollider2D feet;
    Vector2 origin;
    float elapsed;
    readonly RaycastHit2D[] hits = new RaycastHit2D[16];

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        sprite = GetComponent<SpriteRenderer>();
        feet = GetComponent<CapsuleCollider2D>();
        origin = body.position;
    }

    void FixedUpdate()
    {
        // Se detiene y mira al jugador para facilitar la interacción.
        if (player != null && Vector2.Distance(player.position, body.position) < 2.3f)
        {
            float direction = player.position.x - transform.position.x;
            Show(Mathf.Abs(direction) < .4f ? idleDown : direction < 0 ? idleLeft : idleRight);
            return;
        }
        elapsed += Time.fixedDeltaTime;
        float phase = Mathf.Repeat(elapsed + phaseOffset, 10f);
        Vector2 target = origin;
        Sprite[] frames;
        if (phase < 2f) frames = idleLeft;
        else if (phase < 4f)
        {
            target.x -= paceDistance;
            frames = walkLeft;
        }
        else if (phase < 6f)
        {
            target.x -= paceDistance;
            frames = idleRight;
        }
        else if (phase < 8f) frames = walkRight;
        else frames = idleDown;

        Vector2 delta = Vector2.MoveTowards(body.position, target, paceSpeed * Time.fixedDeltaTime) - body.position;
        bool moving = delta.sqrMagnitude > .000001f;
        if (moving)
        {
            var filter = new ContactFilter2D();
            filter.useTriggers = false;
            int count = Physics2D.CapsuleCast(body.position + feet.offset, feet.size,
                feet.direction, 0, delta.normalized, filter, hits, delta.magnitude + .01f);
            for (int i = 0; i < count; i++)
                if (hits[i].rigidbody != body && hits[i].collider != feet) moving = false;
            if (moving) body.MovePosition(body.position + delta);
        }
        if (!moving && frames == walkLeft) frames = idleLeft;
        if (!moving && frames == walkRight) frames = idleRight;
        Show(frames);
    }

    void Show(Sprite[] frames)
    {
        if (frames == null || frames.Length == 0) return;
        Sprite frame = frames[Mathf.FloorToInt(elapsed * 8f) % frames.Length];
        if (frame != null) sprite.sprite = frame;
    }
}
