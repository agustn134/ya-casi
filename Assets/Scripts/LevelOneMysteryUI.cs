using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>Libreta consultable y candado de tres símbolos del misterio de la USB.</summary>
public sealed class LevelOneMysteryUI : MonoBehaviour
{
    enum Page { None, Intro, Notebook, Lock }
    static readonly Color Gold = new Color(1f, .84f, .42f);
    static readonly Color Muted = new Color(.72f, .82f, .85f);
    LevelOneMystery controller;
    Level01Manager manager;
    GameObject overlay;
    GameObject previousSelection;
    GameObject card;
    TMP_Text slotsText, lockFeedback;
    Button confirmButton;
    readonly List<int> attempt = new List<int>(3);
    Page page;
    bool returnToLock;
    float previousTimeScale = 1f;
    int openedFrame, closedFrame = -1;
    Action introClosed;

    public bool IsOpen => overlay != null;
    public bool IsNotebook => page == Page.Notebook;
    public bool BlocksGameplayInput => IsOpen || Time.frameCount <= closedFrame;

    public void Configure(LevelOneMystery mystery)
    {
        controller = mystery;
        manager = mystery != null ? mystery.GetComponent<Level01Manager>() : null;
        if (manager == null) manager = FindFirstObjectByType<Level01Manager>();
    }

    public void ShowIntro(Action onClosed)
    {
        introClosed = onClosed;
        OpenPage(Page.Intro, "El casillero equivocado");
        Label("Título", "El casillero equivocado", 330, 70, 43, Gold);
        Label("Mensaje", "Mensaje para Galindo", 254, 44, 27, Muted);
        Label("Historia", "Movieron tus cosas durante la limpieza.\nEs de noche y el conserje está cerrando la universidad.\nTu USB quedó en uno de los dos pisos.", 151, 150, 30, Color.white);
        Label("Plan", "1. Investiga el salón 101 y sube por la escalera en U.\n2. Habla con el compañero y busca el aviso en el salón 203.\n3. Abre el casillero y vuelve a la salida de planta baja.\nSi te ve el conserje, entra a un salón oscuro y deja de correr.", -32, 166, 26, Color.white);
        Label("Controles", "E / botón sur: investigar o hablar\nN / Libreta: consultar pistas · Q / LB: pedir paso\nEl reloj se detiene al leer la libreta o resolver el candado.", -213, 116, 25, Muted);
        var start = ActionButton("Buscar las pistas", new Vector2(0, -337), new Vector2(520, 64), Close);
        FinishPage(start);
    }

    public void ShowNotebook()
    {
        if (controller == null || controller.Round == null) return;
        bool fromLock = page == Page.Lock;
        if (!IsOpen) returnToLock = false;
        else if (fromLock) returnToLock = true;
        OpenPage(Page.Notebook, "Libreta de Galindo");
        Label("Título", "Libreta de Galindo", 340, 65, 42, Gold);
        int count = 0;
        if (controller.Discovered != null)
            foreach (bool found in controller.Discovered) if (found) count++;
        Label("Progreso", $"Pistas encontradas: {count}/3", 282, 42, 27, Muted);
        string[] sources = { "La nota · Salón 101, planta baja", "El compañero · Segundo piso", "El aviso · Salón 203, segundo piso" };
        for (int i = 0; i < 3; i++)
        {
            bool known = controller.Discovered != null && i < controller.Discovered.Length && controller.Discovered[i];
            float y = 168 - i * 131;
            var block = U2Presentation.Image(card.transform, "Pista " + (i + 1), new Color(.09f, .16f, .19f, 1));
            block.raycastTarget = false;
            block.rectTransform.anchoredPosition = new Vector2(0, y);
            block.rectTransform.sizeDelta = new Vector2(1020, 117);
            U2Presentation.Text(block.transform, "Origen", (i + 1) + ". " + sources[i], new Vector2(0, 35), new Vector2(980, 36), 25, known ? Gold : Muted);
            string value = known ? controller.Round.Clues[i].Replace("fila Azul","sección Azul (A), planta baja").Replace("fila Verde","sección Verde (V), planta alta") : "??? · Todavía falta investigar esta pista.";
            U2Presentation.Text(block.transform, "Anotación", value, new Vector2(0, -17), new Vector2(980, 72), 25, known ? Color.white : Muted);
        }
        Label("Cómo deducir", "Azul (A): planta baja · Verde (V): planta alta.\nBusca las placas A-01…A-17 o V-01…V-17 y los letreros de sección.\nCombina sección, paridad e intervalo. La nota guarda el candado.", -253, 81, 24, Muted);
        var close = ActionButton(returnToLock ? "Volver al candado" : "Cerrar libreta", new Vector2(0, -350), new Vector2(520, 62), BackFromNotebook);
        FinishPage(close);
    }

