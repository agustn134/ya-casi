using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>MenÃº del videojuego y opciones comunes, independientes de la actividad MenÃº II.</summary>
public sealed class U2Presentation : MonoBehaviour
{
    public const string MenuScene = "MenuPrincipal";
    public const string LevelScene = "Nivel01_CaosDelCasillero";
    public const string SaveKey = "YaCasi.U2.ResumeLevel";
    public static float MusicVolume => PlayerPrefs.GetFloat("YaCasi.U2.Music", .45f);
    public static float EffectsVolume => PlayerPrefs.GetFloat("YaCasi.U2.Effects", .7f);
    static TMP_FontAsset Font => Resources.Load<U2GameConfig>("U2GameConfig")?.font;
    static readonly Color Navy = new Color32(24,39,51,255);
    static readonly Color Gold = new Color32(255,213,107,255);
    GameObject root;
    AudioSource music;
    public const string DeliveryScene = "Nivel03_LaRutaDeEntrega";
    const string CampaignKey = "YaCasi.Campaign.Resume";
    const string FinishedKey = "YaCasi.Campaign.Completed";
    static readonly string[] CampaignScenes = { LevelScene, LibraryLevelManager.SceneName, DeliveryScene };
    void Start()
    {
        Time.timeScale = 1;
        root = NewCanvas("Menú principal", 100);
        music = gameObject.AddComponent<AudioSource>();
        music.loop = true;
        music.volume = MusicVolume;
        music.clip = Resources.Load<U2GameConfig>("U2GameConfig")?.menuMusic;
        if (music.clip != null) music.Play();
        Main();
    }

    void Update()
    {
        if (music != null) music.volume = MusicVolume;
    }

    void Clear()
    {
        foreach (Transform child in root.transform)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }
    }

    public void Main()
    {
        Clear();
        var card = Card(root.transform, "Inicio");
        Text(card.transform, "Título", "¡YA CASI!", new Vector2(0, 338), new Vector2(1000, 88), 64, Gold);
        bool finished = PlayerPrefs.GetInt(FinishedKey) == 1;
        Text(card.transform, "Subtítulo", finished ? "Campaña completada" : "Una aventura universitaria contrarreloj",
            new Vector2(0, 262), new Vector2(1100, 54), 28, Color.white);

        var size = new Vector2(620, 80);
        MenuButton(card.transform, "Jugar nivel 1", new Vector2(0, 172), size, 34, () => LoadLevel(1));
        MenuButton(card.transform, "Nivel 2 · Biblioteca", new Vector2(0, 87), size, 34, () => LoadLevel(2));
        MenuButton(card.transform, "Jugar nivel 3", new Vector2(0, 2), size, 34, () => LoadLevel(3));
        MenuButton(card.transform, "Continuar", new Vector2(0, -83), size, 34, StartLevel);
        MenuButton(card.transform, "Opciones", new Vector2(0, -168), size, 34, () => Options(root.transform, null));
        MenuButton(card.transform, "Créditos", new Vector2(0, -253), size, 34, Credits);
        MenuButton(card.transform, "Salir", new Vector2(0, -338), size, 34, Quit);
    }

    static Button MenuButton(Transform parent, string label, Vector2 position, Vector2 size, float fontSize, UnityEngine.Events.UnityAction action)
    {
        var button = Button(parent, label, position, action);
        ((RectTransform)button.transform).sizeDelta = size;
        var text = button.GetComponentInChildren<TMP_Text>();
        text.rectTransform.sizeDelta = new Vector2(size.x - 20, Mathf.Min(size.y, 62));
        text.fontSize = fontSize;
        return button;
    }

    static int ResumeLevel()
    {
        if (PlayerPrefs.HasKey(CampaignKey))
            return Mathf.Clamp(PlayerPrefs.GetInt(CampaignKey), 1, CampaignScenes.Length);
        if (PlayerPrefs.GetInt("YaCasi.Library.Completed") == 1) return 3;
        if (PlayerPrefs.GetInt("YaCasi.U2.Win") == 1) return 2;
        return Mathf.Clamp(PlayerPrefs.GetInt(SaveKey, 1), 1, CampaignScenes.Length);
    }

    static void SaveProgress(int level)
    {
        PlayerPrefs.SetInt(CampaignKey, Mathf.Max(level, ResumeLevel()));
        PlayerPrefs.Save();
    }

    public static void StartLevel() => LoadLevel(ResumeLevel());

    static void LoadLevel(int level)
    {
        if (level < 1 || level > CampaignScenes.Length) return;
        string scene = CampaignScenes[level - 1];
        if (!Application.CanStreamedLevelBeLoaded(scene))
        {
            Debug.LogError("La escena no está habilitada: " + scene);
            return;
        }
        SaveProgress(level);
        Time.timeScale = 1;
        SceneManager.LoadScene(scene);
    }

    public static void ReturnToMenu()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene(MenuScene);
    }

    public static void SaveLibraryProgress() => SaveProgress(2);

    public static void PlayLibrary() => LoadLevel(2);
    public static void SaveDeliveryProgress() => SaveProgress(3);
    public static void PlayDelivery() => LoadLevel(3);

    public static void CompleteCampaign()
    {
        PlayerPrefs.SetInt(FinishedKey, 1);
        SaveProgress(3);
    }

    void Credits()
    {
        Clear();var card=Card(root.transform,"Créditos");var cfg=Resources.Load<U2GameConfig>("U2GameConfig");
        Text(card.transform,"Título","CRÉDITOS",new Vector2(0,320),new Vector2(900,70),54,Gold);
        Text(card.transform,"Autores","Agustín López Parra · Carlos Manuel Palma Muñoz\n\nNivel 1: desarrollo de Agustín; revisión de Carlos.\nUnity · C# · TextMesh Pro\nArte: LimeZu — Modern Interiors, Exteriors y Office\nInterfaz: That Witch Design — Simple Button Set 01\nFuente: Pixelify Sans — SIL OFL\nEfectos: Kenney Interface Sounds y síntesis propia\nGame Over: Alphix — Pixabay (417465)\nMúsica del nivel: "+(cfg!=null?cfg.soundtrackCredit:"pendiente de confirmar")+"\n\nCampaña: Caos del Casillero, Maratón en la Biblioteca y La Ruta de Entrega.",new Vector2(0,20),new Vector2(1120,500),30,Color.white);
        Button(card.transform,"Volver",new Vector2(0,-310),Main);
    }
    static void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying=false;
