using System.Collections.Generic;
using UnityEngine;

/// <summary>Retos de los profesores con trayectorias anunciadas y progreso independiente.</summary>
public sealed class CampusFinalDeliveryChallenge : MonoBehaviour
{
    sealed class Throw
    {
        public GameObject visual;
        public SpriteRenderer renderer;
        public Vector2 start, direction, position;
        public float age;
        public bool flying;
    }
    public FinalDeliveryChallengeState State { get; private set; } = new FinalDeliveryChallengeState();
    readonly Dictionary<int, FinalDeliveryChallengeState> professorStates = new Dictionary<int, FinalDeliveryChallengeState>();
    public int ActiveProfessor { get; private set; } = -1;
    public bool IsRunning => State.Started && !State.Completed;
    public bool InChallengeArea => player != null && (ActiveTarget == null
        || Vector2.Distance(player.FeetPosition, ActiveTarget.position) <= 8f);
    readonly List<Throw> objects = new List<Throw>();
    CampusDeliveryObjectives controller;
    CampusPathPlayer player;
    Transform[] professors;
    Transform ActiveTarget => professors != null && ActiveProfessor >= 0 && ActiveProfessor < professors.Length
        ? professors[ActiveProfessor] : null;
    Sprite paper, pixel;
    Texture2D paperTexture, pixelTexture;
    Material objectMaterial;
    Vector2 previousFeet;
    public int ActiveProjectileCount => objects.FindAll(o => o.flying).Count;
    public Vector3[] ProjectilePositions => objects.FindAll(o => o.flying).ConvertAll(o => (Vector3)o.position).ToArray();

    public void Configure(CampusDeliveryObjectives owner, CampusPathPlayer actor, Transform[] targets = null)
    {
        controller = owner; player = actor; professors = targets;
        previousFeet = player.FeetPosition;
        objectMaterial = new Material(Shader.Find("Sprites/Default")) { name = "Papel y avisos sin iluminación" };
        pixelTexture = new Texture2D(1, 1) { filterMode = FilterMode.Point };
        pixelTexture.SetPixel(0, 0, Color.white); pixelTexture.Apply();
        pixel = Sprite.Create(pixelTexture, new Rect(0, 0, 1, 1), Vector2.one * .5f, 1);
        // Papel doblado con contorno; no requiere texturas externas ni material especial.
        paperTexture = new Texture2D(16, 16) { filterMode = FilterMode.Point };
        for (int y = 0; y < 16; y++) for (int x = 0; x < 16; x++)
        {
            bool inside = x >= 2 && x <= 13 && y >= 3 && y <= 12;
            Color color = !inside ? Color.clear : x == 2 || x == 13 || y == 3 || y == 12
                ? new Color(.15f, .23f, .3f) : (x + y) % 7 == 0 ? new Color(.65f, .78f, .82f) : Color.white;
            paperTexture.SetPixel(x, y, color);
        }
        paperTexture.Apply();
        paper = Sprite.Create(paperTexture, new Rect(0, 0, 16, 16), Vector2.one * .5f, 32);
    }
    public bool IsCleared(int professorIndex, bool isLast = false) => professorStates.TryGetValue(professorIndex, out var state)
        && state.Completed && (!isLast || state.Duration >= FinalDeliveryChallengeState.DurationSeconds);

    public bool BeginForProfessor(int professorIndex, bool isLast)
    {
        if (player == null || professorIndex < 0 || (professors != null && professorIndex >= professors.Length)
            || IsCleared(professorIndex, isLast)) return false;
        bool needsFinalRound = isLast && professorStates.TryGetValue(professorIndex, out var earlier)
            && earlier.Duration < FinalDeliveryChallengeState.DurationSeconds;
        if (ActiveProfessor == professorIndex && State.Started && !needsFinalRound) return false;
        // Cambiar de profesor conserva la parte superada de su reto y elimina los objetos anteriores.
        Clear();
        if (!professorStates.TryGetValue(professorIndex, out var next) || needsFinalRound)
        {
            next = isLast ? new FinalDeliveryChallengeState() : new FinalDeliveryChallengeState(8f, 3, 2f);
            professorStates[professorIndex] = next;
        }
        State = next; ActiveProfessor = professorIndex;
        State.Start(); previousFeet = player.FeetPosition;
        return true;
    }

