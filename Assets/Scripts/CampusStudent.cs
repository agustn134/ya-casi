using UnityEngine;
/// <summary>Estudiante que recorre una fila, cede ante obstáculos y deja libre el paso al esperar.</summary>
public sealed class CampusStudent : MonoBehaviour
{
    public Vector2[] route;
    public float speed=1.7f;
    Level01Manager manager; Rigidbody2D body;Animator animator;SpriteRenderer sprite;
    int next;float blockedTime;float wait;
    readonly RaycastHit2D[] hits=new RaycastHit2D[12];
    public void Initialize(RuntimeAnimatorController controller,Sprite initial,Color tint)
    {
        manager=FindFirstObjectByType<Level01Manager>();
        sprite=gameObject.AddComponent<SpriteRenderer>();sprite.sprite=initial;sprite.color=tint;sprite.sortingOrder=2;
        animator=gameObject.AddComponent<Animator>();animator.runtimeAnimatorController=controller;
        body=gameObject.AddComponent<Rigidbody2D>();body.gravityScale=0;body.freezeRotation=true;body.bodyType=RigidbodyType2D.Kinematic;body.useFullKinematicContacts=true;body.interpolation=RigidbodyInterpolation2D.Interpolate;
        var col=gameObject.AddComponent<CapsuleCollider2D>();col.size=new Vector2(.55f,.4f);col.offset=new Vector2(0,-.45f);
    }
    void FixedUpdate()
    {
        if(manager==null||!manager.IsPlaying||route==null||route.Length<2){if(animator!=null)animator.speed=0;return;}
        if(wait>0){wait-=Time.fixedDeltaTime;return;}
        Vector2 target=route[next];Vector2 delta=target-body.position;
        if(delta.magnitude<.15f){next=(next+1)%route.Length;return;}
        Vector2 direction=delta.normalized;
        var filter=ContactFilter2D.noFilter;filter.useTriggers=false;
        int count=body.Cast(direction,filter,hits,.18f);bool blocked=false;
        for(int i=0;i<count;i++)if(hits[i].collider!=null&&hits[i].collider.GetComponent<CampusStudent>()==null){blocked=true;break;}
        if(blocked)
        {
            blockedTime+=Time.fixedDeltaTime;animator.speed=0;
            // Una fila nunca bloquea para siempre un casillero o la salida.
            if(blockedTime>1.1f){next=(next+1)%route.Length;blockedTime=0;wait=.35f;}
            return;
        }
        blockedTime=0;animator.speed=.8f;string side=Mathf.Abs(direction.x)>Mathf.Abs(direction.y)?(direction.x>0?"Right":"Left"):(direction.y>0?"Up":"Down");
        if(!animator.GetCurrentAnimatorStateInfo(0).IsName("Walk"+side))animator.Play("Base Layer.Walk"+side);
        body.MovePosition(Vector2.MoveTowards(body.position,target,speed*Time.fixedDeltaTime));
    }
    void OnCollisionEnter2D(Collision2D collision)
    {
        var p=collision.gameObject.GetComponent<PlayerMovement>();if(p==null)return;
        var upgrade=FindFirstObjectByType<LevelOneUpgrade>();if(upgrade!=null)upgrade.BumpPlayer();
    }
}
