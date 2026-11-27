using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

/// <summary>Recorrido ficticio: campus, alarma local, cubículo, laptop y regreso.</summary>
public sealed class LevelFourMission : MonoBehaviour
{
    public enum Stage { Waiting, Campus, Terminal, Transition, Ground, Upper, Office, Laptop, Escape, Reunion, Won, Lost }
    public static LevelFourMission Active { get; private set; }
    public Stage CurrentStage { get; private set; }
    public LevelFourIntro Intro { get; private set; }
    public readonly LevelFourAlarm Alarm = new LevelFourAlarm();
    public Transform entranceF, groundSpawn, groundStairs, upperSpawn, torresDoor, officeSpawn, laptop, softwareGroup;
    public Transform groundExit, upperBack, officeExit;
    public Tilemap roofs;
    public Tilemap gardens, paths;
    public Rect[] trunkAreas = new Rect[0];
    public LevelFourPursuer[] guards;
    public bool GradeSaved { get; private set; }
    public bool IsInside => CurrentStage == Stage.Ground || CurrentStage == Stage.Upper || CurrentStage == Stage.Office || CurrentStage == Stage.Laptop;
    public bool CampusActive => CurrentStage == Stage.Campus || CurrentStage == Stage.Escape;
    public bool CanMove => CampusActive || CurrentStage == Stage.Ground || CurrentStage == Stage.Upper || CurrentStage == Stage.Office || CurrentStage == Stage.Reunion;
    public Vector2 Movement { get; private set; }
    public bool Running => motor != null && motor.Sprinting;
    LevelFourMotor motor;
    Rigidbody2D body;
    GameObject hud, panel, cover, clockBadge;
    TMP_Text clock, hint, state, message;
    RectTransform healthFill;
    Image healthColor;
    float shownHealth = 1;
    Vector2 upperLayoutOffset, officeLayoutOffset;
    float messageUntil;
    string selectedGroup = "";
    int selectedGrade = 8;
    TMP_InputField alarmUser, alarmPassword;
    TMP_Text terminalStatus;
    GameObject cableObject;
    Material cableMaterial;
    bool credentialsReady, authorizing;
    public bool AwaitingCredentials => CurrentStage == Stage.Terminal && credentialsReady && !authorizing;
    public bool CredentialsBlank => alarmUser != null && alarmPassword != null && alarmUser.text.Length == 0 && alarmPassword.text.Length == 0;
    static readonly Color Navy = new Color32(17, 26, 39, 255);
    static readonly Color Orange = new Color32(211, 80, 16, 255);
    static readonly Color Cream = new Color32(244, 241, 224, 255);

    void Awake() { Active = this; Intro = GetComponent<LevelFourIntro>(); }
    void Start()
    {
        AlignEditedInterior();
        motor = Intro.player.GetComponent<LevelFourMotor>();
        if (motor == null) motor = Intro.player.gameObject.AddComponent<LevelFourMotor>();
        body = Intro.player.GetComponent<Rigidbody2D>();
        hud = U2Presentation.NewCanvas("Nivel 4 · misión", 180);
        hud.GetComponent<CanvasScaler>().matchWidthOrHeight = 1;
        var badge = U2Presentation.Image(hud.transform, "Alarma · siempre visible", Navy);
        badge.rectTransform.anchoredPosition = new Vector2(750, 390); badge.rectTransform.sizeDelta = new Vector2(300, 115); badge.raycastTarget = false;
        clockBadge = badge.gameObject;
        clock = Label(badge.transform, "", Vector2.zero, new Vector2(280, 110), 38, new Color(1, .76f, .3f));
        state = Label(hud.transform, "", new Vector2(-700, 370), new Vector2(340, 100), 24, Cream);
        var healthBack = U2Presentation.Image(hud.transform, "Barra de vida · fondo", Navy);
        healthBack.rectTransform.anchoredPosition = new Vector2(-700, 322); healthBack.rectTransform.sizeDelta = new Vector2(340, 30); healthBack.raycastTarget = false;
        healthColor = U2Presentation.Image(healthBack.transform, "Vida restante", new Color(.25f, .85f, .48f));
        healthFill = healthColor.rectTransform; healthFill.anchorMin = healthFill.anchorMax = new Vector2(0, .5f); healthFill.pivot = new Vector2(0, .5f); healthFill.anchoredPosition = new Vector2(5, 0); healthFill.sizeDelta = new Vector2(330, 20); healthColor.raycastTarget = false;
        hint = Label(hud.transform, "WASD · Shift correr · Espacio golpear · Esc pausa", new Vector2(0, -510), new Vector2(1500, 45), 24, Cream);
        message = Label(hud.transform, "", new Vector2(0, -410), new Vector2(1400, 70), 29, new Color(1, .84f, .4f));
        if (GetComponent<LevelFourMinimap>() == null) gameObject.AddComponent<LevelFourMinimap>();
    }

