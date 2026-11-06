using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Entrada al campus con la misma fuente, paneles y opciones del nivel 1.</summary>
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
        U2Presentation.Text(card.transform, "Objetivo", "Cinco profesores · Cinco proyectos · Tres minutos\nProyecto equivocado: −5 s · Entrega repetida: −8 s\nSelecciona el proyecto correcto y entrégalo con E.", new Vector2(0, 205), new Vector2(1120, 125), 30, Color.white);
        U2Presentation.Button(card.transform, "Jugar nivel 3", new Vector2(0, 75), () => SceneManager.LoadScene("Nivel03_LaRutaDeEntrega"));
        U2Presentation.Button(card.transform, "Opciones", new Vector2(0, -25), () => U2Presentation.Options(canvas.transform, null, CampusDeliveryObjectives.Controls));
        U2Presentation.Button(card.transform, "Menú principal", new Vector2(0, -125), U2Presentation.ReturnToMenu);
        U2Presentation.Text(card.transform, "Créditos", "Música original: Agustín López Parra, desarrollador · FL Studio 25\nInicioMenu · Nivel3 · GameOver-TiempoFinal\nArte: LimeZu · Fuente: Pixelify Sans (SIL OFL)", new Vector2(0, -285), new Vector2(1120, 190), 26, Color.white);
    }
    void Update() { if (music != null) music.volume = U2Presentation.MusicVolume; }
    void OnDestroy() { if (canvas != null) Destroy(canvas); }
}
