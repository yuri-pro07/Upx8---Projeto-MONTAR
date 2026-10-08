using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace MontAR.Tests
{
    /// <summary>
    /// Teste de ponta a ponta na cena real (MontAR.unity), no modo demonstração 3D: apresentação →
    /// peças → início → todas as etapas (com uma validação reprovada) → conclusão, conferindo o CSV.
    /// Salva capturas das telas em Builds/Screens (fora do git) para revisão visual.
    /// </summary>
    public class AppFlowSmokeTests
    {
        private const int CaptureWidth = 1080;
        private const int CaptureHeight = 2340;

        private string progressPath;
        private string progressBackup;
        private string createdCsv;
        private AppController app;
        private StepFlowController flow;

        private static string ScreensFolder => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Builds", "Screens"));

        [SetUp]
        public void SetUp()
        {
            // Começa sem progresso salvo (senão o app oferece retomar) e devolve o arquivo no fim.
            progressPath = Path.Combine(Application.persistentDataPath, ProgressStore.FileName);
            progressBackup = File.Exists(progressPath) ? File.ReadAllText(progressPath) : null;
            if (progressBackup != null)
                File.Delete(progressPath);
            createdCsv = null;
        }

        [TearDown]
        public void TearDown()
        {
            if (File.Exists(progressPath))
                File.Delete(progressPath);
            if (progressBackup != null)
                File.WriteAllText(progressPath, progressBackup);
            if (createdCsv != null && File.Exists(createdCsv))
                File.Delete(createdCsv);
        }

        [UnityTest]
        [Timeout(240000)]
        public IEnumerator DemoSession_FromWelcomeToCompletion()
        {
            // No Editor o XR Simulation pode falhar ao iniciar (Smart App Control): isso não é falha do app.
            LogAssert.ignoreFailingMessages = true;
            Directory.CreateDirectory(ScreensFolder);

            SceneManager.LoadScene("MontAR");
            yield return null;
            yield return null;

            app = Object.FindAnyObjectByType<AppController>();
            flow = Object.FindAnyObjectByType<StepFlowController>();
            Assert.IsNotNull(app, "AppController na cena");
            Assert.IsNotNull(flow, "StepFlowController na cena");
            Assert.IsInstanceOf<WelcomeScreen>(app.CurrentScreen);

            for (float waited = 0f; !app.IsArChecked && waited < 12f; waited += Time.unscaledDeltaTime)
                yield return null;
            Debug.Log($"[MontAR][Teste] RA disponível no Editor: {app.IsArAvailable}");

            // 1. Apresentação.
            yield return Capture("01_apresentacao");
            Click("Botão Próximo");
            yield return null;
            yield return Capture("02_apresentacao_aponta");
            for (int i = 0; i < 10 && !(app.CurrentScreen is PartsScreen); i++)
            {
                Click("Botão Próximo");
                yield return null;
            }
            Assert.IsInstanceOf<PartsScreen>(app.CurrentScreen, "o último Próximo (Começar) abre a tela de peças");

            // 2. Peças: o roteiro muda com o kit.
            yield return null;
            TMP_Text summary = Find<TMP_Text>("Resumo");
            StringAssert.Contains("9 etapas", summary.text);
            yield return Capture("03_pecas");
            Click("Menos", "Peça gpu");
            StringAssert.Contains("8 etapas", summary.text, "sem placa de vídeo a etapa do PCIe sai");
            Assert.IsFalse(app.Selection.Includes("gpu"));
            Click("Mais", "Peça gpu");
            Click("Menos", "Peça ram");
            StringAssert.Contains("9 etapas", summary.text, "com 1 pente a etapa da RAM troca, não some");
            Assert.AreEqual(1, app.Selection.GetQuantity("ram"));
            Click("Mais", "Peça ram");
            Assert.AreEqual(2, app.Selection.GetQuantity("ram"));
            Click("Botão Continuar");
            yield return null;

            // 3. Início: código inválido é recusado, válido inicia.
            Assert.IsInstanceOf<StartScreen>(app.CurrentScreen);
            TMP_InputField code = Find<TMP_InputField>("Campo do código");
            code.text = "";
            Click("Botão Iniciar");
            Assert.IsInstanceOf<StartScreen>(app.CurrentScreen, "sem código não começa");
            code.text = "T01";
            Click("Modo 3D");
            yield return null;
            yield return Capture("04_inicio");
            Click("Botão Iniciar");
            yield return null;

            // 4. Montagem no modo 3D.
            Assert.IsInstanceOf<AssemblyScreen>(app.CurrentScreen);
            Assert.IsTrue(flow.HasSession);
            if (flow.Mode != GuidanceMode.Demo3D)
            {
                Click("Modo 3D");
                yield return null;
            }
            Assert.AreEqual(GuidanceMode.Demo3D, flow.Mode);
            Assert.AreEqual(9, flow.ActiveSteps.Count);
            Assert.AreEqual(StepPhase.Guiding, flow.Flow.Phase, "no 3D a etapa não espera o marcador");

            int captured = 0;
            bool failedOnce = false;
            for (int guard = 0; guard < 40 && flow.Flow.IsRunning; guard++)
            {
                AssemblyStep step = flow.CurrentStep;
                if (captured < 9)
                {
                    yield return new WaitForSecondsRealtime(0.4f);
                    yield return Capture($"{05 + captured:00}_etapa_{flow.Flow.CurrentIndex + 1}_{step.StepId}");
                    captured++;
                }

                Click("Botão Validar");
                yield return null;
                Assert.AreEqual(StepPhase.Validating, flow.Flow.Phase);

                if (!failedOnce)
                {
                    // Confirmar com itens desmarcados reprova e volta para a etapa.
                    SetFirstItems(1);
                    yield return Capture("14_validacao");
                    Click("Botão Confirmar");
                    yield return null;
                    Assert.AreEqual(StepPhase.Guiding, flow.Flow.Phase);
                    Assert.AreEqual(0, flow.Flow.CompletedCount);
                    failedOnce = true;
                    Click("Botão Validar");
                    yield return null;
                }

                SetFirstItems(int.MaxValue);
                Click("Botão Confirmar");
                yield return null;
            }

            Assert.IsTrue(flow.Flow.IsFinished, "todas as etapas validadas");
            yield return new WaitForSecondsRealtime(1.5f);

            // 5. Conclusão e CSV.
            Assert.IsInstanceOf<CompletionScreen>(app.CurrentScreen);
            yield return Capture("15_conclusao");

            createdCsv = flow.MetricsFilePath;
            Assert.IsTrue(File.Exists(createdCsv), "CSV gravado: " + createdCsv);
            string csv = File.ReadAllText(createdCsv);
            StringAssert.Contains("session_started", csv);
            StringAssert.Contains("mode=demo_3d;kit=placa_mae:", csv);
            StringAssert.Contains("validation_failed", csv);
            StringAssert.Contains("session_completed", csv);
            Assert.AreEqual(9, csv.Split('\n').Count(line => line.Contains(",step_completed,")));
        }

        // ------------------------------------------------------------ utilitários

        private T Find<T>(string objectName) where T : Component
        {
            T found = app.CurrentScreen.GetComponentsInChildren<T>(false).FirstOrDefault(c => c.name == objectName);
            Assert.IsNotNull(found, $"'{objectName}' ({typeof(T).Name}) na tela {app.CurrentScreen.name}");
            return found;
        }

        /// <summary>Toca no botão ativo com esse nome na tela atual (opcionalmente dentro de um objeto pai).</summary>
        private void Click(string buttonName, string parentName = null)
        {
            Button button = app.CurrentScreen.GetComponentsInChildren<Button>(false)
                .FirstOrDefault(b => b.name == buttonName && b.IsInteractable()
                    && (parentName == null || b.GetComponentsInParent<Transform>(true).Any(t => t.name == parentName)));
            Assert.IsNotNull(button, $"botão '{buttonName}'{(parentName != null ? " em " + parentName : "")} na tela {app.CurrentScreen.name}");
            button.onClick.Invoke();
        }

        private void SetFirstItems(int count)
        {
            ChecklistItemView[] items = app.CurrentScreen.GetComponentsInChildren<ChecklistItemView>(false);
            Assert.IsNotEmpty(items, "checklist com itens");
            for (int i = 0; i < items.Length; i++)
                items[i].GetComponent<Toggle>().isOn = i < count;
        }

        /// <summary>
        /// Renderiza a tela num PNG: a bancada 3D pela câmera dela e a interface por uma câmera só de UI, com o
        /// Canvas em Screen Space - Camera a 1 m (mais perto, o texto SDF do TMP some por falta de precisão).
        /// A UI é renderizada sobre fundo preto e sobre fundo branco: a diferença dá a transparência de cada
        /// pixel, e a UI é composta sobre a bancada sem depender do canal alfa do URP.
        /// </summary>
        private IEnumerator Capture(string name)
        {
            // Espera o fade de entrada da tela terminar.
            yield return new WaitForSecondsRealtime(0.3f);

            var canvas = app.CurrentScreen.GetComponentInParent<Canvas>().rootCanvas;
            DemoStage stage = Object.FindAnyObjectByType<DemoStage>();
            Camera stageCamera = stage.StageCamera;
            RenderTexture scene = NewTarget();
            // Alvo definido antes do enquadramento: a proporção da câmera passa a ser a do PNG (retrato).
            stageCamera.targetTexture = scene;
            if (stage.IsVisible)
                stage.ResetView(true);

            var uiCameraObject = new GameObject("Câmera de captura da UI");
            uiCameraObject.transform.position = new Vector3(0f, 1000f, 0f);
            var uiCamera = uiCameraObject.AddComponent<Camera>();
            uiCamera.cullingMask = 1 << canvas.gameObject.layer;
            uiCamera.clearFlags = CameraClearFlags.SolidColor;
            uiCamera.enabled = false;

            RenderTexture uiOnBlack = NewTarget();
            RenderTexture uiOnWhite = NewTarget();
            uiCamera.targetTexture = uiOnBlack;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = uiCamera;
            canvas.planeDistance = 1f;

            yield return null;
            Canvas.ForceUpdateCanvases();

            stageCamera.Render();
            stageCamera.targetTexture = null;
            uiCamera.backgroundColor = Color.black;
            uiCamera.Render();
            uiCamera.backgroundColor = Color.white;
            uiCamera.targetTexture = uiOnWhite;
            uiCamera.Render();

            Color32[] background = ReadPixels(scene);
            Color32[] black = ReadPixels(uiOnBlack);
            Color32[] white = ReadPixels(uiOnWhite);
            for (int i = 0; i < background.Length; i++)
            {
                // Sobre preto: cor da UI já multiplicada pela opacidade. Sobre branco - sobre preto: o quanto o fundo aparece.
                background[i] = new Color32(
                    Composite(black[i].r, white[i].r, background[i].r),
                    Composite(black[i].g, white[i].g, background[i].g),
                    Composite(black[i].b, white[i].b, background[i].b),
                    255);
            }

            var image = new Texture2D(CaptureWidth, CaptureHeight, TextureFormat.RGBA32, false);
            image.SetPixels32(background);
            image.Apply();
            File.WriteAllBytes(Path.Combine(ScreensFolder, name + ".png"), image.EncodeToPNG());

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.worldCamera = null;
            Object.Destroy(uiCameraObject);
            Object.Destroy(image);
            foreach (RenderTexture target in new[] { scene, uiOnBlack, uiOnWhite })
            {
                target.Release();
                Object.Destroy(target);
            }
        }

        private static RenderTexture NewTarget() =>
            new RenderTexture(CaptureWidth, CaptureHeight, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };

        private static byte Composite(byte onBlack, byte onWhite, byte background)
        {
            float transmission = Mathf.Clamp01((onWhite - onBlack) / 255f);
            return (byte)Mathf.Clamp(Mathf.RoundToInt(onBlack + transmission * background), 0, 255);
        }

        private static Color32[] ReadPixels(RenderTexture source)
        {
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = source;
            var pixels = new Texture2D(CaptureWidth, CaptureHeight, TextureFormat.RGBA32, false);
            pixels.ReadPixels(new Rect(0, 0, CaptureWidth, CaptureHeight), 0, 0);
            pixels.Apply();
            RenderTexture.active = previous;
            Color32[] result = pixels.GetPixels32();
            Object.Destroy(pixels);
            return result;
        }
    }
}