    // Compatibilidad: el reto anterior no tenía profesor ni límite de área.
    public void Begin()
    {
        if (player == null || State.Started) return;
        State.Start(); previousFeet = player.FeetPosition;
    }

    void Update()
    {
        if (controller == null || player == null || controller.Session == null) return;
        var session = controller.Session;
        if (session.Failed || session.Completed) { Clear(); return; }
        if (!session.Playing) return;
        State.AdvanceTime(Time.deltaTime, InChallengeArea);
        if (State.Completed)
        {
            Clear();
            previousFeet = player.FeetPosition;
            return;
        }
        Vector2 currentFeet = player.FeetPosition;
        for (int i = objects.Count - 1; i >= 0; i--)
        {
            Throw item = objects[i];
            item.age += Time.deltaTime;
            if (!item.flying)
            {
                item.renderer.color = new Color(1, .55f, .12f, .45f + .3f * Mathf.Sin(item.age * 25));
                if (item.age < FinalDeliveryChallengeState.TelegraphSeconds) continue;
                item.flying = true; item.age = 0;
                item.renderer.sprite = paper; item.renderer.color = Color.white;
                item.visual.transform.localScale = Vector3.one;
                item.visual.transform.position = item.position = item.start;
                continue;
            }
            Vector2 next = item.start + item.direction * (item.age * 10f);
            // Barrido relativo: detecta también un jugador que cruza el proyectil entre frames.
            Vector2 a = item.position - previousFeet, b = next - currentFeet;
            if (DistanceToSegment(a, b) <= .4f)
            {
                Remove(i);
                controller.ReceiveThrownObjectHit();
                if (session.Failed) return;
                continue;
            }
            item.position = next;
            item.visual.transform.position = next;
            item.visual.transform.Rotate(0, 0, 300 * Time.deltaTime);
            if (item.age >= FinalDeliveryChallengeState.FlightSeconds) Remove(i);
        }
        if (State.TryTakeVolley(out int index))
        {
            controller.PlayThrowCue();
            // El origen sigue al profesor sólo hasta anunciarse: después la trayectoria es fija.
            Vector2 origin = ActiveTarget != null ? (Vector2)ActiveTarget.position : currentFeet + Vector2.left * 6;
            Vector2 direction = currentFeet - origin;
            if (direction.sqrMagnitude < .64f)
            {
                direction = index % 2 == 0 ? Vector2.right : Vector2.up;
                origin = currentFeet - direction * 1.4f;
            }
            direction.Normalize();
            Announce(origin, direction);
            if (index >= 3)
            {
                Vector2 sideTarget = currentFeet + new Vector2(-direction.y, direction.x) * (index % 2 == 0 ? 1.35f : -1.35f);
                Announce(origin, (sideTarget - origin).normalized);
            }
        }
        previousFeet = currentFeet;
    }

    void Announce(Vector2 origin, Vector2 direction)
    {
        var visual = new GameObject("Aviso de lanzamiento", typeof(SpriteRenderer));
        visual.transform.SetParent(transform, false);
        visual.transform.position = origin + direction * 6;
        visual.transform.localScale = new Vector3(12, .12f, 1);
        visual.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        var renderer = visual.GetComponent<SpriteRenderer>();
        renderer.sprite = pixel; renderer.sharedMaterial = objectMaterial;
        renderer.sortingOrder = 2500; renderer.color = new Color(1, .55f, .12f, .7f);
        objects.Add(new Throw { visual = visual, renderer = renderer, start = origin, direction = direction });
    }
    static float DistanceToSegment(Vector2 a, Vector2 b)
    {
        Vector2 delta = b - a;
        float t = delta.sqrMagnitude < .000001f ? 0 : Mathf.Clamp01(-Vector2.Dot(a, delta) / delta.sqrMagnitude);
        return (a + t * delta).magnitude;
    }
    void Remove(int index) { if (objects[index].visual != null) Destroy(objects[index].visual); objects.RemoveAt(index); }
    public void Clear() { for (int i = objects.Count - 1; i >= 0; i--) Remove(i); }
    void OnDestroy()
    {
        Clear();
        if (paper != null) Destroy(paper); if (pixel != null) Destroy(pixel);
        if (paperTexture != null) Destroy(paperTexture); if (pixelTexture != null) Destroy(pixelTexture);
        if (objectMaterial != null) Destroy(objectMaterial);
    }
}
