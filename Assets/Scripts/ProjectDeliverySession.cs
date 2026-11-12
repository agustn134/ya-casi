using System;
using System.Collections.Generic;

public enum ProjectDeliveryResult { Accepted, WrongProject, MissingProject, AlreadyDelivered, NotPlaying, UnknownProfessor }

/// <summary>Reglas comprobables con proyectos provisionales, sin cargar el mapa de Unity.</summary>
public sealed class ProjectDeliverySession
{
    readonly Dictionary<string, string> requirements = new Dictionary<string, string>(StringComparer.Ordinal);
    readonly HashSet<string> delivered = new HashSet<string>(StringComparer.Ordinal);
    public ProjectInventory Inventory { get; }
    public int DeliveredCount => delivered.Count;
    public int RequiredCount => requirements.Count;
    public float RemainingTime { get; private set; }
    public float ElapsedTime { get; private set; }
    public float WrongProjectPenalty { get; }
    public float RepeatDeliveryPenalty { get; }
    public float TotalPenaltySeconds { get; private set; }
    public int WarningCount { get; private set; }
    public bool Paused { get; private set; }
    public bool Failed { get; private set; }
    public bool Completed => DeliveredCount == RequiredCount;
    public bool Playing => !Paused && !Failed && !Completed;

    public ProjectDeliverySession(IReadOnlyList<string> professorIds, IReadOnlyList<string> projectIds,
        float seconds, ProjectInventory inventory, float wrongProjectPenalty = 5f, float repeatDeliveryPenalty = 8f)
    {
        if (professorIds == null || projectIds == null || professorIds.Count == 0 || professorIds.Count != projectIds.Count)
            throw new ArgumentException("Cada profesor necesita un proyecto.");
        if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds <= 0) throw new ArgumentOutOfRangeException(nameof(seconds));
        if (float.IsNaN(wrongProjectPenalty) || float.IsInfinity(wrongProjectPenalty) || wrongProjectPenalty < 0)
            throw new ArgumentOutOfRangeException(nameof(wrongProjectPenalty));
        if (float.IsNaN(repeatDeliveryPenalty) || float.IsInfinity(repeatDeliveryPenalty) || repeatDeliveryPenalty < 0)
            throw new ArgumentOutOfRangeException(nameof(repeatDeliveryPenalty));
        WrongProjectPenalty = wrongProjectPenalty;
        RepeatDeliveryPenalty = repeatDeliveryPenalty;
        Inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
        for (int i = 0; i < professorIds.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(professorIds[i]) || string.IsNullOrWhiteSpace(projectIds[i]))
                throw new ArgumentException("Los identificadores no pueden estar vacíos.");
            requirements.Add(professorIds[i], projectIds[i]);
        }
        RemainingTime = seconds;
    }

    public bool IsDelivered(string professorId) => professorId != null && delivered.Contains(professorId);
    public void SetPaused(bool value) { if (!Failed && !Completed) Paused = value; }
    public void AdvanceTime(float seconds)
    {
        if (!Playing || float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds <= 0) return;
        float consumed = Math.Min(RemainingTime, seconds);
        RemainingTime -= consumed;
        ElapsedTime += consumed;
        if (RemainingTime <= 0) Failed = true;
    }

    public ProjectDeliveryResult TryDeliver(string professorId, string selectedProject)
    {
        if (!Playing) return ProjectDeliveryResult.NotPlaying;
        if (professorId == null || !requirements.TryGetValue(professorId, out string expected)) return ProjectDeliveryResult.UnknownProfessor;
        if (delivered.Contains(professorId))
        {
            Penalize(RepeatDeliveryPenalty);
            return ProjectDeliveryResult.AlreadyDelivered;
        }
        if (!string.Equals(expected, selectedProject, StringComparison.Ordinal))
        {
            Penalize(WrongProjectPenalty);
            return ProjectDeliveryResult.WrongProject;
        }
        if (!Inventory.TryRemove(expected)) return ProjectDeliveryResult.MissingProject;
        delivered.Add(professorId);
        return ProjectDeliveryResult.Accepted;
    }

    void Penalize(float seconds)
    {
        WarningCount++;
        float deducted = Math.Min(RemainingTime, seconds);
        RemainingTime -= deducted;
        TotalPenaltySeconds += deducted;
        // El cronómetro mide tiempo real de juego; las sanciones sólo restan al reloj.
        if (RemainingTime <= 0) Failed = true;
    }
}
