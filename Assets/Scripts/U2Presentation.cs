using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Menú del videojuego y opciones comunes, independientes de la actividad Menú II.</summary>
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
    float splashTime;
    bool splash;
    void Start()
    {
        Time.timeScale=1;
        root=NewCanvas("Menú principal",100);
        music=gameObject.AddComponent<AudioSource>();music.loop=true;music.volume=MusicVolume;
        var config=Resources.Load<U2GameConfig>("U2GameConfig");music.clip=config!=null?config.menuMusic:null;if(music.clip!=null)music.Play();
        splash=true;splashTime=Time.unscaledTime+1.8f;
        var card=Card(root.transform,"Splash");
        Text(card.transform,"Logo equipo","AL · CP",new Vector2(0,120),new Vector2(700,120),76,Gold);
        Text(card.transform,"Nombre","¡YA CASI!",Vector2.zero,new Vector2(850,120),82,Color.white);
        Text(card.transform,"Carga","Una aventura universitaria contrarreloj",new Vector2(0,-130),new Vector2(1000,80),34,Color.white);
    }
    void Update(){if(music!=null)music.volume=MusicVolume;if(splash&&Time.unscaledTime>=splashTime){splash=false;Main();}}
    void Clear(){foreach(Transform child in root.transform)Destroy(child.gameObject);}
    public void Main()
    {
        Clear();var card=Card(root.transform,"Inicio");
        Text(card.transform,"Título","¡YA CASI!",new Vector2(0,320),new Vector2(1000,100),76,Gold);
        Text(card.transform,"Subtítulo","Recupera la USB antes de que cierre el edificio",new Vector2(0,240),new Vector2(1100,70),30,Color.white);
        Button(card.transform,"Jugar",new Vector2(0,130),()=>{PlayerPrefs.DeleteKey("YaCasi.U2.Win");StartLevel();});
        var cont=Button(card.transform,"Continuar",new Vector2(0,35),StartLevel);cont.interactable=PlayerPrefs.HasKey(SaveKey);
        Button(card.transform,"Opciones",new Vector2(0,-60),()=>Options(root.transform,Main));
        Button(card.transform,"Créditos",new Vector2(0,-155),Credits);
        Button(card.transform,"Salir",new Vector2(0,-250),Quit);
        Text(card.transform,"Mejor","Mejor puntaje: "+PlayerPrefs.GetInt("YaCasi.U2.Best",0),new Vector2(0,-340),new Vector2(800,55),28,Gold);
    }
    public static void StartLevel(){Time.timeScale=1;SceneManager.LoadScene(LevelScene);}
    public static void ReturnToMenu(){Time.timeScale=1;SceneManager.LoadScene(MenuScene);}
    void Credits()
    {
        Clear();var card=Card(root.transform,"Créditos");var cfg=Resources.Load<U2GameConfig>("U2GameConfig");
        Text(card.transform,"Título","CRÉDITOS",new Vector2(0,320),new Vector2(900,70),54,Gold);
        Text(card.transform,"Autores","Agustín López Parra · Carlos Manuel Palma Muñoz\n\nNivel 1: desarrollo de Agustín; revisión de Carlos.\nUnity · C# · TextMesh Pro\nArte: LimeZu — Modern Interiors, Exteriors y Office\nInterfaz: That Witch Design — Simple Button Set 01\nFuente: Pixelify Sans — SIL OFL\nEfectos: Kenney Interface Sounds y síntesis propia\nMúsica del nivel: "+(cfg!=null?cfg.soundtrackCredit:"pendiente de confirmar")+"\n\nAvance académico: nivel 1 disponible. Niveles 2–4 pendientes.",new Vector2(0,20),new Vector2(1120,500),30,Color.white);
        Button(card.transform,"Volver",new Vector2(0,-310),Main);
    }
    static void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying=false;
#elif UNITY_WEBGL
        Debug.Log("Puedes cerrar la pestaña para salir del juego.");
#else
        Application.Quit();
#endif
    }
    public static void Options(Transform parent,Action back)
    {
        var cover=Image(parent,"Opciones",new Color(0,0,0,.9f));Stretch(cover.rectTransform);
        var card=Card(cover.transform,"Panel opciones");
        Text(card.transform,"Título","OPCIONES",new Vector2(0,320),new Vector2(900,80),56,Gold);
        Slider(card.transform,"Música",165,MusicVolume,v=>PlayerPrefs.SetFloat("YaCasi.U2.Music",v));
        Slider(card.transform,"Efectos",35,EffectsVolume,v=>PlayerPrefs.SetFloat("YaCasi.U2.Effects",v));
        Text(card.transform,"Calidad","Calidad gráfica",new Vector2(-240,-75),new Vector2(400,55),32,Color.white);
        for(int i=0;i<3;i++){int quality=i;var b=Button(card.transform,new[]{"Baja","Media","Alta"}[i],new Vector2(-330+i*330,-140),()=>{QualitySettings.SetQualityLevel(Mathf.RoundToInt((QualitySettings.names.Length-1)*quality/2f),true);PlayerPrefs.SetInt("YaCasi.U2.Quality",quality);});b.GetComponent<RectTransform>().sizeDelta=new Vector2(290,65);}
        Text(card.transform,"Controles","WASD / flechas / joystick: moverte · Shift / gatillo: correr\nE / botón sur: revisar · Esc / Start: pausa\nEn móvil: botones de dirección, correr y revisar.",new Vector2(0,-240),new Vector2(1100,120),27,Color.white);
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
