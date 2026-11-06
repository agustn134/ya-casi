using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>Entregas de prueba del campus. El progreso se reinicia en cada partida.</summary>
public sealed class CampusDeliveryObjectives : MonoBehaviour
{
    [SerializeField] Transform player;
    [SerializeField] Transform[] professors;
    [SerializeField] string[] professorNames;
    [SerializeField] float deliveryDistance = 1.8f;
    [SerializeField] float roundSeconds = 180f;
    [SerializeField] AudioClip levelMusic;
    [SerializeField] AudioClip timeoutMusic;
    bool[] delivered;
    int nearest = -1, count;
    GUIStyle style;
    float remaining;
    bool timedOut;
    AudioSource music;

    void Awake()
    {
        delivered = new bool[professors.Length];
        Time.timeScale = 1f;
        remaining = roundSeconds;
        music = gameObject.AddComponent<AudioSource>();
        music.playOnAwake = false;
        music.spatialBlend = 0;
        music.volume = .65f;
        if (player == null || professorNames.Length != professors.Length)
        {
            Debug.LogError("Campus: revisa las referencias de los profesores y el jugador.", this);
            enabled = false;
            return;
        }
        music.clip = levelMusic;
        music.loop = true;
        if (levelMusic != null) music.Play();
    }

    void Update()
    {
        if (timedOut || count == professors.Length) return;
        remaining = Mathf.Max(0, remaining - Time.deltaTime);
        if (remaining <= 0)
        {
            timedOut = true;
            nearest = -1;
            Time.timeScale = 0f;
            music.Stop();
            music.clip = timeoutMusic;
            music.loop = false;
            if (timeoutMusic != null) music.Play();
            return;
        }
        nearest = -1;
        float best = deliveryDistance * deliveryDistance;
        for (int i = 0; i < professors.Length; i++)
        {
            if (professors[i] == null || delivered[i]) continue;
            float distance = ((Vector2)player.position - (Vector2)professors[i].position).sqrMagnitude;
            if (distance >= best) continue;
            nearest = i;
            best = distance;
        }
        bool pressed = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
        pressed |= Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame;
        if (nearest < 0 || !pressed || Time.timeScale == 0) return;
        delivered[nearest] = true;
        count++;
        var sprite = professors[nearest].GetComponent<SpriteRenderer>();
        if (sprite != null) sprite.color = new Color(.65f, 1f, .75f);
        Debug.Log("Entrega realizada: " + professorNames[nearest] + " (" + count + "/" + professors.Length + ")", this);
        nearest = -1;
    }

    void OnGUI()
    {
        if (style == null)
        {
            style = new GUIStyle(GUI.skin.box);
            style.fontSize = 20;
            style.alignment = TextAnchor.MiddleCenter;
            style.normal.textColor = Color.white;
            style.wordWrap = true;
        }
        int seconds = Mathf.CeilToInt(remaining);
        GUI.Box(new Rect(16, 16, Mathf.Min(420, Screen.width - 32), 54),
            "ENTREGAS " + count + " / " + professors.Length + "   |   "
            + (seconds / 60).ToString("00") + ":" + (seconds % 60).ToString("00"), style);
        if (timedOut)
        {
            float width = Mathf.Min(620, Screen.width - 32);
            var box = new Rect((Screen.width - width) / 2, (Screen.height - 260) / 2, width, 260);
            GUI.Box(box, "TIEMPO FINAL\n\nNo completaste las cinco entregas en tres minutos.", style);
            if (GUI.Button(new Rect(box.x + 24, box.y + 170, width / 2 - 36, 46), "Reintentar"))
            {
                Time.timeScale = 1; SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
            if (GUI.Button(new Rect(box.center.x + 12, box.y + 170, width / 2 - 36, 46), "Menú"))
            {
                Time.timeScale = 1; SceneManager.LoadScene("MenuNivel03");
            }
            GUI.Label(new Rect(16, Screen.height - 64, Screen.width - 32, 52),
                "TiempoFinal — Música original de Agustín López Parra · FL Studio 25", style);
            return;
        }
        string message = count == professors.Length ? "¡Todas las entregas están completas!"
            : nearest >= 0 ? "E / A: entregar a " + professorNames[nearest]
            : "Busca a los profesores del campus: ahora hay " + professors.Length + " entregas.";
        GUI.Box(new Rect(16, Screen.height - 82, Screen.width - 32, 64), message, style);
    }

    void OnDestroy() { Time.timeScale = 1f; }
}