    public void Begin()
    {
        CurrentStage = Stage.Campus;
        Intro.Caption("LLEGA AL EDIFICIO F · EVITA GUARDIAS Y PERROS");
        Intro.Speak(Intro.player, "GALINDO", "El edificio F... Tengo que llegar sin que me vean.", 3);
        foreach (var student in FindObjectsByType<LevelFourStudent>(FindObjectsSortMode.None)) student.Calm();
        motor.Social.Reset();
    }

    void Update()
    {
        if (hud == null || Intro.Paused) { Movement = Vector2.zero; return; }
        Alarm.Tick(Time.deltaTime, false);
        if (Alarm.Expired && CurrentStage != Stage.Lost) Fail("Se agotaron los tres minutos. La alarma del edificio F se reactivó.");
        int totalSeconds = Mathf.CeilToInt(Alarm.Remaining);
        clockBadge.SetActive(Alarm.Running);
        clock.text = Alarm.Running ? $"ALARMA\n{totalSeconds / 60:00}:{totalSeconds % 60:00}" : "";
        clock.color = Alarm.Remaining < 30 ? new Color(1, .25f, .2f) : new Color(1, .76f, .3f);
        state.text = "VIDA  " + motor.Health * 20 + " / 100";
        shownHealth = Mathf.MoveTowards(shownHealth, motor.Health / 5f, Time.unscaledDeltaTime * 2);
        healthFill.sizeDelta = new Vector2(330 * shownHealth, 20);
        healthColor.color = motor.Health <= 1 ? new Color(1, .22f, .18f) : motor.Health <= 2 ? new Color(1, .68f, .2f) : new Color(.25f, .85f, .48f);
        if (Time.time > messageUntil) message.text = "";
        Movement = Vector2.zero;
        if (AwaitingCredentials && Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
        {
            if (alarmUser.isFocused) alarmPassword.ActivateInputField(); else alarmUser.ActivateInputField();
        }
        if (!CanMove) return;
        var keys = Keyboard.current;
        if (keys != null)
        {
            Movement = new Vector2((keys.dKey.isPressed || keys.rightArrowKey.isPressed ? 1 : 0) - (keys.aKey.isPressed || keys.leftArrowKey.isPressed ? 1 : 0), (keys.wKey.isPressed || keys.upArrowKey.isPressed ? 1 : 0) - (keys.sKey.isPressed || keys.downArrowKey.isPressed ? 1 : 0));
            Movement = Vector2.ClampMagnitude(Movement, 1);
            motor.Sprinting = keys.leftShiftKey.isPressed || keys.rightShiftKey.isPressed;
            if (keys.eKey.wasPressedThisFrame) Interact();
        }
        RefreshHint();
    }

    void FixedUpdate()
    {
        if (!CanMove || Intro.Paused || motor == null) return;
        if (CampusActive && !Walkable(body.position)) Teleport(NearestClear(body.position));
        BoundsForStage(out Vector2 min, out Vector2 max);
        bool sneak = Keyboard.current != null && (Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed);
        float speed = sneak ? 1.45f : motor.Sprinting ? 5.2f : 3f;
        Vector2 delta = Movement * (speed * Time.fixedDeltaTime);
        Vector2 desired = body.position + delta;
        if (CampusActive && !Walkable(desired))
        {
            if (!Walkable(body.position + new Vector2(delta.x, 0))) delta.x = 0;
            if (!Walkable(body.position + new Vector2(0, delta.y))) delta.y = 0;
        }
        motor.Move(delta, min, max);
        Intro.AnimateMovement(delta);
        Intro.player.GetComponent<SpriteRenderer>().sortingOrder = CampusActive || CurrentStage == Stage.Reunion ? Mathf.RoundToInt(1000 - (body.position.y - .5f) * 10) : 2500 - Mathf.RoundToInt(body.position.y * 10);
    }

    public void BoundsForStage(out Vector2 min, out Vector2 max)
    {
        min = new Vector2(-51, -48.7f); max = new Vector2(51, 48);
        if (CurrentStage == Stage.Ground) { min = new Vector2(100.2f, 1); max = new Vector2(111.8f, 34); }
        if (CurrentStage == Stage.Upper) { min = new Vector2(125.2f, 1) + upperLayoutOffset; max = new Vector2(136.8f, 30) + upperLayoutOffset; }
        if (CurrentStage == Stage.Office) { min = new Vector2(150.6f, .8f) + officeLayoutOffset; max = new Vector2(155.4f, 8) + officeLayoutOffset; }
        if (CurrentStage == Stage.Reunion) { min = Intro.alleyMin; max = Intro.alleyMax; }
    }

    // Los interiores se pueden recolocar en el editor sin regenerar su diseño.
    void AlignEditedInterior()
    {
        var renders = GetComponentsInChildren<SpriteRenderer>(true);
        var upperFloor = renders.FirstOrDefault(r => r.name == "Piso cerámico del vestíbulo" && r.transform.parent != null && r.transform.parent.name.Contains("Galería superior"));
        var officeFloor = renders.FirstOrDefault(r => r.name == "Loseta del cubículo" && r.transform.parent != null && r.transform.parent.name.Contains("detallado"));
        if (upperFloor != null)
        {
            upperLayoutOffset = (Vector2)upperFloor.transform.position - new Vector2(131, 15.5f);
            AlignMarker(upperSpawn, new Vector2(131, 3.5f), upperLayoutOffset);
            AlignMarker(upperBack, new Vector2(131, 1.5f), upperLayoutOffset);
            AlignMarker(torresDoor, new Vector2(125.8f, 24), upperLayoutOffset);
        }
        if (officeFloor != null)
        {
            officeLayoutOffset = (Vector2)officeFloor.transform.position - new Vector2(153, 4.5f);
            AlignMarker(officeSpawn, new Vector2(153, 2.35f), officeLayoutOffset);
            AlignMarker(officeExit, new Vector2(153, 1.05f), officeLayoutOffset);
            AlignMarker(laptop, new Vector2(153, 6.1f), officeLayoutOffset);
        }
    }
    static void AlignMarker(Transform marker, Vector2 original, Vector2 offset)
    {
        // Respeta un punto que ya haya sido ajustado a mano.
        if (marker != null && Vector2.Distance(marker.position, original) < .05f)
            marker.position = original + offset;
    }

    public bool Walkable(Vector2 position)
    {
        if (position.x < -51 || position.x > 51 || position.y < -48.7f || position.y > 48) return false;
        if (roofs == null) return true;
        for (int x = -1; x <= 1; x++) for (int y = -1; y <= 1; y++)
        {
            Vector2 sample = position + new Vector2(x * .28f, -.5f + y * .21f);
            if (roofs.HasTile(roofs.WorldToCell(sample))) return false;
            foreach (var trunk in trunkAreas) if (trunk.Contains(sample)) return false;
        }
        return true;
    }

    public Vector2 NearestClear(Vector2 position, float radius = 4)
    {
        if (Walkable(position)) return position;
        for (float r = .2f; r <= radius; r += .2f) for (int i = 0; i < 32; i++)
        {
            float angle = i * Mathf.PI * 2 / 32;
            Vector2 candidate = position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * r;
            if (Walkable(candidate)) return candidate;
        }
        return position;
    }

    void RefreshHint()
    {
        string text = "WASD mover · Shift correr · Espacio golpe · Q empujón · Ctrl sigilo · M mapa";
        if (CurrentStage == Stage.Campus)
            text = Near(entranceF, 3) ? "E · conectar el celular al panel de alarma del F" : "EDIFICIO F  ·  " + DirectionToF() + "  ·  " + Mathf.CeilToInt(Vector2.Distance(Intro.player.position, entranceF.position)) + " m  ·  evita los conos de visión";
        if (CurrentStage == Stage.Ground)
            text = Near(groundStairs, 2.2f) ? "E · subir a los cubículos de la planta alta" : Near(groundExit, 2.2f) && GradeSaved ? "E · salir del F antes de que vuelva la alarma" : "SUBE AL FONDO · haz rojo: detente; haz apagado: cruza · Ctrl: sigilo";
        if (CurrentStage == Stage.Upper)
            text = Near(torresDoor, 2.5f) ? "E · entrar al cubículo de Torres (izquierda)" : Near(upperBack, 2.1f) ? "E · bajar a la planta baja" : "TORRES ESTÁ ARRIBA, A LA IZQUIERDA · evita los sensores";
        if (CurrentStage == Stage.Office) text = Near(laptop, 2) ? "E · encender y usar la laptop de Torres" : Near(officeExit, 1.8f) ? "E · volver al pasillo de cubículos" : "LAPTOP AL FONDO · el temporizador sigue corriendo";
        if (CurrentStage == Stage.Escape) text = CanReturnOverFence() ? "E · escalar la malla para salir por este tramo" : "VUELVE A LA MALLA SUR · sin temporizador · cuidado con los guardias";
        if (CurrentStage == Stage.Reunion) text = Near(softwareGroup, 2.8f) ? "E · reunirte con Team Software" : "REGRESA CON TEAM SOFTWARE · callejón";
        hint.text = text;
    }

    bool Near(Transform marker, float distance) => marker != null && Vector2.Distance(Intro.player.position, marker.position) < distance;
    string DirectionToF()
    {
        Vector2 to = entranceF.position - Intro.player.position;
        if (Mathf.Abs(to.x) > Mathf.Abs(to.y) * 2) return to.x > 0 ? "ESTE" : "OESTE";
        if (Mathf.Abs(to.y) > Mathf.Abs(to.x) * 2) return to.y > 0 ? "NORTE" : "SUR";
        return to.y > 0 ? to.x > 0 ? "NORESTE" : "NOROESTE" : to.x > 0 ? "SURESTE" : "SUROESTE";
    }
    public bool CanReturnOverFence() => Intro.player.position.y <= -47.6f && Intro.player.position.x >= Intro.alleyMin.x && Intro.player.position.x <= Intro.alleyMax.x;

    public void Interact()
    {
        if (Intro.Paused) return;
        if (CurrentStage == Stage.Campus && Near(entranceF, 3)) { StartCoroutine(HackAlarm()); return; }
        if (CurrentStage == Stage.Ground)
        {
            if (Near(groundStairs, 2.2f)) { StartCoroutine(Travel(upperSpawn, Stage.Upper, "SUBIENDO A LOS CUBÍCULOS")); return; }
            if (Near(groundExit, 2.2f))
            {
                if (GradeSaved) { Alarm.Stop(); StartCoroutine(Travel(entranceF, Stage.Escape, "SALIENDO DEL EDIFICIO F")); }
                else Feedback("Todavía falta llegar a la laptop de Torres.");
                return;
            }
            Feedback("Baños y cubículos de planta baja cerrados. Torres está arriba.");
        }
        if (CurrentStage == Stage.Upper)
        {
            if (Near(torresDoor, 2.5f)) { StartCoroutine(Travel(officeSpawn, Stage.Office, "CUBÍCULO DEL PROFESOR TORRES")); return; }
            if (Near(upperBack, 2.1f)) { StartCoroutine(Travel(groundStairs, Stage.Ground, "BAJANDO A PLANTA BAJA")); return; }
            Feedback("Los otros cubículos están cerrados. La puerta de Torres está a la izquierda.");
        }
        if (CurrentStage == Stage.Office)
        {
            if (Near(laptop, 2)) { OpenLaptop(); return; }
            if (Near(officeExit, 1.8f)) { StartCoroutine(Travel(torresDoor, Stage.Upper, "REGRESANDO AL PASILLO")); return; }
        }
        if (CurrentStage == Stage.Escape && CanReturnOverFence()) { StartCoroutine(ReturnFence()); return; }
        if (CurrentStage == Stage.Reunion && Near(softwareGroup, 2.8f)) Win();
    }

    IEnumerator Wait(float seconds)
    {
        for (float t = 0; t < seconds;) { if (!Intro.Paused) t += Time.deltaTime; yield return null; }
    }

    IEnumerator HackAlarm()
    {
        CurrentStage = Stage.Terminal; Intro.HideStory();
        Intro.Caption("PANEL DE ALARMA · CONEXIÓN LOCAL DEL CELULAR");
        cableObject = new GameObject("Cable del celular al panel"); var line = cableObject.AddComponent<LineRenderer>();
        cableMaterial = new Material(Shader.Find("Sprites/Default")); line.sharedMaterial = cableMaterial; line.positionCount = 3; line.widthMultiplier = .05f; line.sortingOrder = 6000;
        line.SetPositions(new[] { Intro.player.position + new Vector3(.3f, .05f), (Intro.player.position + entranceF.position) / 2 + Vector3.down * .2f, entranceF.position });
        line.startColor = line.endColor = new Color(.35f, .8f, .8f);
        Panel("CELULAR · TERMINAL DE MANTENIMIENTO", new Color32(8, 20, 22, 255));
        var terminal = Label(panel.transform, "", new Vector2(0, 195), new Vector2(1040, 170), 28, new Color(.4f, 1, .7f));
        terminal.alignment = TextAlignmentOptions.TopLeft;
        string script = "> cable conectado al panel local\n> terminal lista\n> introduce tus credenciales para continuar";
        for (int i = 0; i < script.Length; i += 3) { terminal.text += script.Substring(i, Mathf.Min(3, script.Length - i)); yield return Wait(.01f); }
        alarmUser = CredentialField("Usuario", new Vector2(0, 45), false);
        alarmPassword = CredentialField("Contraseña", new Vector2(0, -65), true);
        terminalStatus = Label(panel.transform, "Escribe las credenciales y pulsa Validar o Enter.", new Vector2(0, -290), new Vector2(1100, 60), 25, new Color(.7f, .85f, .82f));
        Button(panel.transform, "Validar acceso", new Vector2(-190, -195), () => TryAlarmCredentials(alarmUser.text, alarmPassword.text), 490);
        Button(panel.transform, "Desconectar", new Vector2(370, -195), CancelTerminal, 310);
        credentialsReady = true; authorizing = false; alarmUser.ActivateInputField();
    }

    TMP_InputField CredentialField(string caption, Vector2 position, bool password)
    {
        var box = U2Presentation.Image(panel.transform, caption, new Color32(24, 48, 54, 255));
        box.rectTransform.anchoredPosition = position; box.rectTransform.sizeDelta = new Vector2(820, 72);
        var input = box.gameObject.AddComponent<TMP_InputField>();
        var viewport = new GameObject("Texto editable", typeof(RectTransform), typeof(RectMask2D)); viewport.transform.SetParent(box.transform, false);
        var viewportRect = viewport.GetComponent<RectTransform>(); U2Presentation.Stretch(viewportRect); viewportRect.offsetMin = new Vector2(22, 5); viewportRect.offsetMax = new Vector2(-22, -5);
        var text = Label(viewport.transform, "", Vector2.zero, new Vector2(770, 60), 31, Cream); text.alignment = TextAlignmentOptions.MidlineLeft; text.enableAutoSizing = false;
        var placeholder = Label(viewport.transform, caption, Vector2.zero, new Vector2(770, 60), 29, new Color(.5f, .65f, .67f)); placeholder.alignment = TextAlignmentOptions.MidlineLeft;
        input.textViewport = viewportRect; input.textComponent = text; input.placeholder = placeholder; input.targetGraphic = box;
        input.contentType = password ? TMP_InputField.ContentType.Password : TMP_InputField.ContentType.Standard;
        input.characterLimit = 40; input.lineType = TMP_InputField.LineType.SingleLine;
        input.onSubmit.AddListener(_ => { if (!password && alarmPassword != null && string.IsNullOrEmpty(alarmPassword.text)) alarmPassword.ActivateInputField(); else TryAlarmCredentials(alarmUser.text, alarmPassword.text); });
        return input;
    }

    public bool TryAlarmCredentials(string user, string password)
    {
        if (!AwaitingCredentials || Intro.Paused) return false;
        if (user.Trim() != "admin" || password != "admin")
        {
            terminalStatus.color = new Color(1, .4f, .3f); terminalStatus.text = "Acceso denegado. Revisa usuario y contraseña.";
            if (alarmPassword != null) alarmPassword.text = "";
            return false;
        }
        authorizing = true; credentialsReady = false; alarmUser.interactable = alarmPassword.interactable = false;
        terminalStatus.color = new Color(.4f, 1, .7f); terminalStatus.text = "ACCESO VALIDADO · alarma suspendida durante 03:00";
        StartCoroutine(AuthorizeEntry()); return true;
    }

    IEnumerator AuthorizeEntry()
    {
        yield return Wait(.5f);
        if (cableObject != null) Destroy(cableObject); if (cableMaterial != null) Destroy(cableMaterial);
        ClosePanel(); Alarm.Start(); yield return Travel(groundSpawn, Stage.Ground, "INGRESANDO AL EDIFICIO F");
    }

    void CancelTerminal()
    {
        if (authorizing) return;
        StopAllCoroutines(); credentialsReady = false;
        if (cableObject != null) Destroy(cableObject); if (cableMaterial != null) Destroy(cableMaterial);
        ClosePanel(); CurrentStage = Stage.Campus; Intro.Caption("LLEGA AL EDIFICIO F · EVITA GUARDIAS Y PERROS");
    }

    public IEnumerator Travel(Transform target, Stage next, string caption)
    {
        CurrentStage = Stage.Transition; Intro.HideStory();
        cover = U2Presentation.Image(hud.transform, "Transición a negro", Color.black).gameObject;
        U2Presentation.Stretch(cover.GetComponent<RectTransform>());
        Label(cover.transform, caption, Vector2.zero, new Vector2(1500, 150), 40, Cream);
        yield return Wait(.65f);
        // Al salir, aparece delante de la puerta y no dentro del marco del muro.
        Teleport(target == torresDoor && next == Stage.Upper ? target.position + Vector3.right * 1.1f : target.position); CurrentStage = next;
        Intro.Caption(next == Stage.Ground ? "EDIFICIO F · SUBE A LOS CUBÍCULOS" : next == Stage.Upper ? "PLANTA ALTA · CUBÍCULO DE TORRES A LA IZQUIERDA" : next == Stage.Office ? "TORRES · ENCIENDE LA LAPTOP" : "SAL DEL CAMPUS · VUELVE A TEAM SOFTWARE");
        Destroy(cover); cover = null;
    }

    public void Teleport(Vector3 position)
    {
        body.simulated = false; Intro.player.position = position; body.position = position; body.linearVelocity = Vector2.zero; body.simulated = true;
        Physics2D.SyncTransforms(); Intro.view.transform.position = position + new Vector3(0, 1.1f, -10);
    }

    IEnumerator ReturnFence()
    {
        CurrentStage = Stage.Transition; Intro.HideStory();
        Intro.Caption("VOLVIENDO AL CALLEJÓN");
        foreach (var guard in guards) if (guard != null) guard.Stun(4);
        body.simulated = false;
        var actor = Intro.player.GetComponent<SpriteRenderer>(); var animator = Intro.player.GetComponent<Animator>();
        var pose = new GameObject("Galindo · regresando por la malla"); pose.transform.SetParent(Intro.player, false);
        var rig = pose.AddComponent<LevelFourClimbRig>(); rig.Build(actor, Intro.climbBack);
        actor.enabled = false; animator.enabled = false;
        Vector3 from = Intro.player.position, to = new Vector3(from.x, Intro.alleyMax.y - .25f, 0);
        for (float t = 0; t < 1;)
        {
            if (!Intro.Paused) { t += Time.deltaTime / 2.8f; Intro.player.position = Vector3.Lerp(from, to, Mathf.Clamp01(t)) + Vector3.up * Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI) * .5f; rig.Pose(t * 4); }
            yield return null;
        }
        Destroy(pose); actor.enabled = true; animator.enabled = true;
        body.simulated = true; Teleport(to); CurrentStage = Stage.Reunion; motor.Social.Reset();
        Intro.Caption("VUELVE CON TUS AMIGOS · TEAM SOFTWARE");
    }

