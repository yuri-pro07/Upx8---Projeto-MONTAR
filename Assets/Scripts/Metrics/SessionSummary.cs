using System;
using System.Collections.Generic;

namespace MontAR
{
    /// <summary>Tempo e tentativas de uma etapa, para a tela de conclusão.</summary>
    public class StepTiming
    {
        public StepTiming(int stepIndex, string stepId)
        {
            StepIndex = stepIndex;
            StepId = stepId ?? string.Empty;
        }

        public int StepIndex { get; }
        public string StepId { get; }

        /// <summary>Do step_started ao step_completed, em segundos. Negativo enquanto a etapa não termina.</summary>
        public double Seconds { get; internal set; } = -1;

        public int FailedValidations { get; internal set; }
    }

    /// <summary>
    /// Resumo da sessão mostrado na tela de conclusão, calculado a partir dos mesmos eventos
    /// gravados no CSV. Conta só o que aconteceu nesta execução do app: numa sessão retomada,
    /// as etapas feitas antes de fechar o app ficam no CSV, não aqui.
    /// </summary>
    public class SessionSummary
    {
        private readonly Func<double> clock;
        private readonly List<StepTiming> steps = new List<StepTiming>();
        private double startedAt = -1;
        private double finishedAt = -1;
        private double stepStartedAt = -1;
        private StepTiming currentStep;

        public SessionSummary(Func<double> clock)
        {
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        public IReadOnlyList<StepTiming> Steps => steps;

        public bool IsFinished => finishedAt >= 0;

        /// <summary>Etapa em que a sessão começou (maior que zero numa retomada).</summary>
        public int StartedAtStep { get; private set; } = -1;

        public int CompletedSteps { get; private set; }

        public int ValidationAttempts { get; private set; }

        public int FailedValidations { get; private set; }

        public int ObserverInterventions { get; private set; }

        public int ModeChanges { get; private set; }

        /// <summary>Segundos desde o session_started (até o session_completed, se já houve).</summary>
        public double ElapsedSeconds
        {
            get
            {
                if (startedAt < 0)
                    return 0;
                return (finishedAt >= 0 ? finishedAt : clock()) - startedAt;
            }
        }

        public void Record(FlowEvent flowEvent)
        {
            double now = clock();
            switch (flowEvent.Type)
            {
                case FlowEventType.SessionStarted:
                    startedAt = now;
                    break;

                case FlowEventType.StepStarted:
                    if (StartedAtStep < 0)
                        StartedAtStep = flowEvent.StepIndex;
                    currentStep = new StepTiming(flowEvent.StepIndex, flowEvent.StepId);
                    steps.Add(currentStep);
                    stepStartedAt = now;
                    break;

                case FlowEventType.ValidationStarted:
                    ValidationAttempts++;
                    break;

                case FlowEventType.ValidationFailed:
                    FailedValidations++;
                    if (currentStep != null)
                        currentStep.FailedValidations++;
                    break;

                case FlowEventType.StepCompleted:
                    CompletedSteps++;
                    if (currentStep != null)
                        currentStep.Seconds = now - stepStartedAt;
                    break;

                case FlowEventType.ObserverIntervention:
                    ObserverInterventions++;
                    break;

                case FlowEventType.ModeChanged:
                    ModeChanges++;
                    break;

                case FlowEventType.SessionCompleted:
                    finishedAt = now;
                    break;
            }
        }

        /// <summary>Formata segundos como "m:ss" (ou "h:mm:ss" a partir de uma hora).</summary>
        public static string FormatDuration(double seconds)
        {
            if (seconds < 0)
                return "—";
            var span = TimeSpan.FromSeconds(Math.Round(seconds));
            return span.TotalHours >= 1
                ? $"{(int)span.TotalHours}:{span.Minutes:00}:{span.Seconds:00}"
                : $"{span.Minutes}:{span.Seconds:00}";
        }
    }
}
