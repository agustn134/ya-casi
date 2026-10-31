using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Los dos niveles de esta entrega no usan inferencia: omite sus shaders solo en este build.</summary>
public sealed class U2ShaderStripper : IPreprocessShaders, IPreprocessComputeShaders
{
    public int callbackOrder=>0;
    public void OnProcessShader(Shader shader,ShaderSnippetData snippet,IList<ShaderCompilerData> data)
    {
        if(SessionState.GetBool("YaCasi.U2.Build",false)&&shader.name.StartsWith("Hidden/Sentis/"))data.Clear();
    }
    public void OnProcessComputeShader(ComputeShader shader,string kernelName,IList<ShaderCompilerData> data)
    {
        if(SessionState.GetBool("YaCasi.U2.Build",false)&&AssetDatabase.GetAssetPath(shader).Contains("com.unity.ai.inference"))data.Clear();
    }
}
