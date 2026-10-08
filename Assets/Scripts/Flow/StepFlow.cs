using System;
using System.Collections.Generic;

namespace MontAR
{
    /// <summary>
    /// Máquina de estados do roteiro de montagem. Cada etapa passa por
    /// Locating → Guiding → Validating → Completed, e a próxima etapa só é
    /// liberada quando a validação da atual é aprovada.
    /// Classe C# pura: não depende de cena nem de AR, para ser testada em EditMode.
    /// </summary>
    public class StepFlow
    {
        private readonly IReadOnlyList<IStepDefinition> steps;
        private readonly HashSet<string> trackedReferences = new HashSet<string>(StringComparer.Ordinal);

        /// <summary>Emitido a cada mudança relevante do fluxo, já com o estado atualizado.</summary>
        public event Action<FlowEvent> EventRaised;

        public StepFlow(IReadOnlyList<IStepDefinition> steps)
        {
            if (steps == null || steps.Count == 0)
                throw new ArgumentException("O roteiro precisa ter pelo menos uma etapa.", nameof(steps));

            for (int i = 0; i < steps.Count; i++)
            {
                if (steps[i] == null)
                    throw new ArgumentException($"A etapa {i} do roteiro está vazia.", nameof(steps));
            }

            this.steps = steps;
            CurrentIndex = -1;
        }

        public int StepCount => steps.Count;

        /// <summary>Índice da etapa atual; -1 antes de iniciar.</summary>
        public int CurrentIndex { get; private set; }

        /// <summary>Quantas etapas já foram aprovadas. É o ponto de retomada da sessão.</summary>
        public int CompletedCount { get; private set; }

        public StepPhase Phase { get; private set; }

        /// <summary>Modo de orientação atual. Na demonstração 3D a etapa não espera a câmera achar a referência.</summary>
        public GuidanceMode Mode { get; private set; } = GuidanceMode.AugmentedReality;

        public bool HasStarted => CurrentIndex >= 0;

        public bool IsFinished { get; private set; }

        public bool IsRunning => HasStarted && !IsFinished;

        public IStepDefinition CurrentStep => IsRunning ? steps[CurrentIndex] : null;

        public IStepDefinition GetStep(int index) => steps[index];

        /// <summary>
        /// Inicia a sessão na etapa indicada. Use um índice maior que zero para retomar
        /// uma montagem interrompida (as etapas anteriores contam como concluídas).
        /// <paramref name="sessionDetail"/> é anexado ao detail do session_started (ex.: "mode=ar;kit=...").
        /// </summary>
        public void Start(int fromIndex = 0, string sessionDetail = "")
        {
            if (HasStarted)
                throw new InvalidOperationException("O fluxo já foi iniciado.");
            if (fromIndex < 0 || fromIndex >= steps.Count)
                throw new ArgumentOutOfRangeException(nameof(fromIndex));

            CompletedCount = fromIndex;
            string detail = fromIndex > 0 ? $"resumed_from={fromIndex}" : string.Empty;
            if (!string.IsNullOrEmpty(sessionDetail))
                detail = detail.Length > 0 ? detail + ";" + sessionDetail : sessionDetail;
            Raise(FlowEventType.SessionStarted, -1, string.Empty, detail);
            BeginStep(fromIndex);
        }

        /// <summary>
        /// Troca o modo de orientação. Ao entrar na demonstração 3D, a etapa que ainda esperava a
        /// referência (Locating) passa para Guiding. Voltar para a RA não faz a fase regredir.
        /// Durante a sessão, a troca é registrada como mode_changed.
        /// </summary>
        public void SetMode(GuidanceMode mode)
        {
            if (mode == Mode)
                return;

            Mode = mode;
            if (!IsRunning)
                return;

            if (mode == GuidanceMode.Demo3D && Phase == StepPhase.Locating)
                Phase = StepPhase.Guiding;
            Raise(FlowEventType.ModeChanged, "mode=" + mode.ToCsvName());
        }

