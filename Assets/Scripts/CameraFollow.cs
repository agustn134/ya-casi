/*
 * Archivo: CameraFollow.cs
 * Proyecto: Ya Casi
 * Autores del proyecto:
 * Agustín López Parra y Carlos Manuel Palma Muñoz.
 * Función: Sigue horizontalmente al jugador por el pasillo.
 */

using UnityEngine;

/// <summary>
/// Conserva la altura y profundidad iniciales de la cámara.
/// Limita el desplazamiento horizontal entre dos posiciones.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;

    [SerializeField, Min(0.01f)]
    private float smoothTime = 0.15f;

    [Tooltip("Límite izquierdo del centro de la cámara.")]
    [SerializeField] private float minX = 0f;

    [Tooltip("Límite derecho del centro de la cámara.")]
    [SerializeField] private float maxX = 30f;

    private float horizontalVelocity;

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        float desiredX = Mathf.Clamp(
            target.position.x,
            minX,
            maxX
        );

        Vector3 position = transform.position;

        position.x = Mathf.SmoothDamp(
            position.x,
            desiredX,
            ref horizontalVelocity,
            smoothTime
        );

        transform.position = position;
    }
}