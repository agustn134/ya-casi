/*
 * Archivo: PlayerMovement.cs
 * Proyecto: Ya Casi
 * Descripción: Controla el movimiento del jugador en perspectiva top-down 2D.
 * Autores del proyecto: Agustín López Parra y Carlos Manuel Palma Muñoz.
 * Fecha: 20 de octubre de 2026.
 *
 * Configuración necesaria:
 * - Añadir este script al objeto Player.
 * - Rigidbody2D: Gravity Scale = 0 y Freeze Rotation Z activado.
 * - Player Input: asignar PlayerControls y Default Map = Player.
 * - Player Input: Behavior = Send Messages.
 * - Acción Move: Value / Vector2, con WASD y flechas.
 */

using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controla el desplazamiento del jugador en ocho direcciones mediante
/// el Input System y las físicas 2D de Unity.
/// </summary>
/// <remarks>
/// Este componente gestiona únicamente el movimiento.
/// Las animaciones, la carrera y las interacciones se añadirán después.
/// </remarks>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    /// <summary>
    /// Velocidad de desplazamiento en unidades del mundo por segundo.
    /// SerializeField permite ajustarla desde el Inspector.
    /// </summary>
    [SerializeField]
    [Tooltip("Velocidad del jugador en unidades por segundo.")]
    private float moveSpeed = 3f;

    // Referencia al componente que controla las físicas del jugador.
    private Rigidbody2D rb;

    // Dirección solicitada por el jugador: X horizontal e Y vertical.
    private Vector2 moveInput;

    /// <summary>
    /// Obtiene y guarda la referencia al Rigidbody2D al cargar el objeto.
    /// Unity ejecuta Awake antes de comenzar las actualizaciones del juego.
    /// </summary>
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    /// <summary>
    /// Recibe los cambios de la acción Move del Input System.
    /// Player Input llama a este método mediante Send Messages.
    /// </summary>
    /// <param name="value">
    /// Valor de movimiento como Vector2. Al soltar las teclas,
    /// recibe un vector de valor cero.
    /// </param>
    public void OnMove(InputValue value)
    {
        // Limita la magnitud a 1 para evitar mayor velocidad en diagonal.
        moveInput = Vector2.ClampMagnitude(value.Get<Vector2>(), 1f);
    }

    /// <summary>
    /// Aplica la velocidad durante cada actualización de físicas.
    /// Unity ejecuta FixedUpdate a intervalos de tiempo fijos.
    /// </summary>
    private void FixedUpdate()
    {
        // linearVelocity se expresa en unidades por segundo.
        // No se multiplica por deltaTime porque ya representa una velocidad.
        rb.linearVelocity = moveInput * moveSpeed;
    }

    /// <summary>
    /// Detiene el movimiento cuando se desactiva este componente
    /// o el objeto Player, evitando conservar la última velocidad.
    /// </summary>
    private void OnDisable()
    {
        moveInput = Vector2.zero;

        // Comprueba que Awake ya haya obtenido el Rigidbody2D.
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }
}