using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>Instalación explícita y repetible; conserva la escena de la actividad Menú II.</summary>
public static class U2Setup
{
    const string Done="Assets/Resources/U2Installed.txt";
    [MenuItem("Ya Casi/U2/Instalar mejoras nivel 1")]
    public static void Install()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        var current=SceneManager.GetActiveScene();
        if(current.isDirty){Debug.LogWarning("U2: guarda la escena abierta; vuelve a ejecutar Ya Casi/U2/Instalar mejoras nivel 1.");return;}
        Directory.CreateDirectory("Assets/Resources");
        var cfg=AssetDatabase.LoadAssetAtPath<U2GameConfig>("Assets/Resources/U2GameConfig.asset");
        if(cfg==null){cfg=ScriptableObject.CreateInstance<U2GameConfig>();AssetDatabase.CreateAsset(cfg,"Assets/Resources/U2GameConfig.asset");}
        cfg.soundtrack=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Music/PixelArt-Brutaldesign.mp3");
        cfg.menuMusic=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/ActividadMenu/Audio/MusicaMenu.wav");
        cfg.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Art/Fonts/PixelifySans/PixelifySans SDF.asset");
        cfg.roundSeconds=135;cfg.studentCount=6;cfg.wrongLockerPenalty=3;cfg.repeatLockerPenalty=5;
        cfg.soundtrackCredit="Pixel art — Brutaldesign · Pixabay\npixabay.com/music/id-481480/";EditorUtility.SetDirty(cfg);
        string level="Assets/Scenes/Nivel01_CaosDelCasillero.unity";
        EditorSceneManager.OpenScene(level);
        foreach(var old in Object.FindObjectsByType<CampusGameExperience>(FindObjectsSortMode.None))Object.DestroyImmediate(old);
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        var menu=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        new GameObject("Gestor menú principal",typeof(U2Presentation));var camera=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener));camera.tag="MainCamera";camera.GetComponent<Camera>().backgroundColor=new Color(.04f,.1f,.14f);camera.GetComponent<Camera>().clearFlags=CameraClearFlags.SolidColor;
        EditorSceneManager.SaveScene(menu,"Assets/Scenes/MenuPrincipal.unity");
        var paths=new[]{"Assets/Scenes/MenuPrincipal.unity",level}.Concat(EditorBuildSettings.scenes.Select(s=>s.path)).Distinct();
        EditorBuildSettings.scenes=paths.Select(p=>new EditorBuildSettingsScene(p,true)).ToArray();
        File.WriteAllText(Done,"Mejoras instaladas. Música: Pixel art — Brutaldesign, Pixabay.\n");AssetDatabase.SaveAssets();AssetDatabase.Refresh();
        EditorSceneManager.OpenScene(level);Debug.Log("U2_READY: música, dificultad y menú principal instalados.");
    }
    [MenuItem("Ya Casi/U2/Compilar Windows")]
    public static void BuildWindows()
    {
        Directory.CreateDirectory("Z:/DOCUMENTOS/Barron/U2_Entrega/Windows");
        SessionState.SetBool("YaCasi.U2.Build",true);
        UnityEditor.Build.Reporting.BuildReport report;
        try{report=UnityEditor.BuildPipeline.BuildPlayer(new[]{"Assets/Scenes/MenuPrincipal.unity","Assets/Scenes/Nivel01_CaosDelCasillero.unity"},"Z:/DOCUMENTOS/Barron/U2_Entrega/Windows/YaCasi.exe",BuildTarget.StandaloneWindows64,BuildOptions.None);}
        finally{SessionState.SetBool("YaCasi.U2.Build",false);}
        Debug.Log("U2_BUILD: "+report.summary.result);
        File.WriteAllText("Z:/DOCUMENTOS/Barron/U2_Entrega/BuildWindows.txt",report.summary.result+"\n"+report.summary.totalErrors+" errores\n"+report.summary.totalSize+" bytes\n");
    }
    [MenuItem("Ya Casi/U2/Compilar WebGL")]
    public static void BuildWeb()
    {
        if(!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL,BuildTarget.WebGL)){Debug.LogWarning("U2_WEB: instala WebGL Build Support en Unity Hub y reinicia el editor.");return;}
        PlayerSettings.WebGL.compressionFormat=WebGLCompressionFormat.Disabled;
        Directory.CreateDirectory("Z:/DOCUMENTOS/Barron/U2_Entrega/WebGL");
        SessionState.SetBool("YaCasi.U2.Build",true);
        UnityEditor.Build.Reporting.BuildReport report;
        try{report=BuildPipeline.BuildPlayer(new[]{"Assets/Scenes/MenuPrincipal.unity","Assets/Scenes/Nivel01_CaosDelCasillero.unity"},"Z:/DOCUMENTOS/Barron/U2_Entrega/WebGL",BuildTarget.WebGL,BuildOptions.None);}
        finally{SessionState.SetBool("YaCasi.U2.Build",false);}
        File.WriteAllText("Z:/DOCUMENTOS/Barron/U2_Entrega/BuildWebGL.txt",report.summary.result+"\n"+report.summary.totalErrors+" errores\n");Debug.Log("U2_WEB: "+report.summary.result);
    }
}
