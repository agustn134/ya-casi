using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Nivel 2: búsqueda de tres libros, ruido, bibliotecaria y salida contrarreloj.</summary>
public sealed class LibraryLevelManager : MonoBehaviour
{
    public const string SceneName = "Nivel02_MaratonEnLaBiblioteca";
    public PlayerMovement player;
    public LibraryLibrarian librarian;
    public LibraryShelf[] shelves;
    public Transform exit;
    public LibraryExitZone exitZone;
    public Sprite[] bookIcons;
    [SerializeField] AudioClip levelMusicOverride;
    [SerializeField] AudioClip timeoutMusicOverride;
    public float roundSeconds = 300f;
    public float wrongShelfPenalty = 10f;
    public string[] bookNames = { "Algoritmos", "Bases de datos", "Redes" };
    public PlayerMovement Player => player;
    public bool IsPlaying => !ended && !paused;
    public bool PlayerIsRunning => IsPlaying && !frozen && player != null && player.IsRunning && body != null && body.linearVelocity.sqrMagnitude > .1f;
    public int CollectedCount { get; private set; }
    public float RemainingTime => remaining;
    public bool IsCompleted { get; private set; }
    public bool IsFailed => ended && !IsCompleted;
    public int Reprimands { get; private set; }
    public bool AtExit => exitZone != null ? exitZone.Contains(player) : exit != null && Mathf.Abs(player.transform.position.x - exit.position.x) <= 1.8f && Mathf.Abs(player.transform.position.y - exit.position.y) <= 1.3f;
    readonly bool[] collected = new bool[3];
    readonly List<Image> inventory = new List<Image>();
    TMP_Text clock, objective, prompt, feedback, noise, catalog;
    GameObject hud, overlay;
    Rigidbody2D body;
    LibraryCamera libraryCamera;
    TMP_FontAsset libraryFont;
    AudioSource music, effects;
    AudioClip collectSound, errorSound, shushSound, stepSound;
    LibraryShelf nearest;
    LineRenderer ring;
    float remaining, elapsed, feedbackUntil, nextInteraction, stepDistance;
    Vector2 previousPosition;
    int score;
    bool paused, ended, frozen;
    Color gold = new Color(1f, .84f, .42f);

