using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Primera secuencia del nivel 4. Termina al entrar al campus.</summary>
public sealed class LevelFourIntro : MonoBehaviour
{
    public enum Beat { Friends, Notification, Phone, Drinking, Alley, Climbing, End }
    public Beat CurrentBeat { get; private set; }
    public bool Paused { get; private set; }
    public static LevelFourIntro Active { get; private set; }
    public Transform player, climbStart, landing, beer;
    public Transform[] friends;
    public Sprite climbBack;
    public Camera view;
    public Sprite roundedPanel;
    public Vector2 alleyMin = new Vector2(-16, -49.5f), alleyMax = new Vector2(12, -44.5f);
    public float walkSpeed = 3f, climbDistance = 1.5f;
    Rigidbody2D body;
    Animator animator;
    SpriteRenderer actor;
    GameObject hud, phone, pausePanel, dialogue;
    TMP_Text speaker, line, objective;
    Button action;
    AudioSource audioSource;
    AudioClip notification;
    Vector2 movement;
    Transform speakingActor;
    GameObject reply;
    RectTransform bubble;
    TMP_Text replyText;
    string animationState;
    LevelFourMotor motor;
    Coroutine speech;
    static readonly Color Ink = new Color32(19, 28, 36, 255);
    static readonly Color Gold = new Color32(245, 194, 99, 255);

    void Start()
    {
        Active = this;
        Time.timeScale = 1;
        body = player.GetComponent<Rigidbody2D>();
        animator = player.GetComponent<Animator>();
        actor = player.GetComponent<SpriteRenderer>();
        motor = player.GetComponent<LevelFourMotor>();
        if (motor == null) motor = player.gameObject.AddComponent<LevelFourMotor>();
        body.linearVelocity = Vector2.zero;
        body.bodyType = RigidbodyType2D.Kinematic;
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        notification = AudioClip.Create("Aviso de calificación", 14400, 1, 24000, false);
        var samples = new float[14400];
        for (int i = 0; i < samples.Length; i++)
        {
            float t = i / 24000f;
            float pulse = t < .18f ? 880 : 1174;
            samples[i] = Mathf.Sin(t * pulse * Mathf.PI * 2) * .12f * Mathf.Exp(-7 * (t % .2f));
        }
        notification.SetData(samples, 0);
        BuildHud();
        Say("AMIGO", "Un rato aquí y ya nos vamos, ¿no?", "Continuar  [E]", FirstFriend);
        ShowReply("GALINDO", "Sí, ya casi. Nada más me termino esta.");
        objective.text = "NIVEL 4  /  EL CALLEJÓN · POLITÉCNICO NACIONAL";
        Animate(Vector2.zero);
    }

    void Update()
    {
        var keys = Keyboard.current;
        var pad = Gamepad.current;
        if ((keys != null && keys.escapeKey.wasPressedThisFrame) || (pad != null && pad.startButton.wasPressedThisFrame))
            TogglePause();
        movement = Vector2.zero;
        if (Paused) return;
        if ((keys != null && keys.eKey.wasPressedThisFrame) || (pad != null && pad.buttonSouth.wasPressedThisFrame)) Advance();
        if (CurrentBeat != Beat.Alley) return;
        if (keys != null)
        {
            movement.x = (keys.dKey.isPressed || keys.rightArrowKey.isPressed ? 1 : 0) - (keys.aKey.isPressed || keys.leftArrowKey.isPressed ? 1 : 0);
            movement.y = (keys.wKey.isPressed || keys.upArrowKey.isPressed ? 1 : 0) - (keys.sKey.isPressed || keys.downArrowKey.isPressed ? 1 : 0);
        }
        if (pad != null && pad.leftStick.ReadValue().sqrMagnitude > .04f) movement = pad.leftStick.ReadValue();
        movement = Vector2.ClampMagnitude(movement, 1);
        motor.Sprinting = keys != null && (keys.leftShiftKey.isPressed || keys.rightShiftKey.isPressed);
        bool near = CanClimbAt(player.position);
        action.interactable = near;
        action.GetComponentInChildren<TMP_Text>().text = near ? "Trepar la malla  [E]" : "Acércate a la malla";
    }

