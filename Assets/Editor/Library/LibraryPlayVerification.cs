using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>Prueba integrada en Play: inventario, sanciones, pausa, regaño, victoria y derrota.</summary>
[InitializeOnLoad]
public static class LibraryPlayVerification
{
    const string Key = "YaCasi.Library.TestStep";
    static readonly List<string> results = new List<string>();
    static double until;
    static float before;
    static LibraryLevelManager manager;
    static LibraryShelf wrong;
    static Vector3 studentStart;
    static LibraryStudent walkingStudent;
    static LibraryPlayVerification() { EditorApplication.update += Tick; }
    [MenuItem("Ya Casi/Nivel 2/Probar partida completa")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene(LibraryLevelBuilder.Path);
        var configuredStudents=Object.FindObjectsByType<LibraryStudent>(FindObjectsSortMode.None);
        SessionState.SetInt("Library.Test.Seated",configuredStudents.Count(s=>s.seated));
        SessionState.SetInt("Library.Test.Walking",configuredStudents.Count(s=>!s.seated));
        SessionState.SetInt(Key, 1);
        SessionState.SetInt("Library.Test.OldSave", PlayerPrefs.GetInt(U2Presentation.SaveKey, 0));
        SessionState.SetInt("Library.Test.OldCompleted", PlayerPrefs.GetInt("YaCasi.Library.Completed", 0));
        SessionState.SetInt("Library.Test.OldBest", PlayerPrefs.GetInt("YaCasi.Library.Best", 0));
        results.Clear();
        File.WriteAllText("LibraryPlayVerification.txt", "Prueba iniciada\n");
        EditorApplication.isPlaying = true;
    }
    static void Check(bool condition, string label)
    {
        if (!condition) throw new InvalidOperationException("FAIL: " + label);
        results.Add("PASS: " + label);
        File.AppendAllText("LibraryPlayVerification.txt", "PASS: " + label + "\n");
    }
    static void Tick()
    {
        int step = SessionState.GetInt(Key, 0);
        if (step == 0 || !EditorApplication.isPlaying || EditorApplication.isCompiling) return;
        try
        {
            if (step == 1)
            {
                manager = Object.FindFirstObjectByType<LibraryLevelManager>();
                if (manager == null || manager.RemainingTime <= 0) return;
                manager.Resume(); manager.librarian.enabled = false;
                Check(manager.shelves.Length == 30, "30 estantes configurados");
                Check(manager.exitZone.leftDoor!=null && manager.exitZone.rightDoor!=null && manager.exitZone.doorFrames.Length==5,"Puerta doble y cinco fotogramas configurados");
                Check(!manager.exitZone.DoorIsOpen,"Puerta cerrada al iniciar sin libros");
                var playerCollider=manager.player.GetComponent<Collider2D>();
                Vector2 feetOffset=(Vector2)playerCollider.bounds.center-(Vector2)manager.player.transform.position;
                foreach(var shelf in manager.shelves.Where(s=>s.section.StartsWith("D")||s.section.StartsWith("E")))
                {
                    var obstacles=Physics2D.OverlapBoxAll(shelf.Position+feetOffset,(Vector2)playerCollider.bounds.size*.95f,0);
                    var blockers=obstacles.Where(c=>!c.isTrigger && c.GetComponentInParent<PlayerMovement>()==null).ToArray();
                    Check(blockers.Length==0,"Acceso libre al estante trasero "+shelf.section+(blockers.Length>0?"; obstáculos: "+string.Join(",",blockers.Select(c=>c.name))+"; pie: "+(shelf.Position+feetOffset)+"; tamaño: "+playerCollider.bounds.size:""));
                }
                var students = Object.FindObjectsByType<LibraryStudent>(FindObjectsSortMode.None);
                Check(students.Count(s => s.seated) == SessionState.GetInt("Library.Test.Seated",0) && students.Count(s => !s.seated) == SessionState.GetInt("Library.Test.Walking",0), "Se conservan los alumnos configurados en la escena actual");
                walkingStudent = students.First(s => !s.seated); studentStart = walkingStudent.transform.position;
                var assign = typeof(LibraryLevelManager).GetMethod("AssignBooks",System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var layouts = new System.Collections.Generic.HashSet<string>();
                for (int round = 0; round < 24; round++)
                {
                    assign.Invoke(manager,null);
                    Check(manager.shelves.Where(s=>s.bookIndex>=0).Select(s=>s.bookIndex).Distinct().Count()==3,"Sorteo " + round + ": tres libros sin duplicar");
                    layouts.Add(string.Join(",",manager.shelves.Select(s=>s.bookIndex)));
                }
                Check(layouts.Count > 1,"Los libros cambian de estante entre partidas");
                Check(manager.shelves.Count(s => s.bookIndex >= 0) == 3, "Tres libros distintos asignados");
                Check(manager.CollectedCount == 0, "Inventario vacío al iniciar");
                wrong = manager.shelves.First(s => s.bookIndex < 0);
                before = manager.RemainingTime;
                manager.Review(wrong);
                Check(Mathf.Abs(before - manager.RemainingTime - 10) < .01f, "Estante incorrecto resta exactamente 10 s");
                before = manager.RemainingTime; manager.Review(wrong);
                Check(Mathf.Abs(before - manager.RemainingTime - 10) < .01f, "Revisar el mismo estante vacío vuelve a restar 10 s");
                manager.player.transform.position = manager.exit.position;
                until = EditorApplication.timeSinceStartup + .4;
                SessionState.SetInt(Key, 2);
            }
            else if (step == 2 && EditorApplication.timeSinceStartup > until)
            {
                manager.Interact(); Check(!manager.IsCompleted, "Salida bloqueada sin tres libros");
                manager.TogglePause(); before = manager.RemainingTime;
                until = EditorApplication.timeSinceStartup + .5; SessionState.SetInt(Key, 3);
            }
            else if (step == 3 && EditorApplication.timeSinceStartup > until)
            {
                Check(Mathf.Abs(before - manager.RemainingTime) < .01f, "Pausa detiene el reloj");
                manager.Resume(); manager.Reprimand(); manager.Reprimand();
                Check(manager.Reprimands == 1 && !manager.player.enabled, "Regaño inmoviliza y evita acumulación");
                until = EditorApplication.timeSinceStartup + 3.4; SessionState.SetInt(Key, 4);
            }
            else if (step == 4 && EditorApplication.timeSinceStartup > until)
            {
                Check(manager.player.enabled, "Movimiento restaurado tras tres segundos");
                Check(Object.FindObjectsByType<LibraryStudent>(FindObjectsSortMode.None).Where(s=>!s.seated).All(s=>s.DistanceTravelled>.15f), "Los seis alumnos recorren la biblioteca durante la partida");
                Check(Object.FindObjectsByType<LibraryStudent>(FindObjectsSortMode.None).Any(s=>s.VisitedStops>0), "Los alumnos se detienen para buscar libros");
                manager.player.transform.position = new Vector3(-10, -6, 0);
                manager.librarian.transform.position = new Vector3(-5, -6, 0);
                manager.librarian.enabled = true;
                TouchGameControls.Direction = Vector2.right; TouchGameControls.Sprint = true;
                until = EditorApplication.timeSinceStartup + .4; SessionState.SetInt(Key, 40);
            }
            else if (step == 40 && EditorApplication.timeSinceStartup > until)
            {
                Check(manager.player.transform.position.x > -9.5f, "El jugador se desplaza con física e input compartidos");
                Check(manager.librarian.Investigating, "La carrera activa detección real por radio de ruido");
                TouchGameControls.Direction = Vector2.zero; TouchGameControls.Sprint = false;
                manager.librarian.enabled = false;
                manager.player.transform.position = new Vector3(0,-4.7f,0);
                Physics2D.SyncTransforms();
                foreach (var shelf in manager.shelves.Where(s => s.bookIndex >= 0)) manager.Review(shelf);
                Check(manager.CollectedCount == 3, "Tres libros recogidos y registrados");
                TouchGameControls.Direction = Vector2.down;
                until = EditorApplication.timeSinceStartup + 1.1; SessionState.SetInt(Key, 5);
            }
            else if (step == 5 && EditorApplication.timeSinceStartup > until)
            {
                TouchGameControls.Direction = Vector2.zero;
                Check(manager.IsCompleted, "Caminar físicamente hacia la salida permite ganar sin pulsar E");
                Check(manager.exitZone.DoorIsOpen,"La puerta se abre al recuperar los tres libros");
                SessionState.SetInt(Key, 6); manager.Restart();
            }
            else if (step == 6)
            {
                manager = Object.FindFirstObjectByType<LibraryLevelManager>();
                if (manager == null || manager.RemainingTime <= 0 || manager.IsCompleted) return;
                manager.Resume(); manager.librarian.enabled = false;
                Check(manager.CollectedCount == 0, "Reinicio vacía inventario");
                var field = typeof(LibraryLevelManager).GetField("remaining", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                field.SetValue(manager, .02f);
                until = EditorApplication.timeSinceStartup + .4; SessionState.SetInt(Key, 7);
            }
            else if (step == 7 && EditorApplication.timeSinceStartup > until)
            {
                Check(manager.IsFailed, "Tiempo agotado muestra derrota");
                var gameOver=Resources.Load<U2GameConfig>("U2GameConfig").gameOverSound;
                Check(gameOver!=null && manager.GetComponents<AudioSource>().Any(s=>s.clip==gameOver && s.isPlaying && !s.loop && s.ignoreListenerPause),"Derrota reproduce la misma música Game Over del nivel 1");
                File.AppendAllText("LibraryPlayVerification.txt", "LIBRARY_PLAY_VERIFIED_OK\n");
                Debug.Log("LIBRARY_PLAY_VERIFIED_OK: inventario, penalizaciones, pausa, regaño, victoria, reinicio y derrota.");
                Stop();
            }
        }
        catch (Exception e)
        {
            File.AppendAllText("LibraryPlayVerification.txt", e + "\n");
            Debug.LogException(e); Stop();
        }
    }
    static void Stop()
    {
        SessionState.SetInt(Key, 0);
        int save = SessionState.GetInt("Library.Test.OldSave", 0);
        if (save == 0) PlayerPrefs.DeleteKey(U2Presentation.SaveKey); else PlayerPrefs.SetInt(U2Presentation.SaveKey, save);
        PlayerPrefs.SetInt("YaCasi.Library.Completed", SessionState.GetInt("Library.Test.OldCompleted", 0));
        PlayerPrefs.SetInt("YaCasi.Library.Best", SessionState.GetInt("Library.Test.OldBest", 0));
        PlayerPrefs.Save(); EditorApplication.isPlaying = false;
    }
}
