/*
 * Archivo: PlayerInteraction.cs
 * Proyecto: Ya Casi
 * Autores del proyecto: Agustín López Parra y Carlos Manuel Palma Muñoz.
 * Función: Gestiona la interacción con el casillero y muestra
 * el indicador de teclado cuando el jugador está cerca.
 */

using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// Comprueba la distancia al casillero asignado y permite revisarlo
/// mediante la acción Interact de Player Input.
/// </summary>
public class PlayerInteraction : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Casillero disponible para esta primera prueba.")]
    private LockerInteraction targetLocker;

    [SerializeField, Min(0.1f)]
    [Tooltip("Distancia máxima de interacción en unidades del mundo.")]
    private float interactionDistance = 1.5f;

    [SerializeField]
    [Tooltip("Texto del Canvas que muestra E para revisar.")]
    private TMP_Text interactionText;

    /// <summary>
    /// Oculta el indicador al activar el componente.
    /// </summary>
    private void OnEnable()
    {
        SetPromptVisible(false);
    }

    /// <summary>
    /// Actualiza la visibilidad según la distancia y el estado del casillero.
    /// </summary>
    private void Update()
    {
        bool canSearch = IsWithinRange() && !targetLocker.IsSearched;
        SetPromptVisible(canSearch);
    }

    /// <summary>
    /// Comprueba si existe un casillero activo dentro del alcance.
    /// </summary>
    private bool IsWithinRange()
    {
        if (targetLocker == null || !targetLocker.isActiveAndEnabled)
            return false;

        float distance = Vector2.Distance(
            transform.position,
            targetLocker.InteractionPosition
        );

        return distance <= interactionDistance;
    }

    /// <summary>
    /// Recibe la acción Interact mediante Send Messages.
    /// Solo responde al presionar el botón, no al soltarlo.
    /// </summary>
    /// <param name="value">Estado del botón de interacción.</param>
    public void OnInteract(InputValue value)
    {
        if (!value.isPressed || targetLocker == null)
            return;

        if (!IsWithinRange())
        {
            Debug.Log("Acércate al casillero para revisarlo.", this);
            return;
        }

        targetLocker.Interact();

        // Oculta el indicador inmediatamente después de revisar.
        SetPromptVisible(false);
    }

    /// <summary>
    /// Muestra u oculta únicamente el componente de texto.
    /// El Canvas y sus otros elementos permanecen activos.
    /// </summary>
    private void SetPromptVisible(bool visible)
    {
        if (interactionText != null)
            interactionText.enabled = visible;
    }

    /// <summary>
    /// Oculta el indicador si se desactiva el jugador o este componente.
    /// </summary>
    private void OnDisable()
    {
        SetPromptVisible(false);
    }
}