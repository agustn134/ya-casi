using System;

/// <summary>Reloj y avisos de un profesor, independientes de Unity y de su escala de tiempo.</summary>
public sealed class FinalDeliveryChallengeState
{
    public const float DurationSeconds = 16f;
    public const int VolleyCount = 6;
    public const float FirstVolleySeconds = 1f;
    public const float VolleyIntervalSeconds = 2.4f;
    public const float TelegraphSeconds = .8f;
    public const float FlightSeconds = 1.2f;

    // Compensa únicamente el redondeo de los pasos float recibidos desde el juego.
    const double BoundaryTolerance = .000001d;
    double elapsed;
    int nextVolley;
    int pendingVolley = -1;
    readonly float duration, interval;
    readonly int volleyCount;

    public FinalDeliveryChallengeState() : this(DurationSeconds, VolleyCount, VolleyIntervalSeconds) { }

    public FinalDeliveryChallengeState(float durationSeconds, int totalVolleys, float volleyIntervalSeconds)
    {
        if (float.IsNaN(durationSeconds) || float.IsInfinity(durationSeconds) || durationSeconds <= 0)
            throw new ArgumentOutOfRangeException(nameof(durationSeconds));
        if (totalVolleys <= 0) throw new ArgumentOutOfRangeException(nameof(totalVolleys));
        if (float.IsNaN(volleyIntervalSeconds) || float.IsInfinity(volleyIntervalSeconds) || volleyIntervalSeconds <= 0)
            throw new ArgumentOutOfRangeException(nameof(volleyIntervalSeconds));
        if (FirstVolleySeconds + (totalVolleys - 1) * volleyIntervalSeconds + TelegraphSeconds + FlightSeconds > durationSeconds)
            throw new ArgumentException("Las oleadas deben terminar antes de completar el desafío.");
        duration = durationSeconds; volleyCount = totalVolleys; interval = volleyIntervalSeconds;
    }

    public bool Started { get; private set; }
    public bool Completed { get { return Started && elapsed >= duration; } }
    public float Duration => duration;
    public int TotalVolleys => volleyCount;
    public float ElapsedTime { get { return (float)elapsed; } }
    public float RemainingTime { get { return Math.Max(0f, duration - ElapsedTime); } }

    /// <summary>Inicia una sola vez; repetir la llamada conserva todo el progreso.</summary>
    public bool Start()
    {
        if (Started) return false;
        Started = true;
        return true;
    }

    /// <summary>
    /// Avanza únicamente durante la partida. Cada paso sustituye el aviso anterior;
    /// un salto largo conserva solo el último aviso cruzado, evitando ráfagas atrasadas.
    /// </summary>
    public void AdvanceTime(float seconds, bool playing)
    {
        pendingVolley = -1;
        if (!Started || Completed || !playing || float.IsNaN(seconds)
            || float.IsInfinity(seconds) || seconds <= 0f) return;

        elapsed = Math.Min(duration, elapsed + seconds);
        if (elapsed >= duration - BoundaryTolerance)
        {
            elapsed = duration;
            return;
        }

        while (nextVolley < volleyCount
            && elapsed + BoundaryTolerance >= FirstVolleySeconds + nextVolley * (double)interval)
        {
            pendingVolley = nextVolley;
            nextVolley++;
        }
    }

    /// <summary>Consume una sola vez el aviso de este paso, con índice entre cero y cinco.</summary>
    public bool TryTakeVolley(out int volleyIndex)
    {
        volleyIndex = -1;
        if (!Started || Completed || pendingVolley < 0) return false;
        volleyIndex = pendingVolley;
        pendingVolley = -1;
        return true;
    }
}
