using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace MontAR
{
    /// <summary>
    /// Painel de diagnóstico da prova de conceito: estado da sessão AR, referências rastreadas,
    /// FPS e medição do tempo até a detecção. Para medir: tocar em "Medir detecção" com a câmera
    /// fora do marcador e então apontar para ele. Desativar no protótipo do experimento.
    /// </summary>
    public class PocDiagnosticsHud : MonoBehaviour
    {
        [SerializeField] private ImageTrackingController imageTracking;
        [SerializeField] private int fontSize = 34;
        [SerializeField] private int maxSamplesShown = 5;

        private readonly FpsWindow fps = new FpsWindow();
        private readonly List<double> detectionSamples = new List<double>();
        private readonly List<string> trackedNow = new List<string>();
        private double measureStartedAt = -1;
        private GUIStyle labelStyle;
        private GUIStyle buttonStyle;

        private void OnEnable()
        {
            if (imageTracking == null)
            {
                enabled = false;
                return;
            }
            imageTracking.ReferenceTracked += OnReferenceTracked;
            imageTracking.ReferenceLost += OnReferenceLost;
        }

        private void OnDisable()
        {
            if (imageTracking == null)
                return;
            imageTracking.ReferenceTracked -= OnReferenceTracked;
            imageTracking.ReferenceLost -= OnReferenceLost;
        }

        private void Update()
        {
            fps.Tick(Time.unscaledDeltaTime);
        }

        private void OnReferenceTracked(ARTrackedImage image)
        {
            string referenceName = image.referenceImage.name;
            if (!trackedNow.Contains(referenceName))
                trackedNow.Add(referenceName);

            if (measureStartedAt < 0)
                return;

            double seconds = Time.realtimeSinceStartupAsDouble - measureStartedAt;
            measureStartedAt = -1;
            detectionSamples.Add(seconds);
            Debug.Log($"[MontAR][PoC] Detecção de '{referenceName}' em {seconds:F2} s (amostra {detectionSamples.Count}).");
        }

        private void OnReferenceLost(string referenceName)
        {
            trackedNow.Remove(referenceName);
        }

        private void OnGUI()
        {
            if (labelStyle == null)
            {
                labelStyle = new GUIStyle(GUI.skin.box) { fontSize = fontSize, alignment = TextAnchor.UpperLeft, wordWrap = true };
                buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = fontSize };
            }

            float margin = Screen.width * 0.03f;
            float width = Screen.width - 2f * margin;
            var area = new Rect(margin, Screen.safeArea.yMin + margin, width, Screen.height * 0.45f);

            GUILayout.BeginArea(area);
            GUILayout.Label(BuildReport(), labelStyle);

            string buttonText = measureStartedAt < 0 ? "Medir detecção" : "Aponte para o marcador…";
            if (GUILayout.Button(buttonText, buttonStyle, GUILayout.Height(fontSize * 2.5f)) && measureStartedAt < 0)
                measureStartedAt = Time.realtimeSinceStartupAsDouble;
            GUILayout.EndArea();
        }

        private string BuildReport()
        {
            var report = new StringBuilder();
            report.AppendLine($"Sessão AR: {ARSession.state}");
            report.AppendLine($"FPS (1 s): {(fps.Current < 0 ? "—" : fps.Current.ToString("0.0"))}");
            report.AppendLine($"Rastreando: {(trackedNow.Count == 0 ? "nenhuma referência" : string.Join(", ", trackedNow))}");

            if (detectionSamples.Count > 0)
            {
                double sum = 0;
                foreach (double sample in detectionSamples)
                    sum += sample;

                int first = Mathf.Max(0, detectionSamples.Count - maxSamplesShown);
                var recent = new List<string>();
                for (int i = first; i < detectionSamples.Count; i++)
                    recent.Add(detectionSamples[i].ToString("0.00"));

                report.AppendLine($"Detecção: média {sum / detectionSamples.Count:0.00} s em {detectionSamples.Count} amostra(s)");
                report.Append($"Últimas: {string.Join(" | ", recent)} s");
            }
            else
            {
                report.Append("Detecção: sem amostras");
            }

            return report.ToString();
        }
    }
}
