using UnityEngine;

/// <summary>Haz intermitente: rojo detecta movimiento; azul permite cruzar.</summary>
public sealed class LevelFourSensor : MonoBehaviour
{
    public Vector2 size = new Vector2(6, .35f);
    public float phase;
    public bool IsActive { get; private set; }
    float cycle;
    SpriteRenderer beam;
    void Awake() { beam = GetComponent<SpriteRenderer>(); cycle = phase; }
    void Update()
    {
        var mission = LevelFourMission.Active;
        if (mission == null || mission.Intro.Paused) return;
        cycle += Time.deltaTime;
        IsActive = cycle % 6 < 3.8f;
        beam.color = IsActive ? new Color(1, .12f, .1f, .65f) : new Color(.2f, .65f, .9f, .12f);
        if (!mission.IsInside || !IsActive || !mission.CanMove || mission.Movement.sqrMagnitude < .005f) return;
        if (Detects(mission.Intro.player.position, true))
            mission.Fail("Activaste un sensor de movimiento. Espera a que el haz rojo se apague antes de cruzar.");
    }
    public bool Detects(Vector2 position, bool moving) => IsActive && moving && Mathf.Abs(position.x - transform.position.x) < size.x * .5f && Mathf.Abs(position.y - transform.position.y) < size.y * .5f;
}

