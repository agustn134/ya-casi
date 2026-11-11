using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Petición de paso con respuestas parciales y aleatorias de los compañeros.</summary>
public sealed class CampusCourtesyRequest : MonoBehaviour
{
    [SerializeField, Min(.5f)] float radius = 2.8f;
    [SerializeField, Min(.5f)] float cooldown = 3f;
    [SerializeField, Range(0, 1)] float responseChance = .45f;
    [SerializeField, Min(1)] int maximumResponders = 2;
    CampusPathPlayer player;
    float nextRequestTime;
    readonly System.Random random = new System.Random();
    public string LastMessage { get; private set; } = "";
    public float CooldownRemaining => Mathf.Max(0, nextRequestTime - Time.time);
    public event Action<string> Message;

    public void Configure(CampusPathPlayer actor) { player = actor; }

    void Update()
    {
        if ((Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame)
            || (Gamepad.current != null && Gamepad.current.leftShoulder.wasPressedThisFrame)) RequestPassage();
    }

    /// <returns>Número de compañeros que aceptan; -1 si no se puede pedir paso todavía.</returns>
    public int RequestPassage()
    {
        if (player == null || Time.timeScale <= 0) return -1;
        if (Time.time < nextRequestTime)
        {
            Say("Espera " + Mathf.CeilToInt(CooldownRemaining) + " s para volver a pedir permiso.");
            return -1;
        }
        nextRequestTime = Time.time + cooldown;
        var nearby = new List<CampusStudentWalker>();
        foreach (var student in FindObjectsByType<CampusStudentWalker>(FindObjectsSortMode.None))
            if (student.isActiveAndEnabled && Vector2.Distance(student.FeetPosition, player.FeetPosition) <= radius)
                nearby.Add(student);
        for (int i = nearby.Count - 1; i > 0; i--)
        {
            int other = random.Next(i + 1);
            var temporary = nearby[i]; nearby[i] = nearby[other]; nearby[other] = temporary;
        }
        int responded = 0;
        foreach (var student in nearby)
        {
            if (responded >= maximumResponders) break;
            if (random.NextDouble() >= responseChance || !student.TryYieldTo(player.FeetPosition)) continue;
            responded++;
        }
        if (nearby.Count == 0) Say("Galindo: «Con permiso».");
        else if (responded == 0) Say("Galindo: «Con permiso».\nPor ahora, los compañeros siguen platicando.");
        else Say("Galindo: «Con permiso».\n" + (responded == 1 ? "Un compañero se aparta un momento." : responded + " compañeros se apartan un momento."));
        return responded;
    }
    void Say(string value) { LastMessage = value; Message?.Invoke(value); }
}