    public void Feedback(string text) { if (message != null) { message.text = text; messageUntil = Time.time + 3.5f; } }

    void OpenLaptop()
    {
        CurrentStage = Stage.Laptop; Panel("LAPTOP DE TORRES", Navy);
        Label(panel.transform, "SIGA", new Vector2(0, 120), new Vector2(700, 130), 76, Orange);
        Label(panel.transform, "Sistema escolar · sesión del profesor Torres", new Vector2(0, 15), new Vector2(1000, 80), 29, Cream);
        Button(panel.transform, "Entrar al sistema", new Vector2(0, -120), Groups);
        Button(panel.transform, "Cerrar laptop", new Vector2(0, -240), CloseLaptop);
    }

    void Groups()
    {
        Panel("SIGA · TORRES / MIS GRUPOS", Cream);
        SigaHeader("Mis grupos");
        string[] groups = { "GIDS6081-E", "GIRD6092-E", "GIDS6093-E", "GIMT6091-E", "GIDS6102-E" };
        for (int i = 0; i < groups.Length; i++)
        {
            string group = groups[i];
            Button(panel.transform, group, new Vector2(120, 170 - i * 85), () => SelectGroup(group));
        }
    }

    public void SelectGroup(string group)
    {
        selectedGroup = group;
        Panel("SIGA · " + group, Cream); SigaHeader(group);
        if (group != "GIDS6093-E")
        {
            Label(panel.transform, "Galindo no pertenece a este grupo.", new Vector2(100, 70), new Vector2(820, 100), 33, Navy);
            Button(panel.transform, "Volver a mis grupos", new Vector2(100, -70), Groups); return;
        }
        Label(panel.transform, "BASES DE DATOS · CALIFICACIONES", new Vector2(120, 210), new Vector2(830, 60), 31, Navy);
        Label(panel.transform, "Alumno                         Resultado", new Vector2(120, 125), new Vector2(830, 60), 27, Navy);
        Button(panel.transform, "GALINDO     " + (GradeSaved ? "8" : "REPROBADO") + "     Editar", new Vector2(120, 30), GradeEditor);
        Label(panel.transform, "Alumno 02                      9\nAlumno 03                      8\nAlumno 04                      10", new Vector2(120, -130), new Vector2(830, 170), 27, Navy);
    }

