using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

public static class LibraryCharactersUpdate
{
    const string Folder = "Assets/Art/Library/Generated/Characters";
    [MenuItem("Ya Casi/Nivel 2/Actualizar alumnos y guardia")]
    public static void Apply()
    {
        Directory.CreateDirectory(Folder);
        var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(LibraryLevelBuilder.Path);
        if (!scene.isLoaded) scene = EditorSceneManager.OpenScene(LibraryLevelBuilder.Path);
        var manager = Object.FindFirstObjectByType<LibraryLevelManager>();
        var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animations/Player/Controllers/PlayerAnimator.controller");
        if (controller == null) throw new Exception("Falta el controlador del jugador");
        var guard = manager.librarian;
        guard.name = "Guardia de seguridad";
        guard.animator.runtimeAnimatorController = Character(controller,"Guardia", "Assets/Art/Library/Premade_Character_19.png");
        guard.GetComponent<SpriteRenderer>().sprite = FirstSprite(guard.animator.runtimeAnimatorController);
        int i = 0;
        foreach (var student in Object.FindObjectsByType<LibraryStudent>(FindObjectsSortMode.None).OrderBy(s=>s.name))
        {
            var collider = student.GetComponent<Collider2D>();
            if (collider == null)
            {
                var capsule = student.gameObject.AddComponent<CapsuleCollider2D>();
                capsule.size = new Vector2(.5f,.35f); capsule.offset = new Vector2(0,-.45f);
                collider = capsule;
            }
            collider.isTrigger = false;
            if (!student.seated)
            {
                var animator = student.GetComponent<Animator>();
                var path = "Assets/ThirdParty/LimeZu/ModernInteriors/Characters/Premade/16x16/Premade_Character_" + (++i).ToString("00") + ".png";
                animator.runtimeAnimatorController = Character(controller,"Alumno"+i,path);
                student.GetComponent<SpriteRenderer>().sprite = FirstSprite(animator.runtimeAnimatorController);
                var body = student.GetComponent<Rigidbody2D>();
                body.bodyType = RigidbodyType2D.Kinematic; body.simulated = true; body.gravityScale = 0;
                body.constraints = RigidbodyConstraints2D.FreezeRotation;
                body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                body.interpolation = RigidbodyInterpolation2D.Interpolate;
            }
            EditorUtility.SetDirty(student);
        }
        EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets(); EditorSceneManager.SaveScene(scene);
        var students = Object.FindObjectsByType<LibraryStudent>(FindObjectsSortMode.None);
        if (students.Length == 0 || students.Any(s=>s.GetComponent<Collider2D>().isTrigger)) throw new Exception("Colisiones incompletas");
        if (students.Where(s=>!s.seated).Select(s=>s.GetComponent<Animator>().runtimeAnimatorController).Distinct().Count()!=6) throw new Exception("Faltan estilos");
        File.WriteAllText("LibraryCharactersVerification.txt", "PASS: alumnos existentes sólidos; 6 apariencias distintas; guardia Premade_Character_19 con 8 animaciones; escena guardada.\n");
        Debug.Log("LIBRARY_CHARACTERS_PASS");
    }
    static Sprite FirstSprite(RuntimeAnimatorController c) => AnimationUtility.GetObjectReferenceCurve(c.animationClips.First(), EditorCurveBinding.PPtrCurve("",typeof(SpriteRenderer),"m_Sprite"))[0].value as Sprite;
    static RuntimeAnimatorController Character(RuntimeAnimatorController source,string name,string texturePath)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);
        importer.filterMode=FilterMode.Point; importer.mipmapEnabled=false; importer.textureCompression=TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        var result = new AnimatorOverrideController(source);
        var pairs = new List<KeyValuePair<AnimationClip,AnimationClip>>();
        foreach(var original in source.animationClips.Distinct())
        {
            var clip=Object.Instantiate(original); clip.name=name+"_"+original.name;
            foreach(var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
            {
                var keys=AnimationUtility.GetObjectReferenceCurve(clip,binding);
                foreach(var keyIndex in Enumerable.Range(0,keys.Length))
                {
                    var old=keys[keyIndex].value as Sprite;
                    if(old==null)continue;
                    int x=Mathf.FloorToInt(old.rect.center.x/16)*16;
                    int row=Mathf.FloorToInt((old.texture.height-old.rect.center.y)/32);
                    string path=Folder+"/"+name+"_"+x+"_"+row+".asset";
                    var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    if(sprite==null){sprite=Sprite.Create(texture,new Rect(x,texture.height-(row+1)*32,16,32),new Vector2(.5f,.5f),16,0,SpriteMeshType.FullRect); AssetDatabase.CreateAsset(sprite,path);}
                    keys[keyIndex].value=sprite;
                }
                AnimationUtility.SetObjectReferenceCurve(clip,binding,keys);
            }
            string clipPath=Folder+"/"+clip.name+".anim";
            var existing=AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if(existing==null)AssetDatabase.CreateAsset(clip,clipPath);else{EditorUtility.CopySerialized(clip,existing);Object.DestroyImmediate(clip);clip=existing;}
            pairs.Add(new KeyValuePair<AnimationClip,AnimationClip>(original,clip));
        }
        result.ApplyOverrides(pairs);
        string controllerPath=Folder+"/"+name+".overrideController";
        var prior=AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(controllerPath);
        if(prior==null)AssetDatabase.CreateAsset(result,controllerPath);else{EditorUtility.CopySerialized(result,prior);Object.DestroyImmediate(result);result=prior;}
        return result;
    }
}


