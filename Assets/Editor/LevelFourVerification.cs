using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[InitializeOnLoad]
public static class LevelFourVerification
{
    static int step, checks;
    static double until, deadline;
    static string errors = "";
    static LevelFourIntro intro;
    static Vector3 beforePause;
    static float entryX;
    static LevelFourVerification() { EditorApplication.update += Tick; }

    static void Tick()
    {
        if (!EditorApplication.isPlaying || !SessionState.GetBool("Level4.RunTest", false)) return;
        if (EditorApplication.timeSinceStartup < until) return;
        try
        {
            intro = UnityEngine.Object.FindFirstObjectByType<LevelFourIntro>();
            if (intro == null || intro.player == null) return;
            if (step > 0 && EditorApplication.timeSinceStartup > deadline) throw new Exception("La prueba excedió 45 segundos.");
            switch (step)
            {
                case 0:
                    if (UnityEngine.Object.FindFirstObjectByType<Canvas>() == null) return;
                    errors = ""; checks = 0; deadline = EditorApplication.timeSinceStartup + 45;
                    Check(intro.CurrentBeat == LevelFourIntro.Beat.Friends, "Inicio con amigos");
                    Check(UnityEngine.Object.FindFirstObjectByType<CampusDeliveryObjectives>() == null, "Sin misiones del nivel 3");
                    var students = UnityEngine.Object.FindObjectsByType<LevelFourStudent>(FindObjectsSortMode.None);
                    Check(students.Length == 24, "24 alumnos reunidos");
                    Check(students.Select(s => s.sheet).Distinct().Count() == 10, "10 personajes diferentes");
                    var lights = UnityEngine.Object.FindObjectsByType<Light2D>(FindObjectsSortMode.None);
                    Check(lights.Count(l => l.lightType == Light2D.LightType.Point && l.name.StartsWith("Luz cálida ")) == 7, "7 lámparas con luz 2D");
                    Check(lights.Any(l => l.lightType == Light2D.LightType.Global && l.intensity < .6f), "Ambiente nocturno");
                    Check(UnityEngine.Object.FindObjectsByType<LevelFourBubbleTail>(FindObjectsSortMode.None).Length == 2, "Globos con cola sobre los personajes");
                    Check(GameObject.Find("Tramo señalado de la malla") == null && GameObject.Find("Señal del salto") == null, "Sin entrada dibujada");
                    ScreenCapture.CaptureScreenshot("Level4Work/01-Amigos.png"); Next(1); break;
                case 1:
                    intro.Advance(); Check(intro.CurrentBeat == LevelFourIntro.Beat.Notification, "Aviso recibido");
                    ScreenCapture.CaptureScreenshot("Level4Work/04-Globos.png"); Next(.8); break;
                case 2:
                    intro.Advance(); Check(intro.CurrentBeat == LevelFourIntro.Beat.Phone, "Celular abierto");
                    ScreenCapture.CaptureScreenshot("Level4Work/02-Celular.png"); Next(.8); break;
                case 3:
                    intro.TogglePause(); intro.Advance(); Check(intro.CurrentBeat == LevelFourIntro.Beat.Phone, "Pausa bloquea avance");
                    intro.TogglePause(); intro.Advance(); Check(intro.CurrentBeat == LevelFourIntro.Beat.Drinking, "Cerveza tras el mensaje"); Next(3); break;
                case 4:
                    Check(intro.CurrentBeat == LevelFourIntro.Beat.Alley, "Exploración tras la cerveza");
                    intro.Advance(); Check(intro.CurrentBeat == LevelFourIntro.Beat.Alley, "No trepa desde lejos");
                    foreach (float x in new[] { intro.alleyMin.x + 1, 0, intro.alleyMax.x - 1 })
                        Check(intro.CanClimbAt(new Vector2(x, intro.alleyMax.y)), "Puede trepar en x=" + x);
                    Check(!intro.CanClimbAt(new Vector2(intro.alleyMin.x - 1, intro.alleyMax.y)), "Respeta el borde lateral");
                    intro.TogglePause(); beforePause = intro.player.position; Next(.5); break;
                case 5:
                    Check(Vector3.Distance(beforePause, intro.player.position) < .001f, "Pausa mantiene al jugador quieto");
                    intro.TogglePause(); entryX = 14;
                    var entry = new Vector3(entryX, intro.alleyMax.y, 0);
                    intro.player.GetComponent<Rigidbody2D>().position = entry; intro.player.position = entry; Next(.2); break;
                case 6:
                    intro.Advance(); Check(intro.CurrentBeat == LevelFourIntro.Beat.Climbing, "Trepa desde otro tramo de malla"); Next(1.3); break;
                case 7:
                    Check(UnityEngine.Object.FindFirstObjectByType<LevelFourClimbRig>() != null, "Manos y piernas articuladas sobre la malla");
                    ScreenCapture.CaptureScreenshot("Level4Work/05-Escalada.png");
                    Next(.3); break;
                case 8:
                    intro.TogglePause(); beforePause = intro.player.position; Next(.5); break;
                case 9:
                    Check(Vector3.Distance(beforePause, intro.player.position) < .001f, "Pausa detiene también la escalada");
                    intro.TogglePause(); Next(3); break;
                case 10:
                    Check(intro.CurrentBeat == LevelFourIntro.Beat.End, "Final después de escalar");
                    Check(Vector3.Distance(intro.player.position, intro.landing.position) < .01f, "Aterriza dentro del campus");
                    Check(Mathf.Abs(intro.player.position.x - entryX) < .01f, "Entra donde eligió el jugador");
                    Check(intro.player.GetComponent<SpriteRenderer>().enabled && intro.player.GetComponent<Rigidbody2D>().simulated, "Restaura personaje y físicas");
                    ScreenCapture.CaptureScreenshot("Level4Work/03-DentroDelCampus.png"); Next(.8); break;
                case 11: Finish(); break;
            }
        }
        catch (Exception e) { errors += e + "\n"; Finish(); }
    }
    static void Next(double wait) { step++; until = EditorApplication.timeSinceStartup + wait; }
    static void Finish()
    {
        File.WriteAllText("Level4Work/Verificacion.txt", string.IsNullOrEmpty(errors) ? "OK: " + checks + " comprobaciones. Historia, globos, 24 alumnos/10 personajes, noche, escalada libre y pausa.\n" : errors);
        SessionState.SetBool("Level4.RunTest", false); step = 0; EditorApplication.isPlaying = false;
    }
    static void Check(bool condition, string description) { checks++; if (!condition) errors += "ERROR: " + description + "\n"; }
}

