using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[InitializeOnLoad]
public static class RepairCampusRenderer
{
    const string SessionKey = "YaCasi.RendererRepair.v1";
    static RepairCampusRenderer()
    {
        if (!SessionState.GetBool(SessionKey, false))
            EditorApplication.delayCall += RepairWhenReady;
    }

    static void RepairWhenReady()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += RepairWhenReady;
            return;
        }
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.isPlaying = false;
            EditorApplication.delayCall += RepairWhenReady;
            return;
        }
        Repair();
    }

    [MenuItem("Ya Casi/Reparar Renderer 2D")]
    public static void Repair()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Sal de Play antes de reparar el Renderer.");
            return;
        }
        const string pipelinePath = "Assets/Settings/UniversalRP.asset";
        const string rendererPath = "Assets/Settings/CampusRenderer2D.asset";
        var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);
        if (pipeline == null)
        {
            Debug.LogError("No se pudo cargar " + pipelinePath);
            return;
        }
        var renderer = AssetDatabase.LoadAssetAtPath<Renderer2DData>(rendererPath);
        if (renderer == null)
        {
            renderer = ScriptableObject.CreateInstance<Renderer2DData>();
            renderer.name = "CampusRenderer2D";
            AssetDatabase.CreateAsset(renderer, rendererPath);
        }
        Undo.RecordObject(pipeline, "Reparar Renderer 2D");
        var serialized = new SerializedObject(pipeline);
        var list = serialized.FindProperty("m_RendererDataList");
        list.arraySize = 1;
        list.GetArrayElementAtIndex(0).objectReferenceValue = renderer;
        serialized.FindProperty("m_DefaultRendererIndex").intValue = 0;
        serialized.ApplyModifiedProperties();
        EditorUtility.SetDirty(pipeline);
        GraphicsSettings.defaultRenderPipeline = pipeline;
        QualitySettings.renderPipeline = pipeline;
        AssetDatabase.SaveAssets();
        SessionState.SetBool(SessionKey, true);
        Debug.Log("YA_CASI_RENDERER_REPAIRED: CampusRenderer2D creado y asignado a UniversalRP. Pulsa Play.", pipeline);
    }
}