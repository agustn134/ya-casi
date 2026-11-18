using System.Collections.Generic;
using UnityEngine;

/// <summary>Desalojo escalonado de ambas plantas y puertas automáticas animadas.</summary>
public sealed class LevelOneClosingFlow : MonoBehaviour
{
    sealed class Departure { public CampusStudent actor; public float at; public int phase; public Vector2 end; public bool upstairs; }
    sealed class Door { public Transform pivot; public Quaternion closed; public float open; public Vector2 position; }
    readonly List<Departure> crowd=new List<Departure>();
    readonly List<Door> doors=new List<Door>();
    Level01Manager manager;PlayerMovement player;float elapsed;
    void Start()
    {
        manager=GetComponent<Level01Manager>();player=FindFirstObjectByType<PlayerMovement>();
        var config=Resources.Load<U2GameConfig>("U2GameConfig");
        var camera=Camera.main;if(camera!=null)camera.orthographicSize=4.3f;
        if(config==null||config.studentControllers==null||config.studentIdleSprites==null||config.studentControllers.Length==0||config.studentIdleSprites.Length==0)return;
        for(int floor=0;floor<2;floor++)for(int i=0;i<36;i++)
        {
            Vector2 at=new Vector2(-2+(i/3)*6, floor*32+(i%3-1)*1.15f);
            if(Physics2D.OverlapCapsule(at+Vector2.down*.45f,new Vector2(.7f,.5f),CapsuleDirection2D.Vertical,0)!=null)continue;
            int look=i%Mathf.Min(config.studentControllers.Length,config.studentIdleSprites.Length);
            var go=new GameObject("Alumno de salida "+floor+" · "+i);go.transform.SetParent(transform);go.transform.position=at;
            var actor=go.AddComponent<CampusStudent>();actor.Initialize(config.studentControllers[look],config.studentIdleSprites[look],Color.white);
            actor.speed=1.25f+(i%5)*.15f;
            crowd.Add(new Departure{actor=actor,at=5+(35-i)*3.2f+(i%3)*.6f,upstairs=floor==1});
            actor.SetRoute(new[]{at,at+Vector2.right*.18f});
        }
        // El Grid es hermano del gestor, por lo que sus puertas se buscan en la escena.
        var world=LevelOneNightSchool.Active;
        if(world!=null&&world.authoredLayout!=null)
        foreach(var leaf in world.authoredLayout.GetComponentsInChildren<SpriteRenderer>())
        {
            if(!leaf.name.StartsWith("Puerta abierta"))continue;
            var hinge=new GameObject("Bisagra animada").transform;hinge.SetParent(leaf.transform.parent);
            Vector2 p=leaf.transform.position;hinge.position=p+Vector2.right*.35f;
            leaf.transform.SetParent(hinge,false);leaf.transform.localPosition=Vector3.left*1.35f;leaf.transform.localRotation=Quaternion.identity;
            leaf.transform.localScale=new Vector3(2.7f/leaf.sprite.bounds.size.x,.32f/leaf.sprite.bounds.size.y,1);
            doors.Add(new Door{pivot=hinge,closed=Quaternion.identity,position=p-Vector2.right});
        }
    }
    void Update()
    {
        if(manager==null||!manager.IsPlaying)return;elapsed+=Time.deltaTime;
        foreach(var d in crowd)
        {
            if(d.actor==null)continue;
            Vector2 p=d.actor.transform.position;
            if(d.phase==0&&elapsed>=d.at)
            {
                if(d.upstairs){d.end=new Vector2(83.5f,31);d.actor.SetRoute(new[]{p,new Vector2(p.x,31.5f),new Vector2(76.5f,31.5f),new Vector2(76.5f,41.4f),new Vector2(83.5f,41.4f),d.end},true);d.phase=1;}
                else Exit(d,p);
            }
            else if(d.phase==1&&Vector2.Distance(p,d.end)<.6f)
            {
                var body=d.actor.GetComponent<Rigidbody2D>();body.position=new Vector2(72.5f,-.5f);body.linearVelocity=Vector2.zero;d.actor.transform.position=body.position;Exit(d,body.position);
            }
            else if(d.phase==2&&Vector2.Distance(p,d.end)<.6f){Destroy(d.actor.gameObject);d.phase=3;}
        }
        foreach(var door in doors)
        {
            if(door.pivot==null||player==null)continue;
            bool near=Vector2.Distance(player.transform.position,door.position)<3;
            door.open=Mathf.MoveTowards(door.open,near?1:0,Time.deltaTime*2.5f);
            door.pivot.localRotation=door.closed*Quaternion.Euler(0,0,Mathf.Lerp(0,-88,door.open));
        }
    }
    void Exit(Departure d,Vector2 p)
    {
        d.end=new Vector2(-11,-1.1f);
        d.actor.SetRoute(new[]{p,new Vector2(p.x,-.7f),new Vector2(-5,-.7f),new Vector2(-8,-1.1f),d.end},true);
        var gate=GameObject.Find("SalidaNivel01");if(gate!=null)foreach(var collider in gate.GetComponentsInChildren<Collider2D>())Physics2D.IgnoreCollision(d.actor.GetComponent<Collider2D>(),collider);
        d.phase=2;
    }
}