    void Start()
    {
        Time.timeScale = 1;
        remaining = roundSeconds;
        body = player.GetComponent<Rigidbody2D>();
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        libraryCamera = FindFirstObjectByType<LibraryCamera>();
        var font = Resources.Load<Font>("Redaction35-Regular");
        if (font != null) libraryFont = TMP_FontAsset.CreateFontAsset(font);
        if (exitZone != null && exitZone.sign != null && libraryFont != null)
            exitZone.sign.font = libraryFont;
        previousPosition = player.transform.position;
        PlayerPrefs.SetInt(U2Presentation.SaveKey, 2);
        PlayerPrefs.Save();
        AssignBooks();
        BuildHud();
        BuildAudio();
        BuildRing();
        ShowTutorial();
    }
    void AssignBooks()
    {
        // Barajado sin reemplazo: tres libros distintos en tres estantes de todo el mapa.
        var available = new List<LibraryShelf>(shelves);
        foreach (var shelf in shelves) { shelf.bookIndex = -1; shelf.searched = false; }
        for (int book = 0; book < collected.Length && available.Count > 0; book++)
        {
            int chosen = Random.Range(0, available.Count);
            available[chosen].bookIndex = book;
            available.RemoveAt(chosen);
        }
    }
    void Update()
    {
        var keyboard = Keyboard.current;
        var gamepad = Gamepad.current;
        if ((keyboard != null && keyboard.escapeKey.wasPressedThisFrame) || (gamepad != null && gamepad.startButton.wasPressedThisFrame)) TogglePause();
        if (ended)
        {
            if (keyboard != null && keyboard.rKey.wasPressedThisFrame) Restart();
            return;
        }
        if (music != null)
        {
            music.volume = U2Presentation.MusicVolume * .55f;
            music.pitch = Mathf.MoveTowards(music.pitch, librarian.Investigating ? 1.12f : 1f, Time.unscaledDeltaTime);
        }
        if (!IsPlaying) return;
        remaining = Mathf.Max(0, remaining - Time.deltaTime);
        elapsed += Time.deltaTime;
        if (remaining <= 0) { Finish(false); return; }
        if (TryExit(false)) return;
        if (Time.time >= feedbackUntil) feedback.text = "";
        feedback.transform.parent.gameObject.SetActive(!string.IsNullOrEmpty(feedback.text));
        nearest = null;
        float best = 1.35f;
        foreach (var shelf in shelves)
        {
            float distance = Vector2.Distance(player.transform.position, shelf.Position);
            if (distance < best) { best = distance; nearest = shelf; }
        }
        foreach (var shelf in shelves) shelf.Highlight(!frozen && shelf == nearest);
        bool nearExit = AtExit;
        prompt.text = frozen ? "¡Shhh! Espera un momento…" : nearExit ? (CollectedCount == 3 ? "SALIDA ABIERTA · cruza la zona verde" : "Salida cerrada · faltan " + (3 - CollectedCount) + " libros") : nearest != null ? (nearest.searched ? "E · Volver a revisar " + nearest.section + " (−10 s si está vacío)" : "E · Revisar " + nearest.section) : "WASD / flechas · Shift: correr · E: revisar · Esc: pausa";
        if ((keyboard != null && keyboard.eKey.wasPressedThisFrame) || (gamepad != null && gamepad.buttonSouth.wasPressedThisFrame)) Interact();
        int displaySeconds = Mathf.CeilToInt(remaining);
        clock.text = "CIERRE  " + (displaySeconds / 60).ToString("00") + ":" + (displaySeconds % 60).ToString("00");
        clock.color = remaining < 30 ? new Color(1, .4f, .35f) : gold;
        noise.text = frozen ? "¡SHHH! · 3 s sin moverte" : librarian.Investigating ? "TE ESCUCHARON · el guardia se acerca" : PlayerIsRunning ? "RUIDO ALTO · radio de 5 m" : "EN SILENCIO · camina cerca del guardia";
        noise.color = PlayerIsRunning || librarian.Investigating ? new Color(1, .55f, .4f) : new Color(.65f, .88f, .8f);
        ring.enabled = PlayerIsRunning;
        ring.transform.position = player.transform.position;
        var position = (Vector2)player.transform.position;
        stepDistance += Vector2.Distance(position, previousPosition);
        previousPosition = position;
        if (stepDistance > .9f && !frozen)
        {
            stepDistance = 0;
            effects.PlayOneShot(stepSound, U2Presentation.EffectsVolume * .18f);
        }
    }
    public void Interact()
    {
        if (!IsPlaying || frozen || Time.time < nextInteraction) return;
        nextInteraction = Time.time + .35f;
        if (AtExit) { TryExit(true); return; }
        // Recalcula en el momento de pulsar E; no depende del orden de Update del input.
        nearest = null;
        float distance = 1.35f;
        foreach (var shelf in shelves)
        {
            float candidate = Vector2.Distance(player.transform.position, shelf.Position);
            if (candidate < distance) { distance = candidate; nearest = shelf; }
        }
        if (nearest != null) { Review(nearest); return; }
    }
    public bool TryExit(bool showHint)
    {
        if (!IsPlaying || frozen || !AtExit) return false;
        if (CollectedCount == 3)
        {
            if (exitZone != null && !exitZone.DoorIsOpen) return false;
            Finish(true);
            return true;
        }
        if (showHint) Notify("Necesitas los tres libros antes de salir. Te faltan " + (3 - CollectedCount) + ".");
        return false;
    }
    public void Review(LibraryShelf shelf)
    {
        if (!IsPlaying || frozen || shelf == null) return;
        shelf.searched = true;
        if (shelf.bookIndex >= 0 && !collected[shelf.bookIndex])
        {
            int index = shelf.bookIndex;
            collected[index] = true; CollectedCount++; score += 300;
            inventory[index].color = Color.white;
            Notify("¡" + bookNames[index] + " encontrado! " + CollectedCount + "/3" + (CollectedCount == 3 ? " · La salida está abierta." : ""));
            effects.PlayOneShot(collectSound, U2Presentation.EffectsVolume);
        }
        else
        {
            remaining = Mathf.Max(0, remaining - wrongShelfPenalty);
            Notify("No está el libro que necesitas · −" + wrongShelfPenalty + " segundos, aunque ya hayas revisado este estante.");
            effects.PlayOneShot(errorSound, U2Presentation.EffectsVolume * .6f);
            if (remaining <= 0) Finish(false);
        }
        objective.text = "BIBLIOTECA · " + CollectedCount + "/3 LIBROS" + (CollectedCount == 3 ? " · VUELVE A LA SALIDA ↓" : "");
        catalog.text = CatalogText();
    }
    string CatalogText()
    {
        string text = "CATÁLOGO DE PRÉSTAMO\n";
        for (int i = 0; i < 3; i++) text += (collected[i] ? "[OK] " : "[ ] ") + bookNames[i] + (i < 2 ? "\n" : "");
        return text;
    }
    public void Notify(string message) { feedback.text = message; feedback.transform.parent.gameObject.SetActive(true); feedbackUntil = Time.time + 4f; }
    public void Reprimand()
    {
        if (!IsPlaying || frozen) return;
        Reprimands++;
        if (libraryCamera != null) libraryCamera.Shake();
        StartCoroutine(Freeze());
    }
    IEnumerator Freeze()
    {
        frozen = true; player.enabled = false; body.linearVelocity = Vector2.zero;
        Notify("¡Shhh! Aquí se viene a leer. Detenido durante 3 segundos.");
        effects.PlayOneShot(shushSound, U2Presentation.EffectsVolume);
        yield return new WaitForSeconds(3);
        frozen = false;
        if (!ended) player.enabled = true;
    }
    public void TogglePause()
    {
        if (ended) return;
        if (paused) { Resume(); return; }
        paused = true; Time.timeScale = 0;
        var card = Overlay("PAUSA", "Maratón en la Biblioteca\nEncuentra tres libros y vuelve a la salida.");
        LibraryButton(card.transform, "Continuar", new Vector2(0, -40), Resume);
        LibraryButton(card.transform, "Reiniciar", new Vector2(0, -140), Restart);
        LibraryButton(card.transform, "Opciones", new Vector2(0, -240), () => U2Presentation.Options(overlay.transform, null));
        LibraryButton(card.transform, "Menú principal", new Vector2(0, -340), U2Presentation.ReturnToMenu);
    }
    void ShowTutorial()
    {
        paused = true; Time.timeScale = 0;
        var card = Overlay("MARATÓN EN LA BIBLIOTECA",
            "Estudiante: ¡Ya recuperé la USB! Pero aún me faltan las fuentes para la entrega.\n\n" +
            "Guardia: Quedan cinco minutos. Busca Algoritmos, Bases de datos y Redes. Vuelve por esta puerta cuando tengas los tres. Y camina, por favor.\n\n" +
            "WASD o flechas para moverte · E para revisar · Shift para correr\n" +
            "Una búsqueda sin libro cuesta 10 segundos.");
        LibraryButton(card.transform, "Entrar en silencio", new Vector2(0, -320), Resume);
    }
    GameObject Overlay(string title, string description)
    {
        if (overlay != null) Destroy(overlay);
        overlay = U2Presentation.NewCanvas("Biblioteca · diálogo", 120);
        var card = U2Presentation.Card(overlay.transform, "Panel");
        U2Presentation.Text(card.transform, "Título", title, new Vector2(0, 315), new Vector2(1180, 100), 46, gold);
        U2Presentation.Text(card.transform, "Descripción", description, new Vector2(0, 70), new Vector2(1140, 390), 31, Color.white);
        foreach (var text in card.GetComponentsInChildren<TMP_Text>())
            ApplyLibraryFont(text);
        return card;
    }

