/*
 * Archivo: PlayerMovement.cs
 * Proyecto: Ya Casi
 * Autores del proyecto:
 * Agustín López Parra y Carlos Manuel Palma Muñoz.
 * Función: Controla el movimiento y las animaciones del jugador.
 */

using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Mueve al personaje en ocho direcciones y muestra
/// animaciones de caminar y reposo con cuatro orientaciones.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Animator))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField, Min(0f)]
    private float moveSpeed = 3f;

    [SerializeField, Min(0f)]
    private float runSpeed = 5f;

    [SerializeField, Min(0.1f)]
    private float runAnimationSpeed = 1.5f;

    private bool sprintHeld;
    private bool wasRunning;

    private Rigidbody2D rb;
    private Animator animator;
    private Vector2 moveInput;
    private Vector2 keyboardInput;
    private bool hasFocus = true;
    public float EnvironmentSpeedMultiplier { get; set; } = 1f;
    public bool IsRunning => isActiveAndEnabled && hasFocus && sprintHeld && moveInput.sqrMagnitude > .001f;

    // Al comenzar, el personaje mira hacia abajo.
    private string facingDirection = "Down";
    private string currentAnimation;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        UpdateAnimation();
    }

    /// <summary>
    /// Recibe Move desde Player Input con Send Messages.
    /// Limita la velocidad para evitar acelerar en diagonal.
    /// </summary>
    public void OnMove(InputValue value)
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        keyboardInput = Vector2.ClampMagnitude(
            value.Get<Vector2>(), 1f
        );
    }

    /// <summary>
    /// Activa la carrera mientras se mantiene pulsado Shift.
    /// </summary>
    public void OnSprint(InputValue value)
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        // El evento no conserva un estado: la tecla puede soltarse mientras estamos desactivados.
        sprintHeld = ReadSprintHeld();
    }

    private static bool ReadSprintHeld()
    {
        var keyboard = Keyboard.current;
        bool shift = keyboard != null && (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed);
        return shift || TouchGameControls.Sprint || (Gamepad.current != null && Gamepad.current.rightTrigger.isPressed);
    }

    private void Update()
    {
        if (!hasFocus) { ClearMovement(); return; }
        moveInput = keyboardInput;
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            Vector2 held = new Vector2(
                (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1 : 0),
                (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1 : 0));
            if (held.sqrMagnitude > .01f) moveInput = held;
        }
        if (Gamepad.current != null && Gamepad.current.leftStick.ReadValue().sqrMagnitude > .01f)
            moveInput = Gamepad.current.leftStick.ReadValue();
        if (TouchGameControls.Direction.sqrMagnitude > .01f) moveInput = TouchGameControls.Direction;
        moveInput = Vector2.ClampMagnitude(moveInput, 1);
        sprintHeld = ReadSprintHeld();
        UpdateAnimation();
    }

    private void OnApplicationFocus(bool focused)
    {
        hasFocus = focused;
        if (!focused) ClearMovement();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused) ClearMovement();
    }

    private void ClearMovement()
    {
        keyboardInput = moveInput = Vector2.zero;
        sprintHeld = wasRunning = false;
        TouchGameControls.Direction = Vector2.zero;
        TouchGameControls.Sprint = false;
        if (rb != null) rb.linearVelocity = Vector2.zero;
        if (animator != null) animator.speed = 1f;
    }

    private void FixedUpdate()
    {
        float currentSpeed = sprintHeld ? runSpeed : moveSpeed;
        rb.linearVelocity = moveInput * currentSpeed * Mathf.Clamp(EnvironmentSpeedMultiplier, .2f, 1f);
    }

    /// <summary>
    /// Conserva la última orientación al detenerse.
    /// En diagonales exactas utiliza la orientación vertical.
    /// </summary>
    private void UpdateAnimation()
    {
        if (animator == null || !animator.isActiveAndEnabled || animator.runtimeAnimatorController == null) return;
        bool isMoving = moveInput.sqrMagnitude > 0.001f;
        bool isRunning = isMoving && sprintHeld;

        // Se actualiza antes del return para responder a Shift
        // aunque el personaje siga mirando en la misma dirección.
        animator.speed = isRunning ? runAnimationSpeed : 1f;

        if (isRunning != wasRunning)
        {
            if (isMoving)
            {
            }

            wasRunning = isRunning;
        }

        if (isMoving)
        {
            if (Mathf.Abs(moveInput.x) > Mathf.Abs(moveInput.y))
            {
                facingDirection = moveInput.x > 0f
                    ? "Right"
                    : "Left";
            }
            else
            {
                facingDirection = moveInput.y > 0f
                    ? "Up"
                    : "Down";
            }
        }

        string nextAnimation =
            (isMoving ? "Walk" : "Idle") + facingDirection;

        // Evita reiniciar el clip en cada actualización.
        if (currentAnimation == nextAnimation)
        {
            return;
        }

        animator.Play(
            "Base Layer." + nextAnimation,
            0,
            0f
        );

        currentAnimation = nextAnimation;


    }

    /// <summary>
    /// Detiene el movimiento y muestra reposo
    /// cuando el gestor desactiva este componente.
    /// </summary>
    private void OnDisable()
    {
        ClearMovement();

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        if (animator != null &&
            animator.isActiveAndEnabled &&
            animator.runtimeAnimatorController != null)
        {
            UpdateAnimation();
        }
    }
}