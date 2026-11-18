using System;
using TMPro;
using UnityEngine;

/// <summary>Vigilancia nocturna: patrulla, oye ruidos y persigue sin atravesar paredes.</summary>
public sealed class LevelOneCaretaker : MonoBehaviour
{
    enum Behaviour { Patrol, Investigating, Chasing, ChangingFloor }

    public int AssignedFloor=-1;
    public int PatrolDirection=1;
    public float PatrolLaneOffset;
    int patrolStep=1;
    public Vector2 Position => body!=null?body.position:(Vector2)transform.position;
    public bool IsChasing => state==Behaviour.Chasing;
    public int Warnings => world!=null?world.TotalWarnings:0;
    const float SightRange = 10f;
    const float SightAngle = 90f;
    const float DetectionSeconds = 1f;
    const float ChaseSeconds = 8f;
    const float ProtectionSeconds = 8f;
    const int FanSegments = 28;

    public event Action<string> Message;
    public string StatusText { get; private set; } = "El conserje está de ronda";
    public float Suspicion01 => state == Behaviour.Chasing ? 1f : awareness;

    Level01Manager manager;
    PlayerMovement player;
    LevelOneNightSchool world;
    GameObject actor;
    Rigidbody2D body;
    CapsuleCollider2D feet;
    Animator animator;
    TextMeshPro label;
    Mesh fanMesh;
    Material fanMaterial;
    MeshRenderer fanRenderer;
    Transform fanTransform;
    Vector2[] patrol = Array.Empty<Vector2>();
    Vector2 facing = Vector2.left;
    Vector2 moveTarget;
    Vector2 noisePoint;
    Vector2 lastSeen;
    Behaviour state;
    int floor;
    int nextPoint;
    int travelFloor = -1;
    float awareness;
    float stateUntil;
    float stopUntil;
    float protectedUntil;
    float runningSeconds;
    float nextRunningNoise;
    float nextFanUpdate;
    float blockedTime;
    float moveSpeed = 1.45f;
    readonly RaycastHit2D[] rayHits = new RaycastHit2D[64];
    readonly Vector3[] fanVertices = new Vector3[FanSegments + 2];
    readonly int[] fanTriangles = new int[FanSegments * 3];
    readonly Color[] fanColors = new Color[FanSegments + 2];

    public void Configure(Level01Manager levelManager, PlayerMovement actorPlayer,
        LevelOneNightSchool school, U2GameConfig config)
    {
        if (actor != null || levelManager == null || actorPlayer == null || school == null) return;
        manager = levelManager; player = actorPlayer; world = school;
        floor = AssignedFloor>=0?AssignedFloor:Mathf.Max(0, world.FloorAt(player.transform.position));
        patrol = world.PatrolPoints(floor) ?? Array.Empty<Vector2>();
        patrolStep=PatrolDirection>=0?1:-1;
        for(int i=0;i<patrol.Length;i++)patrol[i]+=Vector2.up*PatrolLaneOffset;
        int start=Mathf.Clamp(patrolStep>0?1:patrol.Length-2,0,Mathf.Max(0,patrol.Length-1));
        actor = new GameObject("Intendente · planta "+(floor+1)+" · sentido "+patrolStep);
        actor.transform.SetParent(transform, false);
        actor.AddComponent<LevelOneDepthSort>();
        actor.transform.position = patrol.Length > 0 ? patrol[start] : world.SafePoint(floor);
        var sprite = actor.AddComponent<SpriteRenderer>();
        sprite.sortingOrder = 3;
        int appearance = patrolStep>0?3:2;
        if (config != null && config.studentIdleSprites != null && config.studentIdleSprites.Length > appearance)
            sprite.sprite = config.studentIdleSprites[appearance];
        animator = actor.AddComponent<Animator>();
        if (config != null && config.studentControllers != null && config.studentControllers.Length > appearance)
        {
            animator.runtimeAnimatorController = config.studentControllers[appearance];
            animator.Play("Base Layer.IdleDown", 0, 0);
            animator.Update(0);
        }
        body = actor.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0;
        body.freezeRotation = true;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.useFullKinematicContacts = true;
        feet = actor.AddComponent<CapsuleCollider2D>();
        feet.size = new Vector2(.56f, .40f);
        feet.offset = new Vector2(0, -.45f);
        var title = new GameObject("Identidad del conserje");
        title.transform.SetParent(actor.transform, false);
        title.transform.localPosition = new Vector3(0, 1.05f, 0);
        label = title.AddComponent<TextMeshPro>();
        if (LevelOneTypography.Font != null) label.font = LevelOneTypography.Font;
        label.fontSize = 2.0f;
        label.alignment = TextAlignmentOptions.Center;
        label.rectTransform.sizeDelta = new Vector2(3.4f, .85f);
        label.renderer.sortingOrder = 15;
        CreateFlashlight();
        nextPoint = Mathf.Clamp(start+patrolStep,0,Mathf.Max(0,patrol.Length-1));
        facing=patrolStep>0?Vector2.right:Vector2.left;
        moveTarget = patrol.Length > 0 ? patrol[nextPoint] : body.position;
        protectedUntil = Time.time + 3;
        UpdatePresentation();
    }

