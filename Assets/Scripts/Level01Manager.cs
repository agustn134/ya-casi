/*
 * Archivo: Level01Manager.cs
 * Proyecto: Ya Casi
 * Autores del proyecto: Agustín López Parra y Carlos Manuel Palma Muñoz.
 * Función: Gestiona objetivo, USB, tiempo y resultado del nivel 1.
 */

using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Controla el progreso y las condiciones de victoria y derrota.
/// </summary>
public class Level01Manager : MonoBehaviour
{
    [Header("Interfaz")]
    [SerializeField] private TMP_Text objectiveText;
    [SerializeField] private TMP_Text timerText;

    [Tooltip("Texto temporal con el resultado de revisar un casillero.")]
    [SerializeField] private TMP_Text feedbackText;

    private Coroutine feedbackCoroutine;

    [Header("Sonidos")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip wrongLockerSound;
    [SerializeField] private AudioClip usbFoundSound;
    [SerializeField] private AudioClip victorySound;
    [SerializeField] private AudioClip defeatSound;

    [Header("Escenario y jugador")]
    [SerializeField] private Collider2D exitBarrier;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private PlayerInteraction playerInteraction;

    [Header("Tiempo")]
    [SerializeField, Min(1f)]
    [Tooltip("Duración inicial del nivel en segundos.")]
    private float initialTime = 90f;

    private float remainingTime;

    public bool HasUSB { get; private set; }
    public bool IsCompleted { get; private set; }
    public bool IsFailed { get; private set; }

    /// <summary>
    /// Indica si la partida todavía permite acciones.
    /// </summary>
    public bool IsPlaying => !IsCompleted && !IsFailed;

    /// <summary>
    /// Inicializa el reloj, bloquea la salida y muestra el objetivo.
    /// </summary>
    private void Start()
    {
        remainingTime = initialTime;

        if (exitBarrier != null)
            exitBarrier.enabled = true;

        if (feedbackText != null)
        {
            feedbackText.text = "";
        }

        UpdateObjective();
        UpdateTimer();
    }

    /// <summary>
    /// Descuenta tiempo mientras la partida continúa.
    /// Recuperar la USB no detiene el reloj: todavía hay que salir.
    /// </summary>
    private void Update()
    {
        // Tras terminar, permite reiniciar con R.
        // Se consulta desde el gestor porque la interacción del jugador
        // queda desactivada al ganar o perder.
        if (!IsPlaying)
        {
            if (Keyboard.current != null &&
                Keyboard.current.rKey.wasPressedThisFrame)
            {
                RestartLevel();
            }

            return;
        }

        remainingTime = Mathf.Max(0f, remainingTime - Time.deltaTime);
        UpdateTimer();

        if (remainingTime <= 0f)
            FailLevel();
    }

    /// <summary>
    /// Registra la USB y desbloquea la salida.
    /// </summary>
    public void CollectUSB()
    {
        if (!IsPlaying || HasUSB)
            return;

        HasUSB = true;
        PlaySound(usbFoundSound);

        if (exitBarrier != null)
            exitBarrier.enabled = false;

        UpdateObjective();
    }

    /// <summary>
    /// Resta segundos sin permitir valores negativos.
    /// Puede provocar derrota si agota el tiempo disponible.
    /// </summary>
    /// <param name="seconds">Segundos que se descontarán.</param>
    public void ApplyPenalty(float seconds)
    {
        if (!IsPlaying || seconds <= 0f)
            return;

        remainingTime = Mathf.Max(0f, remainingTime - seconds);
        UpdateTimer();

        if (remainingTime <= 0f)
            FailLevel();
    }

    /// <summary>
    /// Completa el nivel si el jugador sale con la USB a tiempo.
    /// </summary>
    public void CompleteLevel()
    {
        if (!IsPlaying || !HasUSB)
            return;

        IsCompleted = true;
        ClearFeedback();

        if (sfxSource != null)
        {
            sfxSource.Stop();
        }

        PlaySound(victorySound);

        if (objectiveText != null)
        {
            objectiveText.text =
                "¡Nivel 1 completado! USB recuperada.\nPresiona R para reiniciar.";
        }

        StopPlayer();
        Debug.Log("Nivel 1 completado: El Caos del Casillero.", this);
    }

    /// <summary>
    /// Finaliza la partida al agotarse el tiempo.
    /// </summary>
    private void FailLevel()
    {
        if (!IsPlaying)
            return;

        IsFailed = true;
        ClearFeedback();

        if (sfxSource != null)
        {
            sfxSource.Stop();
        }

        PlaySound(defeatSound);

        if (exitBarrier != null)
            exitBarrier.enabled = true;

        if (objectiveText != null)
        {
            objectiveText.text =
                "Tiempo agotado. El edificio cerró contigo dentro.\nPresiona R para reintentar.";
        }

        StopPlayer();
        Debug.Log("Derrota: se agotó el tiempo.", this);
    }

    /// <summary>
    /// Desactiva movimiento e interacción.
    /// Sus métodos OnDisable detienen al jugador y ocultan el indicador.
    /// </summary>
    private void StopPlayer()
    {
        if (playerInteraction != null)
            playerInteraction.enabled = false;

        if (playerMovement != null)
            playerMovement.enabled = false;
    }

    /// <summary>
    /// Actualiza el objetivo según la posesión de la USB.
    /// </summary>
    private void UpdateObjective()
    {
        if (objectiveText != null)
        {
            objectiveText.text = HasUSB
                ? "USB recuperada. Dirígete a la salida."
                : "Objetivo: recupera tu USB";
        }
    }

    /// <summary>
    /// Presenta el tiempo restante como minutos y segundos.
    /// Redondea hacia arriba para mostrar 00:00 solo al agotarse.
    /// </summary>
    private void UpdateTimer()
    {
        if (timerText == null)
            return;

        int seconds = Mathf.CeilToInt(remainingTime);
        timerText.text = $"Tiempo: {seconds / 60:00}:{seconds % 60:00}";
    }

    /// <summary>
    /// Recarga la escena actual para comenzar una nueva partida.
    /// Restablece los objetos y valores guardados en la escena.
    /// </summary>
    public void RestartLevel()
    {
        Scene currentScene = SceneManager.GetActiveScene();

        // Comprueba que la escena esté incluida en la compilación.
        if (!Application.CanStreamedLevelBeLoaded(currentScene.path))
        {
            Debug.LogError(
                "Añade la escena actual a la Scene List de Build Profiles.",
                this
            );
            return;
        }

        SceneManager.LoadScene(currentScene.path);
    }

    /// <summary>
    /// Muestra un mensaje temporal durante la partida.
    /// Si ya había otro mensaje, lo reemplaza y renueva su duración.
    /// </summary>
    public void ShowFeedback(string message)
    {
        if (!IsPlaying || feedbackText == null)
        {
            return;
        }

        ClearFeedback();

        feedbackText.text = message;
        feedbackCoroutine = StartCoroutine(HideFeedbackAfterDelay());
    }

    /// <summary>
    /// Oculta el mensaje después de tres segundos.
    /// </summary>
    private IEnumerator HideFeedbackAfterDelay()
    {
        yield return new WaitForSeconds(3f);

        feedbackText.text = "";
        feedbackCoroutine = null;
    }

    /// <summary>
    /// Cancela cualquier mensaje pendiente y limpia su texto.
    /// </summary>
    private void ClearFeedback()
    {
        if (feedbackCoroutine != null)
        {
            StopCoroutine(feedbackCoroutine);
            feedbackCoroutine = null;
        }

        if (feedbackText != null)
        {
            feedbackText.text = "";
        }
    }

    /// <summary>
    /// Reproduce un efecto de sonido si está configurado.
    /// </summary>
    private void PlaySound(AudioClip clip)
    {
        if (sfxSource != null && clip != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }

    /// <summary>
    /// Reproduce el aviso de casillero incorrecto
    /// mientras la partida continúa.
    /// </summary>
    public void PlayWrongLockerSound()
    {
        if (IsPlaying)
        {
            PlaySound(wrongLockerSound);
        }
    }
}