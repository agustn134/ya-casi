/*
 * Archivo: LockerInteraction.cs
 * Proyecto: Ya Casi
 * Autores del proyecto: Agustín López Parra y Carlos Manuel Palma Muñoz.
 * Función: Permite revisar un casillero una sola vez.
 * Alcance actual: Muestra el resultado en Console.
 */

using UnityEngine;

/// <summary>
/// Guarda el estado de revisión y el contenido de un casillero.
/// </summary>
public class LockerInteraction : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Punto situado en el piso frente al casillero.")]
    private Transform interactionPoint;

    [SerializeField]
    [Tooltip("Indica si este casillero contiene la USB.")]
    private bool containsUSB = true;

    [SerializeField]
    [Tooltip("Gestor que registra la obtención de la USB.")]
    private Level01Manager levelManager;

    private bool searched;

    /// <summary>
    /// Indica si el jugador ya revisó este casillero.
    /// Permite ocultar el indicador de interacción.
    /// </summary>
    public bool IsSearched => searched;

    /// <summary>
    /// Posición utilizada para comprobar la distancia del jugador.
    /// Usa la posición del casillero si falta asignar el punto.
    /// </summary>
    public Vector2 InteractionPosition =>
        interactionPoint != null ? interactionPoint.position : transform.position;

    /// <summary>
    /// Revisa el casillero y comunica su contenido.
    /// Impide obtener el resultado más de una vez.
    /// </summary>
    public void Interact()
    {
        if (searched)
        {
            Debug.Log("Este casillero ya fue revisado.", this);
            return;
        }

        searched = true;

        if (containsUSB)
        {
            // Comunica al gestor que el jugador consiguió la USB.
            if (levelManager != null)
            {
                levelManager.CollectUSB();
            }

            Debug.Log("¡Encontraste la USB!", this);
        }
        else
        {
            Debug.Log("Aquí no está la USB.", this);
        }
    }
}