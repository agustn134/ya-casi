using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Inventario de proyectos, entregas, tiempo y presentación compartida con el nivel 1.</summary>
public sealed class CampusDeliveryObjectives : MonoBehaviour
{
    [SerializeField] Transform player;
    [SerializeField] Transform[] professors;
    [SerializeField] string[] professorNames;
    [SerializeField] string[] projectIds;
    [SerializeField] string[] projectNames;
    [SerializeField] Sprite[] projectIcons;
    [SerializeField] float deliveryDistance = 1.8f;
    [SerializeField] float roundSeconds = 180f;
    [SerializeField] AudioClip levelMusic;
    [SerializeField] AudioClip timeoutMusic;
    [SerializeField] bool showTutorial = true;
    public ProjectDeliverySession Session { get; private set; }
    public int SelectedProject => selected;
    public bool IsPaused => Session != null && Session.Paused;
    GameObject hud, overlay;
    TMP_Text timer, objective, elapsed, prompt, feedback;
    TMP_Text[] itemCaptions;
    Image[] itemBackgrounds;
    Button[] itemButtons;
    AudioSource music, effects;
    AudioClip acceptTone, rejectTone;
    int nearest = -1, selected;
    float feedbackUntil;
    bool resultShown, optionsOpen;
    static readonly Color Gold = new Color32(255, 213, 107, 255);
    static readonly Color Navy = new Color32(24, 39, 51, 255);
    const string TutorialKey = "YaCasi.Nivel03.Tutorial.YC14";
    public const string Controls = "WASD / flechas / joystick: moverte · Shift / gatillo: correr\n1–5 o clic: seleccionar proyecto · Tab / RB: siguiente proyecto\nE / botón sur: entregar · Esc / Start: pausa";

    void Awake()
    {
        Application.runInBackground = true;
        if (player == null || professors == null || professors.Length == 0 || professorNames == null
            || professorNames.Length != professors.Length || Array.Exists(professors, t => t == null))
        {
            Debug.LogError("Nivel 3: faltan referencias de jugador o profesores.", this);
            enabled = false; return;
        }
        if (projectIds == null || projectIds.Length != professors.Length)
        {
            projectIds = new string[professors.Length];
            for (int i = 0; i < professors.Length; i++) projectIds[i] = "proyecto-" + i;
        }
        if (projectNames == null || projectNames.Length != professors.Length)
        {
            projectNames = new string[professors.Length];
            for (int i = 0; i < professors.Length; i++) projectNames[i] = "Proyecto " + (i + 1);
        }
        var inventory = new ProjectInventory();
        var ids = new string[professors.Length];
        for (int i = 0; i < professors.Length; i++) { ids[i] = "profesor-" + i; inventory.Add(projectIds[i]); }
        Session = new ProjectDeliverySession(ids, projectIds, roundSeconds, inventory);
        Time.timeScale = 1;
        music = gameObject.AddComponent<AudioSource>();
        music.playOnAwake = false; music.spatialBlend = 0; music.loop = true;
        music.clip = levelMusic; music.volume = U2Presentation.MusicVolume;
        if (levelMusic != null) music.Play();
        effects = gameObject.AddComponent<AudioSource>(); effects.spatialBlend = 0; effects.playOnAwake = false;
        acceptTone = Tone("Entrega correcta", 760);
        rejectTone = Tone("Proyecto incorrecto", 180);
    }

    void Start()
    {
        if (Session == null) return;
        BuildHud(); RefreshHud();
        if (showTutorial && !PlayerPrefs.HasKey(TutorialKey)) ShowTutorial();
    }

