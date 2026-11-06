// Proyecto Ya Casi — Agustín López Parra y Carlos Manuel Palma Muñoz.
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>Presentación y controles de la partida; no modifica los objetos guardados de la escena.</summary>
public sealed class CampusGameExperience : MonoBehaviour
{
    Level01Manager manager;
    PlayerMovement player;
    PlayerInteraction interaction;
    LockerInteraction[] lockers;
    readonly List<LineRenderer> outlines = new List<LineRenderer>();
    readonly List<GameObject> marks = new List<GameObject>();
    GameObject overlay, resume;
    TMP_Text title, details, timer;
    TMP_FontAsset font;
    AudioSource audioSource;
    AudioClip step, alarm;
    Vector3 lastPosition;
    float distanceWalked, elapsed;
    int lastAlarm = -1;
    bool paused, finished;
    Color timerColor;
    string pauseTitle, pauseDetails;
    Color pauseTitleColor;
    const string VolumeKey = "YaCasi.MasterVolume";

    void Start()
    {
        manager = GetComponent<Level01Manager>();
        player = FindFirstObjectByType<PlayerMovement>();
        interaction = FindFirstObjectByType<PlayerInteraction>();
        lockers = FindObjectsByType<LockerInteraction>(FindObjectsSortMode.None);
        int usbIndex = lockers.Length > 0 ? Random.Range(0, lockers.Length) : -1;
        for (int i = 0; i < lockers.Length; i++)
        {
            lockers[i].InitializeRound(manager, i == usbIndex);
            outlines.Add(CreateBorder(lockers[i]));
            marks.Add(CreateMark(lockers[i]));
        }
        // Mantiene tiempo de recorrido incluso si la USB sale en el último casillero.
        manager.EnsureRoundTime(Mathf.Max(90f, (lockers.Length - 1) * 10f + 60f));
        foreach (TMP_Text text in FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (text.name == "TextoTiempo") timer = text;
            if (text.font != null && text.font.name.Contains("Pixelify")) font = text.font;
        }
        if (font == null && timer != null) font = timer.font;
        if (timer != null) timerColor = timer.color;
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.volume = .35f;
        step = MakeTone("Paso", .085f, 120f, true);
        alarm = MakeTone("Aviso de tiempo", .22f, 740f, false);
        AudioListener.volume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, .8f));
        if (player != null) lastPosition = player.transform.position;
        BuildMenu();
        pauseTitle = title.text;
        pauseDetails = details.text;
        pauseTitleColor = title.color;
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame && !finished)
            SetPause(!paused);
        bool ended = manager.IsCompleted || manager.IsFailed;
        if (ended && !finished)
        {
            finished = true;
            if (paused) SetPause(false);
            title.text = manager.IsCompleted ? "¡USB RECUPERADA!" : "SE ACABÓ EL TIEMPO";
            title.color = manager.IsCompleted ? new Color(1f,.85f,.45f) : new Color(1f,.45f,.4f);
            int searched = 0;
            foreach (var locker in lockers) if (locker != null && locker.IsSearched) searched++;
            details.text = (manager.IsCompleted ? "Llegaste a la salida a tiempo." : "El edificio cerró. ¡Inténtalo de nuevo!")
                + $"\n\nTiempo de juego: {Format(elapsed)}\nTiempo restante: {Format(manager.RemainingTime)}\nCasilleros revisados: {searched} / {lockers.Length}";
            resume.SetActive(false);
            overlay.SetActive(true);
            audioSource.Stop();
        }
        UpdateLockers(!paused && !finished);
        if (paused || finished || player == null) return;
        elapsed += Time.deltaTime;
        float distance = Vector3.Distance(player.transform.position, lastPosition);
        lastPosition = player.transform.position;
        // Los pasos dependen del desplazamiento real: no suenan al empujar una pared.
        if (distance < .5f) distanceWalked += distance;
        if (distanceWalked >= .9f)
        {
            distanceWalked = 0f;
            audioSource.pitch = Random.Range(.9f, 1.1f);
            audioSource.PlayOneShot(step, .5f);
        }
        float remaining = manager.RemainingTime;
        if (timer != null)
            timer.color = remaining <= 20f ? Color.Lerp(new Color(1f,.3f,.25f), timerColor, .2f + .2f * Mathf.Sin(Time.time * 7f)) : timerColor;
        int seconds = Mathf.CeilToInt(remaining);
        if (seconds <= 20 && seconds > 0 && seconds != lastAlarm && (seconds == 20 || seconds <= 10))
        {
            lastAlarm = seconds;
            audioSource.pitch = 1f;
            audioSource.PlayOneShot(alarm, .65f);
        }
    }

    void SetPause(bool value)
    {
        paused = value;
        Time.timeScale = value ? 0f : 1f;
        // El movimiento mantiene sus entradas para reanudar sin perder teclas pulsadas.
        if (interaction != null) interaction.enabled = !value && !finished;
        if (player != null) lastPosition = player.transform.position;
        distanceWalked = 0f;
        if (value) audioSource.Stop();
        title.text = pauseTitle;
        title.color = pauseTitleColor;
        details.text = pauseDetails;
        resume.SetActive(true);
        overlay.SetActive(value);
    }

    void UpdateLockers(bool active)
    {
        LockerInteraction nearest = null;
        if (active && interaction != null && interaction.isActiveAndEnabled)
            nearest = interaction.FindNearestLocker();
        for (int i = 0; i < lockers.Length; i++)
        {
            if (lockers[i] == null) continue;
            outlines[i].enabled = lockers[i] == nearest;
            marks[i].SetActive(lockers[i].IsSearched);
        }
    }

    LineRenderer CreateBorder(LockerInteraction locker)
    {
        var sr = locker.GetComponentInChildren<SpriteRenderer>();
        Bounds bounds = sr != null ? sr.bounds : new Bounds(locker.transform.position, Vector3.one);
        var go = new GameObject("Resaltado de casillero");
        go.transform.SetParent(locker.transform, false);
        var line = go.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.loop = true;
        line.positionCount = 4;
        line.widthMultiplier = .045f;
        line.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
        line.startColor = line.endColor = Color.white;
        Vector3 a = locker.transform.InverseTransformPoint(bounds.min);
        Vector3 b = locker.transform.InverseTransformPoint(bounds.max);
        line.SetPositions(new[] { new Vector3(a.x,a.y,0), new Vector3(a.x,b.y,0), new Vector3(b.x,b.y,0), new Vector3(b.x,a.y,0) });
        line.sortingOrder = sr != null ? sr.sortingOrder + 3 : 3;
        if (sr != null) line.sortingLayerID = sr.sortingLayerID;
        line.enabled = false;
        return line;
    }

    GameObject CreateMark(LockerInteraction locker)
    {
        var go = new GameObject("Revisado");
        go.transform.SetParent(locker.transform, false);
        var sr = locker.GetComponentInChildren<SpriteRenderer>();
        go.transform.position = sr != null ? sr.bounds.center + Vector3.up * sr.bounds.extents.y * .5f : locker.transform.position;
        var text = go.AddComponent<TextMeshPro>();
        text.font = font != null ? font : TMP_Settings.defaultFontAsset;
        text.text = "OK";
        text.fontSize = 3f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(.55f,1f,.72f);
        text.outlineWidth = .2f;
        text.rectTransform.sizeDelta = new Vector2(1f,.5f);
        text.renderer.sortingOrder = sr != null ? sr.sortingOrder + 4 : 4;
        if (sr != null) text.renderer.sortingLayerID = sr.sortingLayerID;
        go.SetActive(false);
        return go;
    }

    void BuildMenu()
    {
        if (FindFirstObjectByType<EventSystem>() == null)
            new GameObject("Eventos del menú", typeof(EventSystem), typeof(InputSystemUIInputModule));
        var events = FindFirstObjectByType<EventSystem>();
        var legacy = events.GetComponent<StandaloneInputModule>();
        if (legacy != null) legacy.enabled = false;
        if (events.GetComponent<InputSystemUIInputModule>() == null) events.gameObject.AddComponent<InputSystemUIInputModule>();
        if (TryBuildSavedMenu()) return;
        var root = new GameObject("Menú de partida", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.transform.SetParent(transform, false);
        root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        root.GetComponent<Canvas>().sortingOrder = 100;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920,1080);
        scaler.matchWidthOrHeight = .5f;
        overlay = Box("Fondo", root.transform, Vector2.zero, Vector2.zero, new Color(0,0,0,.65f));
        var bg = overlay.GetComponent<RectTransform>();
        bg.anchorMin = Vector2.zero; bg.anchorMax = Vector2.one; bg.sizeDelta = Vector2.zero;
        var card = Box("Panel", overlay.transform, Vector2.zero, new Vector2(900,780), new Color(.09f,.14f,.2f,1));
        title = Label("Título",card.transform,new Vector2(0,290),new Vector2(820,80),"PAUSA",52);
        details = Label("Detalles",card.transform,new Vector2(0,110),new Vector2(800,250),"Tu USB puede esperar un momento.\n\nWASD / flechas: moverte\nShift: correr   ·   E: revisar\nEsc: continuar",34);
        resume = Button("Continuar", card.transform, new Vector2(-200,-100), () => SetPause(false));
        Button("Reiniciar", card.transform,new Vector2(200,-100), () => manager.RestartLevel());
        Label("Volumen",card.transform,new Vector2(0,-205),new Vector2(700,50),"VOLUMEN",30);
        var track = Box("Control de volumen",card.transform,new Vector2(0,-270),new Vector2(600,36),new Color(.3f,.4f,.5f));
        var slider = track.AddComponent<Slider>();
        var handle = Box("Deslizador",track.transform,Vector2.zero,new Vector2(30,58),new Color(1f,.84f,.43f));
        slider.handleRect = handle.GetComponent<RectTransform>();
        slider.targetGraphic = handle.GetComponent<Image>();
        slider.minValue = 0f; slider.maxValue = 1f; slider.value = AudioListener.volume;
        slider.onValueChanged.AddListener(v => { AudioListener.volume = v; PlayerPrefs.SetFloat(VolumeKey,v); });
        Label("Ayuda",card.transform,new Vector2(0,-345),new Vector2(800,40),"R para reintentar al terminar",26);
        overlay.SetActive(false);
    }

    // La configuración referencia el prefab original, también en el juego compilado.
    bool TryBuildSavedMenu()
    {
        var settings = Resources.Load<CampusMenuSettings>("CampusMenuSettings");
        if (settings == null || settings.menuPrefab == null) return false;
        var root = Instantiate(settings.menuPrefab, transform, false);
        root.name = settings.menuPrefab.name;
        var background = root.transform.Find("Fondo");
        var panel = background != null ? background.Find("Panel") : null;
        var titleNode = panel != null ? panel.Find("Título") : null;
        var detailsNode = panel != null ? panel.Find("Detalles") : null;
        var continueNode = panel != null ? panel.Find("Continuar") : null;
        var restartNode = panel != null ? panel.Find("Reiniciar") : null;
        var savedTitle = titleNode != null ? titleNode.GetComponent<TMP_Text>() : null;
        var savedDetails = detailsNode != null ? detailsNode.GetComponent<TMP_Text>() : null;
        var continueButton = continueNode != null ? continueNode.GetComponent<UnityEngine.UI.Button>() : null;
        var restartButton = restartNode != null ? restartNode.GetComponent<UnityEngine.UI.Button>() : null;
        if (savedTitle == null || savedDetails == null || continueButton == null || restartButton == null)
        {
            Debug.LogError("El prefab del menú necesita Fondo/Panel con Título, Detalles, Continuar y Reiniciar. Se usará el menú de respaldo.", this);
            root.SetActive(false);
            Destroy(root);
            return false;
        }
        overlay = background.gameObject;
        title = savedTitle;
        details = savedDetails;
        resume = continueNode.gameObject;
        continueButton.onClick.RemoveAllListeners();
        continueButton.onClick.AddListener(() => SetPause(false));
        restartButton.onClick.RemoveAllListeners();
        restartButton.onClick.AddListener(() => manager.RestartLevel());
        foreach (var slider in root.GetComponentsInChildren<Slider>(true))
        {
            slider.SetValueWithoutNotify(AudioListener.volume);
            slider.onValueChanged.RemoveAllListeners();
            slider.onValueChanged.AddListener(v => { AudioListener.volume = v; PlayerPrefs.SetFloat(VolumeKey, v); });
        }
        root.SetActive(true);
        overlay.SetActive(false);
        return true;
    }

    GameObject Box(string name, Transform parent, Vector2 position, Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform),typeof(Image));
        go.transform.SetParent(parent,false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f,.5f);
        rect.anchoredPosition = position; rect.sizeDelta = size;
        go.GetComponent<Image>().color = color;
        return go;
    }
    TMP_Text Label(string name,Transform parent,Vector2 pos,Vector2 size,string content,float fontSize)
    {
        var go = new GameObject(name,typeof(RectTransform));
        go.transform.SetParent(parent,false);
        var text = go.AddComponent<TextMeshProUGUI>();
        text.rectTransform.anchoredPosition = pos; text.rectTransform.sizeDelta = size;
        if (font != null) text.font = font;
        text.text = content; text.fontSize = fontSize; text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(.96f,.96f,.9f); text.raycastTarget = false;
        return text;
    }
    GameObject Button(string caption,Transform parent,Vector2 pos,UnityEngine.Events.UnityAction action)
    {
        var go = Box(caption,parent,pos,new Vector2(340,90),new Color(.18f,.34f,.42f));
        var button = go.AddComponent<UnityEngine.UI.Button>();
        button.targetGraphic = go.GetComponent<Image>(); button.onClick.AddListener(action);
        Label("Texto",go.transform,Vector2.zero,new Vector2(320,80),caption,36);
        return go;
    }
    static string Format(float seconds) { int n = Mathf.CeilToInt(seconds); return $"{n/60:00}:{n%60:00}"; }
    static AudioClip MakeTone(string name,float duration,float frequency,bool noise)
    {
        const int rate = 22050;
        var samples = new float[Mathf.CeilToInt(rate * duration)];
        // Generador local: crear sonidos no altera la selección aleatoria de casilleros.
        var random = new System.Random(73);
        for (int i=0;i<samples.Length;i++)
        {
            float t = i/(float)rate;
            float envelope = Mathf.Sin(Mathf.PI*i/samples.Length) * Mathf.Exp(-t*(noise?35f:8f));
            samples[i] = envelope * (Mathf.Sin(2f*Mathf.PI*frequency*t)*.5f + (noise?(float)(random.NextDouble()-.5)*.3f:0f));
        }
        var clip = AudioClip.Create(name,samples.Length,1,rate,false); clip.SetData(samples,0); return clip;
    }
    void OnDestroy()
    {
        if (paused) Time.timeScale = 1f;
        foreach (var line in outlines) if (line != null) Destroy(line.sharedMaterial);
        if (step != null) Destroy(step);
        if (alarm != null) Destroy(alarm);
    }
}
