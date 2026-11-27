using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>Dos filas detrás de las existentes, con acceso por el lado norte.</summary>
[InitializeOnLoad]
public static class LibraryThirtyShelves
{
    const string Marker = "LibraryThirtyShelvesV3.txt";
    static LibraryThirtyShelves() { EditorApplication.delayCall += AutoApply; }
    static void AutoApply()
    {
        if ((File.Exists(Marker) && File.ReadAllText(Marker).Contains("v3.2")) || !File.Exists("LibraryExpansionV2.txt") || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        Apply();
    }
    [MenuItem("Ya Casi/Nivel 2/Completar 30 estantes")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var scene = SceneManager.GetSceneByPath(LibraryLevelBuilder.Path);
        bool close = !scene.isLoaded;
        if (close) scene = EditorSceneManager.OpenScene(LibraryLevelBuilder.Path,OpenSceneMode.Additive);
        if (scene.isDirty) { Debug.LogWarning("Guarda la biblioteca antes de añadir las filas traseras."); return; }
        if (!File.Exists("LibraryBeforeThirtyShelves.unity.bak")) File.Copy(LibraryLevelBuilder.Path,"LibraryBeforeThirtyShelves.unity.bak");
        var previous = SceneManager.GetActiveScene(); SceneManager.SetActiveScene(scene);
        try
        {
            var manager = scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<LibraryLevelManager>()).Single();
            var shelves = manager.shelves.ToList();
            if (shelves.Count == 18)
            {
                for (int row=0;row<2;row++) for (int column=0;column<6;column++)
                {
                    var source = shelves[row*6+column];
                    var rear = Object.Instantiate(source);
                    rear.section = ((char)('D'+row)).ToString()+(column+1);
                    rear.name = "Estante trasero "+rear.section;
                    rear.transform.position = source.transform.position + new Vector3(0,1.4f,0);
                    rear.searched = false; rear.bookIndex = -1;
                    // Su lado de interacción mira al pasillo de arriba: no quedan puntos encerrados.
                    rear.interactionPoint.localPosition = new Vector3(0,3.3f,0);
                    var collider = rear.GetComponent<BoxCollider2D>();
                    collider.offset = new Vector2(0,.84f);
                    rear.highlight.transform.localPosition = new Vector3(0,1.6f,0);
                    var renderer = rear.GetComponent<SpriteRenderer>();
                    renderer.sortingOrder = 100-Mathf.RoundToInt((rear.transform.position.y+.5f)*20);
                    foreach (var label in rear.GetComponentsInChildren<TMPro.TextMeshPro>())
                    {
                        label.text=rear.section; label.name="Rótulo "+rear.section;
                        label.transform.localPosition=new Vector3(0,2.5f,-.2f);
                    }
                    shelves.Add(rear);
                }
                manager.shelves=shelves.ToArray(); EditorUtility.SetDirty(manager);
            }
            if (manager.shelves.Length != 30) throw new System.InvalidOperationException("Se esperaban exactamente 30 estantes.");
            foreach (var rear in manager.shelves.Where(s=>s.section.StartsWith("D")||s.section.StartsWith("E")))
                rear.interactionPoint.localPosition = new Vector3(0,3.3f,0);
            foreach (var student in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<LibraryStudent>()).Where(s=>!s.seated))
            {
                // Mantiene las rutas del carril superior dentro del pasillo libre entre las filas dobles.
                if (student.route == null || student.route.Length!=10 || student.route[0].y < 2) continue;
                for (int i=0;i<student.route.Length;i++)
                {
                    var point=student.route[i];
                    point.y=(i==2 || i==5)?3.92f:(i==8 || i==9)?4.05f:3.85f;
                    student.route[i]=point;
                }
                var position=student.transform.position; position.y=3.85f; student.transform.position=position;
                EditorUtility.SetDirty(student);
            }
            EditorSceneManager.MarkSceneDirty(scene); AssetDatabase.SaveAssets(); EditorSceneManager.SaveScene(scene);
            File.WriteAllText(Marker,"LIBRARY_30_SHELVES_READY v3.2: 18 originales + 12 traseros; accesos por ambos lados.\n");
            Debug.Log("LIBRARY_30_SHELVES_READY: 30 estantes guardados.");
        }
        finally
        {
            if (previous.isLoaded) SceneManager.SetActiveScene(previous);
            if (close) EditorSceneManager.CloseScene(scene,true);
        }
    }
}
