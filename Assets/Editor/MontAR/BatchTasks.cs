using System;
using System.IO;
using System.Linq;
using MontAR.Dev;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MontAR.EditorTools
{
    /// <summary>
    /// Pontos de entrada para rodar a Unity em linha de comando (-batchmode -executeMethod), por exemplo:
    /// Unity.exe -batchmode -quit -projectPath . -executeMethod MontAR.EditorTools.BatchTasks.BuildContent -logFile -
    /// Num projeto sem a pasta "Assets/TextMesh Pro", importe antes o TMP Essential Resources com
    /// -importPackage "Library/PackageCache/com.unity.ugui@…/Package Resources/TMP Essential Resources.unitypackage".
    /// </summary>
    public static class BatchTasks
    {
        /// <summary>Reconstrói etapas, overlays, interface, cena e o roteiro em Markdown.</summary>
        public static void BuildContent()
        {
            Debug.Log("[MontAR] " + MontagemPreviewBuilder.Rebuild());
            Debug.Log("[MontAR] " + AppBuilder.RebuildApp());
            Debug.Log("[MontAR] Roteiro exportado: " + RoteiroExporter.Export());
        }

        /// <summary>Renderiza as prévias dos overlays em Builds/Previews/v1.</summary>
        public static void RenderPreviews()
        {
            string outDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Builds", "Previews", "v1"));
            Debug.Log("[MontAR] " + MontagemPreviewBuilder.RenderShots(outDir, 720, 1560, 0.55f));
        }

        /// <summary>Gera o APK em Builds/MontAR-&lt;versão&gt;-dev.apk. Sai com código 1 se falhar.</summary>
        public static void BuildAndroid()
        {
            string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            string output = Path.Combine("Builds", $"MontAR-{PlayerSettings.bundleVersion}-dev.apk");
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = output,
                target = BuildTarget.Android,
                options = BuildOptions.None
            });

            BuildSummary summary = report.summary;
            // summary.totalSize soma os arquivos intermediários; o tamanho que interessa é o do APK.
            string size = File.Exists(output) ? $"{new FileInfo(output).Length / 1e6:F1} MB" : "sem arquivo";
            Debug.Log($"[MontAR] Build Android: {summary.result}, {size}, {summary.totalErrors} erro(s), {summary.totalTime:mm\\:ss} → {output}");
            if (Application.isBatchMode)
                EditorApplication.Exit(summary.result == BuildResult.Succeeded ? 0 : 1);
        }
    }
}
