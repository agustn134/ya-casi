/*
 * Archivo: Level01Manager.cs
 * Proyecto: Ya Casi
 * Autores del proyecto: Agustín López Parra y Carlos Manuel Palma Muñoz.
 * Función: Guarda el estado de la USB durante el nivel
 * y actualiza el objetivo mostrado en pantalla.
 */

using UnityEngine;
using TMPro;

/// <summary>
/// Gestiona el objetivo principal del nivel El Caos del Casillero.
/// El estado se conserva únicamente durante la ejecución actual.
/// </summary>
public class Level01Manager : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Texto del Canvas que muestra el objetivo actual.")]
    private TMP_Text objectiveText;

    [SerializeField]
    [Tooltip("Collider sólido que bloquea la salida hasta recuperar la USB.")]
    private Collider2D exitBarrier;

    /// <summary>
    /// Indica si el jugador ya consiguió la USB.
    /// Solo este componente puede modificar el estado.
    /// </summary>
    public bool HasUSB { get; private set; }

    /// <summary>
    /// Presenta el objetivo al comenzar el nivel.
    /// </summary>
    private void Start()
    {
        // Mantiene bloqueada la salida al comenzar.
        if (exitBarrier != null)
        {
            exitBarrier.enabled = true;
        }

        UpdateObjective();
    }

    /// <summary>
    /// Registra la obtención de la USB una sola vez.
    /// Más adelante, la salida consultará este estado.
    /// </summary>
    public void CollectUSB()
    {
        if (HasUSB)
            return;

        HasUSB = true;

        // Abre el paso cuando el jugador recupera la USB.
        if (exitBarrier != null)
        {
            exitBarrier.enabled = false;
        }

        UpdateObjective();
    }

    /// <summary>
    /// Actualiza el mensaje según el progreso del jugador.
    /// </summary>
    private void UpdateObjective()
    {
        if (objectiveText == null)
            return;

        objectiveText.text = HasUSB
            ? "USB recuperada. Dirígete a la salida."
            : "Objetivo: recupera tu USB";
    }

    /// <summary>
    /// Indica si el jugador ya terminó este nivel.
    /// Evita procesar la victoria más de una vez.
    /// </summary>
    public bool IsCompleted { get; private set; }

    /// <summary>
    /// Completa el nivel únicamente si el jugador recuperó la USB.
    /// </summary>
    public void CompleteLevel()
    {
        if (!HasUSB || IsCompleted)
            return;

        IsCompleted = true;

        if (objectiveText != null)
        {
            objectiveText.text = "¡Nivel 1 completado! USB recuperada.";
        }

        Debug.Log("Nivel 1 completado: El Caos del Casillero.", this);
    }
}