    void SigaHeader(string section)
    {
        var orange = U2Presentation.Image(panel.transform, "Barra SIGA", Orange); orange.rectTransform.sizeDelta = new Vector2(1200, 80); orange.rectTransform.anchoredPosition = new Vector2(0, 280);
        Label(orange.transform, "SIGA   /   " + section + "       Profesor Torres", Vector2.zero, new Vector2(1140, 70), 29, Color.white);
        Button(panel.transform, "Mis grupos", new Vector2(-400, 120), Groups, 280);
        Button(panel.transform, "Información", new Vector2(-400, 20), Information, 280);
        Button(panel.transform, "Cerrar sesión", new Vector2(-400, -80), CloseLaptop, 280);
    }

    void Information()
    {
        Panel("SIGA · INFORMACIÓN", Cream); SigaHeader("Información");
        Label(panel.transform, "Profesor: Torres\nMateria: Bases de Datos\nGrupo de Galindo: GIDS6093-E\nObjetivo de la partida: registrar 8 y salir del F.", new Vector2(120, 0), new Vector2(830, 350), 32, Navy);
    }

    void GradeEditor()
    {
        Panel("SIGA · CALIFICACIÓN DE GALINDO", Cream); SigaHeader("Editar resultado");
        Label(panel.transform, "GIDS6093-E  /  Bases de Datos\nGALINDO", new Vector2(120, 160), new Vector2(830, 120), 33, Navy);
        for (int i = 6; i <= 10; i++) { int grade = i; Button(panel.transform, grade.ToString(), new Vector2(-120 + (i - 6) * 120, 10), () => { selectedGrade = grade; Feedback("Calificación seleccionada: " + grade); }, 95); }
        Button(panel.transform, "Guardar calificación", new Vector2(120, -140), () => SaveGrade(selectedGrade));
    }

