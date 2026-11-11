using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Inventario de proyectos, entregas, tiempo y presentación compartida con el nivel 1.</summary>
public sealed class CampusDeliveryObjectives : MonoBehaviour
{
    [Header("Jugador y profesores")]
    [SerializeField] Transform player;
    [SerializeField] Transform[] professors;
    [SerializeField] string[] professorNames;
    [Header("Inventario de proyectos")]
    [SerializeField] string[] projectIds;
    [SerializeField] string[] projectNames;
    [SerializeField] Sprite[] projectIcons;
    [Tooltip("Obtiene el nombre del letrero del profesor y genera PROYECTO DE PROF. NOMBRE.")]
    [SerializeField] bool projectNamesFollowProfessors = true;
    [SerializeField] float deliveryDistance = 1.8f;
    [SerializeField] float roundSeconds = 180f;
    [SerializeField, Min(0)] float wrongProjectPenalty = 5f;
    [SerializeField, Min(0)] float repeatDeliveryPenalty = 8f;
    [SerializeField] AudioClip levelMusic;
    [SerializeField] AudioClip timeoutMusic;
    [SerializeField] bool showTutorial = true;
    [Header("Retos de los profesores")]
    [SerializeField] bool enableFinalChallenge = true;
    [SerializeField, Min(0)] float thrownObjectPenalty = 5f;
    public CampusFinalDeliveryChallenge FinalChallenge { get; private set; }
    public CampusMinimap Minimap { get; private set; }
    public CampusCourtesyRequest Courtesy { get; private set; }
    CampusPathPlayer pathPlayer;
    TMP_Text challengeStatus;
    bool challengeDialogueShown, completionOpen;
    int pendingChallengeProfessor = -1;
    bool[] challengeAnnounced;
    [Header("Contexto narrativo inicial")]
    [SerializeField] bool showNarrativeIntro = true;
    [SerializeField, TextArea(3, 6)] string narrativeIntro = "Galindo: «Cinco proyectos y el sistema cierra en tres minutos... ¡ya casi!»\n\nCompañera: «Los profesores lanzan papel. Esquiva antes de entregar y pide permiso con Q si los alumnos te bloquean».\n\nGalindo: «¡Vamos por esas unidades!»";
    public ProjectDeliverySession Session { get; private set; }
    public int SelectedProject => selected;
    public bool IsPaused => Session != null && Session.Paused;
    GameObject hud, overlay;
    TMP_Text timer, objective, prompt, feedback;
    CanvasGroup promptPanel, feedbackPanel, challengePanel;
    TMP_Text[] itemCaptions;
    TMP_Text[] itemStatuses;
    Image[] itemBackgrounds;
    Button[] itemButtons;
    Outline[] itemHighlights;
    AudioSource music, effects;
    AudioClip acceptTone, rejectTone, throwTone;
    int nearest = -1, nearbyProfessor = -1, selected;
    float feedbackUntil, feedbackBegan;
    bool resultShown, optionsOpen, narrativeOpen, helpOpen;
    static readonly Color Gold = new Color32(255, 213, 107, 255);
    static readonly Color Navy = new Color32(24, 39, 51, 255);
    const string TutorialKey = "YaCasi.Nivel03.Tutorial.YC14.Penalizaciones";
    public const string Controls = "Moverte: WASD / flechas / joystick. Correr: Shift / RT.\nEsquivar: Espacio / B. Pedir permiso: Q / LB.\nProyecto: 1–5 / clic. Siguiente: Tab / RB. Entregar: E / A.\nMinimapa: M. Pausa: Esc / Start.";

    void Awake()
    {
        Application.runInBackground = true;
        if (projectNamesFollowProfessors) SyncProjectNames();
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
        Session = new ProjectDeliverySession(ids, projectIds, roundSeconds, inventory, wrongProjectPenalty, repeatDeliveryPenalty);
        challengeAnnounced = new bool[professors.Length];
        Time.timeScale = 1;
        music = gameObject.AddComponent<AudioSource>();
        music.playOnAwake = false; music.spatialBlend = 0; music.loop = true;
        music.clip = levelMusic; music.volume = U2Presentation.MusicVolume;
        if (levelMusic != null) music.Play();
        effects = gameObject.AddComponent<AudioSource>(); effects.spatialBlend = 0; effects.playOnAwake = false;
        acceptTone = Tone("Entrega correcta", 760);
        rejectTone = Tone("Proyecto incorrecto", 180);
        throwTone = Tone("Lanzamiento de papel", 430);
    }

