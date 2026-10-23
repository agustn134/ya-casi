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

        moveInput = Vector2.ClampMagnitude(
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

        sprintHeld = value.isPressed;
    }

    private void Update()
    {
        UpdateAnimation();
    }

    private void FixedUpdate()
    {
        float currentSpeed = sprintHeld ? runSpeed : moveSpeed;
        rb.linearVelocity = moveInput * currentSpeed;
    }

    /// <summary>
    /// Conserva la última orientación al detenerse.
    /// En diagonales exactas utiliza la orientación vertical.
    /// </summary>
    private void UpdateAnimation()
    {
        bool isMoving = moveInput.sqrMagnitude > 0.001f;
        bool isRunning = isMoving && sprintHeld;

        // Se actualiza antes del return para responder a Shift
        // aunque el personaje siga mirando en la misma dirección.
        animator.speed = isRunning ? runAnimationSpeed : 1f;

        if (isRunning != wasRunning)
        {
            if (isMoving)
            {
                Debug.Log(
                    isRunning
                        ? "[PlayerMovement] Corriendo."
                        : "[PlayerMovement] Caminando.",
                    this
                );
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

        string movementDescription = isMoving
            ? $"Moviéndose hacia {facingDirection} (Input: {moveInput})"
            : $"Detenido mirando hacia {facingDirection}";

        Debug.Log($"[PlayerMovement] Animación activa: {nextAnimation} | {movementDescription}", this);
    }

    /// <summary>
    /// Detiene el movimiento y muestra reposo
    /// cuando el gestor desactiva este componente.
    /// </summary>
    private void OnDisable()
    {
        moveInput = Vector2.zero;
        sprintHeld = false;
        wasRunning = false;

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