    void Update()
    {
        if (actor == null || manager == null || player == null || world == null) return;
        if (!manager.IsPlaying)
        {
            if (animator != null) animator.speed = 0;
            return;
        }
        int playerFloor = world.FloorAt(player.transform.position);
        if (playerFloor >= 0 && playerFloor != floor)
        {
            if(AssignedFloor<0){ChangeFloor(playerFloor);UpdatePresentation();return;}
        }
        if (state == Behaviour.ChangingFloor)
        {
            state = Behaviour.Patrol;
            travelFloor = -1;
            StatusText = "El conserje está de ronda";
        }

        Vector2 playerPosition = player.transform.position;
        bool hidden = world.IsHidden(playerPosition) && !player.IsRunning;
        bool seesPlayer = playerFloor == floor && !hidden && Time.time >= Mathf.Max(protectedUntil,world.SafeUntil) && CanSee(playerPosition);
        if (seesPlayer)
        {
            awareness = Mathf.Min(1, awareness + Time.deltaTime / DetectionSeconds);
            lastSeen = playerPosition;
            if (awareness >= 1 && state != Behaviour.Chasing)
            {
                state = Behaviour.Chasing;
                stateUntil = Time.time + ChaseSeconds;
                StatusText = "¡Te vio! Rompe la visión y entra a un salón oscuro";
                Message?.Invoke("¡El conserje te vio! Busca un salón oscuro.");
            }
        }
        else awareness = Mathf.MoveTowards(awareness, 0, Time.deltaTime * .9f);

        runningSeconds=player.IsRunning&&playerFloor==floor&&!hidden?runningSeconds+Time.deltaTime:
            Mathf.MoveTowards(runningSeconds,0,Time.deltaTime*2);
        if(runningSeconds>=3.5f&&playerFloor==floor&&!hidden&&
            Time.time>=Mathf.Max(protectedUntil,world.SafeUntil)&&Vector2.Distance(body.position,playerPosition)<=14)
        {
            if(state!=Behaviour.Chasing)Message?.Invoke("¡El conserje oyó tus pasos! Entra a un salón y deja de correr.");
            lastSeen=playerPosition;awareness=1;state=Behaviour.Chasing;stateUntil=Time.time+ChaseSeconds;stopUntil=0;
            StatusText="Te oyó correr · busca un salón oscuro";
        }

        if (player.IsRunning && playerFloor == floor && Time.time >= nextRunningNoise &&
            Vector2.Distance(body.position, playerPosition) <= 11f)
        {
            nextRunningNoise = Time.time + 2.5f;
            HearNoise(playerPosition);
        }

        if (state == Behaviour.Chasing)
        {
            moveSpeed = 5.8f;
            moveTarget = ChasePoint(seesPlayer ? playerPosition : lastSeen);
            if (hidden || playerFloor != floor || Time.time >= stateUntil)
            {
                state = Behaviour.Investigating;
                awareness = 0;
                protectedUntil = Mathf.Max(protectedUntil, Time.time + .8f);
                noisePoint = CorridorPoint(lastSeen);
                stateUntil = Time.time + 2.5f;
                StatusText = hidden ? "A salvo en la oscuridad · deja de correr" : "El conserje busca dónde te vio";
                Message?.Invoke(hidden ? "Perdiste al conserje en la oscuridad." : "Rompiste su persecución. Sigue con cuidado.");
            }
            else if (Vector2.Distance(body.position, playerPosition) < .85f && Time.time >= Mathf.Max(protectedUntil,world.SafeUntil))
                CatchPlayer();
        }
        else if (state == Behaviour.Investigating)
        {
            moveSpeed = 1.9f;
            moveTarget = noisePoint;
            if (Time.time >= stateUntil)
            {
                state = Behaviour.Patrol;
                StatusText = "El conserje vuelve a su ronda";
                nextPoint = ClosestPatrolIndex();
                stopUntil = Time.time + .6f;
            }
        }
        else if (state == Behaviour.Patrol)
        {
            StatusText = Time.time < protectedUntil
                ? $"Margen seguro · {Mathf.CeilToInt(protectedUntil - Time.time)} s"
                : "El conserje está de ronda";
            moveSpeed = manager.HasUSB ? 1.7f : 1.45f;
            if (patrol.Length > 0)
            {
                moveTarget = patrol[nextPoint];
                if (Vector2.Distance(body.position, moveTarget) < .18f && Time.time >= stopUntil)
                {
                    AdvancePatrol();
                    stopUntil = Time.time + .85f;
                    facing = nextPoint % 2 == 0 ? Vector2.up : Vector2.down;
                }
            }
        }
        UpdatePresentation();
    }

