/*
 * Archivo: PlayerInteraction.cs
 * Proyecto: Ya Casi
 * Autores del proyecto: Agustín López Parra
 * Función: Selecciona el casillero sin revisar más cercano,
 * muestra el indicador y permite interactuar mediante E.
 */

using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// Gestiona la interacción del jugador con los casilleros del nivel.
/// Requiere Player Input con Behavior = Send Messages.
/// </summary>
public class PlayerInteraction : MonoBehaviour
{
    [SerializeField, Min(0.1f)]
    [Tooltip("Distancia máxima para revisar un casillero.")]
    private float interactionDistance = 1.5f;

    [SerializeField]
    [Tooltip("Texto del Canvas que muestra E para revisar.")]
    private TMP_Text interactionText;

    // Casilleros encontrados al iniciar esta escena.
    private LockerInteraction[] lockers;
    private float nextReview;

    /// <summary>
    /// Obtiene los casilleros activos de la escena una sola vez.
    /// Esta versión supone que todos están colocados antes de iniciar.
    /// </summary>
    private void Awake()
    {
        lockers = FindObjectsByType<LockerInteraction>(
            FindObjectsSortMode.None
        );
    }

    /// <summary>
    /// Oculta el indicador al activar el componente.
    /// </summary>
    private void OnEnable()
    {
        SetPromptVisible(false);
    }

    /// <summary>
    /// Muestra el indicador si existe un casillero disponible cerca.
    /// </summary>
    private void Update()
    {
        var nearest=FindNearestLocker();
        if(interactionText!=null)interactionText.text=nearest!=null&&nearest.IsSearched?"E: ya revisado (−5 s)":"E para revisar";
        SetPromptVisible(nearest != null);
    }

    /// <summary>
    /// Busca el casillero activo y sin revisar más cercano,
    /// siempre que esté dentro del alcance.
    /// </summary>
    /// <returns>Casillero disponible o null si no hay ninguno.</returns>
    public LockerInteraction FindNearestLocker()
    {
        LockerInteraction nearest = null;
        float nearestDistance = interactionDistance;

        if (Time.timeScale <= 0f || lockers == null)
            return null;

        foreach (LockerInteraction locker in lockers)
        {
            if (locker == null ||
                !locker.isActiveAndEnabled)
            {
                continue;
            }

            float distance = Vector2.Distance(
                transform.position,
                locker.InteractionPosition
            );

            if (distance <= nearestDistance)
            {
                nearestDistance = distance;
                nearest = locker;
            }
        }

        return nearest;
    }

    /// <summary>
    /// Revisa el casillero disponible más cercano al presionar E.
    /// Ignora la notificación enviada al soltar la tecla.
    /// </summary>
    /// <param name="value">Estado del botón de interacción.</param>
    public void OnInteract(InputValue value) { if(value.isPressed) TryInteract(); }
    public void TryInteract()
    {
        if(!isActiveAndEnabled || Time.timeScale<=0 || Time.unscaledTime<nextReview)return;
        var nearest=FindNearestLocker();if(nearest==null)return;
        nextReview=Time.unscaledTime+.7f;nearest.Interact();
    }

    /// <summary>
    /// Controla la visibilidad del componente de texto.
    /// </summary>
    private void SetPromptVisible(bool visible)
    {
        if (interactionText != null)
            interactionText.enabled = visible;
    }

    /// <summary>
    /// Oculta el indicador al desactivar la interacción.
    /// </summary>
    private void OnDisable()
    {
        SetPromptVisible(false);
    }
}