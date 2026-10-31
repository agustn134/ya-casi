using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>Variantes de personajes LimeZu con los mismos movimientos del jugador.</summary>
[InitializeOnLoad]
public static class U2StudentArt
{
    static U2StudentArt(){EditorApplication.delayCall+=Generate;}
    [MenuItem("Ya Casi/U2/Preparar estudiantes")]
    public static void Generate()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        var cfg=AssetDatabase.LoadAssetAtPath<U2GameConfig>("Assets/Resources/U2GameConfig.asset");
        if(cfg==null){EditorApplication.delayCall+=Generate;return;}
        if(cfg.studentControllers!=null&&cfg.studentControllers.Length==6)return;
        const string folder="Assets/Animations/Students";Directory.CreateDirectory(folder);
        var controller=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animations/Player/Controllers/PlayerAnimator.controller");
        var controllers=new List<RuntimeAnimatorController>();
        for(int i=1;i<=6;i++)
        {
            string path=$"Assets/ThirdParty/LimeZu/ModernInteriors/Characters/Premade/16x16/Premade_Character_{i:00}.png";
            var importer=AssetImporter.GetAtPath(path) as TextureImporter;if(importer==null)continue;
            if(importer.spritePixelsPerUnit!=16){importer.spritePixelsPerUnit=16;importer.filterMode=FilterMode.Point;importer.SaveAndReimport();}
            var sprites=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
            var variant=new AnimatorOverrideController(controller);var pairs=new List<KeyValuePair<AnimationClip,AnimationClip>>();
            foreach(var clip in controller.animationClips.Distinct())
            {
                var copy=Object.Instantiate(clip);copy.name=$"Student{i}_{clip.name}";
                foreach(var binding in AnimationUtility.GetObjectReferenceCurveBindings(copy))
                {
                    var keys=AnimationUtility.GetObjectReferenceCurve(copy,binding);
                    for(int k=0;k<keys.Length;k++)if(keys[k].value is Sprite old)
                    {var replacement=sprites.FirstOrDefault(s=>s.rect==old.rect);if(replacement!=null)keys[k].value=replacement;}
                    AnimationUtility.SetObjectReferenceCurve(copy,binding,keys);
                }
                AssetDatabase.CreateAsset(copy,$"{folder}/Student{i}_{clip.name}.anim");pairs.Add(new KeyValuePair<AnimationClip,AnimationClip>(clip,copy));
            }
            variant.ApplyOverrides(pairs);AssetDatabase.CreateAsset(variant,$"{folder}/Student{i}.overrideController");controllers.Add(variant);
        }
        cfg.studentControllers=controllers.ToArray();EditorUtility.SetDirty(cfg);AssetDatabase.SaveAssets();Debug.Log("U2_STUDENTS: seis personajes diferenciados preparados.");
    }
}