    public void ShowLock()
    {
        if (controller == null || controller.Round == null || !controller.CanUseLock) return;
        if (page != Page.Notebook) attempt.Clear();
        returnToLock = false;
        OpenPage(Page.Lock, "Candado de la USB");
        Label("Título", "Candado de la USB", 335, 66, 42, Gold);
        Label("Instrucciones", "Introduce los tres símbolos en el orden de la nota.\nPuedes consultar tus pistas mientras lo resuelves.", 254, 85, 27, Color.white);
        slotsText = Label("Símbolos elegidos", "", 147, 78, 35, Gold);
        for (int i = 0; i < LockerMysteryRound.SymbolNames.Length; i++)
        {
            int symbol = i;
            ActionButton((i + 1) + ". " + LockerMysteryRound.SymbolNames[i],
                new Vector2(-369 + i * 246, 38), new Vector2(230, 74), () => AddSymbol(symbol));
        }
        lockFeedback = Label("Respuesta", "Elige tres símbolos. Confirmar comprueba el candado.", -54, 72, 25, Muted);
        ActionButton("Borrar último", new Vector2(-258, -157), new Vector2(470, 63), RemoveSymbol);
        confirmButton = ActionButton("Confirmar", new Vector2(258, -157), new Vector2(470, 63), Submit);
        ActionButton("Ver pistas", new Vector2(-258, -246), new Vector2(470, 63), ShowNotebook);
        var back = ActionButton("Volver al pasillo", new Vector2(258, -246), new Vector2(470, 63), Close);
        Label("Teclado", "1–4: elegir · Retroceso: borrar · Enter: confirmar\nEsc / Start: volver al pasillo", -339, 65, 23, Muted);
        RefreshAttempt();
        FinishPage(confirmButton);
    }

    void AddSymbol(int symbol)
    {
        if (page != Page.Lock || attempt.Count >= 3) return;
        attempt.Add(symbol);
        lockFeedback.text = "";
        RefreshAttempt();
    }

    void RemoveSymbol()
    {
        if (attempt.Count == 0) return;
        attempt.RemoveAt(attempt.Count - 1);
        lockFeedback.text = "";
        RefreshAttempt();
    }

    void RefreshAttempt()
    {
        if (slotsText == null) return;
        var names = new string[3];
        for (int i = 0; i < names.Length; i++)
            names[i] = i < attempt.Count ? LockerMysteryRound.SymbolNames[attempt[i]] : "—";
        slotsText.text = string.Join("   /   ", names);
        if (confirmButton != null) confirmButton.interactable = attempt.Count == 3;
    }

    void Submit()
    {
        if (page != Page.Lock || attempt.Count != 3 || controller == null) return;
        int[] selection = attempt.ToArray();
        if (!controller.Round.MatchesCombination(selection))
        {
            lockFeedback.text = "Secuencia incorrecta · −4 s. Consulta la nota e inténtalo de nuevo.";
            lockFeedback.color = Gold;
            attempt.Clear();
            RefreshAttempt();
        }
        controller.SubmitCombination(selection);
    }

    void BackFromNotebook()
    {
        if (returnToLock && controller != null && controller.CanUseLock) ShowLock();
        else Close();
    }

    public void Close()
    {
        bool wasIntro = page == Page.Intro;
        if (overlay != null)
        {
            overlay.SetActive(false);
            Destroy(overlay);
            overlay = null;
            closedFrame = Time.frameCount;
            if (manager == null || (!manager.IsCompleted && !manager.IsFailed))
                Time.timeScale = previousTimeScale;
        }
        page = Page.None;
        returnToLock = false;
        slotsText = null;
        lockFeedback = null;
        confirmButton = null;
        if (EventSystem.current != null && previousSelection != null && previousSelection.activeInHierarchy)
            EventSystem.current.SetSelectedGameObject(previousSelection);
        if (wasIntro)
        {
            Action callback = introClosed;
            introClosed = null;
            callback?.Invoke();
        }
    }

    void OpenPage(Page next, string title)
    {
        if (overlay == null)
        {
            previousTimeScale = Time.timeScale;
            previousSelection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        }
        else
        {
            overlay.SetActive(false);
            Destroy(overlay);
        }
        Time.timeScale = 0;
        page = next;
        openedFrame = Time.frameCount;
        overlay = U2Presentation.NewCanvas(title, 120);
        card = U2Presentation.Card(overlay.transform, title);
        card.GetComponent<RectTransform>().sizeDelta = new Vector2(1120, 850);
    }

    TMP_Text Label(string name, string value, float y, float height, float size, Color color)
        => U2Presentation.Text(card.transform, name, value, new Vector2(0, y), new Vector2(1040, height), size, color);

    Button ActionButton(string caption, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action)
    {
        var button = U2Presentation.Button(card.transform, caption, position, action);
        button.GetComponent<RectTransform>().sizeDelta = size;
        var text = button.GetComponentInChildren<TMP_Text>();
        text.fontSize = 27;
        text.rectTransform.sizeDelta = size - new Vector2(12, 6);
        return button;
    }

    void FinishPage(Button selected)
    {
        LevelOneTypography.Apply(overlay);
        if (EventSystem.current != null && selected != null)
            EventSystem.current.SetSelectedGameObject(selected.gameObject);
    }

    void Update()
    {
        if (!IsOpen) return;
        if (manager != null && (manager.IsCompleted || manager.IsFailed)) { Close(); return; }
        if (Time.frameCount <= openedFrame) return;
        var keyboard = Keyboard.current;
        bool cancel = keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
        cancel |= Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame;
        if (cancel)
        {
            if (page == Page.Notebook) BackFromNotebook(); else Close();
            return;
        }
        if (page != Page.Lock || keyboard == null) return;
        if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame) AddSymbol(0);
        if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame) AddSymbol(1);
        if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame) AddSymbol(2);
        if (keyboard.digit4Key.wasPressedThisFrame || keyboard.numpad4Key.wasPressedThisFrame) AddSymbol(3);
        if (keyboard.backspaceKey.wasPressedThisFrame) RemoveSymbol();
        if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame) Submit();
    }

    void OnDisable()
    {
        // Un cambio de escena no debe dejar una pantalla modal huérfana.
        introClosed = null;
        Close();
    }
}
