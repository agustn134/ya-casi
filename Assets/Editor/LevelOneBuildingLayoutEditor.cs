using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

/// <summary>Autoría explícita del escenario: guarda Tilemaps normales que el juego no reconstruye.</summary>
public static class LevelOneBuildingLayoutEditor
{
    const string ScenePath="Assets/Scenes/Nivel01_CaosDelCasillero.unity";
    const string TileFolder="Assets/Art/Level1/EdificioF";
    const string RootName="Grid_EdificioF";

    [MenuItem("Ya Casi/Nivel 1/Construir edificio F en Grid %#9")]
    public static void Build()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)return;
        Scene scene=SceneManager.GetActiveScene();
        if(scene.path!=ScenePath){Debug.LogWarning("Abre la escena del nivel 1 para construir el edificio F.");return;}
        var school=Object.FindFirstObjectByType<LevelOneNightSchool>();
        var art=Resources.Load<LevelOneBuildingPalette>("LevelOneBuildingPalette");
        if(school==null||art==null)throw new InvalidOperationException("Falta el controlador o la paleta del edificio F.");
        Directory.CreateDirectory("Temp");
        EditorSceneManager.SaveScene(scene,"Temp/Nivel01_AntesDeEdificioF.unity",true);
        EnsureFolder(TileFolder);
        Undo.IncrementCurrentGroup();int undo=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Construir edificio F en Grid");
        Undo.RecordObject(school,"Asignar arquitectura editable");
        if(school.authoredLayout!=null)Undo.DestroyObjectImmediate(school.authoredLayout);
        var builder=new Builder(school,art);
        AssetDatabase.StartAssetEditing();
        try{school.authoredLayout=builder.Create();}
        finally{AssetDatabase.StopAssetEditing();}
        school.Rebuild();
        EditorUtility.SetDirty(school);
        AssetDatabase.SaveAssets();
        Physics2D.SyncTransforms();
        builder.CheckAccess();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Undo.CollapseUndoOperations(undo);
        Selection.activeGameObject=school.authoredLayout;
        if(SceneView.lastActiveSceneView!=null)
        {
            SceneView.lastActiveSceneView.in2DMode=true;
            SceneView.lastActiveSceneView.LookAt(new Vector3(10,4,0),Quaternion.identity,12,true);
        }
        ExportPreviews();
        Debug.Log("EDIFICIO_F_LISTO: arquitectura y exterior guardados en Grid_EdificioF; dos plantas y ocho aulas.");
    }

    [MenuItem("Ya Casi/Nivel 1/Exportar vistas del edificio F")]
    public static void ExportPreviews()
    {
        if(SceneManager.GetActiveScene().path!=ScenePath)return;
        Directory.CreateDirectory("Temp");
        Render("Temp/EdificioF_Detalle.png",new Vector2(11,4.5f),10);
        Render("Temp/EdificioF_Conjunto.png",new Vector2(36,20),33);
    }

    static void Render(string path,Vector2 position,float size)
    {
        var go=new GameObject("Vista temporal del edificio F"){hideFlags=HideFlags.HideAndDontSave};
        var camera=go.AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=size;
        camera.transform.position=new Vector3(position.x,position.y,-10);
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.05f,.09f,.12f);
        var data=camera.GetUniversalAdditionalCameraData();data.renderPostProcessing=false;
        var target=new RenderTexture(1600,900,24);var previous=RenderTexture.active;
        try
        {
            camera.targetTexture=target;camera.Render();RenderTexture.active=target;
            var texture=new Texture2D(1600,900,TextureFormat.RGB24,false);
            texture.ReadPixels(new Rect(0,0,1600,900),0,0);texture.Apply();
            File.WriteAllBytes(path,texture.EncodeToPNG());Object.DestroyImmediate(texture);
        }
        finally{RenderTexture.active=previous;camera.targetTexture=null;target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(go);}
    }

    static void EnsureFolder(string path)
    {
        string parent=Path.GetDirectoryName(path).Replace('\\','/');
        if(!AssetDatabase.IsValidFolder(parent))EnsureFolder(parent);
        if(!AssetDatabase.IsValidFolder(path))AssetDatabase.CreateFolder(parent,Path.GetFileName(path));
    }

    sealed class Builder
    {
        readonly LevelOneNightSchool school;
        readonly LevelOneBuildingPalette art;
        readonly LevelOneCampusBackdrop campus;
        readonly Dictionary<string,Tile> tiles=new Dictionary<string,Tile>();
        readonly List<BoxCollider2D> solids=new List<BoxCollider2D>();
        readonly float[] centers={10,28,46,64};
        GameObject root;Transform props,collisionRoot;
        Tilemap ground,paving,roofs,groundDetails,floors,lanes,walls,accents,windows;
        Mesh arrow;Material arrowMaterial;
        readonly Color cream=new Color(.98f,.97f,.91f);
        readonly Color red=new Color(.76f,.24f,.16f);

        public Builder(LevelOneNightSchool controller,LevelOneBuildingPalette palette)
        {school=controller;art=palette;campus=school.GetComponent<LevelOneCampusBackdrop>();}

        public GameObject Create()
        {
            root=new GameObject(RootName,typeof(Grid));Undo.RegisterCreatedObjectUndo(root,"Crear Grid del edificio F");
            root.transform.position=new Vector3(-.5f,-.5f,0);
            root.GetComponent<Grid>().cellSize=Vector3.one;
            props=Child("Mobiliario y señalización",root.transform).transform;
            collisionRoot=Child("Colisiones · bases de muros y muebles",root.transform).transform;
            var exterior=Child("Exterior",root.transform).transform;
            ground=Map("Exterior_Pasto",exterior,-60);
            paving=Map("Exterior_Andadores",exterior,-55);
            roofs=Map("Exterior_Volumen_y_techos",exterior,-40);
            groundDetails=Map("Exterior_Bordes_y_jardineras",exterior,-54);
            Exterior();
            for(int level=0;level<2;level++)Floor(level);
            var global=Child("Luz ambiental nocturna",root.transform).AddComponent<Light2D>();
            global.lightType=Light2D.LightType.Global;global.intensity=.68f;global.color=new Color(.86f,.90f,1);
            return root;
        }

        void Exterior()
        {
            var grass=campus!=null?campus.grassTile as Tile:null;
            Fill(ground,"Pasto",grass!=null?grass.sprite:art.floor,-29,-23,140,89,new Color(.54f,.64f,.65f));
            for(int level=0;level<2;level++)
            {
                int y=level*32;
                Fill(paving,"Andador",art.exteriorPaving,-16,y-12,108,5,new Color(.79f,.82f,.82f));
                Fill(paving,"Andador",art.exteriorPaving,-17,y-12,5,31,new Color(.79f,.82f,.82f));
                Fill(paving,"Andador",art.exteriorPaving,88,y-12,4,31,new Color(.79f,.82f,.82f));
                Fill(paving,"Andador",art.exteriorPaving,-17,y+19,109,3,new Color(.73f,.77f,.78f));
                // Una huella continua une los salones; los interiores se recortan encima.
                var roof=campus!=null?campus.roofTile as Tile:null;
                Fill(roofs,"Techo",roof!=null?roof.sprite:art.exteriorPaving,-8,y+6,84,11,new Color(.70f,.48f,.38f));
                Fill(paving,"Andador",art.exteriorPaving,-13,y-3,6,5,new Color(.94f,.94f,.88f));
                for(int x=-4;x<80;x+=14)
                {
                    Fill(groundDetails,"Borde_jardinera",art.exteriorPaving,x,y-16,5,4,new Color(.58f,.62f,.62f));
                    Fill(groundDetails,"Jardinera",grass!=null?grass.sprite:art.floor,x+1,y-15,3,2,new Color(.56f,.69f,.60f));
                    if(art.shrub!=null)Prop("Arbusto de jardinera",art.shrub,new Vector2(x+2,y-14.2f),1.9f,-48,Color.white);
                }
            }
            if(campus!=null&&campus.trees!=null&&campus.trees.Length>0)
                for(int x=-23;x<106;x+=9)
                {
                    Sprite tree=campus.trees[(x+23)/9%campus.trees.Length];if(tree==null)continue;
                    Prop("Árbol del campus",tree,new Vector2(x,-19),3,-47,new Color(.70f,.78f,.76f));
                    Prop("Árbol del campus",tree,new Vector2(x,57),3,-47,new Color(.70f,.78f,.76f));
                }
            Solid("Cierre exterior oeste",new Rect(-13.3f,-3.5f,.3f,5.5f));
            Solid("Cierre exterior sur",new Rect(-13,-3.5f,6,.3f));
            Solid("Cierre exterior norte",new Rect(-13,1.7f,6,.3f));
        }

        void Floor(int level)
        {
            int y=level*32;string prefix=level==0?"PB":"P2";
            var parent=Child(level==0?"Planta baja":"Segundo piso",root.transform).transform;
            floors=Map(prefix+"_Pisos",parent,-10);lanes=Map(prefix+"_Carriles",parent,-9);
            walls=Map(prefix+"_Muros_crema_verde",parent,-2);
            accents=Map(prefix+"_Columnas_rojas",parent,-1);windows=Map(prefix+"_Ventanas_oscuras",parent,0);
            Fill(floors,"Piso_marfil",art.floor,-7,y-5,83,11,cream);
            Fill(lanes,"Carril_gris",art.lane,-7,y-2,83,2,new Color(.80f,.86f,.89f));
            Fill(lanes,"Carril_gris",art.lane,-7,y+1,83,2,new Color(.80f,.86f,.89f));
            for(int x=1;x<73;x+=9){Arrow(new Vector2(x,y+1.5f),false);Arrow(new Vector2(x,y-1.5f),true);}
            HWall("Muro sur",-7.5f,75.5f,y-5.5f,false);
            // Mantiene la altura de las bases originales para no cambiar los accesos.
            if(level==0)
            {
                VWall("Acceso oeste inferior",-7.4f,y-5.3f,y-2.5f,true);
                VWall("Acceso oeste superior",-7.4f,y+.55f,y+5.5f,true);
                Sign("Salida",new Vector2(-6,-.35f),1.6f,new Color(.68f,1,.72f));
            }
            else VWall("Cierre oeste",-7.4f,y-5.3f,y+15.5f,true);
            VWall("Acceso escalera inferior",74.85f,y-5.3f,y-2,false);
            VWall("Acceso escalera superior",74.85f,y+1.1f,y+15.5f,false);
            HWall("Fachada norte",-7.5f,75.5f,y+15.5f,true);
            float cursor=-7.5f;
            foreach(float center in centers)
            {
                HWall("Pared del pasillo",cursor,center-1.5f,y+5.5f,true);
                Room(level,center);cursor=center+1.5f;
            }
            HWall("Pared del pasillo",cursor,75.5f,y+5.5f,true);
            // Columnas en los machones, nunca dentro de los huecos de puerta.
            foreach(int x in new[]{-6,4,8,12,16,22,26,30,34,40,44,48,52,58,62,66,70,74})
            {
                Column(x,y+6);Column(x,y+16);
            }
            foreach(int x in new[]{-4,-3,-2,0,1,2,17,18,19,20,35,36,37,38,53,54,55,56,71,72})
            {Window(x,y+7);Window(x,y+17);}
            Sign(level==0?"Edificio F · Planta baja":"Edificio F · Segundo piso",new Vector2(0,y-6.6f),2.1f,new Color(.94f,.91f,.75f));
            if(art.bench!=null)foreach(int x in new[]{17,53})
            {
                Prop("Banca del pasillo",art.bench,new Vector2(x,y-4.3f),2.4f,0,new Color(.88f,.77f,.66f));
                Solid("Base de banca",new Rect(x-1.05f,y-4.85f,2.1f,.48f));
            }
            Stairs(level);
        }

        void Room(int level,float center)
        {
            int y=level*32,number=101+level*100+Array.IndexOf(centers,center);
            Fill(floors,"Piso_marfil",art.floor,(int)center-5,y+6,11,10,new Color(.40f,.44f,.50f));
            VWall("Lateral izquierdo aula "+number,center-5.6f,y+5.5f,y+15.5f,true);
            VWall("Lateral derecho aula "+number,center+5.4f,y+5.5f,y+15.5f,false);
            // La puerta está abierta y no tiene un collider atravesando el umbral.
            if(school.palette.door!=null)
            {
                var door=Prop("Puerta abierta · TI "+number,school.palette.door,new Vector2(center+1.1f,y+6),.75f,1,new Color(.75f,.70f,.61f));
                door.transform.rotation=Quaternion.Euler(0,0,-70);
            }
            Color frame=new Color(.10f,.14f,.15f);
            Panel("Marco izquierdo · TI "+number,new Rect(center-1.5f,y+5.5f,.12f,1.9f),frame,1);
            Panel("Marco derecho · TI "+number,new Rect(center+1.38f,y+5.5f,.12f,1.9f),frame,1);
            Panel("Dintel · TI "+number,new Rect(center-1.5f,y+7.28f,3,.12f),frame,1);
            Sign("TI · "+number,new Vector2(center,y+7.65f),2.2f,new Color(.86f,.94f,1));
            Panel("Pizarrón · TI "+number,new Rect(center-2.5f,y+14.1f,5,.65f),new Color(.08f,.23f,.19f),1);
            for(int row=0;row<2;row++)for(int col=0;col<3;col++)
            {
                Vector2 position=new Vector2(center+(col-1)*3.5f,y+9.5f+row*3);
                if(school.palette.desk!=null)Prop("Pupitre · TI "+number,school.palette.desk,position,1.55f,0,new Color(.64f,.66f,.68f));
                Solid("Base del pupitre",new Rect(position.x-.74f,position.y-.32f,1.48f,.62f));
                if(school.palette.chair!=null)Prop("Silla · TI "+number,school.palette.chair,position+Vector2.down,.65f,1,new Color(.63f,.67f,.72f));
            }
        }

        void Stairs(int level)
        {
            int y=level*32;
            Fill(floors,"Piso_marfil",art.floor,75,y-2,11,14,new Color(.86f,.86f,.82f));
            Fill(lanes,"Escalon",school.palette.stairs,75,y-2,3,10,new Color(.85f,.87f,.91f));
            Fill(lanes,"Escalon",school.palette.stairs,82,y-2,3,10,new Color(.85f,.87f,.91f));
            // Las bases coinciden con las huellas que usa LevelOneStairTraversal.
            Solid("Separador de escalera",new Rect(78,y-2.3f,4,10.3f));
            Fill(walls,"Pared_inferior",art.wallFaceLower,78,y-2,4,10,new Color(.60f,.68f,.67f));
            HWall("Tapa del separador",78,82,y+8.5f,false,false);
            VWall("Barandal exterior izquierdo",74.72f,y+1.1f,y+11.2f,true);
            VWall("Barandal exterior derecho",85,y-2.3f,y+11.2f,false);
            HWall("Barandal descanso",74.72f,85.27f,y+11.1f,true);
            HWall("Cierre tramo derecho",82,85.28f,y-2.3f,false);
            HWall("Cierre tramo izquierdo",74.75f,78.03f,y-2.3f,false);
            Sign(level==0?"Subir al segundo piso":"Bajar a planta baja",new Vector2(70.5f,y+1),1.8f,new Color(1,.88f,.57f));
            Arrow(new Vector2(76.5f,y+3),false,90);Arrow(new Vector2(79.5f,y+9.5f),false);
            Arrow(new Vector2(83.5f,y+3),false,-90);
        }

        void HWall(string name,float left,float right,float baseline,bool above,bool collision=true)
        {
            int first=Mathf.CeilToInt(left+.5f),last=Mathf.FloorToInt(right-.5f);
            int lower=Mathf.RoundToInt(baseline+(above?.5f:-1.5f));
            for(int x=first;x<=last;x++)
            {
                Cell(walls,"Pared_superior",art.wallFace,new Vector3Int(x,lower+1,0),Color.white);
                Cell(walls,"Pared_inferior",art.wallFaceLower,new Vector3Int(x,lower,0),Color.white);
            }
            // Remates propios de la misma familia de paredes, sin estirar el sprite.
            if(last>first)
            {
                Cell(walls,"Esquina_izquierda",art.cornerTopLeft,new Vector3Int(first,lower+1,0),Color.white);
                Cell(walls,"Esquina_izquierda_inferior",art.cornerTopLeftLower,new Vector3Int(first,lower,0),Color.white);
                Cell(walls,"Esquina_derecha",art.cornerTopRight,new Vector3Int(last,lower+1,0),Color.white);
                Cell(walls,"Esquina_derecha_inferior",art.cornerTopRightLower,new Vector3Int(last,lower,0),Color.white);
            }
            if(collision)Solid(name,new Rect(left,baseline-.08f,right-left,.30f));
        }

        void VWall(string name,float x,float bottom,float top,bool left)
        {
            int cellX=Mathf.RoundToInt(x+(left?-.45f:.45f));
            int low=Mathf.CeilToInt(bottom+.5f),high=Mathf.FloorToInt(top-.5f);
            for(int y=low;y<=high;y++)Cell(walls,left?"Lateral_izquierdo":"Lateral_derecho",left?art.wallLeft:art.wallRight,new Vector3Int(cellX,y,0),Color.white);
            Solid(name,new Rect(x,bottom,.30f,top-bottom));
        }

        void Column(int x,int lower)
        {
            for(int y=lower;y<=lower+1;y++)
            {
                var cell=new Vector3Int(x,y,0);Cell(accents,"Columna_roja",art.column!=null?art.column:art.floor,cell,red);
                accents.SetTransformMatrix(cell,Matrix4x4.Scale(new Vector3(.48f,1,1)));
            }
        }

        void Window(int x,int y)
        {
            var cell=new Vector3Int(x,y,0);Cell(windows,"Vidrio_oscuro",art.window!=null?art.window:art.lane,cell,new Color(.22f,.35f,.39f));
            windows.SetTransformMatrix(cell,Matrix4x4.TRS(new Vector3(0,.05f,0),Quaternion.identity,new Vector3(.94f,.38f,1)));
        }

        void Arrow(Vector2 at,bool reverse,float rotation=0)
        {
            if(arrow==null)
            {
                string path=TileFolder+"/Flecha_del_pasillo.asset";arrow=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(arrow==null)
                {
                    arrow=new Mesh{name="Flecha del pasillo"};
                    arrow.vertices=new[]{new Vector3(-.46f,-.08f),new Vector3(.04f,-.08f),new Vector3(.04f,-.23f),new Vector3(.46f,0),new Vector3(.04f,.23f),new Vector3(.04f,.08f),new Vector3(-.46f,.08f)};
                    arrow.triangles=new[]{0,5,1,0,6,5,2,4,3};arrow.RecalculateBounds();AssetDatabase.CreateAsset(arrow,path);
                }
                path=TileFolder+"/Pintura_azul.asset";arrowMaterial=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(arrowMaterial==null){arrowMaterial=new Material(Shader.Find("Sprites/Default")){name="Pintura azul",color=new Color(.12f,.30f,.47f,.72f)};AssetDatabase.CreateAsset(arrowMaterial,path);}
            }
            var go=Child("Flecha de circulación",props);go.transform.position=at;go.transform.rotation=Quaternion.Euler(0,0,rotation+(reverse?180:0));
            go.AddComponent<MeshFilter>().sharedMesh=arrow;var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=arrowMaterial;renderer.sortingOrder=-8;
        }

        void Panel(string name,Rect rect,Color color,int order)
        {
            var go=Child(name,props);go.transform.position=rect.center;
            var renderer=go.AddComponent<SpriteRenderer>();renderer.sprite=art.floor;renderer.color=color;renderer.sortingOrder=order;
            renderer.sharedMaterial=school.surfaceMaterial;
            go.transform.localScale=new Vector3(rect.width/art.floor.bounds.size.x,rect.height/art.floor.bounds.size.y,1);
        }
        GameObject Prop(string name,Sprite sprite,Vector2 position,float width,int order,Color color)
        {
            var go=Child(name,props);go.transform.position=position;go.transform.localScale=Vector3.one*(width/sprite.bounds.size.x);
            var renderer=go.AddComponent<SpriteRenderer>();renderer.sprite=sprite;renderer.color=color;renderer.sortingOrder=order;
            renderer.sharedMaterial=school.surfaceMaterial;return go;
        }
        void Sign(string text,Vector2 position,float size,Color color)
        {
            var go=Child(text,props);go.transform.position=position;
            var label=go.AddComponent<TextMeshPro>();if(LevelOneTypography.Font!=null)label.font=LevelOneTypography.Font;
            label.text=text;label.fontSize=size;label.color=color;label.alignment=TextAlignmentOptions.Center;
            label.rectTransform.sizeDelta=new Vector2(6,.65f);label.renderer.sortingOrder=2;
        }
        Tilemap Map(string name,Transform parent,int order)
        {
            var go=Child(name,parent);var map=go.AddComponent<Tilemap>();var renderer=go.AddComponent<TilemapRenderer>();
            renderer.sortingOrder=order;renderer.sharedMaterial=school.surfaceMaterial;return map;
        }
        static GameObject Child(string name,Transform parent)
        {var go=new GameObject(name);go.transform.SetParent(parent,false);return go;}
        Tile GetTile(string name,Sprite sprite)
        {
            if(tiles.TryGetValue(name,out var tile))return tile;
            string path=TileFolder+"/"+name+".asset";tile=AssetDatabase.LoadAssetAtPath<Tile>(path);
            if(tile==null){tile=ScriptableObject.CreateInstance<Tile>();AssetDatabase.CreateAsset(tile,path);}
            tile.sprite=sprite;tile.colliderType=Tile.ColliderType.None;tile.flags=TileFlags.None;tile.color=Color.white;
            tile.transform=Matrix4x4.identity;
            if(name=="Columna_roja"){tile.color=red;tile.transform=Matrix4x4.Scale(new Vector3(.48f,1,1));}
            if(name=="Vidrio_oscuro")
            {
                tile.color=new Color(.22f,.35f,.39f);
                tile.transform=Matrix4x4.TRS(new Vector3(0,.05f,0),Quaternion.identity,new Vector3(.94f,.38f,1));
            }
            EditorUtility.SetDirty(tile);tiles[name]=tile;return tile;
        }
        void Cell(Tilemap map,string name,Sprite sprite,Vector3Int cell,Color color)
        {if(sprite==null)return;map.SetTile(cell,GetTile(name,sprite));map.SetColor(cell,color);}
        void Fill(Tilemap map,string name,Sprite sprite,int x,int y,int width,int height,Color color)
        {
            if(sprite==null)return;Tile tile=GetTile(name,sprite);
            for(int cy=y;cy<y+height;cy++)for(int cx=x;cx<x+width;cx++)
            {var cell=new Vector3Int(cx,cy,0);map.SetTile(cell,tile);map.SetColor(cell,color);}
        }
        void Solid(string name,Rect rect)
        {
            if(rect.width<=0||rect.height<=0)return;
            var go=Child(name,collisionRoot);go.transform.position=rect.center;var box=go.AddComponent<BoxCollider2D>();box.size=rect.size;solids.Add(box);
        }

        public void CheckAccess()
        {
            // Geometría del personaje: pies a −0.5, cápsula0.6×0.5. Solo bases estáticas.
            foreach(int level in new[]{0,1})
            {
                float y=level*32;
                foreach(float center in centers)CheckLine(new Vector2(center,y+1),new Vector2(center,y+8),"Acceso al aula");
                var route=new[]{new Vector2(72.5f,y-.5f),new Vector2(76.5f,y-.5f),new Vector2(76.5f,y+9.4f),new Vector2(83.5f,y+9.4f),new Vector2(83.5f,y-1)};
                for(int i=1;i<route.Length;i++)CheckLine(route[i-1],route[i],"Escalera en U");
            }
        }
        void CheckLine(Vector2 a,Vector2 b,string label)
        {
            int samples=Mathf.CeilToInt(Vector2.Distance(a,b)/.05f);
            for(int i=0;i<=samples;i++)
            {
                Vector2 feet=Vector2.Lerp(a,b,i/(float)Mathf.Max(1,samples))+Vector2.down*.5f;
                foreach(var box in solids)
                {
                    Bounds bounds=box.bounds;
                    if(feet.x+.30f>bounds.min.x&&feet.x-.30f<bounds.max.x&&feet.y+.25f>bounds.min.y&&feet.y-.25f<bounds.max.y)
                        throw new InvalidOperationException(label+" bloqueado por "+box.name+" cerca de "+feet);
                }
            }
        }
    }
}