    public void HearNoise(Vector2 position)
    {
        if (manager == null || !manager.IsPlaying || actor == null || state == Behaviour.Chasing ||
            state == Behaviour.ChangingFloor || world.FloorAt(position) != floor ||
            Vector2.Distance(body.position, position) > 14) return;
        bool justHeard = state != Behaviour.Investigating;
        state = Behaviour.Investigating;
        noisePoint = CorridorPoint(position);
        stateUntil = Time.time + 4f;
        stopUntil = 0;
        StatusText = "¿Qué fue eso? El conserje investiga el ruido";
        if (justHeard) Message?.Invoke("El conserje oyó un ruido. Camina y busca cobertura.");
    }

    void FixedUpdate()
    {
        if (body == null || manager == null || !manager.IsPlaying || Time.time < stopUntil ||
            state == Behaviour.ChangingFloor)
        {
            if (animator != null) animator.speed = 0;
            return;
        }
        Vector2 step = Vector2.MoveTowards(body.position, moveTarget, moveSpeed * Time.fixedDeltaTime);
        Vector2 delta = step - body.position;
        if (delta.sqrMagnitude < .00001f) { if (animator != null) animator.speed = 0; return; }
        facing = delta.normalized;
        if (!CanWalkStep(delta))
        {
            blockedTime += Time.fixedDeltaTime;
            if (animator != null) animator.speed = 0;
            if (blockedTime > 1.2f)
            {
                blockedTime = 0;
                state = Behaviour.Patrol;
                if (patrol.Length > 0) AdvancePatrol();
                stopUntil = Time.time + .4f;
            }
            return;
        }
        blockedTime = 0;
        body.MovePosition(step);
        AnimateWalk(facing);
    }

    bool CanSee(Vector2 position)
    {
        Vector2 origin = body.position;
        Vector2 delta = position - origin;
        if (delta.sqrMagnitude > SightRange * SightRange || delta.sqrMagnitude < .0001f) return false;
        if (Vector2.Dot(facing, delta.normalized) < Mathf.Cos(SightAngle * .5f * Mathf.Deg2Rad)) return false;
        return StaticDistance(origin, delta.normalized, delta.magnitude) >= delta.magnitude - .08f;
    }

    float StaticDistance(Vector2 origin, Vector2 direction, float range)
    {
        var filter = ContactFilter2D.noFilter; filter.useTriggers = false;
        int count = Physics2D.Raycast(origin, direction, filter, rayHits, range);
        float closest = range;
        for (int i = 0; i < count; i++)
        {
            var collider = rayHits[i].collider;
            if (!IsStaticObstacle(collider)) continue;
            closest = Mathf.Min(closest, rayHits[i].distance);
        }
        return closest;
    }

    bool CanWalkStep(Vector2 delta)
    {
        var filter = ContactFilter2D.noFilter; filter.useTriggers = false;
        int count = feet.Cast(delta.normalized, filter, rayHits, delta.magnitude + .035f);
        if (count >= rayHits.Length) return false;
        for (int i = 0; i < count; i++)
            if (IsStaticObstacle(rayHits[i].collider)) return false;
        return true;
    }

