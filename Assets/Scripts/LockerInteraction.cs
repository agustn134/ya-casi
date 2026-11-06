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
    public void InitializeRound(Level01Manager manager, bool hasUSB)
    { levelManager = manager; containsUSB = hasUSB; searched = false; }

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
        if(levelManager==null || !levelManager.IsPlaying)return;
        var config=Resources.Load<U2GameConfig>("U2GameConfig");
        var upgrade=levelManager.GetComponent<LevelOneUpgrade>();
        if(searched)
        {
            float penalty=config!=null?config.repeatLockerPenalty:5;
            levelManager.ApplyPenalty(penalty);levelManager.PlayWrongLockerSound();
            levelManager.ShowFeedback($"Ya revisaste este casillero. −{penalty:0} segundos.");
            if(upgrade!=null)upgrade.Reviewed(true,false);return;
        }
        searched=true;if(upgrade!=null)upgrade.Reviewed(false,containsUSB);
        if(containsUSB){levelManager.CollectUSB();levelManager.ShowFeedback("¡Encontraste tu USB! Vuelve a la salida.");}
        else{float penalty=config!=null?config.wrongLockerPenalty:3;levelManager.ApplyPenalty(penalty);levelManager.PlayWrongLockerSound();levelManager.ShowFeedback($"Aquí no está la USB. −{penalty:0} segundos.");}
    }
}
