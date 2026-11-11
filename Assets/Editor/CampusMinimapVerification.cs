using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

/// <summary>Prueba el minimapa en la escena provisional sin modificar el campus pintado.</summary>
[InitializeOnLoad]
public static class CampusMinimapVerification
{
    const string Key = "YaCasi.Nivel03.MinimapTesting";
    const string ScenePath = "Assets/Scenes/Nivel03_PruebaEntregas.unity";
    static readonly List<string> results = new List<string>();
    static bool attached;
    static int phase;
    static double due, deadline;
    static Vector2 movedFeet;
    static Vector3 professorOriginal;
    static Transform testedProfessor;
    static float pausedClock;
    static Color pendingColor;
    static RenderTexture originalTexture;
    static Keyboard simulatedKeyboard, previousKeyboard;
    static int queuedMState = -1, keyboardEventFrame;

    static CampusMinimapVerification()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        if (SessionState.GetBool(Key, false)) EditorApplication.delayCall += Attach;
    }

    [MenuItem("Ya Casi/Nivel 3/Verificar minimapa")]
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
        if (state == PlayModeStateChange.ExitingPlayMode || state == PlayModeStateChange.EnteredEditMode)
            CleanupKeyboard();
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
        phase = 0;
        due = EditorApplication.timeSinceStartup + .75;
        deadline = due + 45;
        results.Clear();
        results.Add("Unity " + Application.unityVersion);
        results.Add("Escena provisional: " + ScenePath);
        results.Add("Inicio UTC: " + DateTime.UtcNow.ToString("O"));
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
    }

    static void Detach() { attached = false; EditorApplication.update -= Tick; }
    static void Check(bool valid, string message)
    {
        if (!valid) throw new InvalidOperationException(message);
        results.Add("PASS " + message);
    }
    static void Next(int value, double delay = .25)
    {
        phase = value;
        due = EditorApplication.timeSinceStartup + delay;
    }
    static Transform Professor(CampusDeliveryObjectives controller, int index)
    {
        return (Transform)new SerializedObject(controller).FindProperty("professors")
            .GetArrayElementAtIndex(index).objectReferenceValue;
    }
    static CampusPathPlayer Player(CampusDeliveryObjectives controller)
    {
        var actor = (Transform)new SerializedObject(controller).FindProperty("player").objectReferenceValue;
        return actor.GetComponent<CampusPathPlayer>();
    }
    static void MoveFeet(CampusPathPlayer actor, Vector2 feetPosition)
    {
        var collider = actor.GetComponent<CapsuleCollider2D>();
        var body = actor.GetComponent<Rigidbody2D>();
        body.position = feetPosition - collider.offset;
        body.linearVelocity = Vector2.zero;
        actor.transform.position = body.position;
        Physics2D.SyncTransforms();
    }
    static Vector2 DifferentPathCell(CampusPathPlayer actor)
    {
        var feet = actor.GetComponent<CapsuleCollider2D>();
        float distance = float.PositiveInfinity;
        Vector2 found = Vector2.zero;
        foreach (Vector3Int cell in actor.Path.cellBounds.allPositionsWithin)
        {
            if (!actor.Path.HasTile(cell)) continue;
            Vector2 point = actor.Path.GetCellCenterWorld(cell);
            float delta = (point - actor.FeetPosition).sqrMagnitude;
            if (delta < 1f || delta >= distance) continue;
            bool interior = true;
            for (int x = -1; x <= 1 && interior; x++)
                for (int y = -1; y <= 1; y++)
                    if (!actor.Path.HasTile(actor.Path.WorldToCell(point + new Vector2(x * feet.size.x * .5f, y * feet.size.y * .5f))))
                    { interior = false; break; }
            if (!interior) continue;
            found = point; distance = delta;
        }
        Check(!float.IsPositiveInfinity(distance), "La prueba encuentra otra celda transitable para mover al jugador.");
        return found;
    }
    static bool Near(Vector2 a, Vector2 b) => (a - b).sqrMagnitude < .0001f;
    static bool Same(Color a, Color b) => Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b) < .01f;

    static void BeginKeyboardCheck()
    {
        previousKeyboard = Keyboard.current;
        simulatedKeyboard = InputSystem.AddDevice<Keyboard>();
        Check(!simulatedKeyboard.native, "La prueba de M utiliza un teclado virtual sin modificar el teclado físico.");
        simulatedKeyboard.MakeCurrent();
        InputSystem.onBeforeUpdate += QueuePendingKeyboardState;
        queuedMState = 1;
    }
    static void QueuePendingKeyboardState()
    {
        // Se encola dentro de la actualización normal del juego, nunca en el buffer del editor.
        if (queuedMState < 0 || simulatedKeyboard == null || InputState.currentUpdateType != InputUpdateType.Dynamic) return;
        simulatedKeyboard.MakeCurrent();
        InputSystem.QueueStateEvent(simulatedKeyboard, queuedMState == 1 ? new KeyboardState(UnityEngine.InputSystem.Key.M) : new KeyboardState());
        queuedMState = -1;
        keyboardEventFrame = Time.frameCount;
    }
    static bool KeyboardFrameCompleted => queuedMState < 0 && Time.frameCount > keyboardEventFrame;
    static void CleanupKeyboard()
    {
        InputSystem.onBeforeUpdate -= QueuePendingKeyboardState;
        queuedMState = -1;
        if (simulatedKeyboard != null && simulatedKeyboard.added) InputSystem.RemoveDevice(simulatedKeyboard);
        simulatedKeyboard = null;
        if (previousKeyboard != null && previousKeyboard.added) previousKeyboard.MakeCurrent();
        previousKeyboard = null;
    }

    static void CheckRenderedTexture(CampusMinimap minimap)
    {
        RenderTexture prior = RenderTexture.active;
        Texture2D sample = null;
        try
        {
            RenderTexture.active = minimap.Texture;
            sample = new Texture2D(minimap.Texture.width, minimap.Texture.height, TextureFormat.RGB24, false);
            sample.ReadPixels(new Rect(0, 0, sample.width, sample.height), 0, 0, false);
            sample.Apply(false, false);
            var pixels = sample.GetPixels32();
            var colors = new HashSet<int>();
            int min = 765, max = 0, gold = 0, blue = 0;
            for (int i = 0; i < pixels.Length; i += 2)
            {
                Color32 pixel = pixels[i];
                colors.Add((pixel.r / 24) * 121 + (pixel.g / 24) * 11 + pixel.b / 24);
                int brightness = pixel.r + pixel.g + pixel.b;
                min = Math.Min(min, brightness); max = Math.Max(max, brightness);
                if (pixel.r > 180 && pixel.r > pixel.b + 50 && pixel.g > pixel.b + 40) gold++;
                if (pixel.b > pixel.r + 50 && pixel.b > pixel.g + 20) blue++;
            }
            Check(colors.Count >= 4 && max - min > 80,
                "La RenderTexture contiene mapa y marcadores visibles; no está vacía (grupos de color: " + colors.Count + ").");
            Check(gold > 3 && blue > 3, "Los profesores dorados y el jugador azul se ven en los píxeles renderizados.");
        }
        finally
        {
            RenderTexture.active = prior;
            if (sample != null) UnityEngine.Object.DestroyImmediate(sample);
        }
    }

    static void Tick()
    {
        if (EditorApplication.isCompiling || EditorApplication.timeSinceStartup < due) return;
        try
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new InvalidOperationException("La verificación del minimapa excedió cuarenta y cinco segundos.");
            if (!EditorApplication.isPlaying) return;
            var controller = UnityEngine.Object.FindFirstObjectByType<CampusDeliveryObjectives>();
            if (controller == null || controller.Session == null) return;
            var minimap = controller.Minimap;
            var actor = Player(controller);
            if (phase == 0)
            {
                Check(minimap != null && minimap.MapCamera != null, "El controlador crea el minimapa y su segunda cámara.");
                Check(minimap.Texture != null && minimap.Texture.width == 512 && minimap.Texture.height == 512
                    && minimap.Texture.IsCreated(), "La RenderTexture de 512 por 512 está creada.");
                int layer = LayerMask.NameToLayer(CampusMinimap.LayerName);
                Check(layer >= 0 && minimap.MapCamera.cullingMask == 1 << layer, "La cámara del minimapa ve únicamente la capa Minimap.");
                Check(actor.ViewCamera != null && (actor.ViewCamera.cullingMask & (1 << layer)) == 0,
                    "La cámara principal excluye la capa del minimapa.");
                Check(minimap.MapCamera.orthographic && minimap.MapCamera.targetTexture == minimap.Texture,
                    "La segunda cámara es ortográfica y apunta a su RenderTexture.");
                Check(minimap.MapCamera.GetComponentsInChildren<AudioListener>(true).Length == 0,
                    "La cámara del minimapa no duplica el AudioListener.");
                Check(minimap.IsExpanded && minimap.MapCamera.enabled, "El minimapa comienza desplegado y su cámara activa.");
                Rect bounds = minimap.MapWorldRect;
                int cells = 0;
                bool containsPath = true;
                foreach (Vector3Int cell in actor.Path.cellBounds.allPositionsWithin)
                    if (actor.Path.HasTile(cell))
                    {
                        containsPath &= bounds.Contains(actor.Path.GetCellCenterWorld(cell));
                        cells++;
                    }
                Check(cells > 0 && containsPath && bounds.width > 0 && bounds.height > 0,
                    "El encuadre cubre todas las celdas del camino (" + cells + ") con dimensiones válidas.");
                Check(Near(minimap.PlayerMapPosition, actor.FeetPosition), "El marcador inicial del jugador coincide con sus pies.");
                for (int i = 0; i < controller.Session.RequiredCount; i++)
                    Check(Near(minimap.ProfessorMapPosition(i), Professor(controller, i).position), "El marcador del profesor " + (i + 1) + " coincide con su posición.");
                originalTexture = minimap.Texture;
                pendingColor = minimap.ProfessorMapColor(1);
                movedFeet = DifferentPathCell(actor);
                MoveFeet(actor, movedFeet);
                Next(1);
            }
            else if (phase == 1)
            {
                Check(Near(minimap.PlayerMapPosition, actor.FeetPosition) && Near(actor.FeetPosition, movedFeet),
                    "El marcador sigue al jugador después de cambiar de celda.");
                CheckRenderedTexture(minimap);
                controller.TogglePause();
                Check(controller.Session.Paused && Time.timeScale == 0, "La partida entra en pausa para comprobar los marcadores.");
                pausedClock = controller.Session.RemainingTime;
                testedProfessor = Professor(controller, 0);
                professorOriginal = testedProfessor.position;
                testedProfessor.position += Vector3.right * .75f;
                Next(2);
            }
            else if (phase == 2)
            {
                Check(controller.Session.RemainingTime == pausedClock, "Revisar el minimapa en pausa no consume tiempo.");
                Check(Near(minimap.ProfessorMapPosition(0), testedProfessor.position),
                    "El marcador del profesor refleja su posición incluso mientras la partida está pausada.");
                testedProfessor.position = professorOriginal;
                controller.Resume();
                MoveFeet(actor, (Vector2)professorOriginal + Vector2.down + actor.GetComponent<CapsuleCollider2D>().offset);
                controller.SelectProject(0);
                Check(controller.TryDeliverTo(0) == ProjectDeliveryResult.Accepted, "Se acepta la primera entrega durante la prueba del minimapa.");
                Next(3);
            }
            else if (phase == 3)
            {
                Color delivered = minimap.ProfessorMapColor(0);
                Check(delivered.g > delivered.r && delivered.g > delivered.b && !Same(delivered, pendingColor),
                    "El profesor que recibió el proyecto cambia a un marcador verde.");
                Check(Same(minimap.ProfessorMapColor(1), pendingColor), "Los demás profesores conservan el color de entrega pendiente.");
                minimap.Toggle();
                Check(!minimap.IsExpanded && !minimap.MapCamera.enabled, "Minimizar oculta el mapa y detiene su cámara.");
                Check(minimap.Texture == originalTexture && minimap.Texture.IsCreated(), "Minimizar conserva la RenderTexture sin volver a crearla.");
                minimap.Toggle();
                Check(minimap.IsExpanded && minimap.MapCamera.enabled && minimap.Texture == originalTexture,
                    "Desplegar recupera el mapa y reutiliza la misma textura.");
                BeginKeyboardCheck();
                Next(4);
            }
            else if (phase == 4)
            {
                if (!KeyboardFrameCompleted) return;
                Check(simulatedKeyboard.mKey.isPressed, "El Input System procesa la primera pulsación de M.");
                Check(!minimap.IsExpanded && !minimap.MapCamera.enabled,
                    "La tecla M minimiza el mapa a través del controlador real de entrada.");
                queuedMState = 0;
                Next(5);
            }
            else if (phase == 5)
            {
                if (!KeyboardFrameCompleted) return;
                Check(!simulatedKeyboard.mKey.isPressed && !minimap.IsExpanded,
                    "Soltar M mantiene el minimapa cerrado y permite una nueva pulsación.");
                queuedMState = 1;
                Next(6);
            }
            else if (phase == 6)
            {
                if (!KeyboardFrameCompleted) return;
                Check(simulatedKeyboard.mKey.isPressed && minimap.IsExpanded && minimap.MapCamera.enabled,
                    "La segunda pulsación de M vuelve a desplegar el mapa.");
                Check(minimap.Texture == originalTexture && minimap.Texture.IsCreated(),
                    "El acceso por teclado conserva la misma RenderTexture.");
                queuedMState = 0;
                Next(7);
            }
            else if (phase == 7)
            {
                if (!KeyboardFrameCompleted) return;
                Check(!simulatedKeyboard.mKey.isPressed && minimap.IsExpanded,
                    "La segunda liberación de M no vuelve a alternar el minimapa.");
                CleanupKeyboard();
                Finish(true);
            }
        }
        catch (Exception exception) { results.Add("FAIL " + exception); Finish(false); }
    }
    static void Finish(bool success)
    {
        CleanupKeyboard();
        if (testedProfessor != null) testedProfessor.position = professorOriginal;
        testedProfessor = null;
        WriteResults(success);
        SessionState.SetBool(Key, false); Detach();
        Time.timeScale = 1;
        EditorApplication.isPlaying = false;
    }
    static void WriteResults(bool success)
    {
        results.Add(success ? "MINIMAP_UNITY_OK" : "MINIMAP_UNITY_FAILED");
        string output = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Tests/YC14/MinimapaUnity.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        File.WriteAllLines(output, results, new UTF8Encoding(false));
        string summary = string.Join("\n", results);
        if (success) Debug.Log(summary); else Debug.LogError(summary);
    }
}