    void Start()
    {
        if (Session == null) return;
        pathPlayer = player.GetComponent<CampusPathPlayer>();
        if (pathPlayer != null)
        {
            Courtesy = gameObject.AddComponent<CampusCourtesyRequest>();
            Courtesy.Configure(pathPlayer);
            Courtesy.Message += OnCourtesyMessage;
        }
        if (enableFinalChallenge) ConfigureFinalChallengeForVerification();
        BuildHud(); RefreshHud();
        if (showNarrativeIntro) ShowNarrativeIntro();
        else if (showTutorial && !PlayerPrefs.HasKey(TutorialKey)) ShowTutorial();
    }

    [ContextMenu("Actualizar nombres desde profesores")]
    public void SyncProjectNames()
    {
        if (professors == null) return;
        if (professorNames == null || professorNames.Length != professors.Length) professorNames = new string[professors.Length];
        if (projectNames == null || projectNames.Length != professors.Length) projectNames = new string[professors.Length];
        for (int i = 0; i < professors.Length; i++)
        {
            if (professors[i] == null) continue;
            var label = professors[i].GetComponentInChildren<CampusBuildingLabel>(true);
            if (label != null && !string.IsNullOrWhiteSpace(label.DisplayName)) professorNames[i] = label.DisplayName;
            if (!string.IsNullOrWhiteSpace(professorNames[i])) projectNames[i] = "PROYECTO DE " + professorNames[i].Trim().ToUpperInvariant();
        }
    }

