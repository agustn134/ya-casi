using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>Comprueba el reto final en la escena provisional sin modificar el campus pintado.</summary>
[InitializeOnLoad]
public static class CampusFinalChallengeVerification
{
    const string Key = "YaCasi.YC14.FinalChallengeTesting";
    const string ScenePath = "Assets/Scenes/Nivel03_PruebaEntregas.unity";
    const int LastProfessor = 2;
    static readonly List<string> results = new List<string>();
    static int phase;
    static bool attached;
    static double due, deadline;
    static float pausedRemaining, pausedElapsed, pausedChallengeElapsed;
    static Vector3[] pausedProjectiles;
    static BoxCollider2D dodgeObstacle;
    static Tilemap dodgePath;
    static Vector2 dodgeStartFeet;
    static float dodgeStartTime;
    static bool repeatDodgeChecked;

    static CampusFinalChallengeVerification()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        if (SessionState.GetBool(Key, false)) EditorApplication.delayCall += Attach;
    }

    [MenuItem("Ya Casi/Nivel 3/Verificar reto final")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        SessionState.SetString(Key + ".Scene", SceneManager.GetActiveScene().path);
        SessionState.SetBool(Key + ".Restore", true);
        results.Clear();
        EditorSceneManager.OpenScene(ScenePath);
        SessionState.SetBool(Key, true);
        EditorApplication.isPlaying = true;
        EditorApplication.delayCall += Attach;
    }

    static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredEditMode) return;
        if (SessionState.GetBool(Key, false))
        {
            results.Add("FAIL La ejecución se interrumpió antes de completar las comprobaciones.");
            WriteResults(false);
            SessionState.SetBool(Key, false);
            Detach();
        }
        if (!SessionState.GetBool(Key + ".Restore", false)) return;
        SessionState.SetBool(Key + ".Restore", false);
        string original = SessionState.GetString(Key + ".Scene", "");
        if (!string.IsNullOrEmpty(original)) EditorSceneManager.OpenScene(original);
    }

    static void Attach()
    {
        if (attached || !SessionState.GetBool(Key, false)) return;
        attached = true;
        phase = -2;
        repeatDodgeChecked = false;
        due = EditorApplication.timeSinceStartup + .5;
        deadline = due + 60;
        results.Clear();
        results.Add("Unity " + Application.unityVersion);
        results.Add("Escena provisional: " + ScenePath);
        results.Add("Inicio UTC: " + DateTime.UtcNow.ToString("O"));
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
    }

    static void Detach()
    {
        attached = false;
        EditorApplication.update -= Tick;
    }

    static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        results.Add("PASS " + message);
    }

    static void Next(int next, double wait = 0)
    {
        phase = next;
        due = EditorApplication.timeSinceStartup + wait;
    }

    static void Near(CampusDeliveryObjectives controller, int index)
    {
        var data = new SerializedObject(controller);
        var professor = (Transform)data.FindProperty("professors").GetArrayElementAtIndex(index).objectReferenceValue;
        var player = (Transform)data.FindProperty("player").objectReferenceValue;
        player.position = professor.position + Vector3.down;
        player.GetComponent<Rigidbody2D>().position = player.position;
        Physics2D.SyncTransforms();
    }

    static string LastProject(CampusDeliveryObjectives controller)
    {
        return new SerializedObject(controller).FindProperty("projectIds")
            .GetArrayElementAtIndex(LastProfessor).stringValue;
    }

    static CampusPathPlayer Player(CampusDeliveryObjectives controller)
    {
        var actor = (Transform)new SerializedObject(controller).FindProperty("player").objectReferenceValue;
        return actor.GetComponent<CampusPathPlayer>();
    }

    static void PrepareDodge(CampusPathPlayer actor)
    {
        dodgePath = (Tilemap)new SerializedObject(actor).FindProperty("path").objectReferenceValue;
        Check(actor.enabled && dodgePath != null, "El jugador y su camino están preparados para un esquive real.");
        Vector2 nearest = Vector2.zero;
        float distance = float.PositiveInfinity;
        foreach (Vector3Int cell in dodgePath.cellBounds.allPositionsWithin)
        {
            bool interior = true;
            for (int x = -1; x <= 1 && interior; x++)
                for (int y = -1; y <= 1; y++)
                    if (!dodgePath.HasTile(cell + new Vector3Int(x, y, 0))) { interior = false; break; }
            if (!interior) continue;
            Vector2 center = dodgePath.GetCellCenterWorld(cell);
            float candidate = (center - actor.FeetPosition).sqrMagnitude;
            if (candidate >= distance) continue;
            distance = candidate;
            nearest = center;
        }
        Check(!float.IsPositiveInfinity(distance), "La prueba encuentra una zona de camino de tres por tres celdas cerca del inicio.");
        var feet = actor.GetComponent<CapsuleCollider2D>();
        var body = actor.GetComponent<Rigidbody2D>();
        body.position = nearest - feet.offset;
        actor.transform.position = body.position;
        body.linearVelocity = Vector2.zero;
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        typeof(CampusPathPlayer).GetField("input", flags).SetValue(actor, Vector2.zero);
        typeof(CampusPathPlayer).GetField("lastDirection", flags).SetValue(actor, Vector2.down);
        dodgeStartFeet = nearest;
        var obstacle = new GameObject("Obstáculo provisional de verificación del esquive");
        obstacle.transform.position = nearest + Vector2.down * .7f;
        dodgeObstacle = obstacle.AddComponent<BoxCollider2D>();
        dodgeObstacle.size = new Vector2(.8f, .1f);
        Physics2D.SyncTransforms();
        dodgeStartTime = Time.time;
        actor.TryDodge();
        Check(actor.IsInvulnerable && actor.DodgeReadyIn > 1f,
            "El esquive real activa invulnerabilidad y su tiempo de recuperación.");
    }

    static bool FeetRemainOnPath(CampusPathPlayer actor)
    {
        Bounds bounds = actor.GetComponent<CapsuleCollider2D>().bounds;
        for (int x = 0; x <= 2; x++)
            for (int y = 0; y <= 2; y++)
            {
                Vector3 sample = new Vector3(Mathf.Lerp(bounds.min.x, bounds.max.x, x * .5f),
                    Mathf.Lerp(bounds.min.y, bounds.max.y, y * .5f), bounds.center.z);
                if (!dodgePath.HasTile(dodgePath.WorldToCell(sample))) return false;
            }
        return true;
    }

    static void ClearDodgeObstacle()
    {
        if (dodgeObstacle != null) UnityEngine.Object.DestroyImmediate(dodgeObstacle.gameObject);
        dodgeObstacle = null;
        Physics2D.SyncTransforms();
    }

    static void Tick()
    {
        if (EditorApplication.isCompiling || EditorApplication.timeSinceStartup < due) return;
        try
        {
            if (EditorApplication.timeSinceStartup > deadline)
                throw new InvalidOperationException("El reto final excedió los sesenta segundos reales de prueba.");
            if (!EditorApplication.isPlaying) return;
            var controller = UnityEngine.Object.FindFirstObjectByType<CampusDeliveryObjectives>();
            if (controller == null || controller.Session == null) return;
            var session = controller.Session;

            if (phase == -2)
            {
                Check(session.Playing && Time.timeScale == 1f, "La prueba del esquive comienza con la simulación activa.");
                PrepareDodge(Player(controller));
                Next(-1, .06);
            }
            else if (phase == -1)
            {
                var actor = Player(controller);
                if (!repeatDodgeChecked)
                {
                    if (Time.time - dodgeStartTime < .03f) return;
                    float ready = actor.DodgeReadyIn;
                    Check(ready > 0f, "La recuperación sigue activa al intentar repetir el esquive.");
                    actor.TryDodge();
                    Check(Mathf.Abs(actor.DodgeReadyIn - ready) < .001f,
                        "Un segundo esquive inmediato no reinicia ni prolonga la recuperación.");
                    repeatDodgeChecked = true;
                    due = EditorApplication.timeSinceStartup + .3;
                    return;
                }
                Bounds feet = actor.GetComponent<CapsuleCollider2D>().bounds;
                Check(actor.FeetPosition.y < dodgeStartFeet.y - .1f,
                    "El esquive desplaza realmente al jugador hacia el obstáculo.");
                Check(feet.min.y >= dodgeObstacle.bounds.max.y - .02f
                    && Mathf.Abs(actor.FeetPosition.x - dodgeStartFeet.x) < .02f,
                    "El esquive no atraviesa el obstáculo ni se desvía de su trayectoria.");
                Check(FeetRemainOnPath(actor), "La huella de los pies permanece dentro del camino después del esquive.");
                ClearDodgeObstacle();
                Next(0);
            }
            else if (phase == 0)
            {
                Check(session.RequiredCount == 5 && session.DeliveredCount == 0,
                    "La escena provisional comienza con cinco entregas pendientes.");
                foreach (int index in new[] { 3, 0, 4, 1 })
                {
                    Near(controller, index);
                    controller.SelectProject(index);
                    Check(controller.TryDeliverTo(index) == ProjectDeliveryResult.Accepted,
                        "Acepta la entrega " + (index + 1) + " en un orden diferente al de la lista.");
                }
                controller.ConfigureFinalChallengeForVerification();
                Check(controller.FinalChallenge != null, "El controlador dispone del reto final.");
                Near(controller, LastProfessor);
                controller.SelectProject(LastProfessor);
                Check(controller.TryDeliverTo(LastProfessor) == ProjectDeliveryResult.ChallengePending,
                    "El último profesor se detecta por su entrega pendiente, aunque no sea el quinto de la lista.");
                Check(session.DeliveredCount == 4 && !session.Completed
                    && session.Inventory.Quantity(LastProject(controller)) == 1,
                    "El reto pendiente conserva el último proyecto y el progreso 4/5.");
                Check(session.Paused && Time.timeScale == 0f && !controller.FinalChallenge.State.Started,
                    "El diálogo final pausa el nivel antes de iniciar las oleadas.");
                pausedRemaining = session.RemainingTime;
                pausedElapsed = session.ElapsedTime;
                Next(1, .5);
            }
            else if (phase == 1)
            {
                Check(session.RemainingTime == pausedRemaining && session.ElapsedTime == pausedElapsed,
                    "El diálogo no consume el reloj ni el cronómetro de recorrido.");
                controller.ResumeFinalChallenge();
                Check(session.Playing && controller.FinalChallenge.State.Started && Time.timeScale == 1f,
                    "Continuar el diálogo inicia el desafío y reanuda el nivel.");
                Check(controller.TryDeliverTo(LastProfessor) == ProjectDeliveryResult.ChallengePending
                    && session.DeliveredCount == 4,
                    "La quinta entrega sigue bloqueada mientras el desafío está activo.");
                float beforeHit = session.RemainingTime;
                float elapsedBeforeHit = session.ElapsedTime;
                Check(controller.ReceiveThrownObjectHit(), "El primer objeto aplica un impacto.");
                Check(Mathf.Abs(beforeHit - session.RemainingTime - 5f) < .01f
                    && session.ElapsedTime == elapsedBeforeHit,
                    "El impacto descuenta cinco segundos sin alterar el cronómetro de recorrido.");
                Check(!controller.ReceiveThrownObjectHit()
                    && Mathf.Abs(beforeHit - session.RemainingTime - 5f) < .01f,
                    "La inmunidad impide descontar dos veces por impactos consecutivos.");
                Time.timeScale = 4f;
                Next(2);
            }
            else if (phase == 2)
            {
                var challenge = controller.FinalChallenge;
                if (challenge.ActiveProjectileCount == 0) return;
                Check(challenge.State.ElapsedTime >= FinalDeliveryChallengeState.FirstVolleySeconds
                    + FinalDeliveryChallengeState.TelegraphSeconds - .05f,
                    "El primer proyectil aparece después de su aviso anticipado.");
                controller.TogglePause();
                Check(session.Paused && Time.timeScale == 0f, "La pausa detiene el reto con un objeto en vuelo.");
                pausedRemaining = session.RemainingTime;
                pausedElapsed = session.ElapsedTime;
                pausedChallengeElapsed = challenge.State.ElapsedTime;
                pausedProjectiles = challenge.ProjectilePositions;
                Check(pausedProjectiles.Length > 0, "La prueba conserva al menos un proyectil para comprobar su pausa.");
                Next(3, .5);
            }
            else if (phase == 3)
            {
                var challenge = controller.FinalChallenge;
                Check(session.RemainingTime == pausedRemaining && session.ElapsedTime == pausedElapsed
                    && challenge.State.ElapsedTime == pausedChallengeElapsed,
                    "Reloj, cronómetro y duración del desafío permanecen congelados durante la pausa.");
                Vector3[] current = challenge.ProjectilePositions;
                bool unchanged = current.Length == pausedProjectiles.Length;
                for (int index = 0; unchanged && index < current.Length; index++)
                    unchanged = (current[index] - pausedProjectiles[index]).sqrMagnitude < .000001f;
                Check(unchanged, "Los objetos en vuelo no se desplazan ni desaparecen durante la pausa.");
                Check(!controller.ReceiveThrownObjectHit() && session.RemainingTime == pausedRemaining,
                    "No se aplica daño mientras la partida está pausada.");
                Check(controller.TryDeliverTo(LastProfessor) != ProjectDeliveryResult.Accepted
                    && session.DeliveredCount == 4,
                    "La pausa también bloquea la última entrega.");
                controller.Resume();
                Check(session.Playing && Time.timeScale == 1f, "Continuar reanuda correctamente la partida.");
                Time.timeScale = 4f;
                Next(4);
            }
            else if (phase == 4)
            {
                var challenge = controller.FinalChallenge;
                if (session.Failed)
                    throw new InvalidOperationException("Las oleadas provocaron una derrota inesperada en la prueba provisional.");
                if (!challenge.State.Completed) return;
                Check(challenge.State.ElapsedTime == FinalDeliveryChallengeState.DurationSeconds
                    && challenge.State.RemainingTime == 0f && challenge.ActiveProjectileCount == 0,
                    "A los dieciséis segundos termina el desafío y se retiran todos los proyectiles.");
                Check(!session.Completed && session.DeliveredCount == 4
                    && session.Inventory.Quantity(LastProject(controller)) == 1,
                    "Superar el reto todavía requiere entregar el proyecto con E.");
                Near(controller, LastProfessor);
                controller.SelectProject(LastProfessor);
                Check(controller.TryDeliverTo(LastProfessor) == ProjectDeliveryResult.Accepted,
                    "La quinta entrega se acepta después de superar el desafío.");
                Check(session.Completed && session.DeliveredCount == 5
                    && session.Inventory.Quantity(LastProject(controller)) == 0 && Time.timeScale == 0f,
                    "La victoria 5/5 consume el último proyecto y detiene el nivel.");
                Finish(true);
            }
        }
        catch (Exception exception)
        {
            results.Add("FAIL " + exception);
            Finish(false);
        }
    }

    static void Finish(bool success)
    {
        ClearDodgeObstacle();
        WriteResults(success);
        SessionState.SetBool(Key, false);
        Detach();
        Time.timeScale = 1f;
        EditorApplication.isPlaying = false;
    }

    static void WriteResults(bool success)
    {
        results.Add(success ? "FINAL_CHALLENGE_UNITY_OK" : "FINAL_CHALLENGE_UNITY_FAILED");
        string output = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Tests/YC14/RetoFinalUnity.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        File.WriteAllLines(output, results, new UTF8Encoding(false));
        string summary = string.Join("\n", results);
        if (success) Debug.Log(summary); else Debug.LogError(summary);
    }
}
