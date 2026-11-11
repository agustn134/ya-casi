using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Prueba del controlador y del HUD con cinco profesores provisionales.</summary>
[InitializeOnLoad]
public static class CampusDeliveryVerification
{
    const string Key = "YaCasi.YC14.Testing";
    const string ScenePath = "Assets/Scenes/Nivel03_PruebaEntregas.unity";
    static int phase;
    static double due, deadline;
    static float pausedTime, pausedElapsed;
    static readonly List<string> results = new List<string>();
    static CampusDeliveryVerification()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.EnteredEditMode || !SessionState.GetBool(Key + ".Restore", false)) return;
            SessionState.SetBool(Key + ".Restore", false);
            string path = SessionState.GetString(Key + ".Scene", "");
            if (!string.IsNullOrEmpty(path)) EditorSceneManager.OpenScene(path);
        };
        if (SessionState.GetBool(Key, false)) EditorApplication.delayCall += Attach;
    }
    [MenuItem("Ya Casi/Nivel 3/Verificar entregas YC14")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        SessionState.SetString(Key + ".Scene", SceneManager.GetActiveScene().path);
        results.Clear();
        EditorSceneManager.OpenScene(ScenePath);
        SessionState.SetBool(Key, true);
        EditorApplication.isPlaying = true;
        EditorApplication.delayCall += Attach;
    }
    static void Attach()
    {
        if (!SessionState.GetBool(Key, false)) return;
        phase = 0; due = EditorApplication.timeSinceStartup + 1; deadline = due + 45;
        EditorApplication.update -= Tick; EditorApplication.update += Tick;
    }
    static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        results.Add("PASS " + message);
    }
    static CampusDeliveryObjectives Controller => UnityEngine.Object.FindFirstObjectByType<CampusDeliveryObjectives>();
    static void Near(CampusDeliveryObjectives controller, int index)
    {
        var data = new SerializedObject(controller);
        var professor = (Transform)data.FindProperty("professors").GetArrayElementAtIndex(index).objectReferenceValue;
        var player = (Transform)data.FindProperty("player").objectReferenceValue;
        player.position = professor.position + Vector3.down;
        player.GetComponent<Rigidbody2D>().position = player.position;
        Physics2D.SyncTransforms();
    }
    static void Next(int value) { phase = value; due = EditorApplication.timeSinceStartup + 1.2; }
    static void Tick()
    {
        if (EditorApplication.isCompiling || EditorApplication.timeSinceStartup < due) return;
        try
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Tiempo de prueba excedido");
            if (!EditorApplication.isPlaying || Controller == null) return;
            var controller = Controller; var session = controller.Session;
            if (session == null) return;
            if (phase == 0)
            {
                Check(session.RequiredCount == 5 && session.DeliveredCount == 0, "Controlador provisional inicializa 0/5");
                var font = CampusTypography.Font;
                var hud = GameObject.Find("HUD nivel 3");
                Check(font != null && hud != null && hud.GetComponentsInChildren<TMP_Text>().All(t => t.font == font), "HUD usa Redaction 35");
                Near(controller, 0); controller.SelectProject(1);
                float beforeWrong = session.RemainingTime;
                var wrong = controller.TryDeliverTo(0);
                Check(wrong == ProjectDeliveryResult.WrongProject && session.DeliveredCount == 0,
                    "Controlador rechaza selección incorrecta (resultado: " + wrong + ")");
                Check(Mathf.Abs(beforeWrong - session.RemainingTime - 5) < .01f && session.WarningCount == 1, "Proyecto incorrecto resta cinco segundos y muestra amonestación");
                controller.SelectProject(0);
                Check(controller.TryDeliverTo(0) == ProjectDeliveryResult.Accepted && session.DeliveredCount == 1, "Primera entrega actualiza inventario y progreso");
                float beforeRepeat = session.RemainingTime;
                Check(controller.TryDeliverTo(0) == ProjectDeliveryResult.AlreadyDelivered && session.DeliveredCount == 1, "No cuenta entrega repetida");
                Check(Mathf.Abs(beforeRepeat - session.RemainingTime - 8) < .01f && session.WarningCount == 2, "Entrega repetida resta ocho segundos");
                controller.TogglePause(); pausedTime = session.RemainingTime; pausedElapsed = session.ElapsedTime;
                Check(session.Paused && Time.timeScale == 0, "Pausa congela simulación"); Next(1);
            }
            else if (phase == 1)
            {
                Check(session.RemainingTime == pausedTime && session.ElapsedTime == pausedElapsed, "Reloj y cronómetro no avanzan en pausa");
                Check(controller.TryDeliverTo(1) == ProjectDeliveryResult.NotPlaying, "Pausa bloquea entregas");
                controller.Resume(); Check(!session.Paused && Time.timeScale == 1, "Continuar reanuda"); Next(2);
            }
            else if (phase == 2)
            {
                Check(session.RemainingTime < pausedTime, "Reloj avanza después de continuar");
                for (int i = 1; i < 5; i++) { Near(controller, i); controller.SelectProject(i); Check(controller.TryDeliverTo(i) == ProjectDeliveryResult.Accepted, "Entrega provisional " + (i + 1)); }
                Check(session.Completed && session.DeliveredCount == 5 && Time.timeScale == 0, "Victoria 5/5 muestra resultado y detiene el nivel");
                controller.Restart(); Next(3);
            }
            else if (phase == 3)
            {
                Check(session.DeliveredCount == 0 && !session.Failed && !session.Completed && session.RemainingTime > 175, "Reiniciar restablece partida");
                session.AdvanceTime(session.RemainingTime - 3);
                Near(controller, 0); controller.SelectProject(1); controller.TryDeliverTo(0);
                Check(session.Failed && session.RemainingTime == 0 && session.DeliveredCount == 0, "Una amonestación agota el reloj sin consumir un proyecto"); Next(4);
            }
            else if (phase == 4)
            {
                Check(session.Failed && Time.timeScale == 0, "Derrota por tiempo detiene el nivel");
                Check(controller.GetComponents<AudioSource>().Any(a => a.clip != null && a.clip.name == "YaCASI-Soundtrack-GameOver-TiempoFinal" && !a.loop), "Derrota selecciona la canción original TiempoFinal");
                Finish();
            }
        }
        catch (Exception e) { results.Add("FAIL " + e); Finish(); }
    }
    static void Finish()
    {
        Directory.CreateDirectory("Tests/YC14");
        File.WriteAllLines("Tests/YC14/IntegracionUnity.txt", results);
        Debug.Log("YC14_UNITY_CHECKS: " + string.Join("\n", results));
        SessionState.SetBool(Key, false); SessionState.SetBool(Key + ".Restore", true); EditorApplication.update -= Tick;
        EditorApplication.isPlaying = false;
    }
}