#elif UNITY_WEBGL
        Debug.Log("Puedes cerrar la pestaÃ±a para salir del juego.");
#else
        Application.Quit();
#endif
    }
    public static void Options(Transform parent,Action back,string controls=null)
    {
        var cover=Image(parent,"Opciones",new Color(0,0,0,.9f));Stretch(cover.rectTransform);
        var card=Card(cover.transform,"Panel opciones");
        Text(card.transform,"TÃ­tulo","OPCIONES",new Vector2(0,320),new Vector2(900,80),56,Gold);
        Slider(card.transform,"MÃºsica",165,MusicVolume,v=>PlayerPrefs.SetFloat("YaCasi.U2.Music",v));
        Slider(card.transform,"Efectos",35,EffectsVolume,v=>PlayerPrefs.SetFloat("YaCasi.U2.Effects",v));
        Text(card.transform,"Calidad","Calidad grÃ¡fica",new Vector2(-240,-75),new Vector2(400,55),32,Color.white);
        for(int i=0;i<3;i++){int quality=i;var b=Button(card.transform,new[]{"Baja","Media","Alta"}[i],new Vector2(-330+i*330,-140),()=>{QualitySettings.SetQualityLevel(Mathf.RoundToInt((QualitySettings.names.Length-1)*quality/2f),true);PlayerPrefs.SetInt("YaCasi.U2.Quality",quality);});b.GetComponent<RectTransform>().sizeDelta=new Vector2(290,65);}
        Text(card.transform,"Controles",controls??"WASD / flechas / joystick: moverte Â· Shift / gatillo: correr\nE / botÃ³n sur: revisar Â· Esc / Start: pausa\nEn mÃ³vil: botones de direcciÃ³n, correr y revisar.",new Vector2(0,-240),new Vector2(1100,120),27,Color.white);
        var close=Button(card.transform,"Volver",new Vector2(0,-350),()=>{PlayerPrefs.Save();Destroy(cover.gameObject);back?.Invoke();});
    }
    static void Slider(Transform parent,string caption,float y,float initial,UnityEngine.Events.UnityAction<float> changed)
    {
        Text(parent,caption,caption,new Vector2(-310,y+32),new Vector2(400,55),34,Color.white);
        var go=DefaultControls.CreateSlider(new DefaultControls.Resources());go.name=caption;go.transform.SetParent(parent,false);var r=go.GetComponent<RectTransform>();r.anchoredPosition=new Vector2(150,y);r.sizeDelta=new Vector2(620,50);var s=go.GetComponent<Slider>();s.SetValueWithoutNotify(initial);s.onValueChanged.AddListener(changed);
    }
    public static GameObject NewCanvas(string name,int order)
    {
        if(FindFirstObjectByType<EventSystem>()==null)new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));
        var events=FindFirstObjectByType<EventSystem>();var legacy=events.GetComponent<StandaloneInputModule>();if(legacy!=null)legacy.enabled=false;if(events.GetComponent<InputSystemUIInputModule>()==null)events.gameObject.AddComponent<InputSystemUIInputModule>();
        var go=new GameObject(name,typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));go.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;go.GetComponent<Canvas>().sortingOrder=order;
        var scaler=go.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;return go;
    }
    public static GameObject Card(Transform parent,string name)
    {
        var background=Image(parent,name+" fondo",new Color(.025f,.045f,.065f,.9f));Stretch(background.rectTransform);
        var image=Image(background.transform,name,Navy);image.rectTransform.sizeDelta=new Vector2(1280,860);return image.gameObject;
    }
    public static TMP_Text Text(Transform parent,string name,string value,Vector2 pos,Vector2 size,float fontSize,Color color)
    {
        var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);var t=go.AddComponent<TextMeshProUGUI>();if(Font!=null)t.font=Font;t.text=value;t.fontSize=fontSize;t.color=color;t.alignment=TextAlignmentOptions.Center;t.raycastTarget=false;t.rectTransform.anchoredPosition=pos;t.rectTransform.sizeDelta=size;return t;
    }
    public static Button Button(Transform parent,string caption,Vector2 pos,UnityEngine.Events.UnityAction action)
    {
        var image=Image(parent,caption,new Color32(52,90,107,255));image.rectTransform.anchoredPosition=pos;image.rectTransform.sizeDelta=new Vector2(620,80);
        var button=image.gameObject.AddComponent<Button>();button.targetGraphic=image;var colors=button.colors;colors.highlightedColor=new Color(1f,.85f,.5f);colors.pressedColor=new Color(.65f,.75f,.8f);button.colors=colors;button.onClick.AddListener(action);
        Text(image.transform,"Texto",caption,Vector2.zero,new Vector2(590,70),36,Color.white);return button;
    }
    public static Image Image(Transform parent,string name,Color color){var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);var i=go.GetComponent<Image>();i.color=color;return i;}
    public static void Stretch(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
}