    public bool SaveGrade(int grade)
    {
        if (CurrentStage != Stage.Laptop || selectedGroup != "GIDS6093-E" || grade != 8)
        { Feedback("Selecciona a Galindo en GIDS6093-E y la calificación 8."); return false; }
        GradeSaved = true; Feedback("Resultado guardado: 8. ¡Sal del F antes de que vuelva la alarma!");
        SelectGroup(selectedGroup); return true;
    }

    public void CloseLaptop() { ClosePanel(); CurrentStage = Stage.Office; Intro.Caption("CALIFICACIÓN " + (GradeSaved ? "GUARDADA · REGRESA A LA SALIDA DEL F" : "PENDIENTE · LAPTOP DE TORRES")); }

    public void Fail(string reason)
    {
        if (CurrentStage == Stage.Lost || CurrentStage == Stage.Won) return;
        CurrentStage = Stage.Lost; Alarm.Stop(); Intro.HideStory(); ClosePanel();
        StopAllCoroutines();
        if (cover != null) Destroy(cover);
        Panel("NO LOGRASTE ESCAPAR", Navy);
        Label(panel.transform, reason, new Vector2(0, 30), new Vector2(1050, 270), 36, Cream);
        GameLevelFlow.ResultActions(panel.transform,4,false,-150);
    }

