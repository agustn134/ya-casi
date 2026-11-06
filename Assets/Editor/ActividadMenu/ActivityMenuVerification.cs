using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using YaCasi.ActividadMenu;

[InitializeOnLoad]
public static class ActivityMenuVerification
{
    const string Output="Z:/DOCUMENTOS/Barron/MENU/EntregaActividad";
    static double next;
    static int qualityBefore;
    static ActivityMenuVerification(){EditorApplication.update+=Tick;}
    static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
    static void Capture(string name){Directory.CreateDirectory(Output+"/Evidencias");ScreenCapture.CaptureScreenshot(Output+"/Evidencias/"+name+".png");}
    static void Tick()
    {
        int step=SessionState.GetInt("ActividadMenuTestStep",-1);
        if(step<0||!EditorApplication.isPlaying||EditorApplication.isCompiling||EditorApplication.timeSinceStartup<next)return;
        next=EditorApplication.timeSinceStartup+1.3;
        try
        {
            var m=UnityEngine.Object.FindFirstObjectByType<MenuController>();
            if(step==0)
            {
                if(m==null)return;
                Require(m.mainPanel.activeSelf&&!m.settingsPanel.activeSelf&&!m.scoresPanel.activeSelf&&!m.creditsPanel.activeSelf,"Inicio no muestra únicamente MainPanel");
                Require(m.playerNameField.characterLimit==16,"Límite del nombre incorrecto");
                Require(m.qualityDropdown.options.Count==4,"Faltan niveles de calidad");
                Require(m.backButtons.Length==3,"Faltan botones Volver");
                Require(m.GetComponentInChildren<AudioSource>().clip!=null,"Falta música");
                qualityBefore=QualitySettings.GetQualityLevel();
                Capture("01_Inicio");
            }
            if(step==1){m.settingsButton.onClick.Invoke();Require(m.settingsPanel.activeSelf&&!m.mainPanel.activeSelf,"No abre Ajustes");}
            if(step==2)
            {
                for(int i=0;i<4;i++){m.qualityDropdown.value=i;Require(QualitySettings.GetQualityLevel()==m.qualityIndices[i],"Calidad no cambia");}
                m.qualityDropdown.value=2;
                m.brightnessSlider.value=.25f;Require(m.brightnessOverlay.color.a>.4f,"Brillo no modifica superposición");m.brightnessSlider.value=.8f;
                m.volumeSlider.value=.3f;Require(Mathf.Abs(m.uiAudio.volume-.3f)<.01f,"Volumen no cambia");m.volumeSlider.value=.6f;
                m.playerNameField.text="Agustín";m.playerNameField.onEndEdit.Invoke("Agustín");Require(PlayerPrefs.GetString("YaCasi.ActividadMenu.Name")=="Agustín","Nombre no se guarda");
                Capture("02_Ajustes");
            }
            if(step==3){m.backButtons[0].onClick.Invoke();Require(m.mainPanel.activeSelf,"Volver de ajustes falla");m.scoresButton.onClick.Invoke();Require(m.scoresPanel.activeSelf,"Puntuaciones no abre");}
            if(step==4)Capture("03_Puntuaciones");
            if(step==5){m.backButtons[1].onClick.Invoke();m.creditsButton.onClick.Invoke();Require(m.creditsPanel.activeSelf,"Créditos no abre");}
            if(step==6)Capture("04_Creditos");
            if(step==7){m.backButtons[2].onClick.Invoke();m.playButton.onClick.Invoke();}
            if(step==8)
            {
                Require(SceneManager.GetActiveScene().name=="PruebaMenu","Jugar no carga PruebaMenu");
                var back=UnityEngine.Object.FindFirstObjectByType<ActivityMenuReturn>();Require(back!=null,"Falta retorno de escena de prueba");Capture("05_EscenaPrueba");
            }
            if(step==9)UnityEngine.Object.FindFirstObjectByType<ActivityMenuReturn>().backButton.onClick.Invoke();
            if(step==10)
            {
                Require(SceneManager.GetActiveScene().name=="MenuActividadCompleta"&&m!=null,"No vuelve al menú desde prueba");
                PlayerPrefs.DeleteKey("YaCasi.ActividadMenu.Name");
                File.WriteAllText(Output+"/VerificacionEditor.txt","CORRECTO\nInicio, navegación de cuatro paneles, tres botones Volver, cuatro opciones de calidad con cambio real, brillo, volumen, nombre de jugador, música y carga de PruebaMenu con retorno.\nPendiente de comprobación manual en ejecutable: clic físico, pantalla completa y Salir.\n");
                SessionState.SetInt("ActividadMenuTestStep",-1);
                EditorApplication.isPlaying=false;
                EditorApplication.delayCall+=CompleteMenuActivity.Build;
                Debug.Log("ACTIVIDAD MENU PRUEBAS: CORRECTO");return;
            }
            SessionState.SetInt("ActividadMenuTestStep",step+1);
        }
        catch(Exception e)
        {
            SessionState.SetInt("ActividadMenuTestStep",-1);
            File.WriteAllText(Output+"/VerificacionEditor.txt","FALLO: "+e);
            EditorApplication.isPlaying=false;
            Debug.LogException(e);
        }
    }
}
