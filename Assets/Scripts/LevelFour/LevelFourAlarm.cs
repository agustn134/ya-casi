/// <summary>Cuenta única de tres minutos; salir del F la detiene.</summary>
public sealed class LevelFourAlarm
{
    public const float Duration = 180f;
    public float Remaining { get; private set; } = Duration;
    public bool Running { get; private set; }
    public bool Expired => Running && Remaining <= 0;
    public void Start() { Remaining = Duration; Running = true; }
    public void Tick(float delta, bool paused) { if (Running && !paused) Remaining = System.Math.Max(0, Remaining - System.Math.Max(0, delta)); }
    public void Stop() { Running = false; }
}