    void Win()
    {
        CurrentStage = Stage.Won; Alarm.Stop(); Intro.Caption("NIVEL 4 COMPLETADO · DE VUELTA CON TEAM SOFTWARE");
        Intro.Speak(Intro.player, "GALINDO", "Ya quedó... Ahora sí, aquí me quedo con Software.", 6);
        Intro.beer.gameObject.SetActive(true);
        Panel("¡YA CASI... AHORA SÍ!", Navy);
        Label(panel.transform, "Volviste con Team Software.\nBases de Datos: 8\nEntraste, saliste del F a tiempo y regresaste por la malla.", new Vector2(0, 20), new Vector2(1050, 280), 36, Cream);
        U2Presentation.CompleteCampaign();
        GameLevelFlow.ResultActions(panel.transform,4,true,-130);
    }

    void Panel(string title, Color background)
    {
        ClosePanel();
        panel = U2Presentation.Image(hud.transform, title, background).gameObject;
        panel.GetComponent<RectTransform>().sizeDelta = new Vector2(1260, 760);
        Label(panel.transform, title, new Vector2(0, 330), new Vector2(1180, 70), 35, background == Cream ? Navy : Cream);
        clockBadge.transform.SetAsLastSibling();
    }
    void ClosePanel() { if (panel != null) { panel.SetActive(false); Destroy(panel); panel = null; } }
    TMP_Text Label(Transform parent, string text, Vector2 pos, Vector2 size, float font, Color color)
    {
        var label = U2Presentation.Text(parent, text, text, pos, size, font, color); label.enableAutoSizing = true; label.fontSizeMin = font - 5; label.fontSizeMax = font; return label;
    }
    void Button(Transform parent, string caption, Vector2 position, UnityEngine.Events.UnityAction callback, float width = 650)
    {
        var button = U2Presentation.Button(parent, caption, position, callback); button.GetComponent<RectTransform>().sizeDelta = new Vector2(width, 65);
        button.GetComponentInChildren<TMP_Text>().rectTransform.sizeDelta = new Vector2(width - 18, 58); button.GetComponentInChildren<TMP_Text>().fontSize = 27;
    }
    void OnDestroy() { if (Active == this) Active = null; if (hud != null) Destroy(hud); if (cableObject != null) Destroy(cableObject); if (cableMaterial != null) Destroy(cableMaterial); }
}
