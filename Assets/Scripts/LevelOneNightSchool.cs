using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>Dos plantas, aulas accesibles y escaleras físicas. El decorado se reconstruye en edición y juego.</summary>
[ExecuteAlways, DefaultExecutionOrder(-500)]
public sealed class LevelOneNightSchool : MonoBehaviour
{
    public static LevelOneNightSchool Active { get; private set; }
    public float roundSeconds=300;
    public LevelOneNightPalette palette;
    public Material surfaceMaterial;
    [Tooltip("Grid guardado en la escena. Sus Tilemaps se editan directamente y no se regeneran al jugar.")]
    public GameObject authoredLayout;
    public LevelOneCaretaker Caretaker { get; private set; }
    public LevelOneCaretaker SecondCaretaker { get; private set; }
    public LevelOneCaretaker[] Watchmen { get; private set; }
    public LevelOneCaretaker MostAlertGuard(int floor)
    {
        LevelOneCaretaker best=null;if(Watchmen==null)return null;
        foreach(var guard in Watchmen)if(guard!=null&&guard.AssignedFloor==floor&&(best==null||guard.Suspicion01>best.Suspicion01))best=guard;
        return best;
    }
    public int TotalWarnings { get; private set; }
    public float SafeUntil { get; private set; }
    public void RegisterCapture(){TotalWarnings++;SafeUntil=Time.time+8;}
    public LevelOneStairTraversal Stairs { get; private set; }
    public const float FloorSpacing=32;
    static readonly float[] RoomCenters={10,28,46,64};
    readonly List<Rect> hiddenRooms=new List<Rect>();
    readonly Dictionary<Sprite,Sprite> tiledSprites=new Dictionary<Sprite,Sprite>();
    GameObject generated;
    PlayerMovement player;
    Level01Manager manager;
    Light2D playerLight;
    Sprite square;
    Texture2D squareTexture;
    const HideFlags GeneratedFlags=HideFlags.DontSaveInEditor|HideFlags.DontSaveInBuild;

