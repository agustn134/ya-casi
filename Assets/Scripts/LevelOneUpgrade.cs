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
    readonly List<CampusStudent> students = new List<CampusStudent>();
    GameObject canvas, overlay;
    TMP_Text progress, inventory, prompt;
    AudioSource music;
    float nextCourtesy;
    int impacts;
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
        BuildHud();
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
        if (manager.IsPlaying)
        {
            elapsed += Time.deltaTime;
            if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame && interaction != null)
                interaction.TryInteract();
            if ((Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame) ||
                (Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame))
                AskForRoom();
        }

        int count = 0;
        var nearest = manager.IsPlaying && interaction != null && interaction.enabled && !manager.HasUSB
            ? interaction.FindNearestLocker() : null;
        for (int i = 0; i < lockers.Length; i++)
        {
            bool searched = lockers[i].IsSearched;
            if (searched) count++;
            marks[i].SetActive(searched && nearest == lockers[i]);
            borders[i].enabled = nearest == lockers[i];
            borders[i].startColor = borders[i].endColor = searched ? new Color(1, .7f, .2f) : Color.white;
        }
        progress.text = $"Revisados {count}/{lockers.Length}";
        inventory.text = manager.HasUSB ? "USB 1/1 · Ve a la salida" : "USB 0/1";
        inventory.color = manager.HasUSB ? new Color(.65f, 1f, .78f) : Color.white;
        prompt.text = nearest != null ? (nearest.IsSearched ? "E · Revisar de nuevo (−5 s)" : "E · Revisar casillero") : "";
        prompt.transform.parent.gameObject.SetActive(nearest != null);
    }

    void BuildHud()
    {
        canvas.GetComponent<CanvasScaler>().matchWidthOrHeight = 0f;
        progress = HudText("Progreso", new Vector2(0, 1), new Vector2(24, -18), new Vector2(256, 48), 24);
        inventory = HudText("Inventario", new Vector2(0, 1), new Vector2(24, -75), new Vector2(340, 48), 24);
        var timer = HudText("Tiempo", Vector2.one, new Vector2(-24, -18), new Vector2(246, 48), 28);
        var notice = HudText("Aviso", Vector2.one, new Vector2(-24, -76), new Vector2(450, 48), 22);
        prompt = HudText("Interacción", new Vector2(.5f, 0), new Vector2(0, 22), new Vector2(405, 46), 23);
        prompt.transform.parent.gameObject.SetActive(false);
        manager.BindCompactHud(timer, notice);
        if (interaction != null) interaction.HideLegacyPrompt();
        HudButton("Pausa", -282, TogglePause);
        HudButton("Ayuda", -398, ShowTutorial);
    }

    TMP_Text HudText(string title, Vector2 anchor, Vector2 position, Vector2 size, float fontSize)
    {
        var panel = U2Presentation.Image(canvas.transform, title, new Color(.05f, .09f, .12f, .86f));
        panel.raycastTarget = false;
        Place(panel.rectTransform, anchor, position, size);
        return U2Presentation.Text(panel.transform, title, "", Vector2.zero, size - new Vector2(14, 4), fontSize, Color.white);
    }

    void HudButton(string title, float x, UnityEngine.Events.UnityAction action)
    {
        var button = U2Presentation.Button(canvas.transform, title, Vector2.zero, action);
        Place((RectTransform)button.transform, Vector2.one, new Vector2(x, -18), new Vector2(106, 48));
        var text = button.GetComponentInChildren<TMP_Text>();
        text.fontSize = 22;
        text.rectTransform.sizeDelta = new Vector2(100, 46);
    }

    static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    void AskForRoom()
    {
        if (!manager.IsPlaying || player == null || Time.time < nextCourtesy) return;
        CampusStudent nearest = null;
        float nearestDistance = 1.55f * 1.55f;
        foreach (var student in students)
        {
            if (student == null) continue;
            float distance = ((Vector2)(student.transform.position - player.transform.position)).sqrMagnitude;
            if (distance >= nearestDistance) continue;
            nearest = student;
            nearestDistance = distance;
        }

        if (nearest == null) return;
        nextCourtesy = Time.time + 1.75f;
        if (Random.value < .65f) nearest.TryMakeRoom(player.transform.position);
        manager.ShowFeedback("Con permiso");
    }

    void AddMarker(LockerInteraction locker)
    {
        var sr=locker.GetComponentInChildren<SpriteRenderer>();var b=sr!=null?sr.bounds:new Bounds(locker.transform.position,Vector3.one);
        var go=new GameObject("Contorno de interacción");go.transform.SetParent(locker.transform,false);var line=go.AddComponent<LineRenderer>();line.useWorldSpace=true;line.loop=true;line.positionCount=4;line.widthMultiplier=.035f;line.material=new Material(Shader.Find("Sprites/Default"));line.sortingOrder=6;
        line.SetPositions(new[]{new Vector3(b.min.x,b.min.y,0),new Vector3(b.min.x,b.max.y,0),new Vector3(b.max.x,b.max.y,0),new Vector3(b.max.x,b.min.y,0)});line.enabled=false;borders.Add(line);
        var mark=new GameObject("Casillero revisado");mark.transform.SetParent(locker.transform,false);mark.transform.position=b.center+Vector3.up*b.extents.y*.5f;var label=mark.AddComponent<TextMeshPro>();if(config!=null&&config.font!=null)label.font=config.font;label.text="OK";label.fontSize=3;label.color=new Color(.7f,1,.8f);label.alignment=TextAlignmentOptions.Center;label.rectTransform.sizeDelta=new Vector2(1,.5f);label.renderer.sortingOrder=7;mark.SetActive(false);marks.Add(mark);
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
        music.spatialBlend = 0f;
        music.mute = false;
        music.ignoreListenerPause = true;
        music.volume = U2Presentation.MusicVolume;
        music.clip = clip;
        music.Play();
        Debug.Log($"Game Over: {clip.name}; volumen de música: {music.volume:0.00}; reproducción: {music.isPlaying}", this);
    }
    public void Reviewed(bool repeated,bool usb)
    {score=Mathf.Max(0,score+(repeated?-75:(usb?500:30)));}
    public void BumpPlayer()
    {
        if(!manager.IsPlaying||player==null||!player.IsRunning||Time.time<bumpUntil)return;
        bumpUntil=Time.time+2; impacts++; calm--;
        manager.ShowFeedback("Impacto: −2 s y −1 de calma"); manager.ApplyPenalty(2);
        if(calm<=0)manager.FailLevel();
    }
    public void TogglePause()
    {
        if(finished)return;
        if(paused){DismissOverlay();return;}
        paused=true;Time.timeScale=0;overlay=U2Presentation.NewCanvas("Pausa",100);var card=U2Presentation.Card(overlay.transform,"Pausa");
        U2Presentation.Text(card.transform,"Título","PAUSA",new Vector2(0,300),new Vector2(1000,80),60,new Color(1,.84f,.4f));
        U2Presentation.Text(card.transform,"Ayuda",$"Recorrido: {Mathf.FloorToInt(elapsed / 60):00}:{Mathf.FloorToInt(elapsed % 60):00} · Impactos: {impacts}\nCalma: {calm}/3 · Puntaje: {score}",new Vector2(0,180),new Vector2(1100,130),32,Color.white);
        U2Presentation.Button(card.transform,"Continuar",new Vector2(0,40),DismissOverlay);
        U2Presentation.Button(card.transform,"Reiniciar",new Vector2(0,-60),manager.RestartLevel);
        U2Presentation.Button(card.transform,"Opciones",new Vector2(0,-160),()=>U2Presentation.Options(overlay.transform,null));
        U2Presentation.Button(card.transform,"Menú principal",new Vector2(0,-260),U2Presentation.ReturnToMenu);
    }
    public void DismissOverlay(){if(overlay!=null)Destroy(overlay);paused=false;Time.timeScale=1;}
    void ShowTutorial()
    {
        if (finished || paused) return;
        paused=true;Time.timeScale=0;overlay=U2Presentation.NewCanvas("Tutorial nivel 1",100);var card=U2Presentation.Card(overlay.transform,"Tutorial");
        U2Presentation.Text(card.transform,"Título","¡EL EDIFICIO ESTÁ POR CERRAR!",new Vector2(0,290),new Vector2(1160,100),48,new Color(1,.84f,.4f));
        U2Presentation.Text(card.transform,"Reglas","Olvidaste tu USB en un casillero. Encuéntrala y vuelve a la salida.\n\nWASD / flechas: moverte · Shift: correr\nE: revisar un casillero · Q: pedir permiso\nEsc: pausa\n\nUn casillero vacío o repetido resta tiempo.\nChocar al correr también reduce tu calma.",new Vector2(0,10),new Vector2(1130,430),34,Color.white);
        U2Presentation.Button(card.transform,"¡Vamos!",new Vector2(0,-300),()=>{PlayerPrefs.SetInt("YaCasi.U2.Tutorial",1);DismissOverlay();});
    }
    void ShowResult()
    {
        DismissOverlay();if(manager.IsCompleted){score+=Mathf.CeilToInt(manager.RemainingTime)*10+calm*100;PlayerPrefs.SetInt("YaCasi.U2.Win",1);PlayerPrefs.SetInt("YaCasi.U2.Best",Mathf.Max(score,PlayerPrefs.GetInt("YaCasi.U2.Best",0)));PlayerPrefs.Save();}
        overlay=U2Presentation.NewCanvas("Resultado nivel 1",100);var card=U2Presentation.Card(overlay.transform,"Resultado");
        U2Presentation.Text(card.transform,"Título",manager.IsCompleted?"¡USB RECUPERADA!":(calm<=0?"NECESITAS UN RESPIRO":"SE ACABÓ EL TIEMPO"),new Vector2(0,300),new Vector2(1160,100),52,new Color(1,.84f,.4f));
        U2Presentation.Text(card.transform,"Resultado",(manager.IsCompleted?"Nivel superado. Llegaste a tiempo a la salida.":"Respira, evita las filas y recuerda los casilleros revisados.")+$"\n\nPuntaje: {score}\nTiempo de juego: {Mathf.FloorToInt(elapsed/60):00}:{Mathf.FloorToInt(elapsed%60):00}\nTiempo restante: {Mathf.CeilToInt(manager.RemainingTime)} s · Impactos: {impacts}\nMejor puntaje: {PlayerPrefs.GetInt("YaCasi.U2.Best",0)}",new Vector2(0,70),new Vector2(1130,300),34,Color.white);
        U2Presentation.Button(card.transform, "Reintentar", new Vector2(0, -150), manager.RestartLevel);
        U2Presentation.Button(card.transform, "Menú principal", new Vector2(0, -250), U2Presentation.ReturnToMenu);
        if (manager.IsCompleted)
        {
            U2Presentation.SaveLibraryProgress();
            U2Presentation.Button(card.transform, "Nivel 2 · Biblioteca", new Vector2(0, -350), U2Presentation.PlayLibrary);
        }
    }
    void SpawnStudents()
    {
        if(player==null||lockers.Length<2)return;
        float min=float.MaxValue,max=float.MinValue,y=0;
        foreach(var locker in lockers){min=Mathf.Min(min,locker.InteractionPosition.x);max=Mathf.Max(max,locker.InteractionPosition.x);y+=locker.InteractionPosition.y;}y/=lockers.Length;
        // Dos carriles conectados: ida frente a casilleros, vuelta por el pasillo.
        float upper=y-.7f,lower=y-2.2f;var anim=player.GetComponent<Animator>();var sr=player.GetComponent<SpriteRenderer>();
        var exit=FindFirstObjectByType<LevelExit>();var doorway=exit!=null?exit.GetComponent<Collider2D>():null;
        int baseCount = config != null ? config.studentCount : 6;
        int studentCount = baseCount + 3;
        var controllers = config != null ? config.studentControllers : null;
        for (int i = 0; i < studentCount; i++)
        {
            var go=new GameObject("Estudiante fila "+(i+1));go.transform.position=new Vector3(Mathf.Lerp(min,max,(i+.5f)/studentCount),i%2==0?upper:lower,0);
            var student=go.AddComponent<CampusStudent>();students.Add(student);student.route=i%2==0?new[]{new Vector2(max,upper),new Vector2(max,lower),new Vector2(min,lower),new Vector2(min,upper)}:new[]{new Vector2(min,lower),new Vector2(min,upper),new Vector2(max,upper),new Vector2(max,lower)};
            if(doorway!=null)
            {
                Vector2 door=doorway.bounds.center;float lane=Mathf.Min(.35f,doorway.bounds.extents.y*.4f);
                student.route=new[]{new Vector2(max,upper),new Vector2(max,lower),new Vector2(min,lower),new Vector2(door.x+1,door.y-lane),new Vector2(door.x-1.4f,door.y-lane),new Vector2(door.x-1.4f,door.y+lane),new Vector2(door.x+1,door.y+lane),new Vector2(min,upper)};
                if(i%2==1)System.Array.Reverse(student.route);
            }
            var controller = controllers != null && controllers.Length > 0
                ? controllers[i % controllers.Length] : null;
            Color tint = i < baseCount && controller != null
                ? Color.white : Color.HSVToRGB(i / (float)studentCount, .20f, 1f);
            student.speed = 1.35f + (i % 6) * .12f;
            student.Initialize(controller != null ? controller : anim.runtimeAnimatorController, sr.sprite, tint);
            // La puerta cerrada limita al jugador; los estudiantes pueden entrar y salir.
            var barrier=GameObject.Find("SalidaNivel01");if(barrier!=null){var gate=barrier.GetComponent<Collider2D>();if(gate!=null)Physics2D.IgnoreCollision(student.GetComponent<Collider2D>(),gate);}
        }
        SpawnLowerHallStudents(min, max, controllers, anim.runtimeAnimatorController, sr.sprite);
    }
    void SpawnLowerHallStudents(float min, float max, RuntimeAnimatorController[] controllers,
        RuntimeAnimatorController fallback, Sprite initialSprite)
    {
        string[] characters = { "Amelia", "Lucy", "Samuel", "Ash" };
        int count = characters.Length;
        const float insideLane = -3.75f;
        const float wallLane = -4.55f;
        float segment = (max - min) / count;
        if (segment < 3f) return;

        for (int i = 0; i < count; i++)
        {
            float left = min + segment * i + .7f;
            float right = min + segment * (i + 1) - .7f;
            bool headsRight = i % 2 == 0;
            float lane = headsRight ? insideLane : wallLane;
            var actor = new GameObject("Estudiante del pasillo inferior " + (i + 1));
            actor.transform.position = new Vector3(Mathf.Lerp(left, right, headsRight ? .3f : .7f), lane, 0);

            var student = actor.AddComponent<CampusStudent>();
            student.route = headsRight
                ? new[] { new Vector2(right, insideLane), new Vector2(right, wallLane),
                    new Vector2(left, wallLane), new Vector2(left, insideLane) }
                : new[] { new Vector2(left, wallLane), new Vector2(left, insideLane),
                    new Vector2(right, insideLane), new Vector2(right, wallLane) };
            student.speed = 1.15f + i * .15f;
            var character = controllers != null && controllers.Length > 0
                ? controllers[(i + 1) % controllers.Length] : null;
            Color tint = character != null ? Color.white : Color.HSVToRGB((i + 1f) / (count + 1), .32f, 1f);
            student.Initialize(character != null ? character : fallback, initialSprite, tint);
            var sheet = Resources.Load<Texture2D>("LevelOneStudents/" + characters[i]);
            if (sheet != null) student.UseCharacterSheet(sheet);
            students.Add(student);
        }
    }

    void BuildTouchControls()
    {
        Vector2[] directions={Vector2.left,Vector2.right,Vector2.up,Vector2.down};string[] captions={"←","→","↑","↓"};Vector2[] places={new Vector2(-820,-400),new Vector2(-580,-400),new Vector2(-700,-290),new Vector2(-700,-500)};
        for(int i=0;i<4;i++){var b=U2Presentation.Button(canvas.transform,captions[i],places[i],()=>{});b.GetComponent<RectTransform>().sizeDelta=new Vector2(100,90);b.gameObject.AddComponent<TouchGameControls>().direction=directions[i];}
        var review=U2Presentation.Button(canvas.transform,"Revisar",new Vector2(650,-300),()=>{});review.GetComponent<RectTransform>().sizeDelta=new Vector2(250,90);review.gameObject.AddComponent<TouchGameControls>().interact=true;
        var courtesy = U2Presentation.Button(canvas.transform, "Permiso", new Vector2(650, -410), AskForRoom);
        ((RectTransform)courtesy.transform).sizeDelta = new Vector2(250, 90);
        var run=U2Presentation.Button(canvas.transform,"Correr",new Vector2(350,-430),()=>{});run.GetComponent<RectTransform>().sizeDelta=new Vector2(250,90);run.gameObject.AddComponent<TouchGameControls>().sprint=true;
    }
    void OnDestroy(){Time.timeScale=1;if(canvas!=null)Destroy(canvas);if(overlay!=null)Destroy(overlay);}
}
