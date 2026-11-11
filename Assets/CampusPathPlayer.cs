using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

/// <summary>Movimiento del nivel 3 limitado al camino pintado y a sus obstáculos físicos.</summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Animator), typeof(CapsuleCollider2D))]
public sealed class CampusPathPlayer : MonoBehaviour
{
    [SerializeField] Tilemap path;
    [SerializeField] Transform spawn;
    [SerializeField] Camera followCamera;
    [SerializeField] float walkSpeed = 3f;
    [SerializeField] float runSpeed = 5f;
    [Header("Esquive")]
    [SerializeField] float dodgeSpeed = 9f;
    [SerializeField] float dodgeDuration = .16f;
    [SerializeField] float dodgeCooldown = 1.8f;
    Vector2 dodgeDirection, lastDirection = Vector2.down;
    float dodgeUntil, nextDodge, immuneUntil, slowUntil, shakeUntil;
    Vector3 cameraBase;
    SpriteRenderer appearance;
    Color originalColor = Color.white;
    public bool IsInvulnerable => Time.time < dodgeUntil || Time.time < immuneUntil;
    public float DodgeReadyIn => Mathf.Max(0, nextDodge - Time.time);
    public Vector2 FeetPosition => body != null && feet != null ? body.position + feet.offset : (Vector2)transform.position;
    public Tilemap Path => path;
    public Camera ViewCamera => followCamera;

    public void TryDodge()
    {
        if (Time.timeScale == 0 || body == null || Time.time < nextDodge) return;
        dodgeDirection = input.sqrMagnitude > .01f ? input.normalized : lastDirection;
        dodgeUntil = Time.time + dodgeDuration;
        nextDodge = Time.time + dodgeCooldown;
    }

    public void ApplyImpactFeedback()
    {
        immuneUntil = Time.time + 1f;
        slowUntil = Time.time + .35f;
        shakeUntil = Time.time + .18f;
    }
    Rigidbody2D body;
    CapsuleCollider2D feet;
    Animator animator;
    Vector2 input;
    bool sprint;
    string facing = "Down", state;
    readonly RaycastHit2D[] hits = new RaycastHit2D[16];

    public void ConfigurePath(Tilemap tilemap) { path = tilemap; }

    void Start()
    {
        body = GetComponent<Rigidbody2D>();
        feet = GetComponent<CapsuleCollider2D>();
        animator = GetComponent<Animator>();
        if (path == null || followCamera == null || spawn == null)
        {
            Debug.LogError("Nivel 3: asigna Camino_Tilemap, Inicio_Jugador y cámara.", this);
            enabled = false;
            return;
        }
        Vector2 desired = (Vector2)spawn.position - feet.offset;
        if (!OnPath(desired))
        {
            // Recuperación si se mueve el marcador fuera del camino durante la edición.
            float nearest = float.PositiveInfinity;
            bool found = false;
            foreach (Vector3Int cell in path.cellBounds.allPositionsWithin)
            {
                if (!path.HasTile(cell)) continue;
                Vector2 candidate = (Vector2)path.GetCellCenterWorld(cell) - feet.offset;
                float distance = (candidate - desired).sqrMagnitude;
                if (distance >= nearest || !OnPath(candidate)) continue;
                nearest = distance;
                desired = candidate;
                found = true;
            }
            if (!found)
            {
                Debug.LogError("Nivel 3: no hay espacio válido para el jugador en Camino_Tilemap.", this);
                enabled = false;
                return;
            }
        }
        body.position = desired;
        followCamera.transform.position = new Vector3(desired.x, desired.y, -10f);
        cameraBase = followCamera.transform.position;
        appearance = GetComponent<SpriteRenderer>();
        if (appearance != null) originalColor = appearance.color;
        Animate(Vector2.zero);
    }

