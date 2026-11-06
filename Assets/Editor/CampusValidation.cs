using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Verifica importación de personajes, referencias y audio del campus.</summary>
public static class CampusValidation
{
    [MenuItem("Ya Casi/Nivel 3/Verificar campus")]
    public static void Run()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene("Assets/Scenes/Nivel03_LaRutaDeEntrega.unity");
        var students = UnityEngine.Object.FindObjectsByType<CampusStudentWalker>(FindObjectsSortMode.None);
        var professors = UnityEngine.Object.FindObjectsByType<CampusProfessorWaiting>(FindObjectsSortMode.None);
        Check(students.Length == 42 && professors.Length == 5, "Conteo incorrecto de estudiantes o profesores");
        int frames = 0;
        foreach (MonoBehaviour actor in students.Cast<MonoBehaviour>().Concat(professors))
        {
            var serialized = new SerializedObject(actor);
            foreach (string field in new[] { "idleDown", "idleUp", "idleLeft", "idleRight", "walkDown", "walkUp", "walkLeft", "walkRight" })
            {
                var clips = serialized.FindProperty(field);
                if (clips == null) continue;
                Check(clips.arraySize > 0, actor.name + ": faltan frames de " + field);
                for (int i = 0; i < clips.arraySize; i++)
                {
                    var sprite = clips.GetArrayElementAtIndex(i).objectReferenceValue as Sprite;
                    Check(sprite != null, actor.name + ": referencia de sprite rota");
                    Check(sprite.rect.width == 16 && sprite.rect.height == 32, actor.name + ": recorte distinto de 16x32");
                    frames++;
                }
            }
            Check(actor.GetComponent<CapsuleCollider2D>() != null, actor.name + ": sin colisión");
            Check(actor.GetComponent<CampusActorSorting>() != null, actor.name + ": sin orden por pies");
        }
        var objectives = UnityEngine.Object.FindFirstObjectByType<CampusDeliveryObjectives>();
        var config = new SerializedObject(objectives);
        Check(config.FindProperty("professors").arraySize == 5, "Faltan objetivos");
        Check(config.FindProperty("roundSeconds").floatValue == 180, "El temporizador debe ser de 180 segundos");
        Check(config.FindProperty("timeoutMusic").objectReferenceValue is AudioClip, "Falta canción de derrota");
        Check(config.FindProperty("levelMusic").objectReferenceValue is AudioClip, "Falta música del nivel 3");
        Check(AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/YaCASI-Soundtrack-InicioMenu.mp3") != null, "Falta música de menú");
        Check(EditorBuildSettings.scenes.Any(s => s.enabled && s.path.EndsWith("MenuNivel03.unity")), "Menú fuera de Build Settings");
        Check(EditorBuildSettings.scenes.Any(s => s.enabled && s.path.EndsWith("Nivel03_LaRutaDeEntrega.unity")), "Nivel fuera de Build Settings");
        Debug.Log("CAMPUS_VALIDATION_OK: 42 estudiantes, 5 profesores, " + frames + " frames 16x32; cinco entregas, 180 segundos y tres canciones originales.");
    }
    static void Check(bool valid, string message)
    {
        if (!valid) throw new InvalidOperationException(message);
    }
}