    Button LibraryButton(Transform parent, string label, Vector2 position, UnityEngine.Events.UnityAction action)
    {
        var button = U2Presentation.Button(parent, label, position, action);
        ApplyLibraryFont(button.GetComponentInChildren<TMP_Text>());
        return button;
    }

    void ApplyLibraryFont(TMP_Text text)
    {
        if (libraryFont != null && text != null) text.font = libraryFont;
    }

    public void Resume()
    {
        if (overlay != null) Destroy(overlay);
        paused = false; Time.timeScale = 1;
        previousPosition = player.transform.position;
    }
    public void Restart() { Time.timeScale = 1; SceneManager.LoadScene(SceneName); }
    void Finish(bool success)
    {
        if (ended) return;
        ended = true; IsCompleted = success; paused = false; Time.timeScale = 0;
        player.enabled = false; body.linearVelocity = Vector2.zero; ring.enabled = false;
        if (music != null) music.Stop();
        if (success)
        {
            score += Mathf.CeilToInt(remaining) * 10;
            PlayerPrefs.SetInt("YaCasi.Library.Completed", 1);
            PlayerPrefs.SetInt("YaCasi.Library.Best", Mathf.Max(score, PlayerPrefs.GetInt("YaCasi.Library.Best", 0)));
            PlayerPrefs.Save();
        }
        else PlayGameOverMusic();
        string stats = $"\n\nLibros: {CollectedCount}/3 · Regaños: {Reprimands}\nTiempo de juego: {Mathf.CeilToInt(elapsed)} s"
            + $"\nPuntaje: {score} · Récord: {PlayerPrefs.GetInt("YaCasi.Library.Best", 0)}";
        if (success)
        {
            U2Presentation.SaveDeliveryProgress();
            if (overlay != null) Destroy(overlay);
            overlay = U2Presentation.NewCanvas("Resultado nivel 2", 120);
            var victory = U2Presentation.Card(overlay.transform, "Resultado");
            U2Presentation.Text(victory.transform, "Título", "¡LECTURAS COMPLETAS!", new Vector2(0, 315), new Vector2(1180, 100), 46, gold);
            U2Presentation.Text(victory.transform, "Resultado", "Saliste con los tres libros para terminar tu trabajo." + stats,
                new Vector2(0, 83), new Vector2(1140, 290), 31, Color.white);
            U2Presentation.Button(victory.transform, "Nivel 3 · La ruta de entrega", new Vector2(0, -160), U2Presentation.PlayDelivery);
            U2Presentation.Button(victory.transform, "Volver a jugar", new Vector2(0, -255), Restart);
            U2Presentation.Button(victory.transform, "Menú principal", new Vector2(0, -350), U2Presentation.ReturnToMenu);
            return;
        }

        var card = Overlay("LA BIBLIOTECA CERRÓ", "Se acabó el tiempo. Camina cerca del guardia\ny evita volver a los estantes que ya revisaste." + stats);
        LibraryButton(card.transform, "Volver a jugar", new Vector2(0, -180), Restart);
        LibraryButton(card.transform, "Menú principal", new Vector2(0, -280), U2Presentation.ReturnToMenu);
    }
    void PlayGameOverMusic()
    {
        var config = Resources.Load<U2GameConfig>("U2GameConfig");
        AudioClip clip = timeoutMusicOverride != null ? timeoutMusicOverride : config?.gameOverSound;
        if (clip == null)
        {
            Debug.LogError("Biblioteca: falta la música de derrota.", this);
            return;
        }
        music.playOnAwake = false;
        music.loop = false;
        music.pitch = 1f;
        music.spatialBlend = 0;
        music.mute = false;
        music.ignoreListenerPause = true;
        music.volume = U2Presentation.MusicVolume;
        music.clip = clip;
        music.Play();
        Debug.Log("LIBRARY_GAME_OVER_MUSIC: " + music.clip.name, this);
    }
    void BuildHud()
    {
        hud = U2Presentation.NewCanvas("HUD Biblioteca", 20);
        objective = PanelText("Objetivo", "BIBLIOTECA · 0/3 LIBROS", new Vector2(0, 1), new Vector2(30, -40), new Vector2(780, 66), 32);
        clock = PanelText("Reloj", "", new Vector2(1, 1), new Vector2(-30, -40), new Vector2(360, 66), 36);
        catalog = PanelText("Catálogo", CatalogText(), new Vector2(0, 1), new Vector2(30, -170), new Vector2(450, 180), 26);
        noise = PanelText("Ruido", "EN SILENCIO", new Vector2(.5f, 1), new Vector2(0, -125), new Vector2(850, 55), 25);
        prompt = PanelText("Interacción", "", new Vector2(.5f, 0), new Vector2(0, 38), new Vector2(1250, 66), 28);
        feedback = PanelText("Avisos", "", new Vector2(.5f, 0), new Vector2(0, 115), new Vector2(1350, 66), 28);
        for (int i = 0; i < 3; i++)
        {
            var icon = U2Presentation.Image(hud.transform, "Libro " + bookNames[i], new Color(.3f, .3f, .3f));
            icon.sprite = bookIcons[i]; icon.preserveAspect = true;
            var rect = icon.rectTransform; rect.anchorMin = rect.anchorMax = Vector2.one; rect.anchoredPosition = new Vector2(-280 + i * 90, -150); rect.sizeDelta = new Vector2(65, 70);
            inventory.Add(icon);
        }
        var pause = LibraryButton(hud.transform, "Pausa", Vector2.zero, TogglePause);
        var pauseRect = pause.GetComponent<RectTransform>(); pauseRect.anchorMin = pauseRect.anchorMax = new Vector2(1, 0); pauseRect.anchoredPosition = new Vector2(-120, 210); pauseRect.sizeDelta = new Vector2(200, 65);
        if (Application.isMobilePlatform || SystemInfo.deviceType == DeviceType.Handheld) TouchControls();
    }
    TMP_Text PanelText(string name, string value, Vector2 anchor, Vector2 position, Vector2 size, float fontSize)
    {
        var background = U2Presentation.Image(hud.transform, name + " fondo", new Color(.04f, .09f, .12f, .87f));
        var rect = background.rectTransform; rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = new Vector2(anchor.x, .5f); rect.anchoredPosition = position; rect.sizeDelta = size;
        var text = U2Presentation.Text(background.transform, name, value, Vector2.zero, size - new Vector2(20, 8), fontSize, Color.white);
        ApplyLibraryFont(text);
        return text;
    }
    void TouchControls()
    {
        Vector2[] directions = { Vector2.left, Vector2.right, Vector2.up, Vector2.down };
        Vector2[] places = { new Vector2(-820,-340), new Vector2(-580,-340), new Vector2(-700,-230), new Vector2(-700,-450) };
        string[] labels = { "←", "→", "↑", "↓" };
        for (int i = 0; i < 4; i++)
        {
            var button = LibraryButton(hud.transform, labels[i], places[i], () => {});
            button.GetComponent<RectTransform>().sizeDelta = new Vector2(100, 90);
            button.gameObject.AddComponent<TouchGameControls>().direction = directions[i];
        }
        var review = LibraryButton(hud.transform, "Revisar / salir", new Vector2(680, -300), Interact);
        review.GetComponent<RectTransform>().sizeDelta = new Vector2(330, 90);
        var sprint = LibraryButton(hud.transform, "Correr", new Vector2(680,-410), () => {});
        sprint.GetComponent<RectTransform>().sizeDelta = new Vector2(330,90);
        sprint.gameObject.AddComponent<TouchGameControls>().sprint = true;
    }
    void BuildRing()
    {
        var go = new GameObject("Radio de ruido al correr");
        go.transform.SetParent(transform);
        ring = go.AddComponent<LineRenderer>(); ring.useWorldSpace = false; ring.loop = true;
        ring.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
        ring.startColor = ring.endColor = new Color(1f,.6f,.3f,.45f);
        ring.widthMultiplier = .035f; ring.sortingOrder = 50; ring.positionCount = 64;
        for (int i = 0; i < 64; i++) { float a = i * Mathf.PI * 2 / 64; ring.SetPosition(i, new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0) * librarian.hearingRadius); }
        ring.enabled = false;
    }
    void BuildAudio()
    {
        effects = gameObject.AddComponent<AudioSource>(); effects.playOnAwake = false;
        music = gameObject.AddComponent<AudioSource>(); music.loop = true; music.playOnAwake = false;
        var config = Resources.Load<U2GameConfig>("U2GameConfig");
        AudioClip clip = levelMusicOverride != null ? levelMusicOverride : config?.soundtrack;
        if (clip != null) { music.clip = clip; music.volume = U2Presentation.MusicVolume * .55f; music.Play(); }
        collectSound = Tone("Libro recogido", 660, .16f, false);
        errorSound = Tone("Libro equivocado", 170, .2f, false);
        shushSound = Tone("Shhh", 0, .65f, true);
        stepSound = Tone("Paso suave", 95, .065f, true);
    }
    AudioClip Tone(string name, float frequency, float seconds, bool noiseSound)
    {
        const int rate = 22050;
        var samples = new float[Mathf.CeilToInt(rate * seconds)];
        var random = new System.Random(71);
        float smooth = 0;
        for (int i = 0; i < samples.Length; i++)
        {
            float t = i / (float)rate;
            smooth = Mathf.Lerp(smooth, (float)random.NextDouble() * 2 - 1, .3f);
            samples[i] = (noiseSound ? smooth : Mathf.Sin(t * frequency * Mathf.PI * 2)) * .35f * Mathf.Sin(Mathf.PI * i / samples.Length);
        }
        var clip = AudioClip.Create(name, samples.Length, 1, rate, false); clip.SetData(samples, 0); return clip;
    }
    void OnDestroy()
    {
        Time.timeScale = 1;
        TouchGameControls.Direction = Vector2.zero; TouchGameControls.Sprint = false;
        foreach (var clip in new[] { collectSound, errorSound, shushSound, stepSound }) if (clip != null) Destroy(clip);
        if (ring != null && ring.sharedMaterial != null) Destroy(ring.sharedMaterial);
        if (libraryFont != null) Destroy(libraryFont);
    }
}
