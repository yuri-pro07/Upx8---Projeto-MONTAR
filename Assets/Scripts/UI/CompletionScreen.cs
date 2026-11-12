using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MontAR
{
    /// <summary>
    /// Tela de conclusão: resumo da sessão (tempo, tentativas, intervenções) e onde o CSV ficou.
    /// Daqui o usuário vai para o histórico de montagens ou começa outra.
    /// </summary>
    public class CompletionScreen : UiScreen
    {
        [SerializeField] private TMP_Text subtitleLabel;
        [SerializeField] private TMP_Text timeValue;
        [SerializeField] private TMP_Text stepsValue;
        [SerializeField] private TMP_Text failuresValue;
        [SerializeField] private TMP_Text interventionsValue;
        [SerializeField] private TMP_Text stepTimesList;
        [SerializeField] private TMP_Text fileLabel;
        [SerializeField] private Button newSessionButton;
        [SerializeField] private Button historyButton;

        public event Action NewSessionRequested;
        public event Action HistoryRequested;

        private void Awake()
        {
            newSessionButton.onClick.AddListener(() => NewSessionRequested?.Invoke());
            historyButton.onClick.AddListener(() => HistoryRequested?.Invoke());
        }

        public void Bind(SessionSummary summary, IReadOnlyList<AssemblyStep> steps, string participantCode, string csvPath, string label = "", int skippedSteps = 0, int simulatedSteps = 0)
        {
            bool resumed = summary.StartedAtStep > 0;
            string who = string.IsNullOrEmpty(label) ? $"Participante {participantCode}" : $"<b>{label}</b> (participante {participantCode})";
            string result = DescribeResult(steps.Count, skippedSteps, simulatedSteps);
            subtitleLabel.text = resumed
                ? $"{who}: {result} (sessão retomada na etapa {summary.StartedAtStep + 1})."
                : $"{who}: {result}.";

            timeValue.text = SessionSummary.FormatDuration(summary.ElapsedSeconds);
            stepsValue.text = summary.CompletedSteps.ToString();
            failuresValue.text = summary.FailedValidations.ToString();
            interventionsValue.text = summary.ObserverInterventions.ToString();

            var text = new StringBuilder();
            foreach (StepTiming timing in summary.Steps)
            {
                string title = timing.StepIndex >= 0 && timing.StepIndex < steps.Count ? steps[timing.StepIndex].Title : timing.StepId;
                if (text.Length > 0)
                    text.Append('\n');
                text.Append(timing.StepIndex + 1).Append(". ").Append(title)
                    .Append("  <color=#71717A>").Append(SessionSummary.FormatDuration(timing.Seconds));
                if (timing.FailedValidations > 0)
                    text.Append(timing.FailedValidations == 1 ? " · 1 reprovação" : $" · {timing.FailedValidations} reprovações");
                if (timing.Skipped)
                    text.Append(" · pulada");
                if (timing.Simulated)
                    text.Append(" · simulada");
                text.Append("</color>");
            }
            stepTimesList.text = text.ToString();

            fileLabel.text = string.IsNullOrEmpty(csvPath)
                ? "Métricas não gravadas nesta sessão."
                : $"Métricas salvas em sessions/{Path.GetFileName(csvPath)} ({CsvCopyHint}).";
        }

        /// <summary>O resumo diz quantas etapas foram mesmo validadas: pulada e simulada não contam.</summary>
        private static string DescribeResult(int total, int skipped, int simulated)
        {
            if (skipped == 0 && simulated == 0)
                return $"as {total} etapas foram validadas";

            string validated = $"{total - skipped - simulated} de {total} etapas validadas";
            string simulatedText = $"{simulated} {(simulated == 1 ? "simulada" : "simuladas")}";
            string skippedText = $"{skipped} {(skipped == 1 ? "pulada" : "puladas")}";
            if (skipped == 0)
                return $"{validated} e {simulatedText}";
            if (simulated == 0)
                return $"{validated} e {skippedText}";
            return $"{validated}, {simulatedText} e {skippedText}";
        }

#if UNITY_IOS
        private const string CsvCopyHint = "copiar no Xcode: Devices and Simulators › MontAR › Download Container";
#else
        private const string CsvCopyHint = "copiar com adb pull";
#endif
    }
}
