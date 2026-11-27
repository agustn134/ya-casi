/// <summary>Escalada de conflicto compartida: caminar no cuenta; los avisos caducan.</summary>
public sealed class LevelFourSocialRules
{
    public int Count { get; private set; }
    public const float Window = 7f;
    public const float ContactCooldown = .95f;
    float last = -100;
    public int Contact(float now, bool dangerous)
    {
        if (!dangerous) return 0;
        if (now - last < ContactCooldown) return 0;
        if (now - last > Window) Count = 0;
        last = now;
        Count = System.Math.Min(3, Count + 1);
        return Count;
    }
    public void Reset() { Count = 0; last = -100; }
}
