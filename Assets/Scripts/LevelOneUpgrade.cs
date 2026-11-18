using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>Reglas del nivel 1: filas de estudiantes, tiempo limitado y revisión con memoria.</summary>
public sealed class LevelOneUpgrade : MonoBehaviour
{
    Level01Manager manager; PlayerMovement player; PlayerInteraction interaction;
    LockerInteraction[] lockers; U2GameConfig config;
    readonly List<LineRenderer> borders=new List<LineRenderer>();
    readonly List<GameObject> marks=new List<GameObject>();
    GameObject canvas,overlay; AudioSource music;
    LevelOneHud hud; LevelOneCourtesyRequest courtesy; Material markerMaterial;
    LevelOneMystery mystery; LevelOneStudentDirector studentDirector;
    LevelOneStudentDirector upstairsStudents;
    LevelOneNightSchool nightSchool;
    enum OverlayKind { None, Pause, Help, Completion, Result }
    OverlayKind overlayKind;
    bool helpReturnsToPause;
    int emptyReviews, repeatReviews, collisions;
    bool finished,paused; float elapsed,bumpUntil; int score,calm=3;
    public int Score=>score; public int Calm=>calm;
    public bool Paused=>paused;
    void Start()
    {
        Application.runInBackground=true;
        manager=GetComponent<Level01Manager>(); player=FindFirstObjectByType<PlayerMovement>(); interaction=FindFirstObjectByType<PlayerInteraction>();
        config=Resources.Load<U2GameConfig>("U2GameConfig");
        nightSchool=GetComponent<LevelOneNightSchool>();
        manager.SetRoundTime(nightSchool!=null?nightSchool.roundSeconds:(config!=null?config.roundSeconds:135));
        if(nightSchool!=null)nightSchool.ConfigureRuntime(manager,player,config);
        lockers=FindObjectsByType<LockerInteraction>(FindObjectsSortMode.None);
        System.Array.Sort(lockers,(a,b)=>{int row=b.InteractionPosition.y.CompareTo(a.InteractionPosition.y);return row!=0?row:a.InteractionPosition.x.CompareTo(b.InteractionPosition.x);});
        mystery=GetComponent<LevelOneMystery>();if(mystery==null)mystery=gameObject.AddComponent<LevelOneMystery>();
        mystery.Configure(manager,player,lockers);
        for(int i=0;i<lockers.Length;i++)AddMarker(lockers[i]);
        AudioListener.volume=1;
        music=gameObject.AddComponent<AudioSource>();music.loop=true;music.spatialBlend=0;music.clip=config!=null?config.soundtrack:null;music.volume=U2Presentation.MusicVolume;if(music.clip!=null)music.Play();
        manager.UseExternalPresentation();
        if(interaction!=null)interaction.UseExternalPrompt();
        canvas=U2Presentation.NewCanvas("HUD nivel 1",50);
        canvas.GetComponent<CanvasScaler>().matchWidthOrHeight=0;
        hud=canvas.AddComponent<LevelOneHud>();hud.Initialize(TogglePause,()=>ShowHelp(false),mystery.OpenNotebook);hud.ConfigureMystery(mystery);
        manager.FeedbackShown+=hud.Notify;
        courtesy=gameObject.AddComponent<LevelOneCourtesyRequest>();courtesy.Configure(player,manager);courtesy.Message+=hud.Notify;
        SpawnStudents();if(Application.isMobilePlatform)BuildTouchControls();PlayerPrefs.SetInt(U2Presentation.SaveKey,1);PlayerPrefs.Save();
        LevelOneTypography.Apply(canvas);
        mystery.UI.ShowIntro(null);
    }
    void Update()
    {
        if(manager==null)return;
        if(music!=null)music.volume=U2Presentation.MusicVolume*(paused?.5f:1);
        foreach(var source in GetComponentsInChildren<AudioSource>())if(source!=music)source.volume=U2Presentation.EffectsVolume;
        bool mysteryInput=(mystery!=null&&mystery.UI!=null&&mystery.UI.BlocksGameplayInput)||(nightSchool!=null&&nightSchool.Stairs!=null&&nightSchool.Stairs.InTransit);
        if(!mysteryInput&&((Keyboard.current!=null&&Keyboard.current.escapeKey.wasPressedThisFrame)||(Gamepad.current!=null&&Gamepad.current.startButton.wasPressedThisFrame)))TogglePause();
        if(!mysteryInput&&!finished&&Keyboard.current!=null&&Keyboard.current.f1Key.wasPressedThisFrame)ShowHelp(paused);
        if(!finished&&(manager.IsCompleted||manager.IsFailed)){finished=true;FinishRound();}
        if(manager.IsPlaying){elapsed+=Time.deltaTime;if(!mysteryInput&&Gamepad.current!=null&&Gamepad.current.buttonSouth.wasPressedThisFrame&&interaction!=null)interaction.TryInteract();}
        int number=0;var nearest=interaction!=null&&interaction.enabled?interaction.FindNearestLocker():null;
        for(int i=0;i<lockers.Length;i++)
        {
            bool searched=lockers[i].IsSearched;
            bool close=player!=null&&Vector2.Distance(player.transform.position,lockers[i].InteractionPosition)<6;
            marks[i].SetActive(searched&&close&&!finished);
            borders[i].enabled=manager.IsPlaying&&!manager.HasUSB&&nearest==lockers[i];
            borders[i].startColor=borders[i].endColor=searched?new Color(1,.7f,.2f):new Color(.45f,1,.85f);
            if(nearest==lockers[i])number=i+1;
        }
        if(hud!=null)hud.Refresh(manager,calm,nearest,number);
    }
    void AddMarker(LockerInteraction locker)
    {
        var sr=locker.GetComponentInChildren<SpriteRenderer>();var b=sr!=null?sr.bounds:new Bounds(locker.transform.position,Vector3.one);
        if(markerMaterial==null)markerMaterial=new Material(Shader.Find("Sprites/Default"));
        var go=new GameObject("Contorno de interacción");go.transform.SetParent(locker.transform,false);var line=go.AddComponent<LineRenderer>();line.useWorldSpace=true;line.loop=true;line.positionCount=4;line.widthMultiplier=.035f;line.sharedMaterial=markerMaterial;line.sortingOrder=6;
        line.SetPositions(new[]{new Vector3(b.min.x,b.min.y,0),new Vector3(b.min.x,b.max.y,0),new Vector3(b.max.x,b.max.y,0),new Vector3(b.max.x,b.min.y,0)});line.enabled=false;borders.Add(line);
        var mark=new GameObject("Casillero revisado");mark.transform.SetParent(locker.transform,false);mark.transform.position=b.center+Vector3.up*b.extents.y*.5f;var label=mark.AddComponent<TextMeshPro>();if(LevelOneTypography.Font!=null)label.font=LevelOneTypography.Font;label.text="OK";label.fontSize=2.4f;label.color=new Color(.7f,1,.8f);label.alignment=TextAlignmentOptions.Center;label.rectTransform.sizeDelta=new Vector2(.7f,.35f);label.renderer.sortingOrder=7;mark.SetActive(false);marks.Add(mark);
    }
    public void StopLevelMusic()
    {
        if (music != null) music.Stop();
    }

