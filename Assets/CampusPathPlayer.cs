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
    }

    void FixedUpdate()
    {
        Vector2 origin = body.position;
        Vector2 delta = input * (sprint ? runSpeed : walkSpeed) * Time.fixedDeltaTime;
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
        followCamera.transform.position = Vector3.Lerp(followCamera.transform.position,
            target, 1f - Mathf.Exp(-10f * Time.deltaTime));
    }
}
