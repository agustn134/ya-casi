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
    GameObject canvas,overlay; TMP_Text stats; AudioSource music;
    bool finished,paused; float elapsed,bumpUntil; int score,calm=3;
    public int Score=>score; public int Calm=>calm;
    public bool Paused=>paused;
    void Start()
    {
        Application.runInBackground=true;
        manager=GetComponent<Level01Manager>(); player=FindFirstObjectByType<PlayerMovement>(); interaction=FindFirstObjectByType<PlayerInteraction>();
        config=Resources.Load<U2GameConfig>("U2GameConfig");
        manager.SetRoundTime(config!=null?config.roundSeconds:135);
        lockers=FindObjectsByType<LockerInteraction>(FindObjectsSortMode.None);
        int chosen=lockers.Length>0?Random.Range(0,lockers.Length):-1;
        for(int i=0;i<lockers.Length;i++){lockers[i].InitializeRound(manager,i==chosen);AddMarker(lockers[i]);}
        AudioListener.volume=1;
        music=gameObject.AddComponent<AudioSource>();music.loop=true;music.spatialBlend=0;music.clip=config!=null?config.soundtrack:null;music.volume=U2Presentation.MusicVolume;if(music.clip!=null)music.Play();
        canvas=U2Presentation.NewCanvas("HUD dificultad",50);
        var strip=U2Presentation.Image(canvas.transform,"Estado",new Color(.05f,.09f,.12f,.95f));strip.rectTransform.anchorMin=strip.rectTransform.anchorMax=new Vector2(.5f,1);strip.rectTransform.pivot=new Vector2(.5f,1);strip.rectTransform.anchoredPosition=new Vector2(0,-115);strip.rectTransform.sizeDelta=new Vector2(1030,65);
        stats=U2Presentation.Text(strip.transform,"Estado texto","",Vector2.zero,new Vector2(1000,60),29,Color.white);
        U2Presentation.Button(canvas.transform,"Pausa",new Vector2(800,-430),TogglePause).GetComponent<RectTransform>().sizeDelta=new Vector2(250,70);
        SpawnStudents();if(Application.isMobilePlatform)BuildTouchControls();PlayerPrefs.SetInt(U2Presentation.SaveKey,1);PlayerPrefs.Save();
        if(!PlayerPrefs.HasKey("YaCasi.U2.Tutorial"))ShowTutorial();
    }
    void Update()
    {
        if(manager==null)return;
        if(music!=null)music.volume=U2Presentation.MusicVolume*(paused?.5f:1);
        foreach(var source in GetComponentsInChildren<AudioSource>())if(source!=music)source.volume=U2Presentation.EffectsVolume;
        if((Keyboard.current!=null&&Keyboard.current.escapeKey.wasPressedThisFrame)||(Gamepad.current!=null&&Gamepad.current.startButton.wasPressedThisFrame))TogglePause();
        if(!finished&&(manager.IsCompleted||manager.IsFailed)){finished=true;ShowResult();}
        if(manager.IsPlaying){elapsed+=Time.deltaTime;if(Gamepad.current!=null&&Gamepad.current.buttonSouth.wasPressedThisFrame&&interaction!=null)interaction.TryInteract();}
        int count=0;var nearest=interaction!=null&&interaction.enabled?interaction.FindNearestLocker():null;
        for(int i=0;i<lockers.Length;i++){bool searched=lockers[i].IsSearched;if(searched)count++;marks[i].SetActive(searched);borders[i].enabled=manager.IsPlaying&&nearest==lockers[i];borders[i].startColor=borders[i].endColor=searched?new Color(1,.7f,.2f):Color.white;}
        stats.text=$"Calma: {calm}/3  ·  Puntos: {score}  ·  Revisados: {count}/{lockers.Length}";
    }
    void AddMarker(LockerInteraction locker)
    {
        var sr=locker.GetComponentInChildren<SpriteRenderer>();var b=sr!=null?sr.bounds:new Bounds(locker.transform.position,Vector3.one);
        var go=new GameObject("Contorno de interacción");go.transform.SetParent(locker.transform,false);var line=go.AddComponent<LineRenderer>();line.useWorldSpace=true;line.loop=true;line.positionCount=4;line.widthMultiplier=.035f;line.material=new Material(Shader.Find("Sprites/Default"));line.sortingOrder=6;
        line.SetPositions(new[]{new Vector3(b.min.x,b.min.y,0),new Vector3(b.min.x,b.max.y,0),new Vector3(b.max.x,b.max.y,0),new Vector3(b.max.x,b.min.y,0)});line.enabled=false;borders.Add(line);
        var mark=new GameObject("Casillero revisado");mark.transform.SetParent(locker.transform,false);mark.transform.position=b.center+Vector3.up*b.extents.y*.5f;var label=mark.AddComponent<TextMeshPro>();if(config!=null&&config.font!=null)label.font=config.font;label.text="OK";label.fontSize=3;label.color=new Color(.7f,1,.8f);label.alignment=TextAlignmentOptions.Center;label.rectTransform.sizeDelta=new Vector2(1,.5f);label.renderer.sortingOrder=7;mark.SetActive(false);marks.Add(mark);
    }
    public void Reviewed(bool repeated,bool usb)
    {score=Mathf.Max(0,score+(repeated?-75:(usb?500:30)));}
    public void BumpPlayer()
    {
        if(!manager.IsPlaying||player==null||!player.IsRunning||Time.time<bumpUntil)return;
        bumpUntil=Time.time+2;calm--;manager.ApplyPenalty(2);manager.ShowFeedback("¡Con cuidado! Chocar al correr: −2 s y −1 de calma.");
        if(calm<=0)manager.FailLevel();
    }
    public void TogglePause()
    {
        if(finished)return;
        if(paused){DismissOverlay();return;}
        paused=true;Time.timeScale=0;overlay=U2Presentation.NewCanvas("Pausa",100);var card=U2Presentation.Card(overlay.transform,"Pausa");
        U2Presentation.Text(card.transform,"Título","PAUSA",new Vector2(0,300),new Vector2(1000,80),60,new Color(1,.84f,.4f));
        U2Presentation.Text(card.transform,"Ayuda","Nivel 1 · El Caos del Casillero\nCamina entre las filas; correr contra alguien reduce tu calma.",new Vector2(0,180),new Vector2(1100,130),32,Color.white);
        U2Presentation.Button(card.transform,"Continuar",new Vector2(0,40),DismissOverlay);
        U2Presentation.Button(card.transform,"Reiniciar",new Vector2(0,-60),manager.RestartLevel);
        U2Presentation.Button(card.transform,"Opciones",new Vector2(0,-160),()=>U2Presentation.Options(overlay.transform,null));
        U2Presentation.Button(card.transform,"Menú principal",new Vector2(0,-260),U2Presentation.ReturnToMenu);
    }
    public void DismissOverlay(){if(overlay!=null)Destroy(overlay);paused=false;Time.timeScale=1;}
    void ShowTutorial()
    {
        paused=true;Time.timeScale=0;overlay=U2Presentation.NewCanvas("Tutorial nivel 1",100);var card=U2Presentation.Card(overlay.transform,"Tutorial");
        U2Presentation.Text(card.transform,"Título","¡EL EDIFICIO ESTÁ POR CERRAR!",new Vector2(0,290),new Vector2(1160,100),48,new Color(1,.84f,.4f));
        U2Presentation.Text(card.transform,"Reglas","Olvidaste tu USB en un casillero. Encuéntrala y vuelve a la salida.\n\nWASD / flechas: moverte · Shift: correr · E: revisar\nCasillero vacío: −3 s. Repetir uno revisado: −5 s.\nLos casilleros revisados muestran OK.\nEvita las filas de estudiantes: chocar corriendo quita calma.\nCon 0 de calma o sin tiempo, pierdes. Esc: pausa.",new Vector2(0,10),new Vector2(1130,430),34,Color.white);
        U2Presentation.Button(card.transform,"¡Vamos!",new Vector2(0,-300),()=>{PlayerPrefs.SetInt("YaCasi.U2.Tutorial",1);DismissOverlay();});
    }
    void ShowResult()
    {
        DismissOverlay();if(manager.IsCompleted){score+=Mathf.CeilToInt(manager.RemainingTime)*10+calm*100;PlayerPrefs.SetInt("YaCasi.U2.Win",1);PlayerPrefs.SetInt("YaCasi.U2.Best",Mathf.Max(score,PlayerPrefs.GetInt("YaCasi.U2.Best",0)));PlayerPrefs.Save();}
        overlay=U2Presentation.NewCanvas("Resultado nivel 1",100);var card=U2Presentation.Card(overlay.transform,"Resultado");
        U2Presentation.Text(card.transform,"Título",manager.IsCompleted?"¡USB RECUPERADA!":(calm<=0?"NECESITAS UN RESPIRO":"SE ACABÓ EL TIEMPO"),new Vector2(0,300),new Vector2(1160,100),52,new Color(1,.84f,.4f));
        U2Presentation.Text(card.transform,"Resultado",(manager.IsCompleted?"Llegaste a tiempo a la salida.":"Respira, evita las filas y recuerda los casilleros revisados.")+$"\n\nPuntaje: {score}\nTiempo de juego: {Mathf.FloorToInt(elapsed/60):00}:{Mathf.FloorToInt(elapsed%60):00}\nTiempo restante: {Mathf.CeilToInt(manager.RemainingTime)} s\nMejor puntaje: {PlayerPrefs.GetInt("YaCasi.U2.Best",0)}",new Vector2(0,70),new Vector2(1130,300),34,Color.white);
        U2Presentation.Button(card.transform,"Reintentar",new Vector2(0,-150),manager.RestartLevel);
        U2Presentation.Button(card.transform,"Menú principal",new Vector2(0,-250),U2Presentation.ReturnToMenu);
        U2Presentation.Text(card.transform,"Siguiente nivel","Nivel 2 en desarrollo",new Vector2(0,-350),new Vector2(900,60),28,Color.white);
    }
    void SpawnStudents()
    {
        if(player==null||lockers.Length<2)return;
        float min=float.MaxValue,max=float.MinValue,y=0;
        foreach(var locker in lockers){min=Mathf.Min(min,locker.InteractionPosition.x);max=Mathf.Max(max,locker.InteractionPosition.x);y+=locker.InteractionPosition.y;}y/=lockers.Length;
        // Dos carriles conectados: ida frente a casilleros, vuelta por el pasillo.
        float upper=y-.7f,lower=y-2.2f;var anim=player.GetComponent<Animator>();var sr=player.GetComponent<SpriteRenderer>();
        var exit=FindFirstObjectByType<LevelExit>();var doorway=exit!=null?exit.GetComponent<Collider2D>():null;
        for(int i=0;i<(config!=null?config.studentCount:6);i++)
        {
            var go=new GameObject("Estudiante fila "+(i+1));go.transform.position=new Vector3(Mathf.Lerp(min,max,(i+.5f)/6f),i%2==0?upper:lower,0);
            var student=go.AddComponent<CampusStudent>();student.route=i%2==0?new[]{new Vector2(max,upper),new Vector2(max,lower),new Vector2(min,lower),new Vector2(min,upper)}:new[]{new Vector2(min,lower),new Vector2(min,upper),new Vector2(max,upper),new Vector2(max,lower)};
            if(doorway!=null)
            {
                Vector2 door=doorway.bounds.center;float lane=Mathf.Min(.35f,doorway.bounds.extents.y*.4f);
                student.route=new[]{new Vector2(max,upper),new Vector2(max,lower),new Vector2(min,lower),new Vector2(door.x+1,door.y-lane),new Vector2(door.x-1.4f,door.y-lane),new Vector2(door.x-1.4f,door.y+lane),new Vector2(door.x+1,door.y+lane),new Vector2(min,upper)};
                if(i%2==1)System.Array.Reverse(student.route);
            }
            var controller=config!=null&&config.studentControllers!=null&&config.studentControllers.Length>i?config.studentControllers[i]:anim.runtimeAnimatorController;
            student.speed=1.35f+i*.12f;student.Initialize(controller,sr.sprite,controller==anim.runtimeAnimatorController?Color.HSVToRGB(i/6f,.25f,1):Color.white);
            // La puerta cerrada limita al jugador; los estudiantes pueden entrar y salir.
            var barrier=GameObject.Find("SalidaNivel01");if(barrier!=null){var gate=barrier.GetComponent<Collider2D>();if(gate!=null)Physics2D.IgnoreCollision(student.GetComponent<Collider2D>(),gate);}
        }
    }
    void BuildTouchControls()
    {
        Vector2[] directions={Vector2.left,Vector2.right,Vector2.up,Vector2.down};string[] captions={"←","→","↑","↓"};Vector2[] places={new Vector2(-820,-400),new Vector2(-580,-400),new Vector2(-700,-290),new Vector2(-700,-500)};
        for(int i=0;i<4;i++){var b=U2Presentation.Button(canvas.transform,captions[i],places[i],()=>{});b.GetComponent<RectTransform>().sizeDelta=new Vector2(100,90);b.gameObject.AddComponent<TouchGameControls>().direction=directions[i];}
        var review=U2Presentation.Button(canvas.transform,"Revisar",new Vector2(650,-300),()=>{});review.GetComponent<RectTransform>().sizeDelta=new Vector2(250,90);review.gameObject.AddComponent<TouchGameControls>().interact=true;
        var run=U2Presentation.Button(canvas.transform,"Correr",new Vector2(350,-430),()=>{});run.GetComponent<RectTransform>().sizeDelta=new Vector2(250,90);run.gameObject.AddComponent<TouchGameControls>().sprint=true;
    }
    void OnDestroy(){Time.timeScale=1;if(canvas!=null)Destroy(canvas);if(overlay!=null)Destroy(overlay);}
}