    // Game Over es música de resultado: no depende del volumen de efectos.
    public void PlayGameOverMusic(AudioClip clip)
    {
        StopLevelMusic();
        if (clip == null) { Debug.LogError("No se asignó la música Game Over.", this); return; }
        if (music == null) music = gameObject.AddComponent<AudioSource>();
        music.playOnAwake = false;
        music.loop = false;
        music.pitch = 1f;
        music.spatialBlend = 0f;
        music.mute = false;
        music.ignoreListenerPause = true;
        music.volume = U2Presentation.MusicVolume;
        music.clip = clip;
        music.Play();
        Debug.Log($"Game Over: {clip.name}; volumen de música: {music.volume:0.00}; reproducción: {music.isPlaying}", this);
    }
    public void Reviewed(bool repeated,bool usb)
    {score=Mathf.Max(0,score+(repeated?-75:(usb?500:30)));if(repeated)repeatReviews++;else if(!usb)emptyReviews++;}
    public void FoundClue(){score+=75;}
    public void BeginReturnSequence(AudioClip clip)
    {
        studentDirector?.BeginReturnRush();
        nightSchool?.Caretaker?.HearNoise(player.transform.position);
        if(music!=null)
        {
            if(clip!=null){music.clip=clip;music.pitch=1;music.Play();}
            else music.pitch=1.12f;
        }
    }
    public void BumpPlayer()
    {
        if(!manager.IsPlaying||player==null||Time.time<bumpUntil)return;
        bumpUntil=Time.time+2;calm--;collisions++;manager.ApplyPenalty(2);manager.ShowFeedback($"Choque con un alumno · −2 s · {collisions}/3");
        if(calm<=0)manager.FailLevel();
    }
    public void TogglePause()
    {
        if(finished)return;
        if(overlayKind==OverlayKind.Help){CloseHelp();return;}
        if(paused){DismissOverlay();return;}
        ShowPause();
    }
    void ShowPause()
    {
        var card=NewOverlay("Pausa",OverlayKind.Pause);
        MenuText(card,"Título","Pausa",310,68,46,new Color(1,.84f,.4f));
        MenuText(card,"Objetivo",manager.HasUSB?"Tienes la USB. Regresa a la salida.":"Encuentra la USB y vuelve a la salida.",246,55,28,Color.white);
        MenuText(card,"Estadísticas",Statistics(),156,120,25,Color.white);
        MenuButton(card,"Continuar",25,DismissOverlay);
        MenuButton(card,"Reiniciar",-62,manager.RestartLevel);
        MenuButton(card,"Ayuda",-149,()=>ShowHelp(true));
        MenuButton(card,"Opciones",-236,()=>{U2Presentation.Options(overlay.transform,null);LevelOneTypography.Apply(overlay);});
        MenuButton(card,"Menú principal",-323,U2Presentation.ReturnToMenu);
        LevelOneTypography.Apply(overlay);
    }
    public void DismissOverlay()
    {
        if(overlay!=null){overlay.SetActive(false);Destroy(overlay);overlay=null;}
        overlayKind=OverlayKind.None;paused=false;Time.timeScale=1;
    }
    void ShowHelp(bool returnToPause)
    {
        if(finished||overlayKind==OverlayKind.Help)return;
        helpReturnsToPause=returnToPause;
        var card=NewOverlay("Ayuda del nivel 1",OverlayKind.Help);
        MenuText(card,"Historia","El edificio está por cerrar",310,70,43,new Color(1,.84f,.4f));
        MenuText(card,"Contexto","Es de noche. El conserje está cerrando los dos pisos.\nInvestiga, abre el candado y regresa a la salida de planta baja.",222,100,29,Color.white);
        string penalties=$"Casillero vacío: −{(config!=null?config.wrongLockerPenalty:3):0} s. Repetir: −{(config!=null?config.repeatLockerPenalty:5):0} s.";
        MenuText(card,"Teclas","WASD / flechas / stick: caminar · Shift / gatillo: correr\nE / botón sur: investigar, hablar o revisar\nN / Libreta: consultar tus tres pistas\nQ / LB: pedir permiso para pasar\nCada choque: −2 s. El tercer choque termina la partida.\nEsc / Start: pausa · F1 / Ayuda: estas indicaciones",50,215,28,Color.white);
        MenuText(card,"Reglas",penalties+" Candado: −4 s. Intendente: dos advertencias; tercera captura, expulsión.\nEntra a un salón y deja de correr para esconderte lejos de la puerta.\nRecorre ambos tramos de la escalera en U para cambiar de piso.\nAzul: planta baja. Verde: segundo piso. La libreta pausa el reloj.",-152,145,26,Color.white);
        MenuButton(card,returnToPause?"Volver a pausa":"Entendido, ¡vamos!",-315,CloseHelp);
        LevelOneTypography.Apply(overlay);
    }
    void CloseHelp()
    {
        PlayerPrefs.SetInt("YaCasi.Level1.ContextualHelp.v1",1);PlayerPrefs.Save();
        if(helpReturnsToPause)ShowPause();else DismissOverlay();
    }
    GameObject NewOverlay(string name,OverlayKind kind)
    {
        if(overlay!=null){overlay.SetActive(false);Destroy(overlay);}
        paused=true;Time.timeScale=0;overlayKind=kind;
        overlay=U2Presentation.NewCanvas(name,100);
        var card=U2Presentation.Card(overlay.transform,name);
        card.GetComponent<RectTransform>().sizeDelta=new Vector2(1120,820);
        return card;
    }
    static void MenuText(GameObject card,string name,string value,float y,float height,float size,Color color)
        =>U2Presentation.Text(card.transform,name,value,new Vector2(0,y),new Vector2(1050,height),size,color);
    static void MenuButton(GameObject card,string caption,float y,UnityEngine.Events.UnityAction action)
    {
        var button=U2Presentation.Button(card.transform,caption,new Vector2(0,y),action);
        button.GetComponent<RectTransform>().sizeDelta=new Vector2(570,68);
        var label=button.GetComponentInChildren<TMP_Text>();label.fontSize=29;label.rectTransform.sizeDelta=new Vector2(550,62);
    }
    string Statistics()
    {
        int searched=0;foreach(var locker in lockers)if(locker.IsSearched)searched++;
        int penalty=Mathf.RoundToInt(Mathf.Max(0,manager.RoundTime-manager.RemainingTime-elapsed));
        return $"Recorrido: {Mathf.FloorToInt(elapsed/60):00}:{Mathf.FloorToInt(elapsed%60):00} · Descuento: {penalty} s\n"+
            $"Choques: {collisions}/3 · Casilleros: {searched}/{lockers.Length}\n"+
            $"Pistas: {mystery.DiscoveredCount}/3 · Equivocados: {mystery.WrongLockers} · Candado: {mystery.WrongCodes} · Puntos: {score}";
    }
    void FinishRound()
    {
        mystery?.UI?.Close();
        canvas.SetActive(false);
        if(!manager.IsCompleted){ShowResult();return;}
        score+=Mathf.CeilToInt(manager.RemainingTime)*10+calm*100;
        PlayerPrefs.SetInt("YaCasi.U2.Win",1);
        PlayerPrefs.SetInt("YaCasi.U2.Best",Mathf.Max(score,PlayerPrefs.GetInt("YaCasi.U2.Best",0)));PlayerPrefs.Save();
        var card=NewOverlay("Nivel superado",OverlayKind.Completion);
        MenuText(card,"Título","¡Nivel superado!",140,100,52,new Color(1,.84f,.4f));
        MenuText(card,"Cierre","USB recuperada. Llegaste a tiempo a la salida.\nGalindo ya tiene sus proyectos para continuar.",0,130,30,Color.white);
        MenuButton(card,"Ver resumen",-180,ShowResult);
        LevelOneTypography.Apply(overlay);
        StartCoroutine(CompletionDelay());
    }
    System.Collections.IEnumerator CompletionDelay()
    {
        yield return new WaitForSecondsRealtime(2.8f);
        if(overlayKind==OverlayKind.Completion)ShowResult();
    }
    void ShowResult()
    {
        var card=NewOverlay("Resultado del nivel 1",OverlayKind.Result);
        MenuText(card,"Título",manager.IsCompleted?"¡USB recuperada!":(nightSchool!=null&&nightSchool.Caretaker!=null&&nightSchool.Caretaker.Warnings>=3?"Expulsado del edificio":calm<=0?"Tres choques: partida terminada":"Se acabó el tiempo"),305,90,44,new Color(1,.84f,.4f));
        MenuText(card,"Resultado",manager.IsCompleted?"Llegaste a tiempo a la salida.":"Recuerda los casilleros revisados y pide permiso para pasar.",212,80,28,Color.white);
        MenuText(card,"Estadísticas",Statistics()+$"\n\nTiempo restante: {Mathf.CeilToInt(manager.RemainingTime)} s · Mejor: {PlayerPrefs.GetInt("YaCasi.U2.Best",0)}\n"+(manager.IsCompleted?mystery.Awards(calm):(nightSchool!=null&&nightSchool.Caretaker!=null&&nightSchool.Caretaker.Warnings>=3?"El intendente te expulsó tras tres capturas.":"Investiga las tres pistas antes de elegir un casillero.")),25,240,27,Color.white);
        MenuButton(card,"Reintentar",-170,manager.RestartLevel);
        MenuButton(card,"Menú principal",-260,U2Presentation.ReturnToMenu);
        MenuText(card,"Fuente","Tipografía: Redaction 35 · SIL Open Font License",-352,40,21,Color.white);
        LevelOneTypography.Apply(overlay);
    }
    void SpawnStudents()
    {
        if(nightSchool!=null){gameObject.AddComponent<LevelOneClosingFlow>();return;}
        studentDirector=gameObject.AddComponent<LevelOneStudentDirector>();
        if(nightSchool!=null)
        {
            studentDirector.Configure(manager,player,config,mystery.TopRow,System.Array.Empty<LockerInteraction>());
            upstairsStudents=gameObject.AddComponent<LevelOneStudentDirector>();
            upstairsStudents.Configure(manager,player,config,mystery.BottomRow,System.Array.Empty<LockerInteraction>());
        }
        else studentDirector.Configure(manager,player,config,mystery.TopRow,mystery.BottomRow);
    }
    void BuildTouchControls()
    {
        hud.UseTouchLayout();
        Vector2[] directions={Vector2.left,Vector2.right,Vector2.up,Vector2.down};
        string[] captions={"Izq.","Der.","Arriba","Abajo"};
        Vector2[] places={new Vector2(80,145),new Vector2(300,145),new Vector2(190,240),new Vector2(190,50)};
        for(int i=0;i<4;i++)
        {
            var button=TouchButton(captions[i],Vector2.zero,places[i],new Vector2(105,85),()=>{});
            button.gameObject.AddComponent<TouchGameControls>().direction=directions[i];
        }
        var review=TouchButton("Revisar",Vector2.right,new Vector2(-145,55),new Vector2(230,78),()=>{});
        review.gameObject.AddComponent<TouchGameControls>().interact=true;
        var run=TouchButton("Correr",Vector2.right,new Vector2(-145,150),new Vector2(230,78),()=>{});
        run.gameObject.AddComponent<TouchGameControls>().sprint=true;
        TouchButton("Con permiso",Vector2.right,new Vector2(-145,245),new Vector2(230,78),()=>courtesy.RequestPassage());
    }
    Button TouchButton(string caption,Vector2 anchor,Vector2 position,Vector2 size,UnityEngine.Events.UnityAction action)
    {
        var button=U2Presentation.Button(canvas.transform,caption,position,action);
        var rect=button.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=anchor;rect.sizeDelta=size;
        var label=button.GetComponentInChildren<TMP_Text>();label.fontSize=25;label.rectTransform.sizeDelta=size-new Vector2(8,8);
        return button;
    }
    void OnDestroy()
    {
        if(manager!=null&&hud!=null)manager.FeedbackShown-=hud.Notify;
        if(courtesy!=null&&hud!=null)courtesy.Message-=hud.Notify;
        Time.timeScale=1;
        if(canvas!=null)Destroy(canvas);if(overlay!=null)Destroy(overlay);
        if(markerMaterial!=null)Destroy(markerMaterial);
    }
}
