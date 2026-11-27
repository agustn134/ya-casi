using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Comprueba las reglas aprobadas y recorre los objetivos reales de la misión.</summary>
[InitializeOnLoad]
public static class LevelFourMissionVerification
{
    static int step, checks;
    static double until, deadline;
    static string errors;
    static LevelFourMission mission;
    static LevelFourIntro intro;
    static LevelFourMotor motor;
    static LevelFourStudent victim;
    static Vector3 original;
    static int aiProbe;
    static Vector3 guardBefore;
    static bool pauseStarted, officeTravelStarted, laptopOpened, loginSubmitted;

    static LevelFourMissionVerification() { EditorApplication.update += Tick; }
    [MenuItem("Ya Casi/Nivel 4/Probar misión completa")]
    public static void Begin()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Detén Play antes de iniciar la prueba.");
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Se conserva la escena sin guardar antes de probar.");
        EditorSceneManager.OpenScene(LevelFourBuilder.ScenePath);
        SessionState.SetBool("Level4.RunMissionTest", true); EditorApplication.isPlaying = true;
    }
    static void Tick()
    {
        if (!EditorApplication.isPlaying || !SessionState.GetBool("Level4.RunMissionTest", false) || EditorApplication.timeSinceStartup < until) return;
        try
        {
            mission = UnityEngine.Object.FindFirstObjectByType<LevelFourMission>();
            intro = UnityEngine.Object.FindFirstObjectByType<LevelFourIntro>();
            if (mission == null || intro == null || UnityEngine.Object.FindFirstObjectByType<Canvas>() == null) return;
            motor = intro.player.GetComponent<LevelFourMotor>();
            if (step > 0 && EditorApplication.timeSinceStartup > deadline) throw new Exception("La prueba excedió 100 segundos.");
            switch (step)
            {
                case 0:
                    checks = 0; errors = ""; deadline = EditorApplication.timeSinceStartup + 100;
                    aiProbe = 0; pauseStarted = officeTravelStarted = laptopOpened = loginSubmitted = false;
                    var rules = new LevelFourSocialRules();
                    Check(rules.Contact(0, false) == 0, "Caminar no provoca");
                    Check(rules.Contact(0, true) == 1, "Primer choque avisa");
                    Check(rules.Contact(.2f, true) == 0, "Un contacto sostenido no suma varios golpes");
                    Check(rules.Contact(1, true) == 2, "Segundo choque empuja");
                    Check(rules.Contact(2, true) == 3, "Tercer choque provoca persecución");
                    Check(rules.Contact(10, true) == 1, "Los avisos caducan al alejarse");
                    var alarm = new LevelFourAlarm(); alarm.Start();
                    Check(alarm.Remaining == 180 && alarm.Running, "La alarma inicia exactamente en 180 segundos");
                    alarm.Tick(20, true); Check(alarm.Remaining == 180, "La pausa detiene el reloj");
                    alarm.Tick(179, false); Check(!alarm.Expired && alarm.Remaining == 1, "No vence antes de tres minutos");
                    alarm.Tick(1, false); Check(alarm.Expired, "Vence al cumplirse tres minutos");
                    alarm.Stop(); alarm.Tick(10, false); Check(!alarm.Running && alarm.Remaining == 0, "Se detiene al salir");
                    var students = UnityEngine.Object.FindObjectsByType<LevelFourStudent>(FindObjectsSortMode.None);
                    Check(students.Length == 24 && students.All(s => s.GetComponent<CapsuleCollider2D>() != null && !s.GetComponent<CapsuleCollider2D>().isTrigger), "24 alumnos sólidos");
                    Check(students.Select(s => s.team).Distinct().Count() == 4, "Cuatro equipos del callejón");
                    Check(mission.guards.Length == 12 && mission.guards.Count(g => g.isDog) == 4, "8 guardias y 4 perros");
                    Check(UnityEngine.Object.FindObjectsByType<LevelFourSensor>(FindObjectsSortMode.None).Length == 5, "Cinco sensores intermitentes");
                    Check(mission.trunkAreas.Length > 0, "Troncos detectados para colisiones puntuales");
                    int freeGrass = 0;
                    foreach (var cell in mission.gardens.cellBounds.allPositionsWithin)
                    {
                        if (!mission.gardens.HasTile(cell)) continue;
                        Vector2 point = mission.gardens.GetCellCenterWorld(cell);
                        if (mission.paths != null && mission.paths.HasTile(mission.paths.WorldToCell(point))) continue;
                        if (mission.Walkable(point + Vector2.up * .5f)) freeGrass++;
                    }
                    Check(freeGrass > 20, "El pasto fuera de los caminos es transitable");
                    bool gap = false;
                    for (int i = 0; i < mission.trunkAreas.Length && !gap; i++) for (int j = i + 1; j < mission.trunkAreas.Length; j++)
                    {
                        var a = mission.trunkAreas[i].center; var b = mission.trunkAreas[j].center;
                        if (Vector2.Distance(a, b) > 6 || Vector2.Distance(a, b) < 1.5f) continue;
                        if (mission.Walkable((a + b) * .5f + Vector2.up * .5f)) { gap = true; break; }
                    }
                    Check(gap, "Hay paso libre entre árboles cercanos");
                    if (mission.trunkAreas.Length > 0)
                    {
                        var trunk = mission.trunkAreas.First(r => r.center.x > -50 && r.center.x < 50 && r.center.y > -48 && r.center.y < 47);
                        Check(!mission.Walkable(trunk.center + Vector2.up * .5f), "El tronco es sólido y el pasto circundante no se bloquea en bloque");
                    }
                    intro.Advance(); intro.Advance(); intro.Advance(); Next(3); break;
                case 1:
                    Check(intro.CurrentBeat == LevelFourIntro.Beat.Alley, "Se conserva la apertura");
                    victim = UnityEngine.Object.FindObjectsByType<LevelFourStudent>(FindObjectsSortMode.None).OrderBy(s => s.transform.position.x).First();
                    original = intro.player.position; mission.Teleport(victim.transform.position + Vector3.left * 1.2f);
                    motor.Sprinting = false;
                    var beforeContact = (Vector2)intro.player.position;
                    motor.Move(Vector2.right * 1.5f, intro.alleyMin, intro.alleyMax);
                    Check(motor.LastResolvedTarget.x < victim.transform.position.x - .5f, "El motor detiene el avance antes del alumno sólido");
                    Check(motor.LastResolvedTarget.x > beforeContact.x, "El jugador puede acercarse hasta el borde del collider");
                    mission.Teleport(beforeContact);
                    motor.Sprinting = true;
                    Check(motor.ContactStudent(victim, true) == 1, "Aviso real de alumno"); Next(1.1); break;
                case 2:
                    Check(motor.ContactStudent(victim, true) == 2, "Empujón real de alumno");
                    mission.Teleport(victim.transform.position + Vector3.left);
                    int healthBefore = victim.GetComponent<LevelFourFighter>().Health;
                    motor.Attack();
                    Check(victim.GetComponent<LevelFourFighter>().Health < healthBefore, "El golpe del jugador inflige daño real");
                    Next(1.1); break;
                case 3:
                    Check(motor.ContactStudent(victim, true) == 3, "Tercer impacto real");
                    Check(victim.transform.parent.GetComponentsInChildren<LevelFourStudent>().Count(s => s.Aggressive) == 2, "Persiguen el afectado y un amigo");
                    ScreenCapture.CaptureScreenshot("Level4Work/06-Conflicto.png");
                    mission.Teleport(original); Next(3.5); break;
                case 4:
                    Check(!victim.Aggressive, "Se calman tras alejarse");
                    mission.Teleport(new Vector3(14, intro.alleyMax.y, 0)); intro.Advance(); Next(4.1); break;
                case 5:
                    if (aiProbe == 0)
                    {
                        Vector2 origin = mission.guards[0].transform.position;
                        Vector2 near = origin + Vector2.down * 1.5f;
                        foreach (var direction in new[] { Vector2.down, Vector2.up, Vector2.left, Vector2.right })
                            if (mission.Walkable(origin + direction * 1.5f)) { near = origin + direction * 1.5f; break; }
                        mission.Teleport(near);
                        Check(mission.guards[0].Sees(near, mission), "El guardia detecta al jugador con línea de visión");
                        Check(LevelFourNavigation.Find(origin, near, mission).Count > 0, "La persecución calcula una ruta caminable");
                        aiProbe = 1; until = EditorApplication.timeSinceStartup + .3; return;
                    }
                    if (aiProbe == 1)
                    {
                        Check(mission.guards[0].Chasing, "El guardia comienza una persecución real");
                        var fighter = mission.guards[0].GetComponent<LevelFourFighter>(); int previousHealth = fighter.Health;
                        guardBefore = mission.guards[0].transform.position;
                        Check(fighter.TakeHit(1, Vector2.right, true) && fighter.Health == previousHealth - 1, "El empujón también causa daño");
                        aiProbe = 2; until = EditorApplication.timeSinceStartup + .5; return;
                    }
                    if (aiProbe == 2)
                    {
                        Check(mission.guards[0].GetComponent<LevelFourFighter>().Downed, "El guardia cae al suelo");
                        Check(!mission.guards[0].GetComponent<Collider2D>().enabled, "Se puede pasar junto al guardia derribado");
                        Check(Vector3.Distance(guardBefore, mission.guards[0].transform.position) > .1f, "El empujón desplaza físicamente al oponente");
                        ScreenCapture.CaptureScreenshot("Level4Work/13-Defensa.png");
                        foreach (var guard in mission.guards) guard.Stun(60);
                        guardBefore = mission.guards[0].transform.position;
                        aiProbe = 3; until = EditorApplication.timeSinceStartup + .3; return;
                    }
                    if (aiProbe == 3)
                    {
                        Check(Vector3.Distance(guardBefore, mission.guards[0].transform.position) < .001f, "Aturdir detiene físicamente al guardia");
                        var map = mission.GetComponent<LevelFourMinimap>();
                        Check(map != null && map.Visible, "Minimapa visible después de trepar");
                        Check(Vector2.Distance(map.TrackedPlayer, intro.player.position) < .4f, "El mapa sigue a Galindo");
                        Check(map.HasRoute && map.RouteDistance > 0, "Ruta y distancia caminable al F");
                        Check(mission.gardens.GetComponent<UnityEngine.Tilemaps.TilemapRenderer>().sortingOrder < intro.player.GetComponent<SpriteRenderer>().sortingOrder, "Los jardines no tapan al personaje");
                        bool testedCorner = false;
                        foreach (var cell in mission.roofs.cellBounds.allPositionsWithin)
                        {
                            if (!mission.roofs.HasTile(cell) || mission.roofs.HasTile(cell + Vector3Int.right)) continue;
                            Vector2 center = mission.roofs.GetCellCenterWorld(cell);
                            float width = Vector2.Distance(center, mission.roofs.GetCellCenterWorld(cell + Vector3Int.right));
                            Vector2 edge = center + Vector2.right * (width * .5f + .08f) + Vector2.up * .5f;
                            if (mission.roofs.HasTile(mission.roofs.WorldToCell(edge + Vector2.down * .5f))) continue;
                            Check(!mission.Walkable(edge), "No permite introducir una parte de los pies en el edificio");
                            Check(mission.Walkable(mission.NearestClear(edge)), "Recupera una posición libre junto al borde");
                            testedCorner = true; break;
                        }
                        Check(testedCorner, "Regresión del borde de edificio ejecutada");
                        map.Toggle(); Check(map.Expanded, "M permite ampliar el mapa");
                        ScreenCapture.CaptureScreenshot("Level4Work/14-MapaCampus.png");
                        aiProbe = 4; until = EditorApplication.timeSinceStartup + .3; return;
                    }
                    if (mission.GetComponent<LevelFourMinimap>().Expanded) mission.GetComponent<LevelFourMinimap>().Toggle();
                    Check(mission.CurrentStage == LevelFourMission.Stage.Campus, "La historia continúa dentro del campus");
                    Check(!mission.Alarm.Running, "El campus inicial no tiene cuenta regresiva");
                    Check(mission.Walkable(mission.entranceF.position), "El panel del F tiene acceso caminable");
                    Check(mission.guards.All(g => g.IsStunned), "Golpes permiten detener temporalmente guardias y perros");
                    mission.Teleport(mission.entranceF.position); mission.Interact();
                    Check(mission.CurrentStage == LevelFourMission.Stage.Terminal, "Conexión del celular y terminal local"); Next(1.6); break;
                case 6:
                    Check(mission.AwaitingCredentials && mission.CredentialsBlank, "Los campos de credenciales esperan al jugador y están vacíos");
                    Check(!mission.TryAlarmCredentials("admin", "incorrecta"), "Rechaza contraseña incorrecta");
                    Check(!mission.Alarm.Running && mission.CurrentStage == LevelFourMission.Stage.Terminal, "No desactiva la alarma ni entra sin validar");
                    ScreenCapture.CaptureScreenshot("Level4Work/07-Terminal.png"); Next(.3); break;
                case 7:
                    if (!loginSubmitted) { Check(mission.TryAlarmCredentials("admin", "admin"), "Valida las credenciales introducidas por el jugador"); loginSubmitted = true; until = EditorApplication.timeSinceStartup + 1.4; return; }
                    Check(mission.CurrentStage == LevelFourMission.Stage.Ground, "Carga a negro e ingreso al F");
                    mission.BoundsForStage(out var lobbyMin, out var lobbyMax);
                    Check(lobbyMax.x - lobbyMin.x > 11, "Nuevo vestíbulo amplio y transitable");
                    Check(mission.Alarm.Running && mission.Alarm.Remaining > 170 && mission.Alarm.Remaining < 180, "El reloj empieza tras desactivar la alarma");
                    ScreenCapture.CaptureScreenshot("Level4Work/08-InteriorF.png");
                    Next(.3); break;
                case 8:
                    if (!pauseStarted) { intro.TogglePause(); original = new Vector3(mission.Alarm.Remaining, 0); pauseStarted = true; until = EditorApplication.timeSinceStartup + .6; return; }
                    Check(Mathf.Abs(mission.Alarm.Remaining - original.x) < .001f, "La pausa real detiene el reloj"); intro.TogglePause();
                    var activeSensor = UnityEngine.Object.FindObjectsByType<LevelFourSensor>(FindObjectsSortMode.None).First(s => s.IsActive);
                    Check(activeSensor.Detects(activeSensor.transform.position, true), "Un haz activo detecta movimiento dentro de su área");
                    Check(!activeSensor.Detects(activeSensor.transform.position, false), "Quedarse quieto no activa el sensor");
                    Check(!activeSensor.Detects((Vector2)activeSensor.transform.position + Vector2.up * 2, true), "No detecta fuera del haz");
                    mission.Teleport(mission.groundStairs.position); mission.Interact(); Next(.9); break;
                case 9:
                    Check(mission.CurrentStage == LevelFourMission.Stage.Upper, "Sube a cubículos de planta alta");
                    ScreenCapture.CaptureScreenshot("Level4Work/09-Cubiculos.png");
                    Next(.3); break;
                case 10:
                    if (!officeTravelStarted) { mission.Teleport(mission.torresDoor.position); mission.Interact(); officeTravelStarted = true; until = EditorApplication.timeSinceStartup + .9; return; }
                    Check(mission.CurrentStage == LevelFourMission.Stage.Office, "Entra al cubículo de Torres por la izquierda");
                    ScreenCapture.CaptureScreenshot("Level4Work/10-Torres.png");
                    Next(.4); break;
                case 11:
                    if (!laptopOpened) { mission.Teleport(mission.laptop.position + Vector3.down * .7f); mission.Interact(); laptopOpened = true; until = EditorApplication.timeSinceStartup + .3; return; }
                    Check(mission.CurrentStage == LevelFourMission.Stage.Laptop, "Laptop interactiva");
                    mission.SelectGroup("GIMT6091-E"); Check(!mission.SaveGrade(8), "Otro grupo no modifica a Galindo");
                    mission.SelectGroup("GIDS6093-E"); Check(!mission.SaveGrade(7), "La misión pide específicamente 8");
                    Check(mission.SaveGrade(8) && mission.GradeSaved, "Guarda el 8 de Galindo en GIDS6093-E");
                    Check(mission.Alarm.Running, "La laptop no pausa la alarma");
                    ScreenCapture.CaptureScreenshot("Level4Work/11-SIGA.png"); Next(.5); break;
                case 12:
                    mission.CloseLaptop(); mission.Teleport(mission.officeExit.position); mission.Interact(); Next(.9); break;
                case 13:
                    Check(mission.CurrentStage == LevelFourMission.Stage.Upper, "Regresa al pasillo");
                    mission.Teleport(mission.upperBack.position); mission.Interact(); Next(.9); break;
                case 14:
                    Check(mission.CurrentStage == LevelFourMission.Stage.Ground, "Baja de nuevo");
                    mission.Teleport(mission.groundExit.position); mission.Interact(); Next(.9); break;
                case 15:
                    Check(mission.CurrentStage == LevelFourMission.Stage.Escape, "Sale del F con el resultado guardado");
                    Check(!mission.Alarm.Running, "El regreso a la malla no tiene tiempo");
                    mission.Teleport(new Vector3(-19, -48.2f)); Check(mission.CanReturnOverFence(), "Se puede salir por otro tramo de malla");
                    mission.Interact(); Next(3.2); break;
                case 16:
                    Check(mission.CurrentStage == LevelFourMission.Stage.Reunion, "Regresa al callejón");
                    Check(Mathf.Abs(intro.player.position.x + 19) < .02f, "Conserva el tramo elegido para regresar");
                    mission.Teleport(mission.softwareGroup.position); mission.Interact(); Next(.5); break;
                case 17:
                    Check(mission.CurrentStage == LevelFourMission.Stage.Won, "Final con Team Software");
                    ScreenCapture.CaptureScreenshot("Level4Work/12-FinalSoftware.png"); Next(.7); break;
                case 18: Finish(); break;
            }
        }
        catch (Exception e) { errors += e + "\n"; Finish(); }
    }
    static void Next(double wait) { step++; until = EditorApplication.timeSinceStartup + wait; }
    static void Check(bool condition, string text) { checks++; if (!condition) errors += "ERROR: " + text + "\n"; }
    static void Finish()
    {
        File.WriteAllText("Level4Work/VerificacionMision.txt", string.IsNullOrEmpty(errors) ? "OK: " + checks + " comprobaciones. Conflicto, sólidos, 180 segundos, F, cubículos, laptop y regreso a Software.\n" : errors);
        SessionState.SetBool("Level4.RunMissionTest", false); step = 0; EditorApplication.isPlaying = false;
    }
}
