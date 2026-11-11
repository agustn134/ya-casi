using UnityEngine;

/// <summary>Recorre los pasillos y se aparta sin atravesar a otros personajes ni el mobiliario.</summary>
public sealed class CampusStudent : MonoBehaviour
{
    public Vector2[] route;
    public float speed = 1.7f;

    const float RoutePadding = .6f;
    Level01Manager manager;
    Rigidbody2D body;
    Animator animator;
    CapsuleCollider2D feet;
    int next;
    int routeDirection = 1;
    float blockedTime;
    float wait;
    float courtesyReadyAt;
    float temporaryUntil;
    Vector2 temporaryTarget;
    bool hasTemporaryTarget;
    bool courtesyActive;
    readonly RaycastHit2D[] hits = new RaycastHit2D[32];
    readonly Collider2D[] overlaps = new Collider2D[32];

    public Vector2 FeetPosition => body != null ? body.position + FeetOffset : (Vector2)transform.position;
    public bool CanRequestPassage => isActiveAndEnabled && body != null && manager != null &&
        manager.IsPlaying && Time.time >= courtesyReadyAt && !courtesyActive;
    Vector2 FeetOffset => feet != null ? (Vector2)transform.TransformVector(feet.offset) : Vector2.zero;

    public void Initialize(RuntimeAnimatorController controller, Sprite initial, Color tint)
    {
        manager = FindFirstObjectByType<Level01Manager>();
        var sprite = gameObject.AddComponent<SpriteRenderer>();
        sprite.sprite = initial;
        sprite.color = tint;
        sprite.sortingOrder = 2;
        animator = gameObject.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        body = gameObject.AddComponent<Rigidbody2D>();
        body.gravityScale = 0;
        body.freezeRotation = true;
        body.bodyType = RigidbodyType2D.Kinematic;
        body.useFullKinematicContacts = true;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        feet = gameObject.AddComponent<CapsuleCollider2D>();
        feet.size = new Vector2(.55f, .4f);
        feet.offset = new Vector2(0, -.45f);
    }

    /// <summary>Cada alumno decide una vez por petición; no siempre concede el paso.</summary>
    public bool TryYieldTo(Vector2 requester)
    {
        if (!CanRequestPassage || route == null || route.Length < 2)
            return false;

        courtesyReadyAt = Time.time + 5.5f;
        if (Random.value >= .45f || !FindYieldTarget(requester, out Vector2 destination))
            return false;

        temporaryTarget = destination;
        temporaryUntil = Time.time + 2f;
        hasTemporaryTarget = true;
        courtesyActive = true;
        blockedTime = 0;
        wait = 0;
        return true;
    }

    void FixedUpdate()
    {
        if (manager == null || !manager.IsPlaying || body == null || route == null || route.Length < 2)
        {
            StopAnimation();
            return;
        }

        if (hasTemporaryTarget && Time.time >= temporaryUntil)
        {
            hasTemporaryTarget = false;
            courtesyActive = false;
        }

        if (wait > 0)
        {
            wait -= Time.fixedDeltaTime;
            StopAnimation();
            return;
        }

        next = (next + route.Length) % route.Length;
        Vector2 target = hasTemporaryTarget ? temporaryTarget : route[next];
        Vector2 delta = target - body.position;
        if (delta.sqrMagnitude < .01f)
        {
            if (hasTemporaryTarget)
            {
                // Espera a un lado; la petición no sustituye la ruta original.
                StopAnimation();
            }
            else
                next = (next + routeDirection + route.Length) % route.Length;
            return;
        }

        Vector2 destination = Vector2.MoveTowards(body.position, target, speed * Time.fixedDeltaTime);
        if (!IsSafePath(body.position, destination))
        {
            StopAnimation();
            blockedTime += Time.fixedDeltaTime;
            if (courtesyActive)
                return;

            // Esperas distintas y desvíos laterales resuelven encuentros de frente.
            // Nunca se omite una esquina para atajar a través de paredes.
            if (blockedTime > .7f)
            {
                blockedTime = 0;
                hasTemporaryTarget = false;
                if (FindDetour(delta.normalized, out Vector2 detour))
                {
                    temporaryTarget = detour;
                    temporaryUntil = Time.time + 1.4f;
                    hasTemporaryTarget = true;
                }
                else
                {
                    routeDirection = -routeDirection;
                    next = (next + routeDirection + route.Length) % route.Length;
                }
                wait = Random.Range(.12f, .45f);
            }
            return;
        }

        blockedTime = 0;
        Animate(delta.normalized);
        body.MovePosition(destination);
    }

