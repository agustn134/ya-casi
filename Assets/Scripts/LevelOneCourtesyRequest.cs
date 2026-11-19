using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Permite pedir paso con Q o LB; los alumnos cercanos pueden negarse.</summary>
public sealed class LevelOneCourtesyRequest : MonoBehaviour
{
    const float Radius = 2.5f;
    const int MaximumResponders = 2;
    PlayerMovement actor;
    Level01Manager manager;
    float readyAt;

    public event Action<string> Message;

    public void Configure(PlayerMovement player, Level01Manager levelManager)
    {
        actor = player;
        manager = levelManager;
    }

    void Update()
    {
        bool keyboard = Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame;
        bool controller = Gamepad.current != null && Gamepad.current.leftShoulder.wasPressedThisFrame;
        if (keyboard || controller)
            RequestPassage();
    }

    public int RequestPassage()
    {
        if (actor == null || !actor.isActiveAndEnabled || manager == null || !manager.IsPlaying)
            return 0;
        if (Time.time < readyAt)
        {
            Message?.Invoke("Espera un momento para pedir paso.");
            return 0;
        }
        readyAt = Time.time + 3f;

        Collider2D playerFeet = actor.GetComponent<Collider2D>();
        Vector2 origin = playerFeet != null ? (Vector2)playerFeet.bounds.center : (Vector2)actor.transform.position;
        var nearby = new List<CampusStudent>();
        foreach (CampusStudent student in FindObjectsByType<CampusStudent>(FindObjectsSortMode.None))
        {
            if (student.CanRequestPassage && (student.FeetPosition - origin).sqrMagnitude <= Radius * Radius)
                nearby.Add(student);
        }

        // Barajar evita que siempre sean los mismos alumnos quienes respondan.
        for (int i = nearby.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            CampusStudent swap = nearby[i];
            nearby[i] = nearby[j];
            nearby[j] = swap;
        }

        int responders = 0;
        foreach (CampusStudent student in nearby)
        {
            if (student.TryYieldTo(origin))
                responders++;
            if (responders >= MaximumResponders)
                break;
        }

        Message?.Invoke(responders > 0 ? "Con permiso. Gracias por dejar pasar." :
            nearby.Count == 0 ? "No hay estudiantes disponibles cerca." : "No te escucharon. Busca otro paso.");
        return responders;
    }
}
