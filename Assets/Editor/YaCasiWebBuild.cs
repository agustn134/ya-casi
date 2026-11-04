using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Compilación reproducible del juego para alojamiento estático.
public static class YaCasiWebBuild
{
    public static void Build()
    {
        string output = Environment.GetEnvironmentVariable("YA_CASI_WEB_OUTPUT");
        if (string.IsNullOrWhiteSpace(output))
            throw new InvalidOperationException("Falta YA_CASI_WEB_OUTPUT.");
        Directory.CreateDirectory(output);
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.WebGL.dataCaching = true;
        PlayerSettings.runInBackground = true;
        SessionState.SetBool("YaCasi.U2.Build", true);
        try
        {
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] {
                    "Assets/Scenes/MenuPrincipal.unity",
                    "Assets/Scenes/Nivel01_CaosDelCasillero.unity"
                },
                locationPathName = output,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            });
            var summary = report.summary;
            File.WriteAllText(Path.Combine(output, "build-result.txt"),
                $"Resultado: {summary.result}\nErrores: {summary.totalErrors}\n" +
                $"Bytes: {summary.totalSize}\nDuración: {summary.totalTime}\n");
            if (summary.result != BuildResult.Succeeded)
                throw new Exception("Falló la compilación WebGL: " + summary.result);
            Debug.Log("YA_CASI_WEB_BUILD_OK: " + output);
        }
        finally { SessionState.SetBool("YaCasi.U2.Build", false); }
    }
}