        /// <summary>
        /// Informa que uma imagem de referência passou a ser rastreada.
        /// Se for a referência da etapa atual, o overlay pode ser exibido e a etapa passa para Guiding.
        /// </summary>
        public void NotifyReferenceDetected(string referenceName)
        {
            if (string.IsNullOrEmpty(referenceName) || !trackedReferences.Add(referenceName))
                return;
            if (!IsRunning)
                return;

            if (Phase == StepPhase.Locating && referenceName == CurrentStep.ReferenceImageName)
                Phase = StepPhase.Guiding;

            Raise(FlowEventType.ReferenceDetected, referenceName);
        }

        /// <summary>
        /// Informa que uma referência deixou de ser rastreada. A fase não regride:
        /// o usuário pode apoiar o celular para executar a etapa sem perder o progresso.
        /// </summary>
        public void NotifyReferenceLost(string referenceName)
        {
            if (string.IsNullOrEmpty(referenceName) || !trackedReferences.Remove(referenceName))
                return;
            if (!IsRunning)
                return;

            Raise(FlowEventType.TrackingLost, referenceName);
        }

        public bool IsReferenceTracked(string referenceName) => trackedReferences.Contains(referenceName);

        /// <summary>Abre a validação da etapa. Só é aceita depois que a referência foi localizada.</summary>
        public bool BeginValidation()
        {
            if (!IsRunning || Phase != StepPhase.Guiding)
                return false;

            Phase = StepPhase.Validating;
            Raise(FlowEventType.ValidationStarted, string.Empty);
            return true;
        }

        /// <summary>Fecha a validação sem resultado e volta para Guiding.</summary>
        public bool CancelValidation()
        {
            if (!IsRunning || Phase != StepPhase.Validating)
                return false;

            Phase = StepPhase.Guiding;
            Raise(FlowEventType.ValidationCancelled, string.Empty);
            return true;
        }

        /// <summary>
        /// Registra o resultado da validação. Aprovada, a etapa é concluída e a próxima é liberada.
        /// Reprovada, a etapa volta para Guiding para o usuário corrigir.
        /// </summary>
        public bool SubmitValidation(bool passed, string detail = "")
        {
            if (!IsRunning || Phase != StepPhase.Validating)
                return false;

            if (!passed)
            {
                Phase = StepPhase.Guiding;
                Raise(FlowEventType.ValidationFailed, detail);
                return true;
            }

            Phase = StepPhase.Completed;
            Raise(FlowEventType.ValidationPassed, detail);

            CompletedCount = CurrentIndex + 1;
            Raise(FlowEventType.StepCompleted, string.Empty);

            if (CurrentIndex + 1 < steps.Count)
            {
                BeginStep(CurrentIndex + 1);
            }
            else
            {
                IsFinished = true;
                Raise(FlowEventType.SessionCompleted, -1, string.Empty, string.Empty);
            }

            return true;
        }

        /// <summary>Registra que o observador precisou intervir na etapa atual.</summary>
        public bool RegisterObserverIntervention(string detail = "")
        {
            if (!IsRunning)
                return false;

            Raise(FlowEventType.ObserverIntervention, detail);
            return true;
        }

        private void BeginStep(int index)
        {
            CurrentIndex = index;
            string reference = steps[index].ReferenceImageName;

            // A fase já vai certa no step_started, para quem escuta o evento (a interface) ver o estado real.
            // Sem referência, ou na demonstração 3D (a bancada virtual faz o papel da referência), não há o que procurar.
            bool hasReference = !string.IsNullOrEmpty(reference);
            Phase = hasReference && Mode != GuidanceMode.Demo3D ? StepPhase.Locating : StepPhase.Guiding;
            Raise(FlowEventType.StepStarted, string.Empty);

            if (hasReference && trackedReferences.Contains(reference))
            {
                // A referência já estava na tela quando a etapa começou: latência zero.
                Phase = StepPhase.Guiding;
                Raise(FlowEventType.ReferenceDetected, reference);
            }
        }

        private void Raise(FlowEventType type, string detail)
        {
            Raise(type, CurrentIndex, steps[CurrentIndex].StepId, detail);
        }

        private void Raise(FlowEventType type, int stepIndex, string stepId, string detail)
        {
            EventRaised?.Invoke(new FlowEvent(type, stepIndex, stepId, detail));
        }
    }
}