    void FixedUpdate()
    {
        if (body == null) return;
        if (CurrentBeat == Beat.Climbing) return;
        if (Paused || CurrentBeat != Beat.Alley) { Animate(Vector2.zero); return; }
        motor.Move(movement * ((motor.Sprinting ? 5.2f : walkSpeed) * Time.fixedDeltaTime), alleyMin, alleyMax);
        Vector2 next = body.position;
        actor.sortingOrder = Mathf.RoundToInt(1000 - (next.y - .5f) * 10);
        if (beer.gameObject.activeSelf) beer.GetComponent<SpriteRenderer>().sortingOrder = actor.sortingOrder + 2;
        Animate(movement);
    }

    void LateUpdate()
    {
        if (view == null || player == null) return;
        Vector3 target = player.position + new Vector3(0, 1.1f, -10);
        if (!Paused) view.transform.position = Vector3.Lerp(view.transform.position, target, 1 - Mathf.Exp(-5 * Time.unscaledDeltaTime));
        PlaceBubble(bubble, speakingActor, new Vector2(-160, 0));
        if (reply != null && reply.activeSelf) PlaceBubble(reply.GetComponent<RectTransform>(), player, new Vector2(175, 135));
    }

    Transform FirstFriend => friends != null && friends.Length > 0 && friends[0] != null ? friends[0] : player;
    public bool CanClimbAt(Vector2 position) => position.x >= alleyMin.x && position.x <= alleyMax.x &&
        position.y >= alleyMax.y - .85f && position.y <= alleyMax.y + .3f;

    void PlaceBubble(RectTransform rect, Transform target, Vector2 offset)
    {
        if (rect == null || target == null || !rect.gameObject.activeSelf) return;
        Vector2 local;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(hud.GetComponent<RectTransform>(), view.WorldToScreenPoint(target.position + Vector3.up * 1.15f), null, out local);
        var canvas = hud.GetComponent<RectTransform>().rect;
        Vector2 pos = local + offset;
        pos.x = Mathf.Clamp(pos.x, canvas.xMin + 255, canvas.xMax - 255);
        pos.y = Mathf.Clamp(pos.y, canvas.yMin + 160, canvas.yMax - 230);
        rect.anchoredPosition = pos;
        var tail = rect.Find("Cola del globo") as RectTransform;
        if (tail != null) tail.anchoredPosition = new Vector2(Mathf.Clamp(local.x - pos.x, -210, 210), -1);
    }

    public void Advance()
    {
        if (Paused) return;
        switch (CurrentBeat)
        {
            case Beat.Friends:
                CurrentBeat = Beat.Notification;
                audioSource.PlayOneShot(notification, U2Presentation.EffectsVolume);
                Say("AMIGO", "Galindo, te llegó un mensaje.", "Ver celular  [E]", FirstFriend);
                ShowReply("GALINDO", "A ver... es de Bases de Datos.");
                break;
            case Beat.Notification:
                CurrentBeat = Beat.Phone;
                dialogue.SetActive(false);
                reply.SetActive(false);
                action.gameObject.SetActive(false);
                ShowPhone();
                break;
            case Beat.Phone:
                phone.SetActive(false);
                CurrentBeat = Beat.Drinking;
                Say("GALINDO", "«¿Reprobado...? No puede ser».", "");
                StartCoroutine(Drink());
                break;
            case Beat.Alley:
                if (!CanClimbAt(player.position)) return;
                CurrentBeat = Beat.Climbing;
                StartCoroutine(Climb());
                break;
            case Beat.End:
                if (GetComponent<LevelFourMission>() == null) SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                break;
        }
    }

    public void TogglePause()
    {
        Paused = !Paused;
        hud.GetComponent<Canvas>().sortingOrder = Paused ? 400 : 150;
        if (Paused) pausePanel.transform.SetAsLastSibling();
        pausePanel.SetActive(Paused);
        if (animator != null) animator.speed = Paused ? 0 : 1;
    }

    IEnumerator Wait(float duration)
    {
        float elapsed = 0;
        while (elapsed < duration) { if (!Paused) elapsed += Time.deltaTime; yield return null; }
    }