    bool FindYieldTarget(Vector2 requester, out Vector2 destination)
    {
        destination = body.position;
        Vector2 away = FeetPosition - requester;
        if (away.sqrMagnitude < .001f)
            away = Vector2.right;
        away.Normalize();

        float bestScore = float.NegativeInfinity;
        float startDistance = Vector2.Distance(FeetPosition, requester);
        for (int directionIndex = 0; directionIndex < 8; directionIndex++)
        {
            float angle = directionIndex * Mathf.PI / 4;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            for (int distanceIndex = 0; distanceIndex < 3; distanceIndex++)
            {
                float distance = 1f - distanceIndex * .25f;
                Vector2 candidate = body.position + direction * distance;
                float clearance = Vector2.Distance(candidate + FeetOffset, requester);
                if (clearance < startDistance + .15f || !IsSafePath(body.position, candidate))
                    continue;
                float score = clearance + distance * .35f + Vector2.Dot(direction, away) * .1f;
                if (score > bestScore)
                {
                    bestScore = score;
                    destination = candidate;
                }
            }
        }
        return bestScore > float.NegativeInfinity;
    }

    bool FindDetour(Vector2 forward, out Vector2 destination)
    {
        destination = body.position;
        Vector2 side = new Vector2(-forward.y, forward.x);
        if ((GetInstanceID() & 1) == 0)
            side = -side;
        for (int i = 0; i < 4; i++)
        {
            Vector2 direction = i < 2 ? side * (i == 0 ? 1 : -1) :
                (side * (i == 2 ? 1 : -1) - forward * .35f).normalized;
            Vector2 candidate = body.position + direction * .5f;
            if (!IsSafePath(body.position, candidate))
                continue;
            destination = candidate;
            return true;
        }
        return false;
    }

    bool IsSafePath(Vector2 from, Vector2 to)
    {
        // También comprueba el recorrido intermedio al apartarse junto a una esquina.
        int samples = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(from, to) / .15f));
        for (int i = 1; i <= samples; i++)
            if (!WithinRoute(Vector2.Lerp(from, to, (float)i / samples)))
                return false;

        var filter = ContactFilter2D.noFilter;
        filter.useTriggers = false;
        Vector2 size = feet.bounds.size;
        Vector2 offset = FeetOffset;
        Vector2 delta = to - from;
        float distance = delta.magnitude;
        if (distance > .0001f)
        {
            int count = Physics2D.CapsuleCast(from + offset, size, feet.direction, body.rotation,
                delta / distance, filter, hits, distance + .025f);
            if (count >= hits.Length)
                return false;
            for (int i = 0; i < count; i++)
            {
                Collider2D other = hits[i].collider;
                if (!IsObstacle(other))
                    continue;
                // Si la partida empezó con dos alumnos tocándose, pueden separarse.
                if (hits[i].distance <= .001f && SeparatingFrom(other, from + offset, to + offset))
                    continue;
                return false;
            }
        }

        int overlapCount = Physics2D.OverlapCapsule(to + offset, size, feet.direction,
            body.rotation, filter, overlaps);
        if (overlapCount >= overlaps.Length)
            return false;
        for (int i = 0; i < overlapCount; i++)
        {
            Collider2D other = overlaps[i];
            if (IsObstacle(other) && !SeparatingFrom(other, from + offset, to + offset))
                return false;
        }
        return true;
    }

    bool IsObstacle(Collider2D other)
    {
        return other != null && other != feet && other.attachedRigidbody != body &&
            !other.isTrigger && !Physics2D.GetIgnoreCollision(feet, other);
    }

    bool SeparatingFrom(Collider2D other, Vector2 from, Vector2 to)
    {
        // Solo admite salida de un contacto existente, nunca entrar en uno nuevo.
        Vector2 closest = other.ClosestPoint(from);
        if ((closest - from).sqrMagnitude > .0004f && !feet.IsTouching(other))
            return false;
        Vector2 center = other.bounds.center;
        return (to - center).sqrMagnitude > (from - center).sqrMagnitude + .00001f;
    }

    bool WithinRoute(Vector2 position)
    {
        if (route == null || route.Length < 2)
            return false;
        for (int i = 0; i < route.Length; i++)
        {
            Vector2 start = route[i];
            Vector2 segment = route[(i + 1) % route.Length] - start;
            float t = segment.sqrMagnitude > .0001f
                ? Mathf.Clamp01(Vector2.Dot(position - start, segment) / segment.sqrMagnitude) : 0;
            if ((position - (start + segment * t)).sqrMagnitude <= RoutePadding * RoutePadding)
                return true;
        }
        return false;
    }

    void Animate(Vector2 direction)
    {
        if (animator == null || animator.runtimeAnimatorController == null)
            return;
        animator.speed = .8f;
        string side = Mathf.Abs(direction.x) > Mathf.Abs(direction.y)
            ? (direction.x > 0 ? "Right" : "Left") : (direction.y > 0 ? "Up" : "Down");
        if (!animator.GetCurrentAnimatorStateInfo(0).IsName("Walk" + side))
            animator.Play("Base Layer.Walk" + side);
    }

    void StopAnimation()
    {
        if (animator != null)
            animator.speed = 0;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        var player = collision.gameObject.GetComponentInParent<PlayerMovement>();
        if (player == null || manager == null || !manager.IsPlaying)
            return;
        var upgrade = FindFirstObjectByType<LevelOneUpgrade>();
        if (upgrade != null)
            upgrade.BumpPlayer();
    }
}
