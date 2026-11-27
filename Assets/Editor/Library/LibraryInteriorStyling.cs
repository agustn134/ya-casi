using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

/// <summary>Ambientación editable de biblioteca y acceso sur, sobre la escena jugable existente.</summary>
[InitializeOnLoad]
public static class LibraryInteriorStyling
{
    const string Art="Assets/Art/Library/Interior/";
    const string Generated="Assets/Art/Library/Generated/";
    const string Marker="LibraryInteriorV4.txt";
    static Transform root;
    static Material material;
    static Sprite pixel;
    static TMP_FontAsset font;
    static LibraryInteriorStyling(){EditorApplication.delayCall+=AutoApply;}
    static void AutoApply()
    {
        if(File.Exists(Marker)||!File.Exists("LibraryThirtyShelvesV3.txt")||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
        Apply();
    }
    [MenuItem("Ya Casi/Nivel 2/Mejorar interior y puerta")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        var scene=SceneManager.GetSceneByPath(LibraryLevelBuilder.Path);
        bool close=!scene.isLoaded;
        if(close)scene=EditorSceneManager.OpenScene(LibraryLevelBuilder.Path,OpenSceneMode.Additive);
        if(scene.isDirty){Debug.LogWarning("Guarda la biblioteca antes de aplicar la decoración.");return;}
        if(scene.GetRootGameObjects().Any(g=>g.name=="Decoración Biblioteca")){Debug.Log("La decoración ya está creada.");return;}
        if(!File.Exists("LibraryBeforeInterior.unity.bak"))File.Copy(LibraryLevelBuilder.Path,"LibraryBeforeInterior.unity.bak");
        var original=SceneManager.GetActiveScene();SceneManager.SetActiveScene(scene);
        try
        {
            foreach(string name in new[]{"Plant13","Plant14","Plant17","BookDisplay","Window","FloorLamp","Generic","Door"})Prepare(Art+name+".png");
            root=new GameObject("Decoración Biblioteca").transform;
            material=AssetDatabase.LoadAssetAtPath<Material>(Generated+"BibliotecaUnlit.mat");
            font=Resources.Load<U2GameConfig>("U2GameConfig").font;
            pixel=SolidPixel();
            var manager=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<LibraryLevelManager>()).Single();
            Floors(scene);
            Perimeter(scene);
            var plant=AssetDatabase.LoadAssetAtPath<Sprite>(Art+"Plant13.png");
            var palm=AssetDatabase.LoadAssetAtPath<Sprite>(Art+"Plant14.png");
            var narrowPlant=AssetDatabase.LoadAssetAtPath<Sprite>(Art+"Plant17.png");
            var lamp=AssetDatabase.LoadAssetAtPath<Sprite>(Art+"FloorLamp.png");
            var display=AssetDatabase.LoadAssetAtPath<Sprite>(Art+"BookDisplay.png");
            var window=AssetDatabase.LoadAssetAtPath<Sprite>(Art+"Window.png");
            foreach(var position in new[]{new Vector2(4.9f,6.7f),new Vector2(13.6f,6.7f),new Vector2(13.9f,1.5f),new Vector2(13.9f,-3.5f)})
                Prop("Maceta de biblioteca",plant,position,.6f,true);
            foreach(var position in new[]{new Vector2(6.1f,-5.1f),new Vector2(12.7f,-5.1f)})
                Prop("Palma junto a recepción",palm,position,.65f,true);
            Prop("Planta junto a salida izquierda",narrowPlant,new Vector2(-2.7f,-7.2f),.65f,true);
            Prop("Planta junto a salida derecha",narrowPlant,new Vector2(2.7f,-7.2f),.65f,true);
            foreach(float x in new[]{5f,9.5f})Prop("Lámpara de lectura",lamp,new Vector2(x,1.65f),.65f,true);
            foreach(float x in new[]{6.8f,11.3f})
            {
                Prop("Ventana de madera",window,new Vector2(x,7.65f),.65f,false).sortingOrder=-450;
                Prop("Novedades y recomendaciones",display,new Vector2(x,6.4f),.65f,true);
            }
            var generic=AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"Generic.png");
            var rug=Slice(generic,"AlfombraBiblioteca",144,64,64,48,new Vector2(.5f,.5f));
            var readingRug=Slice(generic,"AlfombraLectura",144,112,64,48,new Vector2(.5f,.5f));
            var lounge=Prop("Alfombra sala de lectura",readingRug,new Vector2(9,1.4f),1,false);
            lounge.transform.localScale=new Vector3(2.2f,2.5f,1);lounge.color=new Color(.85f,.9f,.9f);lounge.sortingOrder=-940;
            var receptionRug=Prop("Alfombra de préstamos",rug,new Vector2(9,-5.7f),1,false);
            receptionRug.transform.localScale=new Vector3(1.4f,.6f,1);receptionRug.sortingOrder=-940;
            Label("Cartel sala de lectura","SALA DE LECTURA",new Vector2(9,5.45f),new Vector2(8,.6f),3,new Color(.24f,.3f,.29f));
            foreach(var old in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<TextMeshPro>()))
            {
                if(old.gameObject.name=="SALA DE LECTURA")old.gameObject.SetActive(false);
                if(old.text=="PRÉSTAMOS") {old.text="PRÉSTAMOS · INFORMACIÓN";old.transform.position=new Vector3(9,-6.6f,-.1f);old.fontSize=2.4f;}
            }
            Doorway(manager,rug);
            EditorUtility.SetDirty(manager.exitZone);
            EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);
            File.WriteAllText(Marker,"LIBRARY_INTERIOR_READY: piso de madera, sala de lectura, plantas, lámparas, ventanas, recepción y puerta doble animada.\n");
            Debug.Log("LIBRARY_INTERIOR_READY: decoración y puerta guardadas.");
        }
        finally
        {
            if(original.isLoaded)SceneManager.SetActiveScene(original);
            if(close)EditorSceneManager.CloseScene(scene,true);
        }
    }
    static void Floors(Scene scene)
    {
        var room=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Library/RoomBuilder.png");
        var wood=Tile("PisoMaderaBiblioteca",Slice(room,"MaderaBiblioteca",608,336,16,16,new Vector2(.5f,.5f)));
        var cream=Tile("PisoCremaBiblioteca",Slice(room,"CremaBiblioteca",608,208,16,16,new Vector2(.5f,.5f)));
        var carpet=Tile("PasilloBiblioteca",Slice(room,"TejidoBiblioteca",752,304,16,16,new Vector2(.5f,.5f)));
        foreach(var map in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Tilemap>()))
            for(int x=-30;x<30;x++)for(int y=-18;y<18;y++)
            {
                var cell=new Vector3Int(x,y,0);
                bool aisle=(x>=5&&x<=8)||(y>=-14&&y<=-11);
                map.SetTile(cell,aisle?carpet:x>8?cream:wood);
                map.SetTileFlags(cell,TileFlags.None);
                bool border=x<=-29||x>=28||y<=-17||y>=16;
                map.SetColor(cell,border?new Color(.45f,.4f,.34f):aisle?new Color(.35f,.57f,.55f):Color.white);
            }
    }
    static void Perimeter(Scene scene)
    {
        Block("Panel pared de ventanas",new Vector2(9,7.75f),new Vector2(10.7f,1.7f),new Color(.77f,.82f,.76f),-620);
        Block("Zócalo oeste",new Vector2(-14.75f,0),new Vector2(.2f,17.6f),new Color(.32f,.27f,.23f),-600);
        Block("Zócalo este",new Vector2(14.75f,0),new Vector2(.2f,17.6f),new Color(.32f,.27f,.23f),-600);
        foreach(float x in new[]{-8.5f,8.5f})
        {
            Block("Pared sur",new Vector2(x,-8.45f),new Vector2(13,.75f),new Color(.74f,.78f,.73f),-600);
            Block("Moldura sur",new Vector2(x,-8.1f),new Vector2(13,.12f),new Color(.35f,.28f,.23f),-590);
        }
        foreach(var old in scene.GetRootGameObjects().Where(g=>g.name=="Muro norte"))
            old.GetComponent<SpriteRenderer>().color=new Color(.82f,.84f,.76f);
    }
    static void Doorway(LibraryLevelManager manager,Sprite rug)
    {
        var zone=manager.exitZone;
        zone.floor.sprite=rug;zone.floor.transform.position=new Vector3(0,-7.1f,0);zone.floor.transform.localScale=new Vector3(.95f,.65f,1);zone.floor.sortingOrder=-930;
        Block("Interior oscuro del acceso",new Vector2(0,-7.35f),new Vector2(2.3f,2),new Color(.08f,.13f,.13f),-580);
        foreach(float x in new[]{-1.27f,1.27f})
        {
            Block("Marco de puerta",new Vector2(x,-7.35f),new Vector2(.22f,2.3f),new Color(.38f,.27f,.18f),15);
            Block("Moldura dorada de puerta",new Vector2(x,-7.35f),new Vector2(.055f,2.2f),new Color(.78f,.62f,.36f),16);
        }
        Block("Dintel de madera",new Vector2(0,-6.24f),new Vector2(2.76f,.22f),new Color(.38f,.27f,.18f),15);
        Block("Umbral",new Vector2(0,-8.28f),new Vector2(2.65f,.17f),new Color(.72f,.63f,.48f),15);
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"Door.png");
        zone.doorFrames=new Sprite[5];
        for(int i=0;i<5;i++)zone.doorFrames[i]=Slice(texture,"PuertaBiblioteca"+i,i*16,0,16,48,new Vector2(.5f,0));
        zone.leftDoor=Prop("Hoja izquierda de puerta",zone.doorFrames[0],new Vector2(-.5f,-8.25f),1,false);zone.leftDoor.sortingOrder=17;
        zone.rightDoor=Prop("Hoja derecha de puerta",zone.doorFrames[0],new Vector2(.5f,-8.25f),1,false);zone.rightDoor.flipX=true;zone.rightDoor.sortingOrder=17;
        Block("Placa salida",new Vector2(0,-5.7f),new Vector2(4.2f,.85f),new Color(.09f,.22f,.23f),405);
        zone.sign.transform.position=new Vector3(0,-5.7f,-.2f);zone.sign.rectTransform.sizeDelta=new Vector2(4,.8f);zone.sign.fontSize=2.7f;zone.sign.color=new Color(1,.91f,.69f);zone.sign.GetComponent<Renderer>().sortingOrder=410;
        zone.statusLight=Block("Luz estado de salida",new Vector2(0,-6.2f),new Vector2(.32f,.1f),new Color(1,.7f,.3f),20);
    }
    static SpriteRenderer Prop(string name,Sprite sprite,Vector2 position,float scale,bool solid)
    {
        var go=new GameObject(name);go.transform.SetParent(root);go.transform.position=position;go.transform.localScale=new Vector3(scale,scale,1);
        var sr=go.AddComponent<SpriteRenderer>();sr.sprite=sprite;sr.sharedMaterial=material;
        sr.sortingOrder=100-Mathf.RoundToInt((position.y-(sprite!=null?sprite.bounds.size.y*scale*.4f:0))*20);
        if(solid)
        {
            var collider=go.AddComponent<BoxCollider2D>();collider.size=new Vector2(.4f/scale,.25f/scale);
            collider.offset=new Vector2(0,-sprite.bounds.extents.y*.7f);
        }
        return sr;
    }
    static SpriteRenderer Block(string name,Vector2 position,Vector2 size,Color color,int order)
    {
        var sr=Prop(name,pixel,position,1,false);sr.transform.localScale=new Vector3(size.x,size.y,1);sr.color=color;sr.sortingOrder=order;return sr;
    }
    static void Label(string name,string value,Vector2 position,Vector2 size,float fontSize,Color color)
    {
        var go=new GameObject(name);go.transform.SetParent(root);go.transform.position=position;
        var text=go.AddComponent<TextMeshPro>();text.font=font;text.text=value;text.fontSize=fontSize;text.color=color;text.alignment=TextAlignmentOptions.Center;text.rectTransform.sizeDelta=size;text.renderer.sortingOrder=400;
    }
    static void Prepare(string path)
    {
        var importer=AssetImporter.GetAtPath(path)as TextureImporter;
        if(importer==null)throw new System.InvalidOperationException("Falta recurso de interior: "+path);
        importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spritePixelsPerUnit=16;
        importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
    }
    static Sprite Slice(Texture2D texture,string name,int x,int top,int width,int height,Vector2 pivot)
    {
        string path=Generated+name+".asset";var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);if(sprite!=null)return sprite;
        sprite=Sprite.Create(texture,new Rect(x,texture.height-top-height,width,height),pivot,16,0,SpriteMeshType.FullRect);sprite.name=name;AssetDatabase.CreateAsset(sprite,path);return sprite;
    }
    static Tile Tile(string name,Sprite sprite)
    {
        string path=Generated+name+".asset";var tile=AssetDatabase.LoadAssetAtPath<Tile>(path);
        if(tile==null){tile=ScriptableObject.CreateInstance<Tile>();AssetDatabase.CreateAsset(tile,path);}
        tile.sprite=sprite;tile.flags=TileFlags.None;tile.colliderType=UnityEngine.Tilemaps.Tile.ColliderType.None;EditorUtility.SetDirty(tile);return tile;
    }
    static Sprite SolidPixel()
    {
        string path=Generated+"InteriorColorPlano.asset";
        var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);if(sprite!=null)return sprite;
        var texture=new Texture2D(1,1,TextureFormat.RGBA32,false);texture.name="Color plano interior";texture.filterMode=FilterMode.Point;texture.SetPixel(0,0,Color.white);texture.Apply();
        AssetDatabase.CreateAsset(texture,Generated+"InteriorColorPlanoTexture.asset");
        sprite=Sprite.Create(texture,new Rect(0,0,1,1),new Vector2(.5f,.5f),1);sprite.name="Color plano interior";AssetDatabase.CreateAsset(sprite,path);return sprite;
    }
}
