using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Entrada al campus con Redaction 35 y las opciones compartidas del juego.</summary>
public sealed class CampusLevelMenu : MonoBehaviour
{
    [SerializeField] AudioClip menuMusic;
    GameObject canvas;
    AudioSource music;
    void Start()
    {
        Time.timeScale = 1;
        music = gameObject.AddComponent<AudioSource>(); music.clip = menuMusic;
        music.loop = true; music.spatialBlend = 0; music.volume = U2Presentation.MusicVolume;
        if (menuMusic != null) music.Play();
        canvas = U2Presentation.NewCanvas("Menú nivel 3", 100);
        var card = U2Presentation.Card(canvas.transform, "La Ruta de Entrega");
        var gold = new Color32(255, 213, 107, 255);
        U2Presentation.Text(card.transform, "Título", "LA RUTA DE ENTREGA", new Vector2(0, 315), new Vector2(1140, 95), 60, gold);
        U2Presentation.Text(card.transform, "Objetivo", "Cinco profesores · Cinco proyectos · Tres minutos\nEsquiva los lanzamientos de cada profesor y entrega con E.\nQ: pedir paso a los alumnos. Espacio: esquivar.", new Vector2(0, 205), new Vector2(1120, 125), 32, Color.white);
        U2Presentation.Button(card.transform, "Jugar nivel 3", new Vector2(0, 75), () => SceneManager.LoadScene("Nivel03_LaRutaDeEntrega"));
        U2Presentation.Button(card.transform, "Opciones", new Vector2(0, -25), () =>
        {
            U2Presentation.Options(canvas.transform, null, CampusDeliveryObjectives.Controls);
            CampusTypography.Apply(canvas.transform);
        });
        U2Presentation.Button(card.transform, "Menú principal", new Vector2(0, -125), U2Presentation.ReturnToMenu);
        U2Presentation.Text(card.transform, "Créditos", "Música original: Agustín López Parra, desarrollador · FL Studio 25\nInicioMenu · Nivel3 · GameOver-TiempoFinal\nArte: LimeZu · Redaction 35: The Redaction Project (SIL OFL)", new Vector2(0, -285), new Vector2(1120, 190), 28, Color.white);
        CampusTypography.Apply(canvas.transform);
    }
    void Update() { if (music != null) music.volume = U2Presentation.MusicVolume; }
    void OnDestroy() { if (canvas != null) Destroy(canvas); }
}
