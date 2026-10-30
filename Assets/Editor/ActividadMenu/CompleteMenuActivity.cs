// Generador de la actividad Menú II. Conserva el avance original y crea escenas independientes.
using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using YaCasi.ActividadMenu;

[InitializeOnLoad]
public static class CompleteMenuActivity
{
    const string Folder = "Assets/ActividadMenu";
    const string MenuPath = Folder + "/Scenes/MenuActividadCompleta.unity";
    const string TestPath = Folder + "/Scenes/PruebaMenu.unity";
    const string Output = "Z:/DOCUMENTOS/Barron/MENU/EntregaActividad";
    static TMP_FontAsset font;
    static readonly Color Background = new Color32(10,61,74,255);
    static readonly Color Cream = new Color32(232,244,244,255);
    static CompleteMenuActivity() { EditorApplication.delayCall += Auto; }
    static void Auto()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) { EditorApplication.delayCall += Auto; return; }
        if (EditorApplication.isPlayingOrWillChangePlaymode || File.Exists(MenuPath)) return;
        if (SceneManager.GetActiveScene().name == "MenuActividad") Generate();
    }
    [MenuItem("Ya Casi/Actividad Menu II/Completar actividad en escenas independientes")]
    public static void Generate()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogWarning("Sal de Play para crear la actividad."); return; }
        Directory.CreateDirectory(Output);
        var previous = SceneManager.GetActiveScene();
        if (previous.isDirty && !string.IsNullOrEmpty(previous.path)) EditorSceneManager.SaveScene(previous);
        if (!string.IsNullOrEmpty(previous.path)) File.Copy(previous.path, Output + "/MenuActividad_antes.unity.bak", true);
        Directory.CreateDirectory(Folder + "/Scenes");
        Directory.CreateDirectory(Folder + "/Audio");
        Directory.CreateDirectory(Folder + "/Prefabs");
        AssetDatabase.Refresh();
        font = TMP_Settings.defaultFontAsset;
        var music = CreateMusic();
        var click = CreateClick();
        CreateTest();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camera = new GameObject("Main Camera",typeof(Camera),typeof(AudioListener)).GetComponent<Camera>();
        camera.tag = "MainCamera"; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Background;
        camera.transform.position = new Vector3(0,0,-10);
        var canvas = CanvasRoot();
        var main = Panel("MainPanel",canvas.transform);
        var settings = Panel("SettingsPanel",canvas.transform);
        var scores = Panel("ScoresPanel",canvas.transform);
        var credits = Panel("CreditsPanel",canvas.transform);
        var mainLayout = main.AddComponent<VerticalLayoutGroup>();
        mainLayout.spacing = 24; mainLayout.childAlignment = TextAnchor.MiddleCenter;
        mainLayout.childControlWidth = mainLayout.childControlHeight = false;
        mainLayout.childForceExpandWidth = mainLayout.childForceExpandHeight = false;
        var heading = Text("GameTitle",main.transform,"¡YA CASI!",70,620,90);
        var play = MenuButton("PlayButton",main.transform,"Jugar","arrow_right");
        var options = MenuButton("SettingsButton",main.transform,"Ajustes","settings");
        var scoreButton = MenuButton("ScoresButton",main.transform,"Puntuaciones","star");
        var creditButton = MenuButton("CreditsButton",main.transform,"Créditos","info");
        var quit = MenuButton("QuitButton",main.transform,"Salir","x");
        SettingsLayout(settings);
        Text("SettingsTitle",settings.transform,"Ajustes",60,560,80);
        var inputGO = DefaultControl(DefaultControls.CreateInputField,new DefaultControls.Resources(),settings.transform,"PlayerNameField");
        var legacyInput = inputGO.GetComponent<InputField>();
        var textGO = legacyInput.textComponent.gameObject;
        var placeholderGO = legacyInput.placeholder.gameObject;
        UnityEngine.Object.DestroyImmediate(legacyInput);
        UnityEngine.Object.DestroyImmediate(textGO.GetComponent<UnityEngine.UI.Text>());
        UnityEngine.Object.DestroyImmediate(placeholderGO.GetComponent<UnityEngine.UI.Text>());
        var input = inputGO.AddComponent<TMP_InputField>();
        var inputText = textGO.AddComponent<TextMeshProUGUI>(); ApplyText(inputText,36,Background,TextAlignmentOptions.MidlineLeft);
        var placeholder = placeholderGO.AddComponent<TextMeshProUGUI>(); ApplyText(placeholder,32,new Color32(90,125,125,255),TextAlignmentOptions.MidlineLeft);placeholder.text="Tu nombre…";
        var viewport = Rect("Text Area",inputGO.transform);Stretch(viewport,16,8,16,8);viewport.gameObject.AddComponent<RectMask2D>();
        textGO.transform.SetParent(viewport,false);Stretch(textGO.GetComponent<RectTransform>());
        placeholderGO.transform.SetParent(viewport,false);Stretch(placeholderGO.GetComponent<RectTransform>());
        input.textViewport=viewport;input.textComponent=inputText;input.placeholder=placeholder;input.targetGraphic=inputGO.GetComponent<Image>();
        input.contentType=TMP_InputField.ContentType.Standard;input.characterLimit=16;
        Size(inputGO,560,55);
        // TMP crea plantilla, ScrollRect y Toggle del dropdown con sus referencias correctas.
        var dropdownGO=TMP_DefaultControls.CreateDropdown(new TMP_DefaultControls.Resources());
        dropdownGO.name="QualityDropdown";dropdownGO.transform.SetParent(settings.transform,false);Size(dropdownGO,560,55);
        var dropdown=dropdownGO.GetComponent<TMP_Dropdown>();dropdown.ClearOptions();dropdown.AddOptions(new System.Collections.Generic.List<string>{"Baja","Media","Alta","Ultra"});dropdown.value=2;
        foreach(var t in dropdownGO.GetComponentsInChildren<TMP_Text>(true))ApplyText(t,t==dropdown.captionText?34:30,Background,TextAlignmentOptions.MidlineLeft);
        Text("BrightnessLabel",settings.transform,"Brillo",36,560,50).color=new Color32(240,193,75,255);
        var brightness=Slider("BrightnessSlider",settings.transform,.8f);
        var toggleGO=DefaultControl(DefaultControls.CreateToggle,new DefaultControls.Resources(),settings.transform,"FullscreenToggle");Size(toggleGO,560,65);
        var toggle=toggleGO.GetComponent<Toggle>();toggle.isOn=true;
        var labelGO=toggleGO.transform.Find("Label").gameObject;UnityEngine.Object.DestroyImmediate(labelGO.GetComponent<UnityEngine.UI.Text>());
        var toggleLabel=labelGO.AddComponent<TextMeshProUGUI>();ApplyText(toggleLabel,36,new Color32(240,193,75,255),TextAlignmentOptions.MidlineLeft);toggleLabel.text="Pantalla completa";
        var toggleBack=toggleGO.transform.Find("Background").GetComponent<RectTransform>();toggleBack.anchorMin=toggleBack.anchorMax=new Vector2(0,.5f);toggleBack.anchoredPosition=new Vector2(30,0);toggleBack.sizeDelta=new Vector2(60,60);
        toggle.targetGraphic.GetComponent<Image>().sprite=Sprite("Textures/controls/toggle_bg_100.png");
        var check=toggle.graphic.GetComponent<Image>();check.sprite=Icon("checkmark");check.color=Background;check.rectTransform.anchorMin=check.rectTransform.anchorMax=new Vector2(.5f,.5f);check.rectTransform.sizeDelta=new Vector2(34,34);check.rectTransform.anchoredPosition=Vector2.zero;
        var labelRect=labelGO.GetComponent<RectTransform>();labelRect.offsetMin=new Vector2(80,0);labelRect.offsetMax=Vector2.zero;
        Text("VolumeLabel",settings.transform,"Volumen de la música",32,560,45);
        var volume=Slider("VolumeSlider",settings.transform,.6f);
        Text("SettingsHint",settings.transform,"La pantalla completa se comprueba en el ejecutable.",24,900,45);
        var backSettings=Back(settings);
        SettingsLayout(scores);Text("ScoresTitle",scores.transform,"Mejores puntuaciones",56,900,80);
        Text("ExampleNotice",scores.transform,"Datos de ejemplo para practicar el diseño",28,900,55);
        foreach(string line in new[]{"1. JugadorA — 12500","2. JugadorB — 9800","3. JugadorC — 7600","4. JugadorD — 5400"})Text("Score",scores.transform,line,36,700,65);
        var backScores=Back(scores);
        var creditText=Text("CreditsText",credits.transform,"¡YA CASI!\n\nAgustín López Parra\nCarlos Manuel Palma Muñoz\n\nCreación de Videojuegos · UTNG\nDocente: Gabriel Barrón Rodríguez\n\nInterfaz: Simple Button Set 01 — That Witch Design\nMúsica y clic: sonidos sintetizados para esta actividad",34,1250,700);
        creditText.rectTransform.anchoredPosition=Vector2.zero;
        var backCredits=Back(credits);
        var dim=Image("BrightnessOverlay",canvas.transform,Color.clear);Stretch(dim.rectTransform);dim.raycastTarget=false;
        var managerGO=new GameObject("MenuController");var manager=managerGO.AddComponent<MenuController>();
        manager.mainPanel=main;manager.settingsPanel=settings;manager.scoresPanel=scores;manager.creditsPanel=credits;
        manager.playButton=play;manager.settingsButton=options;manager.scoresButton=scoreButton;manager.creditsButton=creditButton;manager.quitButton=quit;
        manager.backButtons=new[]{backSettings,backScores,backCredits};manager.playerNameField=input;manager.qualityDropdown=dropdown;manager.brightnessSlider=brightness;manager.volumeSlider=volume;manager.fullscreenToggle=toggle;manager.brightnessOverlay=dim;
        int max=Mathf.Max(0,QualitySettings.names.Length-1);manager.qualityIndices=new[]{0,Mathf.RoundToInt(max*.4f),Mathf.RoundToInt(max*.8f),max};
        var musicGO=new GameObject("MusicPlayer",typeof(AudioSource));musicGO.transform.SetParent(managerGO.transform);var source=musicGO.GetComponent<AudioSource>();source.clip=music;source.loop=true;source.playOnAwake=true;source.volume=.6f;
        manager.uiAudio=managerGO.AddComponent<AudioSource>();manager.uiAudio.playOnAwake=false;manager.clickClip=click;
        new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));
        PrefabUtility.SaveAsPrefabAsset(play.gameObject,Folder+"/Prefabs/MenuButton.prefab");
        settings.SetActive(false);scores.SetActive(false);credits.SetActive(false);
        EditorSceneManager.SaveScene(scene,MenuPath);
        var list=EditorBuildSettings.scenes.ToList();foreach(string path in new[]{MenuPath,TestPath})if(!list.Any(s=>s.path==path))list.Add(new EditorBuildSettingsScene(path,true));EditorBuildSettings.scenes=list.ToArray();
        WriteGuide(manager);
        AssetDatabase.SaveAssets();
        Debug.Log("ACTIVIDAD MENU II COMPLETA: "+MenuPath+". Todos los controles conectados; nivel 1 intacto.");
        SessionState.SetInt("ActividadMenuTestStep",0);
        EditorApplication.delayCall+=()=>EditorApplication.isPlaying=true;
    }
    static void CreateTest()
    {
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var camera=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener)).GetComponent<Camera>();camera.backgroundColor=Background;camera.clearFlags=CameraClearFlags.SolidColor;
        var canvas=CanvasRoot();var panel=Panel("TestPanel",canvas.transform);SettingsLayout(panel);
        Text("Title",panel.transform,"¡La navegación funciona!",60,1300,100);
        Text("Description",panel.transform,"Esta es la escena de prueba de la actividad.\nEl nivel 1 y sus sistemas permanecen separados.",36,1300,200);
        var button=MenuButton("ReturnButton",panel.transform,"Volver al menú","arrow_left");button.gameObject.AddComponent<LayoutElement>().preferredHeight=95;
        var ret=new GameObject("ActivityMenuReturn").AddComponent<ActivityMenuReturn>();ret.backButton=button;
        new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));
        EditorSceneManager.SaveScene(scene,TestPath);
    }
    static Canvas CanvasRoot()
    {
        var go=new GameObject("Canvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));var c=go.GetComponent<Canvas>();c.renderMode=RenderMode.ScreenSpaceOverlay;
        var s=go.GetComponent<CanvasScaler>();s.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;s.referenceResolution=new Vector2(1920,1080);s.matchWidthOrHeight=.5f;return c;
    }
    static GameObject Panel(string name,Transform parent){var i=Image(name,parent,Background);Stretch(i.rectTransform);return i.gameObject;}
    static RectTransform Rect(string name,Transform parent){var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);return r;}
    static Image Image(string name,Transform parent,Color color){var r=Rect(name,parent);var i=r.gameObject.AddComponent<Image>();i.color=color;return i;}
    static void Stretch(RectTransform r,float l=0,float t=0,float right=0,float bottom=0){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=new Vector2(l,bottom);r.offsetMax=new Vector2(-right,-t);}
    static TMP_Text Text(string name,Transform parent,string value,float size,float width,float height)
    {
        var r=Rect(name,parent);r.sizeDelta=new Vector2(width,height);var t=r.gameObject.AddComponent<TextMeshProUGUI>();ApplyText(t,size,Cream,TextAlignmentOptions.Center);t.text=value;Size(r.gameObject,width,height);return t;
    }
    static void ApplyText(TMP_Text t,float size,Color color,TextAlignmentOptions alignment){t.font=font;t.fontSize=size;t.color=color;t.alignment=alignment;t.raycastTarget=false;}
    static void Size(GameObject go,float w,float h){var r=go.GetComponent<RectTransform>();r.sizeDelta=new Vector2(w,h);var l=go.GetComponent<LayoutElement>()??go.AddComponent<LayoutElement>();l.preferredWidth=w;l.preferredHeight=h;l.flexibleWidth=0;l.flexibleHeight=0;}
    static Sprite Sprite(string path){return AssetDatabase.LoadAssetAtPath<Sprite>("Assets/FreeButtonSet/"+path);}
    static Sprite Icon(string name){return Sprite("Textures/icons/64x64/"+name+".png")??Sprite("Textures/controls/"+name+".png");}
    static Button MenuButton(string name,Transform parent,string caption,string icon)
    {
        var image=Image(name,parent,Color.white);image.sprite=Sprite("Textures/buttons/button_100.png");image.type=UnityEngine.UI.Image.Type.Sliced;Size(image.gameObject,580,95);
        var b=image.gameObject.AddComponent<Button>();b.targetGraphic=image;var colors=b.colors;colors.highlightedColor=new Color32(62,207,207,255);colors.pressedColor=new Color32(160,160,160,255);colors.fadeDuration=.1f;b.colors=colors;
        var layout=image.gameObject.AddComponent<HorizontalLayoutGroup>();layout.spacing=14;layout.childAlignment=TextAnchor.MiddleCenter;layout.childControlWidth=layout.childControlHeight=true;layout.childForceExpandWidth=layout.childForceExpandHeight=false;
        var i=Image("Icon",image.transform,Background);i.sprite=Icon(icon)??Icon("info");i.preserveAspect=true;i.raycastTarget=false;Size(i.gameObject,44,44);
        var label=Text("Text (TMP)",image.transform,caption,56,400,75);label.color=Background;label.textWrappingMode=TextWrappingModes.NoWrap;
        return b;
    }
    static Button Back(GameObject panel)
    {
        var image=Image("BackButton",panel.transform,Color.white);image.sprite=Sprite("Textures/buttons/button_round_130.png");image.type=UnityEngine.UI.Image.Type.Sliced;
        var r=image.rectTransform;r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=new Vector2(65,-65);r.sizeDelta=new Vector2(85,85);image.gameObject.AddComponent<LayoutElement>().ignoreLayout=true;
        var button=image.gameObject.AddComponent<Button>();button.targetGraphic=image;var colors=button.colors;colors.highlightedColor=new Color32(62,207,207,255);colors.pressedColor=Color.gray;button.colors=colors;
        var icon=Image("Icon",image.transform,Background);icon.sprite=Icon("arrow_left");icon.preserveAspect=true;icon.raycastTarget=false;icon.rectTransform.sizeDelta=new Vector2(44,44);return button;
    }
    static void SettingsLayout(GameObject go)
    {
        var l=go.AddComponent<VerticalLayoutGroup>();l.padding=new RectOffset(100,100,70,70);l.spacing=32;l.childAlignment=TextAnchor.UpperCenter;l.childControlWidth=l.childControlHeight=true;l.childForceExpandWidth=l.childForceExpandHeight=false;
    }
    static GameObject DefaultControl(Func<DefaultControls.Resources,GameObject> factory,DefaultControls.Resources resources,Transform parent,string name){var go=factory(resources);go.name=name;go.transform.SetParent(parent,false);return go;}
    static Slider Slider(string name,Transform parent,float value)
    {
        var go=DefaultControl(DefaultControls.CreateSlider,new DefaultControls.Resources(),parent,name);Size(go,560,48);var slider=go.GetComponent<Slider>();slider.minValue=0;slider.maxValue=1;slider.value=value;slider.wholeNumbers=false;
        var background=go.transform.Find("Background").GetComponent<Image>();background.sprite=Sprite("Textures/controls/universal_panel_10.png");background.type=UnityEngine.UI.Image.Type.Sliced;background.color=new Color(.15f,.25f,.28f);
        var fill=slider.fillRect.GetComponent<Image>();fill.sprite=background.sprite;fill.type=UnityEngine.UI.Image.Type.Sliced;fill.color=new Color32(62,207,207,255);
        var handle=slider.handleRect.GetComponent<Image>();handle.sprite=Sprite("Textures/controls/slider_handle.png");handle.preserveAspect=true;slider.handleRect.sizeDelta=new Vector2(40,0);return slider;
    }
    static AudioClip CreateMusic(){return WriteWave("MusicaMenu",8f,true);}
    static AudioClip CreateClick(){return WriteWave("ClickMenu",.09f,false);}
    static AudioClip WriteWave(string name,float duration,bool music)
    {
        string path=Folder+"/Audio/"+name+".wav";if(File.Exists(path))return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        const int rate=22050;int count=(int)(duration*rate);
        using(var w=new BinaryWriter(File.Create(path)))
        {
            w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));w.Write(36+count*2);w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));w.Write(16);w.Write((short)1);w.Write((short)1);w.Write(rate);w.Write(rate*2);w.Write((short)2);w.Write((short)16);w.Write(System.Text.Encoding.ASCII.GetBytes("data"));w.Write(count*2);
            float[] notes={261.63f,329.63f,392f,329.63f,293.66f,349.23f,440f,349.23f,261.63f,329.63f,392f,523.25f,392f,329.63f,293.66f,196f};
            for(int i=0;i<count;i++){float t=i/(float)rate;float tone=music?notes[Mathf.Min(notes.Length-1,(int)(t/.5f))]:700f;float phase=music?(t%.5f)/.5f:t/duration;float env=Mathf.Sin(Mathf.PI*phase)*Mathf.Exp(-phase*2);float sample=Mathf.Sin(2*Mathf.PI*tone*t)*env*(music?.18f:.3f);w.Write((short)(sample*short.MaxValue));}
        }
        AssetDatabase.ImportAsset(path);return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
    }
    static void WriteGuide(MenuController manager)
    {
        string mappings=string.Join(", ",manager.qualityIndices.Select((q,i)=>new[]{"Baja","Media","Alta","Ultra"}[i]+" → "+QualitySettings.names[q]));
        File.WriteAllText(Output+"/LEEME_ACTIVIDAD.md","# Actividad Menú II\n\nAbrir "+MenuPath+". La escena original MenuActividad y el nivel 1 no se sustituyen.\n\nPuntos 1–5: sprites 9-slice, Canvas 1920×1080 match .5, cuatro paneles, Layout Groups, botones con iconos y prefab.\nPunto 6: nombre limitado a 16 caracteres, calidad, brillo real por atenuación del Canvas, pantalla completa y volumen opcional.\nPuntos 7–8: puntuaciones de ejemplo y créditos.\nPunto 9: música sintetizada propia, en bucle, MusicPlayer.\nPunto 10: referencias conectadas en MenuController; Jugar carga PruebaMenu, Volver retorna al menú.\nPunto 11: capturas y compilación de la actividad.\n\nCalidad usa los niveles reales existentes, sin modificar Project Settings: "+mappings+". Al salir se restaura la calidad anterior. Pantalla completa debe comprobarse en Windows; el Editor no ofrece una verificación fiel.\n\nLos nombres y ajustes se guardan con claves YaCasi.ActividadMenu.*. El volumen solo afecta al audio del menú. No se cambia la escena inicial del juego.\n\nRecursos: Simple Button Set 01 — That Witch Design, https://assetstore.unity.com/packages/2d/gui/icons/simple-button-set-01-153979 . Fuente Liberation Sans (TMP Essentials); música y clic sintetizados para la actividad.\n");
    }
    [MenuItem("Ya Casi/Actividad Menu II/Compilar entrega Windows")]
    public static void Build()
    {
        Directory.CreateDirectory(Output+"/Windows");
        var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{MenuPath,TestPath},locationPathName=Output+"/Windows/ActividadMenu.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
        File.WriteAllText(Output+"/ResultadoCompilacion.txt",result.summary.result+"\nErrores: "+result.summary.totalErrors+"\nAvisos: "+result.summary.totalWarnings);
        Debug.Log("ACTIVIDAD MENU BUILD: "+result.summary.result);
    }
}
