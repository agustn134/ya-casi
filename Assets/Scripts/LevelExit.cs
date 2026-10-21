/*
 * Archivo: LevelExit.cs
 * Proyecto: Ya Casi
 * Autores del proyecto: Agustín López Parra y Carlos Manuel Palma Muñoz.
 * Función: Detecta al jugador en la salida y solicita completar el nivel.
 */

using UnityEngine;

/// <summary>
/// Detecta la llegada del jugador mediante un Collider2D de tipo Trigger.
/// Debe colocarse en el objeto ZonaSalida.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class LevelExit : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Gestor que comprueba si el jugador tiene la USB.")]
    private Level01Manager levelManager;

    /// <summary>
    /// Comprueba quién entró en la zona y si puede terminar el nivel.
    /// </summary>
    /// <param name="other">Collider que entró en el trigger.</param>
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Identifica al jugador por su componente de movimiento.
        PlayerMovement player =
            other.GetComponentInParent<PlayerMovement>();

        if (player == null || levelManager == null)
            return;

        if (!levelManager.HasUSB || levelManager.IsCompleted)
            return;

        levelManager.CompleteLevel();

        // OnDisable del movimiento detiene también el Rigidbody2D.
        player.enabled = false;

        // Desactiva las interacciones y oculta su indicador.
        PlayerInteraction interaction =
            player.GetComponent<PlayerInteraction>();

        if (interaction != null)
        {
            interaction.enabled = false;
        }
    }
}