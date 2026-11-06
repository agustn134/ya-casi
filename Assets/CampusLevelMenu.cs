using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Entrada al nivel 3 y créditos de su música original.</summary>
public sealed class CampusLevelMenu : MonoBehaviour
{
    [SerializeField] AudioClip menuMusic;
    GUIStyle title, body;
    void Awake()
    {
        Time.timeScale = 1;
        var music = gameObject.AddComponent<AudioSource>();
        music.clip = menuMusic;
        music.loop = true;
        music.spatialBlend = 0;
        music.volume = .65f;
        if (menuMusic != null) music.Play();
    }
    void OnGUI()
    {
        if (title == null)
        {
            title = new GUIStyle(GUI.skin.label) { fontSize = 38, alignment = TextAnchor.MiddleCenter };
            body = new GUIStyle(GUI.skin.label) { fontSize = 20, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            title.normal.textColor = body.normal.textColor = Color.white;
        }
        float width = Mathf.Min(760, Screen.width - 32);
        float x = (Screen.width - width) / 2;
        float y = Mathf.Max(16, (Screen.height - 460) / 2);
        GUI.Box(new Rect(x, y, width, 460), "");
        GUI.Label(new Rect(x, y + 32, width, 64), "¡YA CASI!", title);
        GUI.Label(new Rect(x + 16, y + 106, width - 32, 110),
            "Nivel 3 · La Ruta de Entrega\nCinco profesores. Tres minutos.\nWASD / flechas: caminar · Shift: correr · E: entregar", body);
        if (GUI.Button(new Rect(x + width / 2 - 130, y + 230, 260, 54), "Jugar nivel 3"))
            SceneManager.LoadScene("Nivel03_LaRutaDeEntrega");
        GUI.Label(new Rect(x + 16, y + 310, width - 32, 130),
            "CRÉDITOS MUSICALES\nComposición y producción: Agustín López Parra, desarrollador.\nCreado en FL Studio 25.\nInicioMenu · Nivel3 · GameOver-TiempoFinal", body);
    }
}
