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
    /// Revisa el casillero una sola vez.
    /// Entrega la USB o descuenta diez segundos si está vacío.
    /// </summary>
    public void Interact()
    {
        if (levelManager != null && !levelManager.IsPlaying)
        {
            return;
        }

        if (searched)
        {
            return;
        }

        searched = true;

        if (containsUSB)
        {
            if (levelManager != null)
            {
                levelManager.CollectUSB();
                levelManager.ShowFeedback(
                    "¡Encontraste tu USB! Busca la salida."
                );
            }

            Debug.Log("¡Encontraste la USB!", this);
        }
        else
        {
            if (levelManager != null)
            {
                levelManager.ApplyPenalty(10f);

                // Si la penalización terminó la partida,
                // se conserva únicamente el mensaje de derrota.
                if (levelManager.IsPlaying)
                {
                    levelManager.ShowFeedback(
                        "Aquí no está la USB. −10 segundos."
                    );
                }
            }

            Debug.Log(
                "Aquí no está la USB. Penalización: 10 segundos.",
                this
            );
        }
    }
}