    void Update()
    {
        if (Session == null || hud == null) return;
        music.volume = U2Presentation.MusicVolume * (Session.Paused ? .5f : 1);
        effects.volume = U2Presentation.EffectsVolume;
        bool pause = (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            || (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame);
        if (pause) TogglePause();
        if (Session.Failed || Session.Completed)
        {
            if (!resultShown) { ShowResult(); RefreshHud(); }
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) Restart();
            return;
        }
        Session.AdvanceTime(Time.deltaTime);
        if (Session.Failed) { ShowResult(); RefreshHud(); return; }
        if (Session.Paused) return;
        HandleSelection(); FindNearest();
        bool interact = (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            || (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame);
        if (interact) TryInteract();
        RefreshHud();
    }

    void HandleSelection()
    {
        var k = Keyboard.current;
        if (k != null)
        {
            if (k.digit1Key.wasPressedThisFrame) SelectProject(0);
            if (k.digit2Key.wasPressedThisFrame) SelectProject(1);
            if (k.digit3Key.wasPressedThisFrame) SelectProject(2);
            if (k.digit4Key.wasPressedThisFrame) SelectProject(3);
            if (k.digit5Key.wasPressedThisFrame) SelectProject(4);
        }
        if ((k != null && k.tabKey.wasPressedThisFrame)
            || (Gamepad.current != null && Gamepad.current.rightShoulder.wasPressedThisFrame)) NextProject();
    }
    public void SelectProject(int index)
    {
        if (Session == null || !Session.Playing || index < 0 || index >= projectIds.Length
            || Session.Inventory.Quantity(projectIds[index]) == 0) return;
        selected = index; RefreshHud();
    }
    void NextProject()
    {
        for (int step = 1; step <= projectIds.Length; step++)
        {
            int index = (selected + step) % projectIds.Length;
            if (Session.Inventory.Quantity(projectIds[index]) > 0) { selected = index; return; }
        }
    }
    void FindNearest()
    {
        nearest = -1; float best = deliveryDistance * deliveryDistance;
        for (int i = 0; i < professors.Length; i++)
        {
            float distance = ((Vector2)player.position - (Vector2)professors[i].position).sqrMagnitude;
            if (distance >= best) continue;
            nearest = i; best = distance;
        }
    }
    public void TryInteract()
    {
        if (Session == null || !Session.Playing) return;
        FindNearest();
        if (nearest >= 0) TryDeliverTo(nearest);
        else SetFeedback("Acércate a un profesor para entregar.", false);
    }
    public ProjectDeliveryResult TryDeliverTo(int professor)
    {
        if (Session == null || !Session.Playing) return ProjectDeliveryResult.NotPlaying;
        if (professor < 0 || professor >= professors.Length) return ProjectDeliveryResult.UnknownProfessor;
        if (Vector2.Distance(player.position, professors[professor].position) > deliveryDistance)
        {
            SetFeedback("Acércate más al profesor.", false); return ProjectDeliveryResult.NotPlaying;
        }
        var result = Session.TryDeliver("profesor-" + professor, projectIds[selected]);
        switch (result)
        {
            case ProjectDeliveryResult.Accepted:
                var sprite = professors[professor].GetComponent<SpriteRenderer>();
                if (sprite != null) sprite.color = new Color(.65f, 1f, .75f);
                SetFeedback(projectNames[professor] + " entregado a " + professorNames[professor] + ".", true);
                if (!Session.Completed) NextProject(); else ShowResult();
                break;
            case ProjectDeliveryResult.WrongProject:
                SetFeedback(professorNames[professor] + " necesita: " + projectNames[professor] + ". Selecciónalo en el inventario.", false); break;
            case ProjectDeliveryResult.AlreadyDelivered:
                SetFeedback("Este profesor ya recibió su proyecto. No cuenta de nuevo.", false); break;
            case ProjectDeliveryResult.MissingProject:
                SetFeedback("Ese proyecto ya no está en tu inventario.", false); break;
        }
        RefreshHud(); return result;
    }
    void SetFeedback(string value, bool success)
    {
        if (feedback != null) { feedback.text = value; feedback.color = success ? new Color(.7f, 1, .8f) : Gold; }
        feedbackUntil = Time.unscaledTime + 4;
        effects.PlayOneShot(success ? acceptTone : rejectTone, .45f);
    }

    void BuildHud()
    {
        hud = U2Presentation.NewCanvas("HUD nivel 3", 50);
        var band = U2Presentation.Image(hud.transform, "Cabecera", new Color(.025f, .045f, .065f, .96f));
        Anchor(band.rectTransform, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -20), new Vector2(1460, 140));
        objective = U2Presentation.Text(band.transform, "Objetivo", "", new Vector2(-260, 30), new Vector2(850, 60), 35, Color.white);
        timer = U2Presentation.Text(band.transform, "Reloj", "", new Vector2(490, 30), new Vector2(410, 65), 43, Gold);
        elapsed = U2Presentation.Text(band.transform, "Cronómetro", "", new Vector2(-250, -30), new Vector2(920, 50), 25, Color.white);
        var pause = U2Presentation.Button(band.transform, "Pausa", new Vector2(490, -35), TogglePause);
        ResizeButton(pause, new Vector2(280, 48), 26);
        prompt = U2Presentation.Text(hud.transform, "Interacción", "", Vector2.zero, new Vector2(1480, 65), 31, Color.white);
        Anchor(prompt.rectTransform, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, 250), new Vector2(1480, 65));
        feedback = U2Presentation.Text(hud.transform, "Avisos", "", Vector2.zero, new Vector2(1480, 60), 29, Gold);
        Anchor(feedback.rectTransform, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, 190), new Vector2(1480, 60));
        var tray = U2Presentation.Image(hud.transform, "Inventario de proyectos", new Color(.025f, .045f, .065f, .96f));
        Anchor(tray.rectTransform, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, 15), new Vector2(1460, 165));
        U2Presentation.Text(tray.transform, "Título inventario", "PROYECTOS · 1–5 / clic / Tab para seleccionar", new Vector2(0, 56), new Vector2(1400, 38), 25, Gold);
        itemCaptions = new TMP_Text[projectIds.Length]; itemBackgrounds = new Image[projectIds.Length]; itemButtons = new Button[projectIds.Length];
        float width = 1380f / projectIds.Length;
        for (int i = 0; i < projectIds.Length; i++)
        {
            int index = i;
            var button = U2Presentation.Button(tray.transform, "", new Vector2((i - (projectIds.Length - 1) / 2f) * width, -12), () => SelectProject(index));
            ResizeButton(button, new Vector2(width - 12, 96), 24);
            itemButtons[i] = button; itemBackgrounds[i] = button.GetComponent<Image>();
            itemCaptions[i] = button.GetComponentInChildren<TMP_Text>();
            itemCaptions[i].rectTransform.anchoredPosition = new Vector2(25, 0);
            itemCaptions[i].rectTransform.sizeDelta = new Vector2(width - 75, 88);
            var icon = U2Presentation.Image(button.transform, "Icono proyecto", Color.HSVToRGB(i / (float)projectIds.Length, .35f, 1));
            icon.rectTransform.anchoredPosition = new Vector2(-width / 2 + 37, 0); icon.rectTransform.sizeDelta = new Vector2(36, 48); icon.raycastTarget = false;
            if (projectIcons != null && i < projectIcons.Length && projectIcons[i] != null) icon.sprite = projectIcons[i];
            else for (int line = 0; line < 3; line++)
            {
                var ink = U2Presentation.Image(icon.transform, "Línea documento", Navy);
                ink.rectTransform.anchoredPosition = new Vector2(0, 10 - line * 10); ink.rectTransform.sizeDelta = new Vector2(23, 3); ink.raycastTarget = false;
            }
        }
        if (Application.isMobilePlatform) BuildTouchControls();
    }
    void RefreshHud()
    {
        if (hud == null || Session == null) return;
        objective.text = "LA RUTA DE ENTREGA · " + Session.DeliveredCount + "/" + Session.RequiredCount;
        timer.text = "Tiempo: " + Format(Session.RemainingTime, true);
        timer.color = Session.RemainingTime <= 20 ? new Color(1, .4f, .3f) : Gold;
        elapsed.text = "Recorrido: " + Format(Session.ElapsedTime, false) + " · Esc: pausa · E: entregar el proyecto seleccionado";
        prompt.text = nearest < 0 ? "Busca a los profesores señalados en el campus."
            : Session.IsDelivered("profesor-" + nearest) ? professorNames[nearest] + " · Proyecto recibido"
            : "E: entregar a " + professorNames[nearest] + " · Pide: " + projectNames[nearest];
        if (Time.unscaledTime >= feedbackUntil) feedback.text = "";
        for (int i = 0; i < projectIds.Length; i++)
        {
            bool carried = Session.Inventory.Quantity(projectIds[i]) > 0;
            itemCaptions[i].text = (i + 1) + ". " + projectNames[i] + (carried ? "" : "\nENTREGADO");
            itemButtons[i].interactable = carried;
            itemBackgrounds[i].color = !carried ? new Color(.12f, .25f, .2f) : i == selected ? new Color(.55f, .4f, .15f) : new Color(.2f, .35f, .42f);
        }
    }
    public void TogglePause()
    {
        if (Session == null || Session.Failed || Session.Completed || optionsOpen) return;
        if (Session.Paused) { Resume(); return; }
        SetPaused(true);
        overlay = U2Presentation.NewCanvas("Pausa nivel 3", 100);
        var card = U2Presentation.Card(overlay.transform, "Pausa");
        U2Presentation.Text(card.transform, "Título", "PAUSA", new Vector2(0, 300), new Vector2(1000, 80), 60, Gold);
        U2Presentation.Text(card.transform, "Ayuda", "Nivel 3 · La Ruta de Entrega\nEl reloj y los personajes están detenidos. " + Session.DeliveredCount + "/" + Session.RequiredCount + " proyectos entregados.", new Vector2(0, 180), new Vector2(1100, 130), 32, Color.white);
        U2Presentation.Button(card.transform, "Continuar", new Vector2(0, 40), Resume);
        U2Presentation.Button(card.transform, "Reiniciar", new Vector2(0, -60), Restart);
        U2Presentation.Button(card.transform, "Opciones", new Vector2(0, -160), () =>
        {
            optionsOpen = true;
            U2Presentation.Options(overlay.transform, () => optionsOpen = false, Controls);
        });
        U2Presentation.Button(card.transform, "Menú principal", new Vector2(0, -260), U2Presentation.ReturnToMenu);
    }
    void SetPaused(bool value) { Session.SetPaused(value); Time.timeScale = value ? 0 : 1; }
    public void Resume()
    {
        if (Session == null || Session.Failed || Session.Completed) return;
        if (overlay != null) Destroy(overlay); optionsOpen = false; SetPaused(false);
    }
    void ShowTutorial()
    {
        SetPaused(true); overlay = U2Presentation.NewCanvas("Tutorial nivel 3", 100);
        var card = U2Presentation.Card(overlay.transform, "Tutorial");
        U2Presentation.Text(card.transform, "Título", "¡LOS PROYECTOS ESTÁN LISTOS!", new Vector2(0, 300), new Vector2(1140, 90), 49, Gold);
        U2Presentation.Text(card.transform, "Reglas", "Llevas cinco proyectos en el inventario. Cada profesor recibe uno distinto.\nSelecciona el correcto y acércate para entregarlo con E.\nUn proyecto equivocado no se consume. Las entregas repetidas no cuentan.\nCompleta las cinco entregas antes de que terminen los tres minutos.\n\n" + Controls, new Vector2(0, 20), new Vector2(1120, 430), 32, Color.white);
        U2Presentation.Button(card.transform, "¡Vamos!", new Vector2(0, -300), () => { PlayerPrefs.SetInt(TutorialKey, 1); PlayerPrefs.Save(); Resume(); });
    }
    void ShowResult()
    {
        if (resultShown) return;
        resultShown = true; optionsOpen = false; Time.timeScale = 0;
        if (overlay != null) Destroy(overlay);
        music.Stop();
        if (Session.Failed) { music.clip = timeoutMusic; music.loop = false; if (timeoutMusic != null) music.Play(); }
        overlay = U2Presentation.NewCanvas("Resultado nivel 3", 100);
        var card = U2Presentation.Card(overlay.transform, "Resultado");
        U2Presentation.Text(card.transform, "Título", Session.Completed ? "¡PROYECTOS ENTREGADOS!" : "SE ACABÓ EL TIEMPO", new Vector2(0, 300), new Vector2(1160, 100), 52, Gold);
        U2Presentation.Text(card.transform, "Resultado", (Session.Completed ? "Todos los profesores recibieron su proyecto correcto." : "Quedaron proyectos sin entregar. Revisa el inventario y busca otra ruta.")
            + "\n\nEntregas: " + Session.DeliveredCount + "/" + Session.RequiredCount + "\nTiempo de juego: " + Format(Session.ElapsedTime, false) + "\nTiempo restante: " + Format(Session.RemainingTime, true),
            new Vector2(0, 70), new Vector2(1130, 300), 34, Color.white);
        U2Presentation.Button(card.transform, "Reintentar", new Vector2(0, -150), Restart);
        U2Presentation.Button(card.transform, "Menú principal", new Vector2(0, -250), U2Presentation.ReturnToMenu);
        U2Presentation.Text(card.transform, "Crédito musical", "Música original: Agustín López Parra · FL Studio 25", new Vector2(0, -350), new Vector2(1100, 60), 25, Color.white);
    }
    public void Restart()
    {
        Time.timeScale = 1;
#if UNITY_EDITOR
        if (SceneManager.GetActiveScene().buildIndex < 0)
        {
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(SceneManager.GetActiveScene().path, new LoadSceneParameters(LoadSceneMode.Single));
            return;
        }
#endif
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    static string Format(float value, bool roundUp)
    {
        int seconds = roundUp ? Mathf.CeilToInt(value) : Mathf.FloorToInt(value);
        return (seconds / 60).ToString("00") + ":" + (seconds % 60).ToString("00");
    }
    static void Anchor(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
    { rect.anchorMin = rect.anchorMax = anchor; rect.pivot = pivot; rect.anchoredPosition = position; rect.sizeDelta = size; }
    static void ResizeButton(Button button, Vector2 size, float fontSize)
    {
        button.GetComponent<RectTransform>().sizeDelta = size;
        var text = button.GetComponentInChildren<TMP_Text>(); text.rectTransform.sizeDelta = size - new Vector2(16, 4); text.fontSize = fontSize;
    }
    void BuildTouchControls()
    {
        Vector2[] dirs = { Vector2.left, Vector2.right, Vector2.up, Vector2.down };
        string[] captions = { "←", "→", "↑", "↓" };
        Vector2[] places = { new Vector2(-820, -220), new Vector2(-580, -220), new Vector2(-700, -110), new Vector2(-700, -330) };
        for (int i = 0; i < dirs.Length; i++)
        {
            var b = U2Presentation.Button(hud.transform, captions[i], places[i], () => { }); ResizeButton(b, new Vector2(100, 90), 34);
            b.gameObject.AddComponent<TouchGameControls>().direction = dirs[i];
        }
        var deliver = U2Presentation.Button(hud.transform, "Entregar", new Vector2(730, -220), TryInteract); ResizeButton(deliver, new Vector2(250, 90), 30);
        var run = U2Presentation.Button(hud.transform, "Correr", new Vector2(440, -220), () => { }); ResizeButton(run, new Vector2(250, 90), 30);
        run.gameObject.AddComponent<TouchGameControls>().sprint = true;
    }
    static AudioClip Tone(string name, float frequency)
    {
        const int rate = 24000; const int length = 2400;
        var data = new float[length];
        for (int i = 0; i < length; i++) data[i] = Mathf.Sin(2 * Mathf.PI * frequency * i / rate) * .15f * (1f - i / (float)length);
        var clip = AudioClip.Create(name, length, 1, rate, false); clip.SetData(data, 0); return clip;
    }
    void OnDestroy()
    {
        Time.timeScale = 1;
        if (hud != null) Destroy(hud); if (overlay != null) Destroy(overlay);
        if (acceptTone != null) Destroy(acceptTone); if (rejectTone != null) Destroy(rejectTone);
    }
}
