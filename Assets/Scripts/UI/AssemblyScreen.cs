using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MontAR
{
    /// <summary>
    /// Tela da montagem: progresso, situação da etapa (Aponta → Guia → Valida), instrução, alerta
    /// do erro comum, checklist de validação, troca entre RA e demonstração 3D e o botão discreto
    /// do observador (toque longo). Lê o estado do <see cref="StepFlowController"/>.
    /// </summary>
    public class AssemblyScreen : UiScreen
    {
        [Header("Topo")]
        [SerializeField] private Button exitButton;
        [SerializeField] private TMP_Text stepCounter;
        [SerializeField] private TMP_Text componentLabel;
        [SerializeField] private Image progressFill;
        [SerializeField] private LongPressButton observerButton;

        [Header("Situação e modo")]
        [SerializeField] private Image statusChip;
        [SerializeField] private TMP_Text statusLabel;
        [SerializeField] private Button modeArButton;
        [SerializeField] private Image modeArBackground;
        [SerializeField] private TMP_Text modeArLabel;
        [SerializeField] private Button modeDemoButton;
        [SerializeField] private Image modeDemoBackground;
        [SerializeField] private TMP_Text modeDemoLabel;

        [Header("Etapa")]
        [SerializeField] private GameObject stepSheet;
        [SerializeField] private Button collapseButton;
        [SerializeField] private TMP_Text collapseLabel;
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private GameObject detailsGroup;
        [SerializeField] private TMP_Text instructionLabel;
        [SerializeField] private GameObject alertBox;
        [SerializeField] private TMP_Text alertLabel;
        [SerializeField] private Button validateButton;
        [SerializeField] private TMP_Text validateLabel;

        [Header("Validação")]
        [SerializeField] private GameObject validationSheet;
        [SerializeField] private TMP_Text validationHint;
        [SerializeField] private RectTransform checklistContainer;
        [SerializeField] private ChecklistItemView checklistTemplate;
        [SerializeField] private Button confirmButton;
        [SerializeField] private TMP_Text confirmLabel;
        [SerializeField] private Button cancelButton;

        [Header("Demonstração 3D")]
        [SerializeField] private GameObject orbitSurface;
        [SerializeField] private GameObject demoHint;

        [SerializeField] private Toast toast;

        private readonly List<ChecklistItemView> items = new List<ChecklistItemView>();
        private StepFlowController controller;
        private bool collapsed;
        private bool arAvailable = true;

        public event Action ExitRequested;
        public event Action<GuidanceMode> ModeRequested;

        /// <summary>A validação está aberta (o botão voltar do Android fecha ela antes de sair).</summary>
        public bool IsValidating => controller != null && controller.Flow != null && controller.Flow.IsRunning
            && controller.Flow.Phase == StepPhase.Validating;

        private void Awake()
        {
            checklistTemplate.gameObject.SetActive(false);
            exitButton.onClick.AddListener(() => ExitRequested?.Invoke());
            collapseButton.onClick.AddListener(() =>
            {
                collapsed = !collapsed;
                Refresh();
            });
            validateButton.onClick.AddListener(() =>
            {
                if (controller != null)
                    controller.RequestValidation();
                Refresh();
            });
            confirmButton.onClick.AddListener(() =>
            {
                if (controller != null)
                    controller.ConfirmChecklist();
            });
            cancelButton.onClick.AddListener(() =>
            {
                if (controller != null)
                    controller.CancelValidation();
            });
            modeArButton.onClick.AddListener(() => ModeRequested?.Invoke(GuidanceMode.AugmentedReality));
            modeDemoButton.onClick.AddListener(() => ModeRequested?.Invoke(GuidanceMode.Demo3D));
            observerButton.OnLongPress.AddListener(() =>
            {
                if (controller != null && controller.RegisterObserverIntervention())
                    toast.Show("Intervenção do observador registrada");
            });
        }

        public void Bind(StepFlowController flowController)
        {
            controller = flowController;
        }

        public void SetArAvailable(bool available)
        {
            arAvailable = available;
            if (IsVisible)
                Refresh();
        }

        public void HandleFlowEvent(FlowEvent flowEvent)
        {
            switch (flowEvent.Type)
            {
                case FlowEventType.StepStarted:
                    collapsed = false;
                    break;
                case FlowEventType.ValidationStarted:
                    RebuildChecklist();
                    break;
                case FlowEventType.ValidationFailed:
                    toast.Show("Etapa ainda não validada. Corrija o que ficou desmarcado e valide de novo.", 3.2f);
                    break;
                case FlowEventType.StepCompleted:
                    // A última etapa leva direto para a tela de conclusão.
                    StepFlow flow = controller != null ? controller.Flow : null;
                    if (flow != null && flowEvent.StepIndex + 1 < flow.StepCount)
                        toast.Show($"Etapa {flowEvent.StepIndex + 1} concluída!", 1.8f);
                    break;
            }

            if (IsVisible)
                Refresh();
        }

        public override void Show()
        {
            base.Show();
            Refresh();
        }

        public void Refresh()
        {
            StepFlow flow = controller != null ? controller.Flow : null;
            AssemblyStep step = controller != null ? controller.CurrentStep : null;
            if (flow == null || step == null)
                return;

            int total = flow.StepCount;
            stepCounter.text = $"Etapa {flow.CurrentIndex + 1} de {total}";
            componentLabel.text = step.ComponentName;
            progressFill.fillAmount = total > 0 ? (float)flow.CompletedCount / total : 0f;

            titleLabel.text = step.Title;
            instructionLabel.text = step.Instruction;
            bool hasAlert = !string.IsNullOrEmpty(step.CommonErrorAlert);
            alertBox.SetActive(hasAlert);
            alertLabel.text = hasAlert ? "<b>Atenção:</b> " + step.CommonErrorAlert : string.Empty;

            bool demo = flow.Mode == GuidanceMode.Demo3D;
            bool validating = flow.Phase == StepPhase.Validating;
            RefreshMode(demo);
            RefreshStatus(flow, step, demo);

            stepSheet.SetActive(!validating);
            validationSheet.SetActive(validating);
            detailsGroup.SetActive(!collapsed);
            collapseLabel.text = collapsed ? "Mostrar instrução" : "Esconder instrução";

            bool locating = flow.Phase == StepPhase.Locating;
            validateButton.interactable = flow.Phase == StepPhase.Guiding;
            validateLabel.text = locating ? "Aponte para o marcador A" : "Validar etapa";

            orbitSurface.SetActive(demo && !validating);
            demoHint.SetActive(demo && !validating);

            if (validating)
                RefreshValidation();
        }

        private void RefreshMode(bool demo)
        {
            modeArBackground.color = demo ? Color.clear : UiTheme.Primary;
            modeArLabel.color = demo ? (arAvailable ? UiTheme.TextOnDark : UiTheme.TextOnDarkMuted) : UiTheme.TextOnDark;
            modeDemoBackground.color = demo ? UiTheme.Primary : Color.clear;
            modeDemoLabel.color = UiTheme.TextOnDark;
            modeArButton.interactable = arAvailable;
        }

        private void RefreshStatus(StepFlow flow, AssemblyStep step, bool demo)
        {
            switch (flow.Phase)
            {
                case StepPhase.Locating:
                    statusChip.color = UiTheme.StatusLocating;
                    statusLabel.text = "Aponte a câmera para o marcador A do tapete";
                    break;
                case StepPhase.Validating:
                    statusChip.color = UiTheme.StatusValidating;
                    statusLabel.text = "Confira a etapa antes de avançar";
                    break;
                default:
                    if (demo)
                    {
                        statusChip.color = UiTheme.StatusDemo;
                        statusLabel.text = step.OverlayPrefab != null ? "Demonstração 3D: veja como encaixa" : "Demonstração 3D";
                    }
                    else
                    {
                        statusChip.color = UiTheme.StatusGuiding;
                        statusLabel.text = string.IsNullOrEmpty(step.ReferenceImageName)
                            ? "Siga a instrução abaixo"
                            : "Siga a peça fantasma sobre a placa";
                    }
                    break;
            }
        }

        private void RebuildChecklist()
        {
            foreach (ChecklistItemView item in items)
                Destroy(item.gameObject);
            items.Clear();

            ChecklistValidator checklist = controller != null ? controller.CurrentChecklist : null;
            if (checklist == null)
                return;

            for (int i = 0; i < checklist.Count; i++)
            {
                int index = i;
                ChecklistItemView item = Instantiate(checklistTemplate, checklistContainer);
                item.gameObject.SetActive(true);
                item.name = $"Item {i + 1}";
                item.Bind(checklist.GetItem(i), checklist.IsChecked(i), value =>
                {
                    ChecklistValidator current = controller.CurrentChecklist;
                    if (current == null || index >= current.Count)
                        return;
                    current.SetChecked(index, value);
                    RefreshValidation();
                });
                items.Add(item);
            }
        }

        private void RefreshValidation()
        {
            ChecklistValidator checklist = controller.CurrentChecklist;
            ReferenceSeenValidator visual = controller.CurrentVisualValidator;

            if (visual != null)
            {
                checklistContainer.gameObject.SetActive(false);
                confirmButton.gameObject.SetActive(false);
                validationHint.text = $"Aponte a câmera para o marcador <b>{visual.ReferenceName}</b>. A etapa é validada sozinha quando ele aparecer.";
                return;
            }

            checklistContainer.gameObject.SetActive(true);
            confirmButton.gameObject.SetActive(true);
            validationHint.text = "Marque só o que você conferiu na peça. Se algo não bate, deixe desmarcado: a etapa volta para você corrigir.";

            int done = 0;
            int count = checklist != null ? checklist.Count : 0;
            for (int i = 0; i < count; i++)
            {
                if (checklist.IsChecked(i))
                    done++;
            }
            confirmLabel.text = count == 0 ? "Confirmar" : $"Confirmar ({done}/{count})";
        }
    }
}