    void OnEnable(){Active=this;Rebuild();}
    void OnValidate()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.delayCall-=RebuildIfActive;
        UnityEditor.EditorApplication.delayCall+=RebuildIfActive;
#endif
    }
    void RebuildIfActive(){if(this!=null&&isActiveAndEnabled)Rebuild();}

    [ContextMenu("Reconstruir edificio de dos pisos")]
    public void Rebuild()
    {
        if(!gameObject.scene.IsValid())return;
        if(authoredLayout!=null)
        {
            ClearGenerated();hiddenRooms.Clear();
            for(int floor=0;floor<2;floor++)foreach(float center in RoomCenters)
                hiddenRooms.Add(new Rect(center-5.3f,floor*FloorSpacing+7,10.6f,8.5f));
            return;
        }
        if(palette==null)palette=Resources.Load<LevelOneNightPalette>("LevelOneNightPalette");
        if(palette==null)return;
        ClearGenerated();hiddenRooms.Clear();
        generated=NewObject("Edificio nocturno · dos plantas",transform);
        // Un fondo continuo permite centrar la cámara incluso junto a los bordes.
        Tiled("Campus nocturno",null,new Rect(-32,-24,144,88),new Color(.055f,.095f,.12f),-60);
        var campus=GetComponent<LevelOneCampusBackdrop>();
        var grass=campus!=null?campus.grassTile as UnityEngine.Tilemaps.Tile:null;
        if(grass!=null&&grass.sprite!=null)
            Tiled("Jardines de noche",grass.sprite,new Rect(-32,-24,144,88),new Color(.18f,.25f,.32f),-59);
        if(campus!=null&&campus.trees!=null&&campus.trees.Length>0)
            for(int x=-20;x<100;x+=8)
            {
                var tree=campus.trees[(x+20)/8%campus.trees.Length];if(tree==null)continue;
                Prop("Árbol exterior",tree,new Vector2(x,-11),2.6f,-48,new Color(.22f,.30f,.38f));
                Prop("Árbol exterior",tree,new Vector2(x,53),2.6f,-48,new Color(.22f,.30f,.38f));
            }
        for(int floor=0;floor<2;floor++)BuildFloor(floor);
        Tiled("Acceso a la salida",palette.floor,new Rect(-13,-3.3f,6,5),new Color(.40f,.47f,.53f),-10);
        Wall("Cierre exterior oeste",new Rect(-13.3f,-3.5f,.3f,5.5f));
        Wall("Cierre exterior sur",new Rect(-13,-3.5f,6,.3f));
        Wall("Cierre exterior norte",new Rect(-13,1.7f,6,.3f));
        bool hasGlobal=false;
        foreach(var light in FindObjectsByType<Light2D>(FindObjectsSortMode.None))
            if(light.lightType==Light2D.LightType.Global)hasGlobal=true;
        if(!hasGlobal)
        {
            var global=NewObject("Iluminación nocturna",generated.transform).AddComponent<Light2D>();
            global.lightType=Light2D.LightType.Global;global.intensity=.55f;global.color=new Color(.74f,.82f,1);
        }
        Physics2D.SyncTransforms();
    }

    void BuildFloor(int floor)
    {
        float y=floor*FloorSpacing;
        Color hall=new Color(.62f,.69f,.80f);
        Tiled("Pasillo "+(floor==0?"planta baja":"segundo piso"),palette.floor,new Rect(-7.2f,y-5.2f,82.2f,10.4f),hall,-10);
        Tiled("Franja central del pasillo",palette.floor,new Rect(-7.2f,y-.85f,82.2f,1.7f),new Color(.29f,.48f,.66f),-9);
        Wall("Muro sur",new Rect(-7.2f,y-5.45f,82.4f,.35f));
        if(floor==0)
        {
            Wall("Muro acceso inferior",new Rect(-7.4f,y-5.3f,.35f,2.8f));
            Wall("Muro acceso superior",new Rect(-7.4f,y+.55f,.35f,4.85f));
            Label("Salida",new Vector2(-5.9f,-.2f),new Color(.65f,1,.75f),1.9f);
        }
        else Wall("Muro oeste segundo piso",new Rect(-7.4f,y-5.3f,.35f,10.7f));
        Wall("Muro este inferior",new Rect(74.85f,y-5.3f,.35f,3.3f));
        Wall("Muro este superior",new Rect(74.85f,y+1.1f,.35f,4.3f));
        float cursor=-7.2f;
        foreach(float center in RoomCenters)
        {
            Wall("Pared entre aulas",new Rect(cursor,y+5.2f,center-1.35f-cursor,.35f));
            BuildRoom(floor,center);
            cursor=center+1.35f;
        }
        Wall("Fin de pared norte",new Rect(cursor,y+5.2f,75-cursor,.35f));
        Label(floor==0?"Planta baja · Fila Azul":"Segundo piso · Fila Verde",new Vector2(2,y-4.1f),new Color(.64f,.76f,.9f),2.2f);
        BuildStairs(floor);
        for(float x=-1;x<73;x+=15)
        {
            if(palette.lamp!=null)Prop("Lámpara del pasillo",palette.lamp,new Vector2(x,y+4.3f),.30f,-1,new Color(.62f,.70f,.83f));
            Tiled("Luz tenue del pasillo",null,new Rect(x-1.2f,y+3.8f,2.4f,.12f),new Color(.90f,.77f,.45f),0);
        }
    }

    void BuildRoom(int floor,float center)
    {
        float y=floor*FloorSpacing;
        int number=floor*100+101+System.Array.IndexOf(RoomCenters,center);
        var room=new Rect(center-6,y+5.35f,12,10);
        Tiled("Salón "+number,palette.floor,room,new Color(.31f,.37f,.48f),-10);
        Wall("Pared izquierda aula "+number,new Rect(room.xMin-.20f,room.yMin,.30f,room.height));
        Wall("Pared derecha aula "+number,new Rect(room.xMax-.10f,room.yMin,.30f,room.height));
        Wall("Pared del fondo aula "+number,new Rect(room.xMin,room.yMax-.15f,room.width,.35f));
        // Las hojas quedan junto al marco; el hueco de la puerta permanece transitable.
        if(palette.door!=null)
        {
            var door=Prop("Puerta abierta aula "+number,palette.door,new Vector2(center+1.1f,y+5.75f),.80f,1,new Color(.56f,.62f,.74f));
            door.transform.rotation=Quaternion.Euler(0,0,-70);
        }
        Label("Salón "+number,new Vector2(center,y+4.7f),new Color(.80f,.87f,1),1.8f);
        Tiled("Pizarrón aula "+number,palette.board,new Rect(center-2.5f,y+14.05f,5,.7f),new Color(.05f,.19f,.17f),0);
        Label("Apaga la luz al salir",new Vector2(center,y+14.45f),new Color(.55f,.73f,.65f),1.35f);
        for(int row=0;row<2;row++)
            for(int col=0;col<3;col++)
            {
                Vector2 spot=new Vector2(center+(col-1)*3.4f,y+9.3f+row*2.7f);
                if(palette.desk!=null)Prop("Pupitre",palette.desk,spot,1.55f,0,new Color(.58f,.62f,.72f));
                Solid("Colisión del pupitre",new Rect(spot.x-.74f,spot.y-.32f,1.48f,.62f));
                if(palette.chair!=null)Prop("Silla",palette.chair,spot+Vector2.down*.90f,.65f,1,new Color(.58f,.64f,.77f));
            }
        // Solo el interior, más allá del marco, oculta al jugador. La puerta no es un escondite.
        hiddenRooms.Add(new Rect(center-5.9f,y+7.0f,11.8f,9.0f));
        Label("Zona de sombra",new Vector2(center-3.3f,y+6.7f),new Color(.47f,.64f,.77f),1.6f);
    }

    void BuildStairs(int floor)
    {
        float y=floor*FloorSpacing;
        Tiled("Primer tramo de escalera",palette.stairs,new Rect(75,y-2,3,10),new Color(.57f,.66f,.80f),-8);
        Tiled("Descanso de escalera",palette.floor,new Rect(75,y+8,10,3.2f),new Color(.65f,.71f,.82f),-8);
        Tiled("Segundo tramo de escalera",palette.stairs,new Rect(82,y-2,3,10),new Color(.57f,.66f,.80f),-8);
        for(int step=0;step<12;step++)
        {
            float sy=y-1.65f+step*.78f;
            Tiled("Borde de peldaño",null,new Rect(75.08f,sy,2.84f,.07f),new Color(.14f,.21f,.33f),-5);
            Tiled("Borde de peldaño",null,new Rect(82.08f,sy,2.84f,.07f),new Color(.14f,.21f,.33f),-5);
        }
        Wall("Separador de la escalera en U",new Rect(78,y-2.3f,4,10.3f));
        Wall("Barandal exterior izquierdo",new Rect(74.72f,y+1.1f,.28f,10.2f));
        Wall("Barandal exterior derecho",new Rect(85,y-2.3f,.28f,13.6f));
        Wall("Barandal del descanso",new Rect(74.72f,y+11.1f,10.55f,.28f));
        Wall("Cierre de tramo derecho",new Rect(82,y-2.3f,3.28f,.25f));
        Wall("Cierre de tramo izquierdo",new Rect(74.75f,y-2.3f,3.28f,.25f));
        Label(floor==0?"Subir · Escalera en U":"Bajar · Escalera en U",new Vector2(70.5f,y+1.3f),new Color(1,.85f,.50f),2.0f);
        Label("1",new Vector2(76.5f,y+4.5f),new Color(1,.9f,.65f),3);
        Label("2",new Vector2(83.5f,y+4.5f),new Color(1,.9f,.65f),3);
    }

    public void ConfigureRuntime(Level01Manager levelManager,PlayerMovement actor,U2GameConfig config)
    {
        manager=levelManager;player=actor;Active=this;
        if(generated==null)Rebuild();
        Watchmen=new LevelOneCaretaker[4];
        for(int floor=0;floor<2;floor++)for(int direction=0;direction<2;direction++)
        {
            var guard=gameObject.AddComponent<LevelOneCaretaker>();guard.AssignedFloor=floor;
            guard.PatrolDirection=direction==0?1:-1;guard.PatrolLaneOffset=direction==0?-.65f:.65f;
            guard.Configure(manager,player,this,config);guard.Message+=manager.ShowFeedback;
            Watchmen[floor*2+direction]=guard;
        }
        Caretaker=Watchmen[0];SecondCaretaker=Watchmen[2];
        gameObject.AddComponent<LevelOneDepthSetup>();
        Stairs=gameObject.AddComponent<LevelOneStairTraversal>();Stairs.Configure(this,manager,player);
        Stairs.Message+=manager.ShowFeedback;
        foreach(var light in FindObjectsByType<Light2D>(FindObjectsSortMode.None))
            if(light.lightType==Light2D.LightType.Global)light.intensity=authoredLayout!=null?.68f:.55f;
        var glow=new GameObject("Visibilidad del jugador de noche");glow.transform.SetParent(player.transform,false);
        playerLight=glow.AddComponent<Light2D>();playerLight.lightType=Light2D.LightType.Point;
        playerLight.color=new Color(.75f,.84f,1);playerLight.intensity=.8f;
        playerLight.pointLightInnerRadius=.7f;playerLight.pointLightOuterRadius=4;
    }

    void Update()
    {
        if(Application.isPlaying&&playerLight!=null&&player!=null)
        {
            bool hidden=IsHidden(player.transform.position)&&!player.IsRunning;
            playerLight.pointLightOuterRadius=hidden?1.6f:4;
            playerLight.intensity=hidden?.25f:.8f;
        }
    }

    public bool IsHidden(Vector2 position)
    {foreach(var room in hiddenRooms)if(room.Contains(position))return true;return false;}
    public int FloorAt(Vector2 position)
    {if(position.x>=75&&position.x<=85.5f)return -1;return position.y>=16?1:0;}
    public Vector2 SafePoint(int floor)=>new Vector2(0,Mathf.Clamp(floor,0,1)*FloorSpacing-.5f);
    public Vector2[] PatrolPoints(int floor)
    {
        float y=Mathf.Clamp(floor,0,1)*FloorSpacing-.5f;
        return new[]{new Vector2(-3,y),new Vector2(12,y),new Vector2(29,y),new Vector2(46,y),new Vector2(63,y),new Vector2(72,y)};
    }
    public Vector2 CluePoint(int index)
    {return index==0?new Vector2(10,7.8f):index==1?new Vector2(54,31.1f):new Vector2(46,39.8f);}
    public bool TryGetWorldPrompt(out string text)
    {
        text=null;if(player==null||manager==null||!manager.IsPlaying)return false;
        Vector2 p=player.transform.position;
        if(FloorAt(p)<0){text="Escalera en U\nRecorre ambos tramos y el descanso";return true;}
        if(IsHidden(p)){text=player.IsRunning?"Salón oscuro\nDeja de correr para ocultarte":"Salón oscuro · Estás oculto\nCamina con cuidado al salir";return true;}
        float localY=p.y-Mathf.Max(0,FloorAt(p))*FloorSpacing;
        bool doorway=false;foreach(float center in RoomCenters)if(Mathf.Abs(p.x-center)<1.35f&&localY>5.5f&&localY<7.0f)doorway=true;
        if(doorway){text="Salón oscuro\nAléjate de la puerta para ocultarte";return true;}
        return false;
    }
    public void TeleportPlayer(Vector2 destination)
    {
        if(player==null)return;
        var body=player.GetComponent<Rigidbody2D>();if(body!=null){body.linearVelocity=Vector2.zero;body.position=destination;}
        player.transform.position=new Vector3(destination.x,destination.y,player.transform.position.z);
        Physics2D.SyncTransforms();
    }

    GameObject NewObject(string name,Transform parent)
    {var go=new GameObject(name){hideFlags=GeneratedFlags};go.transform.SetParent(parent,false);return go;}
    Sprite Square()
    {
        if(square!=null)return square;
        squareTexture=new Texture2D(1,1){hideFlags=HideFlags.HideAndDontSave,filterMode=FilterMode.Point};
        squareTexture.SetPixel(0,0,Color.white);squareTexture.Apply();
        square=Sprite.Create(squareTexture,new Rect(0,0,1,1),new Vector2(.5f,.5f),1,0,SpriteMeshType.FullRect);square.hideFlags=HideFlags.HideAndDontSave;return square;
    }
    Sprite TiledSprite(Sprite source)
    {
        if(source==null)return Square();
        if(tiledSprites.TryGetValue(source,out var result)&&result!=null)return result;
        // Los sprites importados usan malla Tight. Una copia rectangular permite
        // repetirlos sin modificar los recursos compartidos con los otros niveles.
        Rect rect=source.rect;
        result=Sprite.Create(source.texture,rect,new Vector2(source.pivot.x/rect.width,source.pivot.y/rect.height),
            source.pixelsPerUnit,0,SpriteMeshType.FullRect);
        result.name=source.name+" · mosaico rectangular";result.hideFlags=HideFlags.HideAndDontSave;
        tiledSprites[source]=result;return result;
    }
    GameObject Tiled(string name,Sprite sprite,Rect rect,Color color,int order)
    {
        var go=NewObject(name,generated.transform);go.transform.position=rect.center;
        var renderer=go.AddComponent<SpriteRenderer>();renderer.sprite=TiledSprite(sprite);renderer.color=color;
        renderer.sortingOrder=order;if(surfaceMaterial!=null)renderer.sharedMaterial=surfaceMaterial;
        renderer.drawMode=SpriteDrawMode.Tiled;
        float scale=sprite!=null?1/Mathf.Max(.01f,sprite.bounds.size.x):1;
        go.transform.localScale=Vector3.one*scale;renderer.size=rect.size/scale;return go;
    }
    GameObject Prop(string name,Sprite sprite,Vector2 position,float width,int order,Color color)
    {
        var go=NewObject(name,generated.transform);go.transform.position=position;
        var renderer=go.AddComponent<SpriteRenderer>();renderer.sprite=sprite;renderer.sortingOrder=order;renderer.color=color;
        if(surfaceMaterial!=null)renderer.sharedMaterial=surfaceMaterial;
        go.transform.localScale=Vector3.one*(width/Mathf.Max(.01f,sprite.bounds.size.x));return go;
    }
    void Wall(string name,Rect rect)
    {if(rect.width<=0||rect.height<=0)return;Tiled(name,palette.wall,rect,new Color(.36f,.43f,.57f),-4);Solid(name+" · colisión",rect);}
    void Solid(string name,Rect rect)
    {var go=NewObject(name,generated.transform);go.transform.position=rect.center;var box=go.AddComponent<BoxCollider2D>();box.size=rect.size;}
    void Label(string text,Vector2 position,Color color,float size)
    {
        var go=NewObject(text,generated.transform);go.transform.position=position;var label=go.AddComponent<TextMeshPro>();
        if(LevelOneTypography.Font!=null)label.font=LevelOneTypography.Font;
        label.text=text;label.fontSize=size;label.color=color;label.alignment=TextAlignmentOptions.Center;
        label.rectTransform.sizeDelta=new Vector2(6,.7f);label.renderer.sortingOrder=1;
    }
    void ClearGenerated()
    {
        // Recupera también hijos transitorios si Unity recarga los scripts.
        for(int i=transform.childCount-1;i>=0;i--)
        {
            var child=transform.GetChild(i).gameObject;
            if(child!=generated&&(child.name!="Edificio nocturno · dos plantas"||(child.hideFlags&GeneratedFlags)==0))continue;
            child.SetActive(false);if(Application.isPlaying)Destroy(child);else DestroyImmediate(child);
        }
        generated=null;
    }
    void OnDisable()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.delayCall-=RebuildIfActive;
#endif
        ClearGenerated();if(Active==this)Active=null;
        foreach(var sprite in tiledSprites.Values)if(sprite!=null){if(Application.isPlaying)Destroy(sprite);else DestroyImmediate(sprite);}
        tiledSprites.Clear();
        if(square!=null){if(Application.isPlaying)Destroy(square);else DestroyImmediate(square);}
        if(squareTexture!=null){if(Application.isPlaying)Destroy(squareTexture);else DestroyImmediate(squareTexture);}
    }
}