    IEnumerator Drink()
    {
        action.gameObject.SetActive(false);
        Vector3 rest = beer.localPosition;
        for (float t = 0; t < 1;)
        {
            if (!Paused)
            {
                t += Time.deltaTime / 1.4f;
                beer.localPosition = rest + Vector3.up * Mathf.Sin(Mathf.PI * Mathf.Clamp01(t)) * .55f;
                beer.localRotation = Quaternion.Euler(0, 0, -Mathf.Sin(Mathf.PI * Mathf.Clamp01(t)) * 75);
            }
            yield return null;
        }
        beer.gameObject.SetActive(false);
        yield return Wait(.35f);
        CurrentBeat = Beat.Alley;
        objective.text = "ACÉRCATE A LA MALLA · WASD / FLECHAS";
        Say("GALINDO", "Voy a entrar. No me puedo quedar con esto.", "Acércate a la malla");
        action.interactable = false;
    }

    IEnumerator Climb()
    {
        action.gameObject.SetActive(false);
        objective.text = "GALINDO TREPA LA MALLA";
        dialogue.SetActive(false);
        reply.SetActive(false);
        body.simulated = false;
        Vector3 start = player.position;
        Vector3 grip = new Vector3(start.x, alleyMax.y + .15f, 0);
        Vector3 top = new Vector3(start.x, alleyMax.y + 2.15f, 0);
        Vector3 end = new Vector3(start.x, alleyMax.y + 3.3f, 0);
        climbStart.position = grip;
        landing.position = end;
        int oldOrder = actor.sortingOrder;
        actor.sortingOrder = 5000;
        Animate(Vector2.up);
        for (float t = 0; t < 1;)
        {
            if (!Paused)
            {
                t += Time.deltaTime / .35f;
                player.position = Vector3.Lerp(start, grip, Mathf.Clamp01(t));
            }
            yield return null;
        }
        animator.enabled = false;
        actor.enabled = false;
        var poseObject = new GameObject("Galindo · manos y pies sobre la malla");
        poseObject.transform.SetParent(player, false);
        var rig = poseObject.AddComponent<LevelFourClimbRig>();
        rig.Build(actor, climbBack != null ? climbBack : actor.sprite);
        for (float t = 0; t < 1;)
        {
            if (!Paused)
            {
                t += Time.deltaTime / 2.65f;
                float progress = Mathf.Clamp01(t);
                // Cuatro apoyos: breve esfuerzo detenido antes de cada impulso.
                float step = progress * 4;
                float lift = (Mathf.Floor(step) + Mathf.SmoothStep(0, 1, Mathf.Clamp01((step % 1 - .2f) / .8f))) / 4;
                player.position = Vector3.Lerp(grip, top, lift);
                rig.Pose(step);
            }
            yield return null;
        }
        for (float t = 0; t < 1;)
        {
            if (!Paused)
            {
                t += Time.deltaTime / .7f;
                float p = Mathf.Clamp01(t);
                player.position = Vector3.Lerp(top, end, p) + Vector3.up * Mathf.Sin(p * Mathf.PI) * .35f;
                poseObject.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(p * Mathf.PI) * -35);
            }
            yield return null;
        }
        Destroy(poseObject);
        animator.enabled = true;
        actor.enabled = true;
        player.position = end;
        player.localRotation = Quaternion.identity;
        actor.sortingOrder = oldOrder;
        Animate(Vector2.zero);
        body.position = end;
        body.simulated = true;
        CurrentBeat = Beat.End;
        objective.text = "GALINDO YA ESTÁ DENTRO DE LA UNIVERSIDAD";
        var mission = GetComponent<LevelFourMission>();
        if (mission != null) { HideStory(); mission.Begin(); }
        else Say("GALINDO", "Ya estoy dentro...", "Volver a ver el inicio  [E]");
    }

    public void Caption(string message) { if (objective != null) objective.text = message; }
    public void HideStory() { dialogue.SetActive(false); reply.SetActive(false); action.gameObject.SetActive(false); }
    public void Speak(Transform target, string name, string message, float seconds)
    {
        if (speech != null) StopCoroutine(speech);
        speech = StartCoroutine(Speech(target, name, message, seconds));
    }
    IEnumerator Speech(Transform target, string name, string message, float seconds)
    {
        Say(name, message, "", target);
        yield return Wait(seconds);
        if (CurrentBeat == Beat.Alley) Say("GALINDO", "Mejor sigo mi camino...", "Acércate a la malla");
        else HideStory();
        speech = null;
    }

    public void AnimateMovement(Vector2 direction) { Animate(direction); }

    void Animate(Vector2 direction)
    {
        if (animator == null || !animator.enabled) return;
        string facing = direction.sqrMagnitude < .01f ? "Down" : Mathf.Abs(direction.x) > Mathf.Abs(direction.y) ? direction.x > 0 ? "Right" : "Left" : direction.y > 0 ? "Up" : "Down";
        string next = (direction.sqrMagnitude > .01f ? "Walk" : "Idle") + facing;
        if (animationState == next) return;
        animator.Play("Base Layer." + next, 0, 0);
        animationState = next;
    }

    Image Panel(Transform parent, string name, Vector2 pos, Vector2 size, Color color)
    {
        var image = U2Presentation.Image(parent, name, color);
        image.rectTransform.anchoredPosition = pos;
        image.rectTransform.sizeDelta = size;
        if (roundedPanel != null) { image.sprite = roundedPanel; image.type = Image.Type.Sliced; }
        return image;
    }

    TMP_Text Text(Transform parent, string name, string value, Vector2 pos, Vector2 size, float font, Color color, TextAlignmentOptions alignment = TextAlignmentOptions.Left)
    {
        var text = U2Presentation.Text(parent, name, value, pos, size, font, color);
        text.alignment = alignment;
        text.enableAutoSizing = true;
        text.fontSizeMin = font - 4;
        text.fontSizeMax = font;
        return text;
    }

    Button Button(Transform parent, string label, Vector2 pos, Vector2 size, UnityEngine.Events.UnityAction callback)
    {
        var panel = Panel(parent, label, pos, size, new Color32(181, 110, 55, 255));
        var button = panel.gameObject.AddComponent<Button>();
        button.targetGraphic = panel;
        button.onClick.AddListener(callback);
        Text(panel.transform, "Texto", label, Vector2.zero, size - new Vector2(24, 12), 27, Color.white, TextAlignmentOptions.Center);
        return button;
    }

    void BuildHud()
    {
        hud = U2Presentation.NewCanvas("Nivel 4 · Historia de Galindo", 150);
        hud.GetComponent<CanvasScaler>().matchWidthOrHeight = 1;
        var title = Panel(hud.transform, "Objetivo", new Vector2(-300, 470), new Vector2(1150, 60), Ink);
        objective = Text(title.transform, "Texto", "", Vector2.zero, new Vector2(1100, 48), 25, Gold);
        Button(hud.transform, "Pausa", new Vector2(780, 470), new Vector2(170, 60), TogglePause);
        dialogue = SpeechBubble("Globo de diálogo", out speaker, out line);
        bubble = dialogue.GetComponent<RectTransform>();
        TMP_Text replyName;
        reply = SpeechBubble("Respuesta de Galindo", out replyName, out replyText);
        replyName.text = "GALINDO";
        reply.SetActive(false);
        action = Button(hud.transform, "Continuar  [E]", new Vector2(0, -460), new Vector2(500, 70), Advance);
        pausePanel = Panel(hud.transform, "Pausa", Vector2.zero, new Vector2(700, 400), Ink).gameObject;
        Text(pausePanel.transform, "Título", "PAUSA", new Vector2(0, 120), new Vector2(600, 70), 42, Gold, TextAlignmentOptions.Center);
        Button(pausePanel.transform, "Continuar", Vector2.zero, new Vector2(500, 70), TogglePause);
        Button(pausePanel.transform, "Reiniciar secuencia", new Vector2(0, -105), new Vector2(500, 70), () => SceneManager.LoadScene(SceneManager.GetActiveScene().name));
        pausePanel.SetActive(false);
    }

    GameObject SpeechBubble(string name, out TMP_Text who, out TMP_Text message)
    {
        var paper = new Color32(255, 247, 222, 255);
        var panel = Panel(hud.transform, name, Vector2.zero, new Vector2(500, 156), paper);
        panel.raycastTarget = false;
        panel.rectTransform.pivot = new Vector2(.5f, 0);
        panel.gameObject.AddComponent<Shadow>().effectColor = new Color(0, 0, 0, .38f);
        var tail = U2Presentation.Image(panel.transform, "Cola del globo", paper);
        tail.gameObject.AddComponent<LevelFourBubbleTail>();
        var rect = tail.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, 0);
        rect.anchoredPosition = new Vector2(0, -1); rect.sizeDelta = new Vector2(25, 25);
        rect.localRotation = Quaternion.Euler(0, 0, 45);
        tail.raycastTarget = false;
        who = Text(panel.transform, "Persona", "", new Vector2(0, 48), new Vector2(440, 30), 21, new Color32(137, 84, 44, 255));
        message = Text(panel.transform, "Diálogo", "", new Vector2(0, -12), new Vector2(440, 96), 28, Ink);
        return panel.gameObject;
    }

    void ShowReply(string name, string text)
    {
        reply.SetActive(true);
        replyText.text = text;
    }

    void Say(string name, string message, string button, Transform target = null)
    {
        speakingActor = target != null ? target : player;
        if (reply != null) reply.SetActive(false);
        dialogue.SetActive(true);
        speaker.text = name; line.text = message;
        action.gameObject.SetActive(!string.IsNullOrEmpty(button));
        action.interactable = true;
        action.GetComponentInChildren<TMP_Text>().text = button;
    }

    void ShowPhone()
    {
        phone = U2Presentation.Image(hud.transform, "Celular abierto", new Color(0, 0, 0, .65f)).gameObject;
        U2Presentation.Stretch(phone.GetComponent<RectTransform>());
        var bezel = Panel(phone.transform, "Carcasa", new Vector2(0, 25), new Vector2(510, 875), new Color32(10, 13, 18, 255));
        var screen = Panel(bezel.transform, "Pantalla", Vector2.zero, new Vector2(474, 835), new Color32(237, 240, 233, 255));
        Panel(screen.transform, "Auricular", new Vector2(0, 385), new Vector2(112, 13), Ink);
        Text(screen.transform, "Barra de estado", "18:42                         LTE   84%", new Vector2(0, 348), new Vector2(415, 40), 21, Ink);
        Panel(screen.transform, "Encabezado", new Vector2(0, 265), new Vector2(474, 100), new Color32(32, 76, 72, 255));
        Text(screen.transform, "Aplicación", "CONTROL ESCOLAR", new Vector2(0, 275), new Vector2(410, 40), 29, Color.white);
        Text(screen.transform, "Tipo de aviso", "Nueva calificación", new Vector2(0, 235), new Vector2(410, 30), 21, new Color32(189, 218, 204, 255));
        Text(screen.transform, "Destinatario", "GALINDO", new Vector2(0, 155), new Vector2(410, 35), 22, new Color32(92, 109, 101, 255));
        Text(screen.transform, "Materia", "Bases de Datos", new Vector2(0, 90), new Vector2(410, 80), 41, Ink);
        var grade = Panel(screen.transform, "Estado de la materia", new Vector2(0, -40), new Vector2(416, 126), new Color32(251, 219, 214, 255));
        Text(grade.transform, "REPROBADO", "REPROBADO", Vector2.zero, new Vector2(395, 75), 43, new Color32(158, 42, 39, 255), TextAlignmentOptions.Center);
        Text(screen.transform, "Mensaje", "Se ha publicado tu resultado.\nTu calificación en la materia\nno es aprobatoria.", new Vector2(0, -175), new Vector2(410, 130), 27, Ink);
        Button(screen.transform, "Guardar celular  [E]", new Vector2(0, -310), new Vector2(410, 75), Advance);
        Panel(screen.transform, "Indicador de inicio", new Vector2(0, -390), new Vector2(130, 6), Ink);
    }

    void OnDestroy()
    {
        if (Active == this) Active = null;
        if (notification != null) Destroy(notification);
        if (hud != null) Destroy(hud);
    }
}
