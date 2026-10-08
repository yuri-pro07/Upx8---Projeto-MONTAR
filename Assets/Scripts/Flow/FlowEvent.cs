namespace MontAR
{
    /// <summary>Tipos de evento emitidos pelo <see cref="StepFlow"/>.</summary>
    public enum FlowEventType
    {
        SessionStarted,
        SessionCompleted,
        StepStarted,
        StepCompleted,
        ReferenceDetected,
        TrackingLost,
        ValidationStarted,
        ValidationPassed,
        ValidationFailed,
        ValidationCancelled,
        ObserverIntervention,
        ModeChanged
    }

    /// <summary>
    /// Evento do fluxo de montagem. É a unidade gravada no CSV da sessão.
    /// </summary>
    public readonly struct FlowEvent
    {
        public FlowEvent(FlowEventType type, int stepIndex, string stepId, string detail)
        {
            Type = type;
            StepIndex = stepIndex;
            StepId = stepId ?? string.Empty;
            Detail = detail ?? string.Empty;
        }

        public FlowEventType Type { get; }

        /// <summary>Índice da etapa (base 0), ou -1 nos eventos de sessão.</summary>
        public int StepIndex { get; }

        public string StepId { get; }

        public string Detail { get; }

        public override string ToString() => $"{Type.ToCsvName()} [{StepIndex}:{StepId}] {Detail}";
    }

    /// <summary>Conversão dos tipos de evento para os nomes usados no CSV.</summary>
    public static class FlowEventTypeExtensions
    {
        public static string ToCsvName(this FlowEventType type)
        {
            switch (type)
            {
                case FlowEventType.SessionStarted: return "session_started";
                case FlowEventType.SessionCompleted: return "session_completed";
                case FlowEventType.StepStarted: return "step_started";
                case FlowEventType.StepCompleted: return "step_completed";
                case FlowEventType.ReferenceDetected: return "reference_detected";
                case FlowEventType.TrackingLost: return "tracking_lost";
                case FlowEventType.ValidationStarted: return "validation_started";
                case FlowEventType.ValidationPassed: return "validation_passed";
                case FlowEventType.ValidationFailed: return "validation_failed";
                case FlowEventType.ValidationCancelled: return "validation_cancelled";
                case FlowEventType.ObserverIntervention: return "observer_intervention";
                case FlowEventType.ModeChanged: return "mode_changed";
                default: return type.ToString();
            }
        }
    }
}