    bool IsStaticObstacle(Collider2D collider)
    {
        if (collider == null || collider.isTrigger || collider == feet || collider.attachedRigidbody == body ||
            collider.GetComponentInParent<PlayerMovement>() != null ||
            collider.GetComponentInParent<CampusStudent>() != null) return false;
        return collider.attachedRigidbody == null || collider.attachedRigidbody.bodyType == RigidbodyType2D.Static;
    }

    Vector2 ChasePoint(Vector2 point)
    {
        float levelY = floor * LevelOneNightSchool.FloorSpacing;
        // Los salones quedan fuera de la persecución: el conserje espera en el pasillo.
        if (world.FloorAt(point) != floor || point.y < levelY - 5.2f || point.y > levelY + 5.2f)
            return CorridorPoint(point);
        float minX = float.PositiveInfinity, maxX = float.NegativeInfinity;
        foreach (var patrolPoint in patrol)
        { minX = Mathf.Min(minX, patrolPoint.x); maxX = Mathf.Max(maxX, patrolPoint.x); }
        if (patrol.Length == 0) return body.position;
        // Permite acercarse por distintos carriles sin entrar en la fila de casilleros.
        return new Vector2(Mathf.Clamp(point.x, minX - 3.2f, maxX + 2.2f),
            Mathf.Clamp(point.y, levelY - 4.0f, levelY + 1.8f));
    }

    Vector2 CorridorPoint(Vector2 point)
    {
        if (patrol.Length == 0) return body.position;
        Vector2 best = patrol[0]; float bestDistance = float.PositiveInfinity;
        for (int i = 0; i < patrol.Length; i++)
        {
            Vector2 a = patrol[i], segment = patrol[(i + 1) % patrol.Length] - a;
            float t = segment.sqrMagnitude > .001f ? Mathf.Clamp01(Vector2.Dot(point - a, segment) / segment.sqrMagnitude) : 0;
            Vector2 candidate = a + segment * t;
            if ((point - candidate).sqrMagnitude < bestDistance)
            { bestDistance = (point - candidate).sqrMagnitude; best = candidate; }
        }
        return best;
    }

    void AdvancePatrol()
    {
        if(patrol.Length<2){nextPoint=0;return;}
        int target=nextPoint+patrolStep;
        if(target<0||target>=patrol.Length){patrolStep=-patrolStep;target=nextPoint+patrolStep;}
        nextPoint=Mathf.Clamp(target,0,patrol.Length-1);
    }

    int ClosestPatrolIndex()
    {
        int result = 0; float distance = float.PositiveInfinity;
        for (int i = 0; i < patrol.Length; i++)
            if ((patrol[i] - body.position).sqrMagnitude < distance)
            { result = i; distance = (patrol[i] - body.position).sqrMagnitude; }
        return result;
    }

    void CatchPlayer()
    {
        Vector2 checkpoint = world.SafePoint(floor);
        float nearest = float.PositiveInfinity;
        foreach (Vector2 candidate in patrol)
        {
            float distance = Vector2.Distance(candidate, player.transform.position);
            if (Vector2.Distance(candidate, body.position) >= 6f && distance < nearest)
            { checkpoint = candidate; nearest = distance; }
        }
        protectedUntil = Time.time + ProtectionSeconds;
        awareness = 0;
        state = Behaviour.Patrol;
        stopUntil = Time.time + 2f;
        nextPoint = ClosestPatrolIndex();AdvancePatrol();
        world.RegisterCapture();
        if(Warnings>=3){StatusText="Expulsado del edificio";Message?.Invoke("El intendente te expulsó tras tres capturas.");manager.ShowFeedback(StatusText);manager.FailLevel();return;}
        if (!manager.IsFailed) world.TeleportPlayer(checkpoint);
        StatusText = "Amonestación · protección temporal";
        Message?.Invoke(Warnings==1?"El edificio está cerrando. Debes salir.":"Última advertencia, Galindo.");
    }

