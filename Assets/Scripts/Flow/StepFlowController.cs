using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace MontAR
{
    /// <summary>
    /// Liga o <see cref="StepFlow"/> à cena: monta o roteiro do kit escolhido, repassa o rastreamento,
    /// troca o overlay a cada etapa (na RA e na bancada virtual), grava as métricas e salva o progresso.
    /// A interface chama os métodos públicos (iniciar, validar, cancelar, intervenção, modo) e escuta
    /// <see cref="FlowEventRaised"/>.
    /// </summary>
    public class StepFlowController : MonoBehaviour
    {
        [SerializeField] private AssemblyProcedure procedure;
        [SerializeField] private ImageTrackingController imageTracking;
        [SerializeField] private ARContentManager contentManager;
        [Tooltip("Bancada virtual da demonstração 3D (opcional).")]
        [SerializeField] private DemoStage demoStage;
        [SerializeField] private SessionMetricsLogger metricsLogger;

        private readonly List<AssemblyStep> activeSteps = new List<AssemblyStep>();
        private StepFlow flow;
        private ProgressStore progressStore;
        private SessionProgress progress;
        private PartSelection selection;
        private IStepValidator currentValidator;
        private GuidanceMode mode = GuidanceMode.AugmentedReality;

        /// <summary>Repassa os eventos do fluxo para a interface, depois que a cena já reagiu.</summary>
        public event Action<FlowEvent> FlowEventRaised;

        public AssemblyProcedure Procedure => procedure;

        public StepFlow Flow => flow;

        /// <summary>Há uma sessão aberta (em andamento ou recém-concluída)?</summary>
        public bool HasSession => flow != null;

        /// <summary>Roteiro da sessão atual, já filtrado pelo kit.</summary>
        public IReadOnlyList<AssemblyStep> ActiveSteps => activeSteps;

        public PartSelection Selection => selection;

        public SessionProgress Progress => progress;

        public GuidanceMode Mode => mode;

        /// <summary>CSV da última sessão gravada (continua valendo depois do fim da sessão).</summary>
        public string MetricsFilePath => metricsLogger != null ? metricsLogger.CurrentFilePath : null;

        public AssemblyStep CurrentStep => flow != null && flow.IsRunning ? activeSteps[flow.CurrentIndex] : null;

        /// <summary>Checklist da etapa atual, ou null se a validação for visual.</summary>
        public ChecklistValidator CurrentChecklist => currentValidator as ChecklistValidator;

        /// <summary>Validação visual da etapa atual, ou null se for checklist.</summary>
        public ReferenceSeenValidator CurrentVisualValidator => currentValidator as ReferenceSeenValidator;

        private void Awake()
        {
            progressStore = ProgressStore.CreateDefault();
        }

        private void OnDestroy()
        {
            Detach();
        }

        /// <summary>Etapas que o kit gera, na ordem do roteiro (para a tela de peças mostrar antes de começar).</summary>
        public List<AssemblyStep> StepsFor(PartSelection kit)
        {
            return procedure != null ? StepFilter.Filter(procedure.Steps, kit) : new List<AssemblyStep>();
        }

        /// <summary>
        /// Há uma montagem interrompida deste mesmo roteiro? A tela inicial usa isso
        /// para oferecer "continuar".
        /// </summary>
        public bool TryGetResumableProgress(out SessionProgress saved)
        {
            if (procedure != null
                && progressStore.TryLoad(out saved)
                && !saved.finished
                && saved.procedureId == procedure.ProcedureId
                && saved.parts != null && saved.parts.Count > 0
                && saved.completedSteps > 0
                && saved.completedSteps < StepsFor(PartSelection.FromEntries(saved.parts)).Count)
            {
                return true;
            }

            saved = null;
            return false;
        }

        public void StartNewSession(string participantCode, PartSelection kit)
        {
            if (kit == null)
                throw new ArgumentNullException(nameof(kit));

            progress = new SessionProgress
            {
                sessionId = ParticipantCode.CreateSessionId(participantCode, DateTime.UtcNow),
                participantCode = ParticipantCode.Normalize(participantCode),
                procedureId = procedure.ProcedureId,
                completedSteps = 0,
                parts = kit.ToEntries()
            };
            Begin(kit.Clone(), 0);
        }

        public void ResumeSession(SessionProgress saved)
        {
            progress = saved ?? throw new ArgumentNullException(nameof(saved));
            Begin(PartSelection.FromEntries(saved.parts), saved.completedSteps);
        }

        /// <summary>
        /// Fecha a sessão aberta sem apagar o progresso salvo (sair para o menu, ou depois da conclusão).
        /// Uma montagem interrompida pode ser retomada depois com <see cref="ResumeSession"/>.
        /// </summary>
        public void LeaveSession()
        {
            Detach();
            if (metricsLogger != null)
                metricsLogger.EndSession();
            if (contentManager != null)
                contentManager.Clear();
            if (demoStage != null)
                demoStage.Clear();
            flow = null;
            currentValidator = null;
        }

        /// <summary>Troca entre RA e demonstração 3D. Vale para a sessão atual e para as próximas.</summary>
        public void SetGuidanceMode(GuidanceMode newMode)
        {
            mode = newMode;
            flow?.SetMode(newMode);
        }

        /// <summary>Botão "Validar etapa". Só funciona depois que a referência foi localizada.</summary>
        public bool RequestValidation()
        {
            if (flow == null || !flow.BeginValidation())
                return false;

            TrySubmitVisualValidation();
            return true;
        }

        /// <summary>Botão "Confirmar" do checklist. Itens pendentes reprovam a validação.</summary>
        public bool ConfirmChecklist()
        {
            ChecklistValidator checklist = CurrentChecklist;
            if (flow == null || checklist == null || flow.Phase != StepPhase.Validating)
                return false;

            return flow.SubmitValidation(checklist.IsSatisfied, checklist.Describe());
        }

        public bool CancelValidation() => flow != null && flow.CancelValidation();

        /// <summary>Botão discreto do observador (alimenta a taxa de conclusão sem intervenção).</summary>
        public bool RegisterObserverIntervention(string detail = "") =>
            flow != null && flow.RegisterObserverIntervention(detail);

        private void Begin(PartSelection kit, int fromIndex)
        {
            if (flow != null)
                throw new InvalidOperationException("Já existe uma sessão em andamento.");
            if (procedure == null || procedure.Steps.Count == 0)
                throw new InvalidOperationException("Nenhum roteiro de montagem configurado.");

            selection = kit;
            activeSteps.Clear();
            activeSteps.AddRange(StepsFor(kit));
            if (activeSteps.Count == 0)
                throw new InvalidOperationException("O kit escolhido não gera nenhuma etapa no roteiro.");
            if (fromIndex >= activeSteps.Count)
                throw new InvalidOperationException("O progresso salvo não corresponde ao roteiro deste kit.");

            flow = new StepFlow(activeSteps);
            flow.SetMode(mode);

            // O logger se inscreve antes do controlador: assim grava cada evento
            // (inclusive session_completed) antes de o controlador reagir a ele.
            if (metricsLogger != null)
            {
                var info = new SessionInfo(progress.sessionId, progress.participantCode, Application.version, SystemInfo.deviceModel);
                metricsLogger.BeginSession(flow, info);
            }
            flow.EventRaised += OnFlowEvent;

            if (imageTracking != null)
            {
                // Referências que já estão na tela antes do início contam como localizadas.
                foreach (string referenceName in imageTracking.TrackedReferenceNames)
                    flow.NotifyReferenceDetected(referenceName);
                imageTracking.ReferenceTracked += OnReferenceTracked;
                imageTracking.ReferenceLost += OnReferenceLost;
            }

            progressStore.Save(progress);
            flow.Start(fromIndex, $"mode={mode.ToCsvName()};kit={kit.Describe()}");
        }

        private void Detach()
        {
            if (imageTracking != null)
            {
                imageTracking.ReferenceTracked -= OnReferenceTracked;
                imageTracking.ReferenceLost -= OnReferenceLost;
            }
            if (flow != null)
                flow.EventRaised -= OnFlowEvent;
        }

        private void OnReferenceTracked(ARTrackedImage image)
        {
            flow.NotifyReferenceDetected(image.referenceImage.name);
            TrySubmitVisualValidation();
        }

        private void OnReferenceLost(string referenceName)
        {
            flow.NotifyReferenceLost(referenceName);
        }

        private void TrySubmitVisualValidation()
        {
            if (flow.Phase == StepPhase.Validating && currentValidator is ReferenceSeenValidator visual && visual.IsSatisfied)
                flow.SubmitValidation(true, visual.Describe());
        }

        private void OnFlowEvent(FlowEvent flowEvent)
        {
            switch (flowEvent.Type)
            {
                case FlowEventType.StepStarted:
                    AssemblyStep step = activeSteps[flowEvent.StepIndex];
                    currentValidator = CreateValidator(step);
                    if (contentManager != null)
                        contentManager.ShowStep(step);
                    if (demoStage != null)
                        demoStage.ShowStep(step);
                    break;

                case FlowEventType.ValidationFailed:
                case FlowEventType.ValidationCancelled:
                    // Nova tentativa começa com o checklist limpo.
                    currentValidator = CreateValidator(activeSteps[flowEvent.StepIndex]);
                    break;

                case FlowEventType.StepCompleted:
                    progress.completedSteps = flow.CompletedCount;
                    progressStore.Save(progress);
                    break;

                case FlowEventType.SessionCompleted:
                    progress.finished = true;
                    progressStore.Save(progress);
                    currentValidator = null;
                    if (contentManager != null)
                        contentManager.Clear();
                    if (demoStage != null)
                        demoStage.Clear();
                    if (metricsLogger != null)
                        metricsLogger.EndSession();
                    break;
            }

            FlowEventRaised?.Invoke(flowEvent);
        }

        private IStepValidator CreateValidator(AssemblyStep step)
        {
            if (step.ValidationType == StepValidationType.ReferenceSeen)
            {
                if (!string.IsNullOrEmpty(step.ValidationReferenceName))
                    return new ReferenceSeenValidator(step.ValidationReferenceName, flow.IsReferenceTracked);

                Debug.LogWarning($"[MontAR] Etapa '{step.StepId}' com validação visual sem marcador; usando o checklist.", step);
            }
            return new ChecklistValidator(step.ChecklistItems);
        }
    }
}
