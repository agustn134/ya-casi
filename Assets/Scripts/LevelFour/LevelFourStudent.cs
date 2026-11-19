using UnityEngine;

/// <summary>Animación tranquila de alumnos reunidos en el callejón.</summary>
public sealed class LevelFourStudent : MonoBehaviour
{
    public string team = "Software";
    public bool Aggressive { get; private set; }
    public bool IsStunned => Time.time < stunUntil;
    Vector2 home;
    Rigidbody2D body;
    float stunUntil, farFor;
    public Texture2D sheet;
    public int facing;
    public float phase;
    public Transform bottle;
    Sprite[] frames;
    SpriteRenderer appearance;
    Vector3 bottleRest;
    float clock;

    void Awake()
    {
        home = transform.position;
        body = GetComponent<Rigidbody2D>();
        if (body == null) body = gameObject.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic; body.useFullKinematicContacts = true;
        if (GetComponent<CapsuleCollider2D>() == null)
        {
            var collider = gameObject.AddComponent<CapsuleCollider2D>();
            collider.size = new Vector2(.6f, .55f); collider.offset = new Vector2(0, -.55f);
        }
        appearance = GetComponent<SpriteRenderer>();
        frames = new Sprite[6];
        for (int i = 0; i < frames.Length; i++)
            frames[i] = Sprite.Create(sheet, new Rect((facing * 6 + i) * 16, 0, 16, 32), new Vector2(.5f, .4f), 16);
        if (bottle != null) bottleRest = bottle.localPosition;
        clock = phase;
    }

    public void Provoke() { Aggressive = true; farFor = 0; }
    public void Stun(float seconds) { stunUntil = Time.time + seconds; }
    public void Calm() { Aggressive = false; farFor = 0; }

    void FixedUpdate()
    {
        var intro = LevelFourIntro.Active;
        if (intro == null || intro.Paused || IsStunned) return;
        var fighter = GetComponent<LevelFourFighter>();
        if (fighter != null && fighter.ControlsMotion) return;
        Vector2 destination = home;
        if (Aggressive)
        {
            float distance = Vector2.Distance(transform.position, intro.player.position);
            farFor = distance > 7 ? farFor + Time.fixedDeltaTime : 0;
            if (farFor > 3 || intro.CurrentBeat == LevelFourIntro.Beat.Climbing) Calm();
            else
            {
                destination = intro.player.position;
                if (distance < .95f) { intro.player.GetComponent<LevelFourMotor>().Damage((Vector2)intro.player.position - body.position); destination = body.position; }
            }
        }
        if (Vector2.Distance(body.position, destination) > .15f)
        {
            Vector2 next = Vector2.MoveTowards(body.position, destination, (Aggressive ? 3.1f : 1.5f) * Time.fixedDeltaTime);
            next.y = Mathf.Clamp(next.y, intro.alleyMin.y + .2f, intro.alleyMax.y - .05f);
            body.MovePosition(next);
        }
    }

    void Update()
    {
        var intro = LevelFourIntro.Active;
        if (intro != null && intro.Paused) return;
        clock += Time.deltaTime;
        appearance.sprite = frames[Mathf.FloorToInt(clock * 5) % frames.Length];
        appearance.sortingOrder = Mathf.RoundToInt(1000 - (transform.position.y - .5f) * 10);
        if (bottle == null) return;
        var fighter = GetComponent<LevelFourFighter>();
        bottle.gameObject.SetActive(!Aggressive && !IsStunned && (fighter == null || !fighter.Downed));
        float sip = Mathf.Clamp01(Mathf.Sin(clock * .65f) * 4 - 2.8f);
        bottle.localPosition = bottleRest + Vector3.up * (.42f * sip);
        bottle.localRotation = Quaternion.Euler(0, 0, -55 * sip);
        bottle.GetComponent<SpriteRenderer>().sortingOrder = appearance.sortingOrder + 2;
    }

    void OnDestroy() { if (frames != null) foreach (var frame in frames) if (frame != null) Destroy(frame); }
}
