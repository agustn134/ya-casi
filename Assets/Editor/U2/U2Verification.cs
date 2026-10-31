using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Prueba de integración ejecutada una vez durante la instalación.</summary>
[InitializeOnLoad]
public static class U2Verification
{
    const string Report="Z:/DOCUMENTOS/Barron/U2_Entrega/Pruebas.txt";
    static int phase=-1;static double due;static Vector3[] positions;
    static readonly List<string> results=new List<string>();
    static readonly string[] keys={"YaCasi.U2.Tutorial","YaCasi.U2.ResumeLevel","YaCasi.U2.Best","YaCasi.U2.Win"};
    static readonly Dictionary<string,int> previous=new Dictionary<string,int>();
    static U2Verification(){if(SessionState.GetBool("U2Testing",false))EditorApplication.delayCall+=Begin;}
    [MenuItem("Ya Casi/U2/Verificar nivel 1")]
    public static void Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||SceneManager.GetActiveScene().isDirty)return;
        if(File.Exists(Report))File.Move(Report,Report+"."+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".bak");Begin();
    }
    static void Begin()
    {
        if(EditorApplication.isPlaying&&SessionState.GetBool("U2Testing",false))
        {foreach(var key in keys)previous[key]=SessionState.GetInt("U2Test."+key,-987654);phase=0;due=EditorApplication.timeSinceStartup+4;EditorApplication.update+=Tick;return;}
        if(File.Exists(Report)||EditorApplication.isPlayingOrWillChangePlaymode)return;
        if(!File.Exists("Assets/Resources/U2Installed.txt")){EditorApplication.delayCall+=Begin;return;}
        foreach(var key in keys){previous[key]=PlayerPrefs.GetInt(key,-987654);SessionState.SetInt("U2Test."+key,previous[key]);}SessionState.SetBool("U2Testing",true);
        EditorSceneManager.OpenScene("Assets/Scenes/Nivel01_CaosDelCasillero.unity");
        phase=0;due=EditorApplication.timeSinceStartup+4;EditorApplication.update+=Tick;EditorApplication.isPaused=false;Application.runInBackground=true;EditorApplication.isPlaying=true;
    }
    static void Check(bool ok,string message){results.Add((ok?"PASS ":"FAIL ")+message);}
    static void Next(int n,float delay=1){phase=n;due=EditorApplication.timeSinceStartup+delay;}
    static void Tick()
    {
        EditorApplication.isPaused=false;Application.runInBackground=true;EditorApplication.QueuePlayerLoopUpdate();
        if(EditorApplication.timeSinceStartup<due||EditorApplication.isCompiling)return;
        try
        {
            var manager=UnityEngine.Object.FindFirstObjectByType<Level01Manager>();var upgrade=UnityEngine.Object.FindFirstObjectByType<LevelOneUpgrade>();
            if(phase==0)
            {
                if(!EditorApplication.isPlaying){due=EditorApplication.timeSinceStartup+1;return;}
                if(manager==null||upgrade==null){Check(false,"Gestor y reglas presentes");Finish();return;}
                Check(Mathf.Abs(manager.RoundTime-135)<.1f,"Duración inicial fija de 135 segundos");
                Check(upgrade.GetComponents<AudioSource>().Any(a=>a.clip!=null&&a.clip.name=="PixelArt-Brutaldesign"&&a.loop&&a.isPlaying),"Soundtrack MP3 en reproducción y bucle ("+string.Join(", ",upgrade.GetComponents<AudioSource>().Select(a=>(a.clip!=null?a.clip.name:"null")+":"+a.isPlaying))+")");
                var students=UnityEngine.Object.FindObjectsByType<CampusStudent>(FindObjectsSortMode.InstanceID);Check(students.Length==6,"Seis estudiantes con collider y animación");positions=students.Select(s=>s.transform.position).ToArray();
                var lockers=UnityEngine.Object.FindObjectsByType<LockerInteraction>(FindObjectsSortMode.None);var field=typeof(LockerInteraction).GetField("containsUSB",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);Check(lockers.Count(l=>(bool)field.GetValue(l))==1,"Exactamente una USB aleatoria por ronda");
                upgrade.DismissOverlay();Next(1,3);
            }
            else if(phase==1)
            {
                var students=UnityEngine.Object.FindObjectsByType<CampusStudent>(FindObjectsSortMode.InstanceID);Check(students.Where((s,i)=>Vector3.Distance(s.transform.position,positions[i])>.1f).Any(),"Los estudiantes avanzan por el pasillo");
                var field=typeof(LockerInteraction).GetField("containsUSB",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);var locker=UnityEngine.Object.FindObjectsByType<LockerInteraction>(FindObjectsSortMode.None).First(l=>!(bool)field.GetValue(l));
                float time=manager.RemainingTime;locker.Interact();Check(Mathf.Abs(time-manager.RemainingTime-3)<.1f,"Casillero vacío resta 3 segundos");
                time=manager.RemainingTime;locker.Interact();Check(Mathf.Abs(time-manager.RemainingTime-5)<.1f,"Repetir casillero resta 5 segundos");
                upgrade.TogglePause();Check(Time.timeScale==0&&upgrade.Paused,"Pausa detiene la simulación");upgrade.TogglePause();Check(Time.timeScale==1&&!upgrade.Paused,"Continuar reanuda la simulación");
                manager.CollectUSB();manager.CompleteLevel();Next(2);
            }
            else if(phase==2)
            {
                Check(manager.IsCompleted,"Victoria al salir con USB");Check(GameObject.Find("Resultado nivel 1")!=null,"Pantalla de victoria con puntaje");Check(PlayerPrefs.GetInt("YaCasi.U2.Best",0)>0,"Mejor puntaje persistido");
                manager.RestartLevel();Next(3,3);
            }
            else if(phase==3){upgrade.DismissOverlay();manager.ApplyPenalty(99999);Next(4);}
            else if(phase==4){Check(manager.IsFailed,"Derrota al agotar tiempo");Check(GameObject.Find("Resultado nivel 1")!=null,"Pantalla de derrota");U2Presentation.ReturnToMenu();Next(5,4);}
            else if(phase==5)
            {
                Check(SceneManager.GetActiveScene().name=="MenuPrincipal","Regreso al menú principal");Check(UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Length>=5,"Jugar, Continuar, Opciones, Créditos y Salir disponibles");
                var root=GameObject.Find("Menú principal");U2Presentation.Options(root.transform,null);Check(UnityEngine.Object.FindObjectsByType<Slider>(FindObjectsSortMode.None).Length==2,"Música y efectos tienen controles separados");Finish();
            }
        }
        catch(Exception ex){Check(false,ex.ToString());Finish();}
    }
    static void Finish()
    {
        SessionState.SetBool("U2Testing",false);
        EditorApplication.update-=Tick;foreach(var key in keys){if(previous[key]==-987654)PlayerPrefs.DeleteKey(key);else PlayerPrefs.SetInt(key,previous[key]);}PlayerPrefs.Save();
        Directory.CreateDirectory(Path.GetDirectoryName(Report));File.WriteAllLines(Report,results);Debug.Log("U2_TESTS: "+string.Join("; ",results));EditorApplication.isPlaying=false;
        EditorApplication.delayCall+=()=>{EditorSceneManager.OpenScene("Assets/Scenes/Nivel01_CaosDelCasillero.unity");};
    }
}