    void Update()
    {
        input = Vector2.zero;
        sprint = false;
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            input.x = ((keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) ? 1 : 0)
                - ((keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) ? 1 : 0);
            input.y = ((keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) ? 1 : 0)
                - ((keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) ? 1 : 0);
            sprint = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
        }
        if (Gamepad.current != null)
        {
            Vector2 stick = Gamepad.current.leftStick.ReadValue();
            if (stick.sqrMagnitude > .04f) input = stick;
            sprint |= Gamepad.current.rightTrigger.isPressed;
        }
        if (TouchGameControls.Direction.sqrMagnitude > .01f) input = TouchGameControls.Direction;
        sprint |= TouchGameControls.Sprint;
        input = Vector2.ClampMagnitude(input, 1f);
        if (input.sqrMagnitude > .01f) lastDirection = input.normalized;
        if ((keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
            || (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame)) TryDodge();
        if (appearance != null)
            appearance.color = Time.time < immuneUntil && Mathf.FloorToInt(Time.time * 16) % 2 == 0
                ? new Color(1, .4f, .35f, .7f) : Time.time < dodgeUntil ? new Color(.6f, 1, 1) : originalColor;
    }

    void FixedUpdate()
    {
        Vector2 origin = body.position;
        bool dodging = Time.time < dodgeUntil;
        Vector2 velocity = dodging ? dodgeDirection * dodgeSpeed : input * (sprint ? runSpeed : walkSpeed);
        if (!dodging && Time.time < slowUntil) velocity *= .55f;
        Vector2 delta = velocity * Time.fixedDeltaTime;
        Vector2 next = origin;
        // Subpasos impiden atravesar huecos incluso con velocidades altas.
        int steps = Mathf.Max(1, Mathf.CeilToInt(delta.magnitude / .05f));
        delta /= steps;
        for (int i = 0; i < steps; i++)
        {
            if (CanMove(next, delta)) next += delta;
            else
            {
                Vector2 horizontal = new Vector2(delta.x, 0);
                Vector2 vertical = new Vector2(0, delta.y);
                if (CanMove(next, horizontal)) next += horizontal;
                if (CanMove(next, vertical)) next += vertical;
            }
        }
        body.MovePosition(next);
        Animate(next - origin);
    }

    bool OnPath(Vector2 position)
    {
        Vector2 center = position + feet.offset;
        Vector2 half = feet.size * .5f;
        // La huella completa de los pies debe permanecer sobre el camino.
        for (int x = -1; x <= 1; x++)
            for (int y = -1; y <= 1; y++)
                if (!path.HasTile(path.WorldToCell(center + new Vector2(x * half.x, y * half.y))))
                    return false;
        return true;
    }

    bool CanMove(Vector2 position, Vector2 delta)
    {
        if (delta.sqrMagnitude < .0000001f) return false;
        if (!OnPath(position + delta)) return false;
        var filter = new ContactFilter2D();
        filter.useTriggers = false;
        int count = Physics2D.CapsuleCast(position + feet.offset, feet.size,
            feet.direction, 0f, delta.normalized, filter, hits, delta.magnitude + .01f);
        for (int i = 0; i < count; i++)
            if (hits[i].collider != feet && hits[i].rigidbody != body) return false;
        return true;
    }

    void Animate(Vector2 movement)
    {
        bool moving = movement.sqrMagnitude > .000001f;
        if (moving)
            facing = Mathf.Abs(movement.x) > Mathf.Abs(movement.y)
                ? (movement.x > 0 ? "Right" : "Left") : (movement.y > 0 ? "Up" : "Down");
        string next = (moving ? "Walk" : "Idle") + facing;
        animator.speed = moving && sprint ? 1.5f : 1f;
        if (state == next) return;
        animator.Play("Base Layer." + next, 0, 0f);
        state = next;
    }

    void LateUpdate()
    {
        if (followCamera == null || body == null) return;
        Vector3 target = new Vector3(transform.position.x, transform.position.y, -10f);
        cameraBase = Vector3.Lerp(cameraBase,
            target, 1f - Mathf.Exp(-10f * Time.deltaTime));
        Vector3 shake = Time.time < shakeUntil
            ? new Vector3(Mathf.Sin(Time.time * 140), Mathf.Cos(Time.time * 113), 0) * .065f : Vector3.zero;
        followCamera.transform.position = cameraBase + shake;
    }

    void OnDisable()
    {
        if (appearance != null) appearance.color = originalColor;
        if (followCamera != null && body != null) followCamera.transform.position = cameraBase;
    }
}