    void Update()
    {
        if (Session == null || hud == null) return;
        UpdateFeedbackFade();
        music.volume = U2Presentation.MusicVolume * (Session.Paused ? .5f : 1);
        effects.volume = U2Presentation.EffectsVolume;
        bool pause = (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            || (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame);
        if (pause) { if (helpOpen) CloseHelp(); else TogglePause(); }
        if (Session.Failed || Session.Completed)
        {
            if (!resultShown && !completionOpen) { ShowResult(); RefreshHud(); }
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) Restart();
            return;
        }
        Session.AdvanceTime(Time.deltaTime);
        if (Session.Failed) { ShowResult(); RefreshHud(); return; }
        if (Session.Paused) return;
        UpdateProfessorChallenges();
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
        nearbyProfessor = -1; float nearbyBest = 3.2f * 3.2f;
        for (int i = 0; i < professors.Length; i++)
        {
            float distance = ((Vector2)player.position - (Vector2)professors[i].position).sqrMagnitude;
            if (distance < nearbyBest) { nearbyBest = distance; nearbyProfessor = i; }
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
        float before = Session.RemainingTime;
        if (enableFinalChallenge && FinalChallenge != null
            && !Session.IsDelivered("profesor-" + professor) && selected == professor && !ProfessorChallengeCleared(professor))
        {
            StartProfessorChallenge(professor);
            if (Session.Playing) SetFeedback("Primero supera el reto", false);
            return ProjectDeliveryResult.ChallengePending;
        }
        var result = Session.TryDeliver("profesor-" + professor, projectIds[selected]);
        string deduction = "−" + (before - Session.RemainingTime).ToString("0.#") + " s";
        switch (result)
        {
            case ProjectDeliveryResult.Accepted:
                var sprite = professors[professor].GetComponent<SpriteRenderer>();
                if (sprite != null) sprite.color = new Color(.65f, 1f, .75f);
                SetFeedback(ShortProfessorName(professor) + ": proyecto entregado", true);
                if (!Session.Completed) NextProject(); else ShowLevelComplete();
                break;
            case ProjectDeliveryResult.WrongProject:
                SetFeedback("Proyecto equivocado · " + deduction, false); break;
            case ProjectDeliveryResult.AlreadyDelivered:
                SetFeedback("Entrega repetida · " + deduction, false); break;
            case ProjectDeliveryResult.MissingProject:
                SetFeedback("Ese proyecto ya no está en tu inventario.", false); break;
        }
        if (Session.Failed) ShowResult();
        RefreshHud(); return result;
    }
    void SetFeedback(string value, bool success)
    {
        if (feedback != null) { feedback.text = value; feedback.color = success ? new Color(.7f, 1, .8f) : Gold; }
        feedbackBegan = Time.time; feedbackUntil = feedbackBegan + 2.8f;
        effects.PlayOneShot(success ? acceptTone : rejectTone, .45f);
    }

    public void ConfigureFinalChallengeForVerification()
    {
        enableFinalChallenge = true;
        if (FinalChallenge != null) return;
        pathPlayer = player.GetComponent<CampusPathPlayer>();
        if (pathPlayer == null) return;
        FinalChallenge = gameObject.AddComponent<CampusFinalDeliveryChallenge>();
        FinalChallenge.Configure(this, pathPlayer, professors);
    }
    int LastProfessor()
    {
        for (int i = 0; i < professors.Length; i++)
            if (!Session.IsDelivered("profesor-" + i)) return i;
        return -1;
    }
    public void RequestFinalChallenge()
    {
        int last = LastProfessor();
        if (last >= 0) StartProfessorChallenge(last);
    }
    void OnCourtesyMessage(string message)
    {
        string value = message.StartsWith("Espera", StringComparison.Ordinal) ? "Permiso: espera " + Mathf.CeilToInt(Courtesy.CooldownRemaining) + " s"
            : message.Contains("se apart") ? "«Con permiso» · Te dejan pasar"
            : message.Contains("siguen") ? "«Con permiso» · No se apartan" : "«Con permiso»";
        SetFeedback(value, true);
    }
    bool ProfessorChallengeCleared(int professor) => FinalChallenge.IsCleared(professor, Session.DeliveredCount == Session.RequiredCount - 1);
    string ShortProfessorName(int index)
    {
        string value = professorNames[index].Trim();
        foreach (string prefix in new[] { "Profesora ", "Profesor ", "Prof. ", "Prof " })
            if (value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return value.Substring(prefix.Length).Trim();
        return value;
    }
    void UpdateProfessorChallenges()
    {
        if (!enableFinalChallenge || FinalChallenge == null) return;
        int active = FinalChallenge.ActiveProfessor;
        if (active >= 0 && Session.DeliveredCount == Session.RequiredCount - 1
            && !Session.IsDelivered("profesor-" + active) && FinalChallenge.State.Duration < FinalDeliveryChallengeState.DurationSeconds)
            StartProfessorChallenge(active);
        if (active >= 0 && FinalChallenge.IsCleared(active) && !challengeAnnounced[active])
        {
            challengeAnnounced[active] = true;
            if (!Session.IsDelivered("profesor-" + active))
                SetFeedback("¡Reto superado!", true);
        }
        if (FinalChallenge.IsRunning && FinalChallenge.InChallengeArea) return;
        int candidate = -1; float best = 4.2f;
        for (int i = 0; i < professors.Length; i++)
        {
            if (Session.IsDelivered("profesor-" + i) || ProfessorChallengeCleared(i)) continue;
            float distance = Vector2.Distance(player.position, professors[i].position);
            if (distance >= best) continue;
            best = distance; candidate = i;
        }
        if (candidate >= 0) StartProfessorChallenge(candidate);
    }
    void StartProfessorChallenge(int professor)
    {
        if (FinalChallenge == null || !Session.Playing || ProfessorChallengeCleared(professor)) return;
        if (challengeDialogueShown)
        {
            if (FinalChallenge.BeginForProfessor(professor, Session.DeliveredCount == Session.RequiredCount - 1))
            {
                challengeAnnounced[professor] = false;
            }
            return;
        }
        pendingChallengeProfessor = professor;
        challengeDialogueShown = true; narrativeOpen = true; SetPaused(true);
        overlay = U2Presentation.NewCanvas("Diálogo retos de entrega", 100);
        var card = U2Presentation.Card(overlay.transform, "Esquiva y entrega");
        U2Presentation.Text(card.transform, "Título", "Esquiva y entrega", new Vector2(0, 300), new Vector2(1120, 90), 48, Gold);
        U2Presentation.Text(card.transform, "Diálogo", professorNames[professor] + ": «Antes de entregarme el proyecto, esquiva estos papeles».\n\nGalindo: «¿Todos los profesores van a hacer lo mismo?»\n\nCompañera: «Sí. Fíjate en las líneas naranjas y pide permiso a los alumnos con Q si te bloquean».",
            new Vector2(0, 80), new Vector2(1100, 300), 34, Color.white);
        U2Presentation.Text(card.transform, "Reglas", "Cada profesor: 8 s de esquive. Última entrega: 16 s.\nEspacio / B: esquivar. Cada impacto resta " + thrownObjectPenalty.ToString("0.#") + " s.\nAl terminar el reto, entrega con E. El reloj está detenido mientras lees.",
            new Vector2(0, -140), new Vector2(1100, 140), 32, Gold);
        U2Presentation.Button(card.transform, "¡Puedo hacerlo!", new Vector2(0, -310), ResumeFinalChallenge);
        CampusTypography.Apply(overlay.transform);
    }
    public void ResumeFinalChallenge()
    {
        if (!challengeDialogueShown || !narrativeOpen || FinalChallenge == null || Session.Failed || Session.Completed) return;
        narrativeOpen = false; Resume();
        int professor = pendingChallengeProfessor;
        pendingChallengeProfessor = -1;
        if (professor >= 0) FinalChallenge.BeginForProfessor(professor, Session.DeliveredCount == Session.RequiredCount - 1);
    }
    public bool ReceiveThrownObjectHit()
    {
        if (FinalChallenge == null || !FinalChallenge.State.Started || FinalChallenge.State.Completed
            || pathPlayer == null || pathPlayer.IsInvulnerable || !Session.Playing) return false;
        float before = Session.RemainingTime;
        if (!Session.ApplyHazardHit(thrownObjectPenalty)) return false;
        pathPlayer.ApplyImpactFeedback();
        SetFeedback("Impacto · −" + (before - Session.RemainingTime).ToString("0.#") + " s", false);
        if (Session.Failed) ShowResult();
        RefreshHud(); return true;
    }
    public void PlayThrowCue()
    {
        if (effects != null && Session.Playing) effects.PlayOneShot(throwTone, .22f);
    }

    void BuildHud()
    {
        hud = U2Presentation.NewCanvas("HUD nivel 3", 50);
        var progressCard = U2Presentation.Image(hud.transform, "Progreso", new Color(.025f, .045f, .065f, .9f));
        Anchor(progressCard.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(18, -18), new Vector2(260, 54));
        objective = U2Presentation.Text(progressCard.transform, "Objetivo", "", Vector2.zero, new Vector2(240, 44), 26, Color.white);
        var clockCard = U2Presentation.Image(hud.transform, "Tiempo restante", new Color(.025f, .045f, .065f, .9f));
        Anchor(clockCard.rectTransform, Vector2.one, Vector2.one, new Vector2(-18, -18), new Vector2(220, 54));
        timer = U2Presentation.Text(clockCard.transform, "Reloj", "", Vector2.zero, new Vector2(200, 44), 30, Gold);
        var pause = U2Presentation.Button(hud.transform, "Pausa", Vector2.zero, TogglePause);
        Anchor(pause.GetComponent<RectTransform>(), new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(-72, -18), new Vector2(130, 44));
        ResizeButton(pause, new Vector2(130, 44), 22);
        var help = U2Presentation.Button(hud.transform, "Ayuda", Vector2.zero, ShowHelp);
        Anchor(help.GetComponent<RectTransform>(), new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(72, -18), new Vector2(130, 44));
        ResizeButton(help, new Vector2(130, 44), 22);
        prompt = U2Presentation.Text(hud.transform, "Interacción", "", Vector2.zero, new Vector2(440, 64), 22, Color.white);
        Anchor(prompt.rectTransform, Vector2.zero, Vector2.zero, new Vector2(18, 122), new Vector2(440, 64));
        feedback = U2Presentation.Text(hud.transform, "Avisos", "", Vector2.zero, new Vector2(520, 48), 23, Gold);
        Anchor(feedback.rectTransform, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, 126), new Vector2(520, 48));
        if (Application.isMobilePlatform)
        {
            Anchor(prompt.rectTransform, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, 122), new Vector2(440, 64));
            feedback.rectTransform.anchoredPosition = new Vector2(0, 196);
        }
        var tray = U2Presentation.Image(hud.transform, "Inventario de proyectos", new Color(.025f, .045f, .065f, .96f));
        Anchor(tray.rectTransform, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, 12), new Vector2(1060, 96));
        itemCaptions = new TMP_Text[projectIds.Length]; itemStatuses = new TMP_Text[projectIds.Length]; itemBackgrounds = new Image[projectIds.Length]; itemButtons = new Button[projectIds.Length];
        itemHighlights = new Outline[projectIds.Length];
        var sheets = Resources.LoadAll<Sprite>("CampusProjectSheet");
        var notebooks = Resources.LoadAll<Sprite>("CampusProjectNotebook");
        float width = 1000f / projectIds.Length;
        for (int i = 0; i < projectIds.Length; i++)
        {
            int index = i;
            var button = U2Presentation.Button(tray.transform, "", new Vector2((i - (projectIds.Length - 1) / 2f) * width, 0), () => SelectProject(index));
            ResizeButton(button, new Vector2(width - 8, 68), 23);
            itemButtons[i] = button; itemBackgrounds[i] = button.GetComponent<Image>();
            itemHighlights[i] = button.gameObject.AddComponent<Outline>();
            itemHighlights[i].effectColor = new Color(.4f, .95f, 1); itemHighlights[i].effectDistance = new Vector2(2, -2); itemHighlights[i].enabled = false;
            itemCaptions[i] = button.GetComponentInChildren<TMP_Text>();
            itemCaptions[i].rectTransform.anchoredPosition = new Vector2(22, 13);
            itemCaptions[i].rectTransform.sizeDelta = new Vector2(width - 65, 32);
            itemStatuses[i] = U2Presentation.Text(button.transform, "Estado del proyecto", "", new Vector2(22, -15), new Vector2(width - 65, 24), 17, Gold);
            U2Presentation.Text(button.transform, "Tecla del proyecto", (i + 1).ToString(), new Vector2(-width / 2 + 18, 23), new Vector2(24, 18), 18, Gold);
            var icon = U2Presentation.Image(button.transform, "Hoja o cuaderno de proyecto", Color.white);
            icon.rectTransform.anchoredPosition = new Vector2(-width / 2 + 30, -7); icon.rectTransform.sizeDelta = new Vector2(32, 44); icon.raycastTarget = false;
            icon.preserveAspect = true;
            if (projectIcons != null && i < projectIcons.Length && projectIcons[i] != null) icon.sprite = projectIcons[i];
            else if (i % 2 == 1 && notebooks.Length > 0) icon.sprite = notebooks[0];
            else if (sheets.Length > 0) icon.sprite = sheets[0];
            else icon.enabled = false;
        }
        if (Application.isMobilePlatform) BuildTouchControls();
        challengeStatus = U2Presentation.Text(hud.transform, "Reto", "", Vector2.zero, new Vector2(360, 46), 24, Gold);
        Anchor(challengeStatus.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(18, -90), new Vector2(360, 46));
        promptPanel = AddTextBacking(prompt); feedbackPanel = AddTextBacking(feedback); challengePanel = AddTextBacking(challengeStatus);
        if (pathPlayer != null)
        {
            Minimap = gameObject.AddComponent<CampusMinimap>();
            Minimap.Configure(hud.transform, pathPlayer, professors, Session);
        }
        CampusTypography.Apply(hud.transform, true);
    }
    void RefreshHud()
    {
        if (hud == null || Session == null) return;
        objective.text = "Proyectos " + Session.DeliveredCount + "/" + Session.RequiredCount;
        timer.text = Format(Session.RemainingTime, true);
        timer.color = Session.RemainingTime <= 20 ? new Color(1, .4f, .3f) : Gold;
        bool inChallenge = FinalChallenge != null && FinalChallenge.IsRunning;
        string challengeText = "";
        if (inChallenge)
        {
            int active = FinalChallenge.ActiveProfessor;
            string name = active >= 0 ? ShortProfessorName(active) : "Profesor";
            challengeText = FinalChallenge.InChallengeArea ? "Esquiva · " + Mathf.CeilToInt(FinalChallenge.State.RemainingTime) + " s" : "Regresa con " + name;
        }
        ShowNotice(challengeStatus, challengePanel, challengeText);
        string nearbyText = "";
        if (!inChallenge && nearbyProfessor >= 0)
        {
            bool delivered = Session.IsDelivered("profesor-" + nearbyProfessor);
            bool ready = !enableFinalChallenge || FinalChallenge == null || ProfessorChallengeCleared(nearbyProfessor);
            nearbyText = professorNames[nearbyProfessor] + " · Proyecto " + (nearbyProfessor + 1) + "\n"
                + (delivered ? "Entregado" : !ready ? "Esquiva para entregar" : nearest == nearbyProfessor ? "E: entregar" : "Acércate para entregar");
        }
        ShowNotice(prompt, promptPanel, nearbyText);
        for (int i = 0; i < projectIds.Length; i++)
        {
            bool carried = Session.Inventory.Quantity(projectIds[i]) > 0;
            itemCaptions[i].text = ShortProfessorName(i);
            bool requested = carried && i == nearbyProfessor && !Session.IsDelivered("profesor-" + i);
            itemStatuses[i].text = !carried ? "Entregado" : i == selected ? "Elegido" : requested ? "Lo pide" : "Pendiente";
            itemStatuses[i].color = !carried ? new Color(.6f, 1f, .7f) : requested ? new Color(.4f, .95f, 1) : i == selected ? Gold : Color.white;
            itemHighlights[i].enabled = requested;
            itemButtons[i].interactable = carried;
            itemBackgrounds[i].color = !carried ? new Color(.12f, .25f, .2f) : i == selected ? new Color(.55f, .4f, .15f) : new Color(.2f, .35f, .42f);
        }
    }
    public void TogglePause()
    {
        if (Session == null || Session.Failed || Session.Completed || optionsOpen || narrativeOpen || helpOpen) return;
        if (Session.Paused) { Resume(); return; }
        SetPaused(true);
        overlay = U2Presentation.NewCanvas("Pausa nivel 3", 100);
        var card = U2Presentation.Card(overlay.transform, "Pausa");
        U2Presentation.Text(card.transform, "Título", "Pausa", new Vector2(0, 300), new Vector2(1000, 80), 44, Gold);
        U2Presentation.Text(card.transform, "Estadísticas", "Proyectos: " + Session.DeliveredCount + "/" + Session.RequiredCount
            + " · Tiempo restante: " + Format(Session.RemainingTime, true)
            + "\nRecorrido: " + Format(Session.ElapsedTime, false)
            + "\nAmonestaciones: " + Session.WarningCount + " · Impactos: " + Session.HazardHitCount
            + "\nDescuento acumulado: −" + Session.TotalPenaltySeconds.ToString("0.#") + " s",
            new Vector2(0, 165), new Vector2(1100, 150), 28, Color.white);
        U2Presentation.Button(card.transform, "Continuar", new Vector2(0, 40), Resume);
        U2Presentation.Button(card.transform, "Reiniciar", new Vector2(0, -60), Restart);
        U2Presentation.Button(card.transform, "Opciones", new Vector2(0, -160), () =>
        {
            optionsOpen = true;
            U2Presentation.Options(overlay.transform, () => optionsOpen = false, Controls);
            CampusTypography.Apply(overlay.transform);
        });
        U2Presentation.Button(card.transform, "Menú principal", new Vector2(0, -260), U2Presentation.ReturnToMenu);
        CampusTypography.Apply(overlay.transform);
    }
    public void ShowHelp()
    {
        if (Session == null || !Session.Playing || helpOpen || narrativeOpen) return;
        helpOpen = true; SetPaused(true);
        overlay = U2Presentation.NewCanvas("Ayuda nivel 3", 100);
        var card = U2Presentation.Card(overlay.transform, "Ayuda");
        U2Presentation.Text(card.transform, "Título", "Controles y entregas", new Vector2(0, 300), new Vector2(1100, 80), 44, Gold);
        U2Presentation.Text(card.transform, "Controles", Controls, new Vector2(0, 120), new Vector2(1100, 220), 30, Color.white);
        U2Presentation.Text(card.transform, "Reglas", "El borde azul del inventario señala el proyecto del profesor cercano.\nEsquiva sus objetos y luego entrega con E.\nProyecto equivocado: −" + wrongProjectPenalty + " s. Repetir: −" + repeatDeliveryPenalty + " s.\nLos alumnos no siempre responden cuando pides permiso.",
            new Vector2(0, -100), new Vector2(1100, 180), 28, Gold);
        U2Presentation.Button(card.transform, "Volver al juego", new Vector2(0, -300), CloseHelp);
        CampusTypography.Apply(overlay.transform);
    }
    void CloseHelp() { if (!helpOpen) return; helpOpen = false; Resume(); }
    void SetPaused(bool value) { Session.SetPaused(value); Time.timeScale = value ? 0 : 1; }
    public void Resume()
    {
        if (Session == null || Session.Failed || Session.Completed || narrativeOpen) return;
        if (overlay != null) Destroy(overlay); optionsOpen = false; SetPaused(false);
    }
    void ShowTutorial()
    {
        SetPaused(true); overlay = U2Presentation.NewCanvas("Tutorial nivel 3", 100);
        var card = U2Presentation.Card(overlay.transform, "Tutorial");
        U2Presentation.Text(card.transform, "Título", "¡Los proyectos están listos!", new Vector2(0, 300), new Vector2(1140, 90), 49, Gold);
        U2Presentation.Text(card.transform, "Reglas", "Cada profesor lanza papel al acercarte: esquiva y luego entrega.\nReto normal: 8 s. Última entrega: 16 s. Impacto: −" + thrownObjectPenalty + " s.\nProyecto equivocado: −" + wrongProjectPenalty.ToString("0.#") + " s. Entrega repetida: −" + repeatDeliveryPenalty.ToString("0.#") + " s.\nCon Q puedes pedir paso; los alumnos no siempre se apartan.\n\n" + Controls, new Vector2(0, 20), new Vector2(1120, 430), 32, Color.white);
        U2Presentation.Button(card.transform, "¡Vamos!", new Vector2(0, -300), () => { PlayerPrefs.SetInt(TutorialKey, 1); PlayerPrefs.Save(); Resume(); });
        CampusTypography.Apply(overlay.transform);
    }
    void ShowNarrativeIntro()
    {
        narrativeOpen = true; SetPaused(true);
        overlay = U2Presentation.NewCanvas("Contexto narrativo nivel 3", 100);
        var card = U2Presentation.Card(overlay.transform, "Contexto");
        U2Presentation.Text(card.transform, "Título", "Galindo · La ruta de entrega", new Vector2(0, 300), new Vector2(1150, 90), 48, Gold);
        U2Presentation.Text(card.transform, "Historia", narrativeIntro, new Vector2(0, 100), new Vector2(1100, 290), 34, Color.white);
        U2Presentation.Text(card.transform, "Objetivo", "Cinco proyectos · Tres minutos\n" + Controls,
            new Vector2(0, -115), new Vector2(1100, 140), 24, Gold);
        U2Presentation.Button(card.transform, "Comenzar", new Vector2(0, -240), () =>
        {
            narrativeOpen = false;
            PlayerPrefs.SetInt(TutorialKey, 1); PlayerPrefs.Save(); Resume();
        });
        var rules = U2Presentation.Button(card.transform, "Controles y reglas", new Vector2(0, -340), () =>
        {
            narrativeOpen = false; Destroy(overlay); ShowTutorial();
        });
        ResizeButton(rules, new Vector2(620, 60), 28);
        CampusTypography.Apply(overlay.transform);
    }
    void ShowLevelComplete()
    {
        if (completionOpen || resultShown) return;
        completionOpen = true; optionsOpen = false; narrativeOpen = false; Time.timeScale = 0;
        if (FinalChallenge != null) FinalChallenge.Clear();
        if (overlay != null) Destroy(overlay);
        music.Stop();
        overlay = U2Presentation.NewCanvas("Nivel 3 superado", 100);
        var card = U2Presentation.Card(overlay.transform, "Nivel superado");
        U2Presentation.Text(card.transform, "Título", "¡Nivel superado!", new Vector2(0, 270), new Vector2(1120, 110), 64, Gold);
        U2Presentation.Text(card.transform, "Proyectos entregados", "Proyectos entregados: " + Session.DeliveredCount + "/" + Session.RequiredCount
            + "\n\nTodos los profesores recibieron su proyecto.\n\nGalindo: «¡Ya casi... ahora sí terminé!»",
            new Vector2(0, 50), new Vector2(1100, 280), 38, Color.white);
        U2Presentation.Button(card.transform, "Ver resumen", new Vector2(0, -210), () => { completionOpen = false; ShowResult(); });
        U2Presentation.Button(card.transform, "Volver al menú", new Vector2(0, -310), U2Presentation.ReturnToMenu);
        CampusTypography.Apply(overlay.transform);
    }
    void ShowResult()
    {
        if (resultShown) return;
        resultShown = true; optionsOpen = false; Time.timeScale = 0;
        if (FinalChallenge != null) FinalChallenge.Clear();
        if (overlay != null) Destroy(overlay);
        music.Stop();
        if (Session.Failed) { music.clip = timeoutMusic; music.loop = false; if (timeoutMusic != null) music.Play(); }
        overlay = U2Presentation.NewCanvas("Resultado nivel 3", 100);
        var card = U2Presentation.Card(overlay.transform, "Resultado");
        U2Presentation.Text(card.transform, "Título", Session.Completed ? "Proyectos entregados" : "Se acabó el tiempo", new Vector2(0, 300), new Vector2(1160, 100), 52, Gold);
        U2Presentation.Text(card.transform, "Resultado", (Session.Completed ? "PROFESOR: «Recibido, Galindo. Llegaste justo antes del cierre».\nGALINDO: «¡Por fin! Todos los proyectos están entregados»." : "GALINDO: «El sistema cerró... Debo elegir mejor la ruta y esquivar el papel».")
            + "\n\nEntregas: " + Session.DeliveredCount + "/" + Session.RequiredCount + "\nTiempo de juego: " + Format(Session.ElapsedTime, false) + "\nTiempo restante: " + Format(Session.RemainingTime, true)
            + "\nAmonestaciones: " + Session.WarningCount + " · Impactos: " + Session.HazardHitCount + "\nDescuento total: −" + Session.TotalPenaltySeconds.ToString("0.#") + " s",
            new Vector2(0, 70), new Vector2(1130, 340), 32, Color.white);
        U2Presentation.Button(card.transform, "Reintentar", new Vector2(0, -150), Restart);
        U2Presentation.Button(card.transform, "Menú principal", new Vector2(0, -250), U2Presentation.ReturnToMenu);
        U2Presentation.Text(card.transform, "Crédito musical", "Música original: Agustín López Parra · FL Studio 25", new Vector2(0, -350), new Vector2(1100, 60), 25, Color.white);
        CampusTypography.Apply(overlay.transform);
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
    static CanvasGroup AddTextBacking(TMP_Text text)
    {
        var source = text.rectTransform;
        Vector2 size = source.sizeDelta;
        var container = new GameObject("Tarjeta " + text.name, typeof(RectTransform), typeof(CanvasGroup));
        container.transform.SetParent(text.transform.parent, false);
        container.transform.SetSiblingIndex(text.transform.GetSiblingIndex());
        Anchor(container.GetComponent<RectTransform>(), source.anchorMin, source.pivot, source.anchoredPosition, size);
        var backing = U2Presentation.Image(container.transform, "Fondo", new Color(.025f, .045f, .065f, .88f));
        U2Presentation.Stretch(backing.rectTransform);
        backing.raycastTarget = false;
        text.transform.SetParent(container.transform, false);
        Anchor(source, Vector2.one * .5f, Vector2.one * .5f, Vector2.zero, size - new Vector2(20, 8));
        var group = container.GetComponent<CanvasGroup>();
        group.alpha = 0; group.blocksRaycasts = false; group.interactable = false;
        return group;
    }
    static void ShowNotice(TMP_Text label, CanvasGroup panel, string value)
    {
        bool show = !string.IsNullOrEmpty(value);
        if (show) label.text = value;
        panel.alpha = Mathf.MoveTowards(panel.alpha, show ? 1 : 0, Time.unscaledDeltaTime * 8);
        if (!show && panel.alpha <= .001f) label.text = "";
    }
    void UpdateFeedbackFade()
    {
        if (feedbackPanel == null) return;
        float remaining = feedbackUntil - Time.time;
        feedbackPanel.alpha = remaining <= 0 ? 0 : Mathf.Min(Mathf.Clamp01((Time.time - feedbackBegan) / .12f), Mathf.Clamp01(remaining / .45f));
        if (remaining <= 0) feedback.text = "";
    }
    void BuildTouchControls()
    {
        Vector2[] dirs = { Vector2.left, Vector2.right, Vector2.up, Vector2.down };
        string[] captions = { "A", "D", "W", "S" };
        Vector2[] places = { new Vector2(-820, -220), new Vector2(-580, -220), new Vector2(-700, -110), new Vector2(-700, -330) };
        for (int i = 0; i < dirs.Length; i++)
        {
            var b = U2Presentation.Button(hud.transform, captions[i], places[i], () => { }); ResizeButton(b, new Vector2(100, 90), 26);
            b.gameObject.AddComponent<TouchGameControls>().direction = dirs[i];
        }
        var deliver = U2Presentation.Button(hud.transform, "Entregar", new Vector2(730, -220), TryInteract); ResizeButton(deliver, new Vector2(250, 90), 24);
        var run = U2Presentation.Button(hud.transform, "Correr", new Vector2(440, -220), () => { }); ResizeButton(run, new Vector2(250, 90), 24);
        run.gameObject.AddComponent<TouchGameControls>().sprint = true;
        var dodge = U2Presentation.Button(hud.transform, "Esquivar", new Vector2(730, -100), () => pathPlayer?.TryDodge());
        ResizeButton(dodge, new Vector2(250, 90), 24);
        var courtesy = U2Presentation.Button(hud.transform, "Permiso", new Vector2(440, -100), () => Courtesy?.RequestPassage());
        ResizeButton(courtesy, new Vector2(250, 90), 24);
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
        if (Courtesy != null) Courtesy.Message -= OnCourtesyMessage;
        if (hud != null) Destroy(hud); if (overlay != null) Destroy(overlay);
        if (acceptTone != null) Destroy(acceptTone); if (rejectTone != null) Destroy(rejectTone);
        if (throwTone != null) Destroy(throwTone);
    }
}
