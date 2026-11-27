using TMPro;
using UnityEngine;

/// <summary>Daño, retroceso y caída temporal de alumnos y perseguidores.</summary>
public sealed class LevelFourFighter : MonoBehaviour
{
    public int maximumHealth = 3;
    public int Health { get; private set; }
    public bool Downed => down > 0;
    public bool ControlsMotion => Downed || pushTime > 0;
    Rigidbody2D body;
    Collider2D feet;
    SpriteRenderer appearance;
    Vector2 push;
    float down, pushTime, flash, recovery;
    GameObject bar;
    Transform fill;
    Sprite pixel;
    readonly RaycastHit2D[] hits = new RaycastHit2D[12];

    void Awake()
    {
        Health = maximumHealth;
        body = GetComponent<Rigidbody2D>(); feet = GetComponent<Collider2D>(); appearance = GetComponent<SpriteRenderer>();
        pixel = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.one * .5f, 1);
        bar = new GameObject("Resistencia · " + name);
        var back = bar.AddComponent<SpriteRenderer>(); back.sprite = pixel; back.color = new Color(.07f, .09f, .12f); back.sortingOrder = 6500;
        bar.transform.localScale = new Vector3(.8f, .10f, 1);
        var child = new GameObject("Vida restante"); child.transform.SetParent(bar.transform, false);
        var front = child.AddComponent<SpriteRenderer>(); front.sprite = pixel; front.color = new Color(1, .45f, .2f); front.sortingOrder = 6501; fill = child.transform;
        bar.SetActive(false);
    }

    public bool TakeHit(int damage, Vector2 away, bool shove)
    {
        if (Downed || Time.time < recovery) return false;
        Health = Mathf.Max(0, Health - damage);
        push = (away.sqrMagnitude > .001f ? away.normalized : Vector2.down) * (shove ? 8 : 4.2f);
        pushTime = shove ? .34f : .22f; flash = .22f;
        bool fall = shove || Health == 0;
        down = fall ? Health == 0 ? 4.2f : 2.1f : 0;
        if (fall && feet != null) feet.enabled = false;
        var pursuer = GetComponent<LevelFourPursuer>();
        if (pursuer != null) pursuer.Stun(fall ? down : 1.1f);
        var student = GetComponent<LevelFourStudent>();
        if (student != null) student.Stun(fall ? down : 1.1f);
        var number = new GameObject(shove ? "¡EMPÚJON!" : "Daño infligido"); number.transform.position = transform.position + Vector3.up;
        var text = number.AddComponent<TextMeshPro>(); text.text = shove ? "¡AL SUELO!" : "-" + damage; text.fontSize = 3; text.color = new Color(1, .8f, .4f); text.alignment = TextAlignmentOptions.Center;
        text.rectTransform.sizeDelta = new Vector2(12, 3); text.transform.localScale = Vector3.one * .25f; text.GetComponent<MeshRenderer>().sortingOrder = 6505;
        number.AddComponent<LevelFourDamageNumber>();
        return true;
    }

    void FixedUpdate()
    {
        var intro = LevelFourIntro.Active;
        if (intro == null || intro.Paused) return;
        if (flash > 0) { flash -= Time.fixedDeltaTime; appearance.color = new Color(1, .55f, .38f); }
        else if (appearance != null && ControlsMotion) appearance.color = Color.white;
        if (pushTime > 0)
        {
            pushTime -= Time.fixedDeltaTime;
            Vector2 delta = push * Time.fixedDeltaTime;
            if (feet != null && feet.enabled)
            {
                float distance = delta.magnitude;
                int count = feet.Cast(delta.normalized, new ContactFilter2D { useTriggers = false }, hits, distance + .02f);
                for (int i = 0; i < count; i++) if (hits[i].rigidbody != body) distance = Mathf.Min(distance, Mathf.Max(0, hits[i].distance - .02f));
                delta = delta.normalized * distance;
            }
            Vector2 next = body.position + delta;
            var mission = LevelFourMission.Active;
            if (mission != null && mission.CampusActive) { if (mission.Walkable(next)) body.MovePosition(next); }
            else if (next.x >= intro.alleyMin.x && next.x <= intro.alleyMax.x && next.y >= intro.alleyMin.y && next.y <= intro.alleyMax.y) body.MovePosition(next);
        }
        if (down > 0)
        {
            down -= Time.fixedDeltaTime;
            transform.localRotation = Quaternion.Euler(0, 0, Mathf.LerpAngle(transform.localEulerAngles.z, push.x < 0 ? 78 : -78, .3f));
            if (down <= 0)
            {
                transform.localRotation = Quaternion.identity;
                if (feet != null) feet.enabled = true;
                if (Health <= 0) Health = maximumHealth;
                recovery = Time.time + .8f;
            }
        }
    }

    void LateUpdate()
    {
        if (bar == null) return;
        var student = GetComponent<LevelFourStudent>(); var guard = GetComponent<LevelFourPursuer>();
        bar.SetActive(Health < maximumHealth || Downed || student != null && student.Aggressive || guard != null && guard.Chasing);
        bar.transform.position = transform.position + Vector3.up * 1.05f;
        fill.localScale = new Vector3((float)Health / maximumHealth, .65f, 1);
    }
    void OnDestroy() { if (bar != null) Destroy(bar); if (pixel != null) Destroy(pixel); }
}

public sealed class LevelFourDamageNumber : MonoBehaviour
{
    float life = .9f;
    void Update()
    {
        if (LevelFourIntro.Active != null && LevelFourIntro.Active.Paused) return;
        life -= Time.deltaTime; transform.position += Vector3.up * Time.deltaTime * .5f;
        if (life <= 0) Destroy(gameObject);
    }
}
