using System;
using System.IO;
using System.Linq;
using MontAR.Dev;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.XR.Management;

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
        private const string IosBundleId = "br.facens.yupen.montar";
        private const string IosLoader = "UnityEngine.XR.ARKit.ARKitLoader";
        private static readonly string[] IosDefines = { "UNITY_XR_ARKIT_LOADER_ENABLED", "APP_UI_EDITOR_ONLY" };
        private const string IosCameraUsage = "O MontAR usa a câmera para achar o marcador do tapete e mostrar a peça em 3D sobre a placa. Nada é gravado nem enviado.";

        /// <summary>Reconstrói etapas, overlays, interface, cena e o roteiro em Markdown.</summary>
        public static void BuildContent()
        {
            Debug.Log("[MontAR] " + MontagemPreviewBuilder.Rebuild());
            Debug.Log("[MontAR] " + AppBuilder.RebuildApp());
            Debug.Log("[MontAR] Roteiro exportado: " + RoteiroExporter.Export());
        }

        /// <summary>
        /// Reconstrói só a interface e a cena. Use quando a mudança é na UI: o BuildContent também gera os
        /// overlays de novo, com IDs internos novos em todos os prefabs, e enche o diff sem mudar o conteúdo.
        /// </summary>
        public static void BuildUi()
        {
            Debug.Log("[MontAR] " + AppBuilder.RebuildApp());
        }

        /// <summary>Recria o catálogo de peças com os valores padrão do AppBuilder (sobrescreve ajustes no Inspector).</summary>
        public static void RebuildCatalog()
        {
            Debug.Log("[MontAR] Catálogo recriado: " + AssetDatabase.GetAssetPath(AppBuilder.BuildCatalog(true)));
            AssetDatabase.SaveAssets();
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
            string output = Path.Combine("Builds", $"MontAR-{PlayerSettings.bundleVersion}-dev.apk");
            BuildSummary summary = Build(BuildTarget.Android, output);
            // summary.totalSize soma os arquivos intermediários; o tamanho que interessa é o do APK.
            string size = File.Exists(output) ? $"{new FileInfo(output).Length / 1e6:F1} MB" : "sem arquivo";
            Debug.Log($"[MontAR] Build Android: {summary.result}, {size}, {summary.totalErrors} erro(s), {summary.totalTime:mm\\:ss} → {output}");
            ExitIfBatch(summary);
        }

        /// <summary>
        /// Deixa o projeto pronto para iOS: identificador do app, texto da permissão da câmera, assinatura
        /// automática e ARKit como provedor da RA. Pode rodar de novo sem mudar nada.
        /// </summary>
        public static void ConfigureIos()
        {
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, IosBundleId);
            PlayerSettings.iOS.cameraUsageDescription = IosCameraUsage;
            PlayerSettings.iOS.appleEnableAutomaticSigning = true;

            // Os pacotes adicionam estes símbolos sozinhos, mas em linha de comando isso acontece tarde demais, com os
            // scripts já compilados. O do ARKit põe o marcador no build (sem ele, o marcador fica de fora, sem erro);
            // o do App UI deixa fora do build esse pacote, que o app não usa, como já acontece no Android.
            var defines = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.iOS)
                .Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();
            foreach (string define in IosDefines)
            {
                if (!defines.Contains(define))
                    defines.Add(define);
            }
            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.iOS, string.Join(";", defines));

            if (!EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.settingsKey, out XRGeneralSettingsPerBuildTarget perTarget))
                throw new InvalidOperationException("Falta o XR Plug-in Management configurado no projeto.");
            if (!perTarget.HasSettingsForBuildTarget(BuildTargetGroup.iOS))
                perTarget.CreateDefaultSettingsForBuildTarget(BuildTargetGroup.iOS);
            if (!perTarget.HasManagerSettingsForBuildTarget(BuildTargetGroup.iOS))
                perTarget.CreateDefaultManagerSettingsForBuildTarget(BuildTargetGroup.iOS);

            // O XR liga ao abrir o app, como no Android; a câmera só é pedida quando a sessão de RA liga.
            perTarget.SettingsForBuildTarget(BuildTargetGroup.iOS).InitManagerOnStart = true;
            XRManagerSettings manager = perTarget.ManagerSettingsForBuildTarget(BuildTargetGroup.iOS);
            if (!XRPackageMetadataStore.AssignLoader(manager, IosLoader, BuildTargetGroup.iOS))
                throw new InvalidOperationException($"Não deu para ligar o {IosLoader}. O pacote com.unity.xr.arkit está instalado?");

            EditorUtility.SetDirty(perTarget);
            AssetDatabase.SaveAssets();
            Debug.Log($"[MontAR] iOS configurado: {IosBundleId}, {manager.activeLoaders.Count} provedor(es) de RA.");
        }

        /// <summary>
        /// Gera o projeto do Xcode em Builds/iOS. Sai com código 1 se falhar. Funciona também no Windows:
        /// sem o xcrun, o pacote do ARKit deixa o marcador num catálogo que o próprio Xcode compila.
        /// </summary>
        [MenuItem("MontAR/iOS/Gerar projeto do Xcode")]
        public static void BuildIos()
        {
            ConfigureIos();
            string output = Path.Combine("Builds", "iOS");
            BuildSummary summary = Build(BuildTarget.iOS, output);
            Debug.Log($"[MontAR] Build iOS: {summary.result}, {summary.totalErrors} erro(s), {summary.totalTime:mm\\:ss} → {output}");
            ExitIfBatch(summary);
        }

        private static BuildSummary Build(BuildTarget target, string output)
        {
            string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            return BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = output,
                target = target,
                options = BuildOptions.None
            }).summary;
        }

        private static void ExitIfBatch(BuildSummary summary)
        {
            if (Application.isBatchMode)
                EditorApplication.Exit(summary.result == BuildResult.Succeeded ? 0 : 1);
        }
    }
}