    void ChangeFloor(int desiredFloor)
    {
        awareness = 0;
        if (state != Behaviour.ChangingFloor || travelFloor != desiredFloor)
        {
            state = Behaviour.ChangingFloor; travelFloor = desiredFloor;
            stateUntil = Time.time + 6f;
            StatusText = "El conserje se dirige a la escalera";
            Message?.Invoke("El conserje está buscando la escalera. Aprovecha para investigar.");
        }
        if (animator != null) animator.speed = 0;
        if (Time.time < stateUntil) return;
        Vector2[] newPatrol = world.PatrolPoints(desiredFloor) ?? Array.Empty<Vector2>();
        Vector2 arrival = newPatrol.Length > 0 ? newPatrol[0] : world.SafePoint(desiredFloor);
        foreach (var point in newPatrol) if (point.x > arrival.x) arrival = point;
        if (Vector2.Distance(player.transform.position, arrival) < 9f) return;
        floor = desiredFloor; patrol = newPatrol; body.position = arrival;
        actor.transform.position = arrival;
        nextPoint = ClosestPatrolIndex();
        state = Behaviour.Patrol; travelFloor = -1; protectedUntil = Time.time + 2;
        facing = Vector2.left; stopUntil = Time.time + .6f;
        StatusText = "El conserje llegó al " + (floor == 0 ? "primer piso" : "segundo piso");
        Message?.Invoke(StatusText + ". Busca cobertura.");
    }

    void AnimateWalk(Vector2 direction)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return;
        animator.speed = state == Behaviour.Chasing ? 1.25f : .8f;
        string side = Mathf.Abs(direction.x) > Mathf.Abs(direction.y)
            ? (direction.x > 0 ? "Right" : "Left") : (direction.y > 0 ? "Up" : "Down");
        if (!animator.GetCurrentAnimatorStateInfo(0).IsName("Walk" + side)) animator.Play("Base Layer.Walk" + side);
    }

    void CreateFlashlight()
    {
        var go = new GameObject("Cono visible de linterna"); go.transform.SetParent(actor.transform, false);
        fanTransform = go.transform;
        fanMesh = new Mesh { name = "Linterna del conserje" };
        go.AddComponent<MeshFilter>().sharedMesh = fanMesh;
        fanRenderer = go.AddComponent<MeshRenderer>(); fanRenderer.sortingOrder = 1;
        fanMaterial = new Material(Shader.Find("Sprites/Default")); fanRenderer.sharedMaterial = fanMaterial;
        for (int i = 0; i < FanSegments; i++)
        { fanTriangles[i * 3] = 0; fanTriangles[i * 3 + 1] = i + 1; fanTriangles[i * 3 + 2] = i + 2; }
    }

    void UpdatePresentation()
    {
        bool sameFloor = world.FloorAt(player.transform.position) == floor;
        label.gameObject.SetActive(sameFloor && Vector2.Distance(player.transform.position, body.position) < 12);
        label.text = "Conserje" + (state == Behaviour.Chasing ? " · !" : state == Behaviour.Investigating || awareness > .1f ? " · ?" : "");
        label.color = state == Behaviour.Chasing ? new Color(1, .4f, .35f) : new Color(1, .85f, .45f);
        fanRenderer.enabled = sameFloor && state != Behaviour.ChangingFloor;
        if (Time.time < nextFanUpdate) return;
        nextFanUpdate = Time.time + .10f;
        float heading = Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg;
        fanVertices[0] = Vector3.zero;
        Color color = state == Behaviour.Chasing ? new Color(1, .30f, .15f, .22f) : new Color(1, .90f, .45f, .20f);
        fanColors[0] = color;
        for (int i = 0; i <= FanSegments; i++)
        {
            float angle = (heading - SightAngle * .5f + SightAngle * i / FanSegments) * Mathf.Deg2Rad;
            Vector2 ray = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            float length = StaticDistance(body.position, ray, SightRange);
            fanVertices[i + 1] = ray * length;
            fanColors[i + 1] = new Color(color.r, color.g, color.b, color.a * .48f);
        }
        fanMesh.Clear(); fanMesh.vertices = fanVertices; fanMesh.triangles = fanTriangles; fanMesh.colors = fanColors;
        fanMesh.RecalculateBounds();
        fanTransform.localPosition = Vector3.zero;
    }

    void OnDestroy()
    {
        if (actor != null) Destroy(actor);
        if (fanMesh != null) Destroy(fanMesh);
        if (fanMaterial != null) Destroy(fanMaterial);
    }
}
