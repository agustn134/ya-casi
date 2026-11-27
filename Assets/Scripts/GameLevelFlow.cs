using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

/// <summary>Orden de la campaña y acciones comunes de las pantallas de resultado.</summary>
public static class GameLevelFlow
{
    public static string Label(int level)
    {
        switch(level) {
            case 1: return "Nivel 1 · El caos del casillero";
            case 2: return "Nivel 2 · Maratón en la biblioteca";
            case 3: return "Nivel 3";
            case 4: return "Nivel 4 · El callejón";
            default: return "Campaña completada";
        }
    }
    // Busca por número para respetar el nombre definitivo de cada escena.
    public static string ScenePath(int level)
    {
        string[] scenes={"Nivel01_CaosDelCasillero","Nivel02_MaratonEnLaBiblioteca","Nivel03_LaRutaDeEntrega","Nivel04_ElCallejon"};
        if(level<1 || level>scenes.Length) return null;
        string expected="Assets/Scenes/"+scenes[level-1]+".unity";
        if(Application.CanStreamedLevelBeLoaded(expected)) return expected;
        return null;
    }
    public static bool IsAvailable(int level) => ScenePath(level)!=null;
    public static string ProgressLabel => Label(PlayerPrefs.GetInt(U2Presentation.SaveKey,1));
    public static void LoadLevel(int level)
    {
        string path=ScenePath(level);
        if(path==null) { Debug.LogWarning("El nivel "+level+" todavía no está incluido en la compilación."); return; }
        PlayerPrefs.SetInt(U2Presentation.SaveKey,level); PlayerPrefs.SetString("YaCasi.LastPlayedScene",Path.GetFileNameWithoutExtension(path)); PlayerPrefs.Save();
        Time.timeScale=1; SceneManager.LoadScene(path);
    }
    public static void Restart()
    {
        Time.timeScale=1; SceneManager.LoadScene(SceneManager.GetActiveScene().path);
    }
    public static void ResultActions(Transform parent,int level,bool success,float firstY=-170)
    {
        if(success) {
            PlayerPrefs.SetInt("YaCasi.Level."+level+".Completed",1);
            PlayerPrefs.SetInt(U2Presentation.SaveKey,IsAvailable(level+1)?level+1:level);
            if(IsAvailable(level+1)) PlayerPrefs.SetString("YaCasi.LastPlayedScene",Path.GetFileNameWithoutExtension(ScenePath(level+1)));
            PlayerPrefs.Save();
        }
        UnityEngine.UI.Button first=null;
        if(success && level<4) {
            int next=level+1; bool available=IsAvailable(next);
            first=U2Presentation.Button(parent,available?"Continuar al nivel "+next:"Nivel "+next+" · Próximamente",new Vector2(0,firstY),()=>LoadLevel(next));
            first.interactable=available;
            firstY-=95;
        }
        else if(success) {
            U2Presentation.Text(parent,"Campaña completa","¡Completaste la aventura!",new Vector2(0,firstY+15),new Vector2(1100,60),28,new Color(1,.84f,.4f));
            firstY-=65;
        }
        var restart=U2Presentation.Button(parent,"Reiniciar nivel",new Vector2(0,firstY),Restart);
        U2Presentation.Button(parent,"Menú principal",new Vector2(0,firstY-95),U2Presentation.ReturnToMenu);
        if(EventSystem.current!=null) EventSystem.current.SetSelectedGameObject(first!=null && first.interactable?first.gameObject:restart.gameObject);
    }
}
