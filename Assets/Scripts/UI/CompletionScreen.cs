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

        public void Bind(SessionSummary summary, IReadOnlyList<AssemblyStep> steps, string participantCode, string csvPath, string label = "")
        {
            bool resumed = summary.StartedAtStep > 0;
            string who = string.IsNullOrEmpty(label) ? $"Participante {participantCode}" : $"<b>{label}</b> (participante {participantCode})";
            subtitleLabel.text = resumed
                ? $"{who}: as {steps.Count} etapas foram validadas (sessão retomada na etapa {summary.StartedAtStep + 1})."
                : $"{who}: as {steps.Count} etapas foram validadas.";

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
                text.Append("</color>");
            }
            stepTimesList.text = text.ToString();

            fileLabel.text = string.IsNullOrEmpty(csvPath)
                ? "Métricas não gravadas nesta sessão."
                : $"Métricas salvas em sessions/{Path.GetFileName(csvPath)} (copiar com adb pull).";
        }
    }
}
