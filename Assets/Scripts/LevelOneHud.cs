using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Información de la USB en los bordes; solo muestra las acciones cercanas.</summary>
public sealed class LevelOneHud : MonoBehaviour
{
    static readonly Color Panel = new Color(.035f, .075f, .09f, .90f);
    static readonly Color Gold = new Color(1f, .84f, .42f);
    TMP_Text goal, timer, inventory, context, notification;
    Image contextPanel, inventoryIcon;
    CanvasGroup notificationGroup;
    float notificationUntil;

    public void Initialize(Action pause, Action help)
    {
        var status = Card("Objetivo", new Vector2(0, 1), new Vector2(20, -20), new Vector2(340, 96));
        Text(status.transform, "Nivel", "El caos del casillero", new Vector2(0, 23), new Vector2(316, 34), 25);
        goal = Text(status.transform, "Objetivo actual", "Encuentra tu USB · 0/1", new Vector2(0, -19), new Vector2(316, 38), 25);

        var clock = Card("Reloj y controles", Vector2.one, new Vector2(-20, -20), new Vector2(286, 112));
        timer = Text(clock.transform, "Tiempo", "02:15", new Vector2(0, 28), new Vector2(260, 48), 35);
        SmallButton(clock.transform, "Pausa", new Vector2(-67, -29), pause);
        SmallButton(clock.transform, "Ayuda", new Vector2(67, -29), help);

        var bag = Card("Inventario USB", Vector2.zero, new Vector2(20, 20), new Vector2(300, 90));
        // Silueta de una memoria USB: conector metálico, dos contactos y carcasa.
        inventoryIcon = U2Presentation.Image(bag.transform, "Memoria USB", new Color(.36f, .53f, .60f));
        Place(inventoryIcon.rectTransform, new Vector2(-109, -7), new Vector2(32, 44));
        inventoryIcon.raycastTarget = false;
        var metal = U2Presentation.Image(inventoryIcon.transform, "Conector", new Color(.77f, .83f, .86f));
        Place(metal.rectTransform, new Vector2(0, 29), new Vector2(24, 18)); metal.raycastTarget = false;
        for (int i = 0; i < 2; i++)
        {
            var contact = U2Presentation.Image(metal.transform, "Contacto", Panel);
            Place(contact.rectTransform, new Vector2(-5 + i * 10, 1), new Vector2(4, 7)); contact.raycastTarget = false;
        }
        inventory = Text(bag.transform, "USB y calma", "USB pendiente\nCalma: 3/3", new Vector2(26, 0), new Vector2(230, 76), 25);

        contextPanel = Card("Acción cercana", new Vector2(.5f, 0), new Vector2(0, 20), new Vector2(480, 88));
        context = Text(contextPanel.transform, "Acción", "", Vector2.zero, new Vector2(450, 78), 25);
        contextPanel.gameObject.SetActive(false);

        var toast = Card("Aviso temporal", new Vector2(.5f, 1), new Vector2(0, -20), new Vector2(650, 68));
        notificationGroup = toast.gameObject.AddComponent<CanvasGroup>();
        notificationGroup.blocksRaycasts = false; notificationGroup.interactable = false; notificationGroup.alpha = 0;
        notification = Text(toast.transform, "Aviso", "", Vector2.zero, new Vector2(620, 56), 25);
        LevelOneTypography.Apply(gameObject);
    }

    public void Refresh(Level01Manager manager, int calm, LockerInteraction nearest, int lockerNumber)
    {
        int seconds = Mathf.CeilToInt(manager.RemainingTime);
        timer.text = $"{seconds / 60:00}:{seconds % 60:00}";
        timer.color = seconds <= 25 ? new Color(1, .48f, .40f) : Gold;
        goal.text = manager.HasUSB ? "Vuelve a la salida · 1/1" : "Encuentra tu USB · 0/1";
        inventory.text = (manager.HasUSB ? "USB recuperada" : "USB pendiente") + $"\nCalma: {calm}/3";
        inventoryIcon.color = manager.HasUSB ? new Color(.38f, .85f, .65f) : new Color(.36f, .53f, .60f);
        bool visible = manager.IsPlaying && nearest != null && !manager.HasUSB;
        contextPanel.gameObject.SetActive(visible);
        if (visible)
        {
            float penalty = Resources.Load<U2GameConfig>("U2GameConfig")?.repeatLockerPenalty ?? 5;
            context.text = nearest.IsSearched
                ? $"Casillero {lockerNumber} · Ya revisado\nRepetir cuesta {penalty:0} s"
                : $"Casillero {lockerNumber}\nE / botón sur: revisar";
            context.color = nearest.IsSearched ? Gold : Color.white;
        }
    }

    public void Notify(string message)
    {
        if (notification == null || string.IsNullOrWhiteSpace(message)) return;
        notification.text = message;
        notification.color = message.Contains("−") ? Gold : Color.white;
        notificationUntil = Time.unscaledTime + 2.8f;
        notificationGroup.alpha = 1;
    }

    public void UseTouchLayout()
    {
        var bag = inventory.transform.parent as RectTransform;
        if (bag != null) bag.anchoredPosition = new Vector2(20, 320);
    }

    void Update()
    {
        if (notificationGroup != null)
            notificationGroup.alpha = Mathf.Clamp01((notificationUntil - Time.unscaledTime) / .6f);
    }

    Image Card(string name, Vector2 anchor, Vector2 position, Vector2 size)
    {
        var image = U2Presentation.Image(transform, name, Panel);
        image.raycastTarget = false;
        var rect = image.rectTransform;
        rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
        Place(rect, position, size);
        return image;
    }

    static TMP_Text Text(Transform parent, string name, string value, Vector2 position, Vector2 size, float fontSize)
        => U2Presentation.Text(parent, name, value, position, size, fontSize, Color.white);

    static void SmallButton(Transform parent, string caption, Vector2 position, Action action)
    {
        var button = U2Presentation.Button(parent, caption, position, () => action());
        Place(button.GetComponent<RectTransform>(), position, new Vector2(122, 37));
        var label = button.GetComponentInChildren<TMP_Text>();
        label.rectTransform.sizeDelta = new Vector2(118, 35); label.fontSize = 23;
    }

    static void Place(RectTransform rect, Vector2 position, Vector2 size)
    { rect.anchoredPosition = position; rect.sizeDelta = size; }
}
