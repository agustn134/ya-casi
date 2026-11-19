using UnityEngine;
/// <summary>Estudiante que recorre una fila, cede ante obstáculos y deja libre el paso al esperar.</summary>
public sealed class CampusStudent : MonoBehaviour
{
    public Vector2[] route;
    public float speed=1.7f;
    Level01Manager manager; Rigidbody2D body;Animator animator;SpriteRenderer sprite;
    int next;float blockedTime;float wait;
    Vector2? courtesyTarget;

    Sprite[] characterFrames;
    int facing = 3;
    float walkCycle;

    public void UseCharacterSheet(Texture2D sheet)
    {
        const int frameWidth = 16;
        const int frameHeight = 32;
        sheet.filterMode = FilterMode.Point;
        animator.enabled = false;
        sprite.color = Color.white;
        characterFrames = new Sprite[28];
        for (int direction = 0; direction < 4; direction++)
        {
            characterFrames[direction * 7] = Sprite.Create(sheet,
                new Rect(direction * frameWidth, frameHeight, frameWidth, frameHeight),
                new Vector2(.5f, .5f), 16f);
            for (int frame = 0; frame < 6; frame++)
                characterFrames[direction * 7 + frame + 1] = Sprite.Create(sheet,
                    new Rect((direction * 6 + frame) * frameWidth, 0, frameWidth, frameHeight),
                    new Vector2(.5f, .5f), 16f);
        }
        ShowIdle();
    }

    void ShowIdle()
    {
        if (characterFrames != null) sprite.sprite = characterFrames[facing * 7];
        if (animator != null) animator.speed = 0f;
    }

    void OnDestroy()
    {
        if (characterFrames == null) return;
        foreach (var frame in characterFrames) Destroy(frame);
    }

    public void TryMakeRoom(Vector2 playerPosition)
    {
        if (body == null || courtesyTarget.HasValue) return;
        Vector2 away = body.position - playerPosition;
        if (away.sqrMagnitude < .001f) return;
        away.Normalize();
        Vector2 side = new Vector2(-away.y, away.x);
        if (Random.value < .5f) side = -side;

        var filter = ContactFilter2D.noFilter;
        filter.useTriggers = false;
        const float clearance = .72f;
        if (body.Cast(side, filter, hits, clearance) > 0) side = -side;
        if (body.Cast(side, filter, hits, clearance) > 0) return;

        wait = 0f;
        blockedTime = 0f;
        courtesyTarget = body.position + side * .65f;
    }
    readonly RaycastHit2D[] hits=new RaycastHit2D[12];
    public void Initialize(RuntimeAnimatorController controller,Sprite initial,Color tint)
    {
        manager=FindFirstObjectByType<Level01Manager>();
        sprite=gameObject.AddComponent<SpriteRenderer>();sprite.sprite=initial;sprite.color=tint;sprite.sortingOrder=2;
        animator=gameObject.AddComponent<Animator>();animator.runtimeAnimatorController=controller;
        if (controller != null)
        {
            animator.Play("Base Layer.IdleDown", 0, 0f);
            animator.Update(0f);
        }
        body=gameObject.AddComponent<Rigidbody2D>();body.gravityScale=0;body.freezeRotation=true;body.bodyType=RigidbodyType2D.Kinematic;body.useFullKinematicContacts=true;body.interpolation=RigidbodyInterpolation2D.Interpolate;
        var col=gameObject.AddComponent<CapsuleCollider2D>();col.size=new Vector2(.55f,.4f);col.offset=new Vector2(0,-.45f);
    }
    void FixedUpdate()
    {
        if(manager==null||!manager.IsPlaying||route==null||route.Length<2){ShowIdle();return;}
        if(wait>0){wait-=Time.fixedDeltaTime;return;}
        Vector2 target = courtesyTarget ?? route[next]; Vector2 delta = target - body.position;
        if (delta.magnitude < .15f)
        {
            if (courtesyTarget.HasValue)
            {
                courtesyTarget = null;
                wait = .6f;
                ShowIdle();
            }
            else next = (next + 1) % route.Length;
            return;
        }
        Vector2 direction=delta.normalized;
        var filter=ContactFilter2D.noFilter;filter.useTriggers=false;
        int count=body.Cast(direction,filter,hits,.18f);bool blocked=false;
        for(int i=0;i<count;i++)if(hits[i].collider!=null&&hits[i].collider.GetComponent<CampusStudent>()==null){blocked=true;break;}
        if(blocked)
        {
            if (courtesyTarget.HasValue)
            {
                courtesyTarget = null;
                ShowIdle();
                return;
            }
            blockedTime+=Time.fixedDeltaTime;ShowIdle();
            // Una fila nunca bloquea para siempre un casillero o la salida.
            if(blockedTime>1.1f){next=(next+1)%route.Length;blockedTime=0;wait=.35f;}
            return;
        }
        blockedTime = 0;
        if (characterFrames != null)
        {
            if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
                facing = direction.x > 0 ? 0 : 2;
            else
                facing = direction.y > 0 ? 1 : 3;
            walkCycle += Time.fixedDeltaTime * 9f;
            sprite.sprite = characterFrames[facing * 7 + 1 + (Mathf.FloorToInt(walkCycle) % 6)];
        }
        else
        {
            animator.speed = .8f;
            string side = Mathf.Abs(direction.x) > Mathf.Abs(direction.y)
                ? (direction.x > 0 ? "Right" : "Left") : (direction.y > 0 ? "Up" : "Down");
            if (!animator.GetCurrentAnimatorStateInfo(0).IsName("Walk" + side))
                animator.Play("Base Layer.Walk" + side);
        }
        body.MovePosition(Vector2.MoveTowards(body.position,target,speed*Time.fixedDeltaTime));
    }
    void OnCollisionEnter2D(Collision2D collision)
    {
        var p=collision.gameObject.GetComponent<PlayerMovement>();if(p==null)return;
        var upgrade=FindFirstObjectByType<LevelOneUpgrade>();if(upgrade!=null)upgrade.BumpPlayer();
    }
}
