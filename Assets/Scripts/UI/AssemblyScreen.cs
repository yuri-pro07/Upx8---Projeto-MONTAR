using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MontAR
{
    /// <summary>
    /// Tela da montagem: progresso e situação da etapa (Aponta → Guia → Validação) no cartão do topo,
    /// troca entre RA e demonstração 3D (com o cadeado que diz se o 3D gira), instrução, alerta do
    /// erro comum, erro da última validação perto do botão, checklist de validação e o botão discreto
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

        [Header("Situação da etapa (no cartão do topo)")]
        [SerializeField] private GameObject statusRow;
        [SerializeField] private Image statusChip;
        [SerializeField] private Image statusIcon;
        [SerializeField] private TMP_Text statusLabel;
        [SerializeField] private Sprite locatingSprite;
        [SerializeField] private Sprite guidingSprite;
        [SerializeField] private Sprite demoSprite;

        [Header("Modo e visualização 3D")]
        [SerializeField] private Button modeArButton;
        [SerializeField] private Image modeArBackground;
        [SerializeField] private TMP_Text modeArLabel;
        [SerializeField] private Button modeDemoButton;
        [SerializeField] private Image modeDemoBackground;
        [SerializeField] private TMP_Text modeDemoLabel;
        [Tooltip("Aviso com cadeado: 3D livre para girar ou travado durante a validação.")]
        [SerializeField] private GameObject viewHint;
        [SerializeField] private Image viewHintBackground;
        [SerializeField] private Image lockIcon;
        [SerializeField] private TMP_Text lockLabel;
        [SerializeField] private Sprite lockedSprite;
        [SerializeField] private Sprite unlockedSprite;
        [Tooltip("Parte de baixo dos cartões do topo: a peça 3D fica entre ela e o painel.")]
        [SerializeField] private RectTransform topArea;

        [Header("Etapa")]
        [SerializeField] private GameObject stepSheet;
        [SerializeField] private SheetDragHandler sheetDrag;
        [SerializeField] private Button collapseButton;
        [SerializeField] private TMP_Text collapseLabel;
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private GameObject detailsGroup;
        [SerializeField] private TMP_Text instructionLabel;
        [SerializeField] private GameObject alertBox;
        [SerializeField] private TMP_Text alertLabel;
        [Tooltip("Erro da última validação, logo acima do botão. Não some ao esconder a instrução.")]
        [SerializeField] private GameObject errorBox;
        [SerializeField] private TMP_Text errorLabel;
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
        [Tooltip("Bancada virtual (na cena): recebe a área livre da tela para centralizar a peça.")]
        [SerializeField] private DemoStage demoStage;

        [Tooltip("Aviso curto que aparece logo acima do painel da etapa, sem cobrir os cartões.")]
        [SerializeField] private Toast toast;
        [SerializeField] private float toastGap = 20f;

        private readonly List<ChecklistItemView> items = new List<ChecklistItemView>();
        private readonly List<string> pendingAtConfirm = new List<string>();
        private readonly Vector3[] corners = new Vector3[4];
        private StepFlowController controller;
        private bool collapsed;
        private bool arAvailable = true;
        private string validationError;

        public event Action ExitRequested;
        public event Action<GuidanceMode> ModeRequested;

        /// <summary>A validação está aberta (o botão voltar do Android fecha ela antes de sair).</summary>
        public bool IsValidating => controller != null && controller.Flow != null && controller.Flow.IsRunning
            && controller.Flow.Phase == StepPhase.Validating;

        /// <summary>Erro da última validação reprovada (vazio quando não há).</summary>
        public string ValidationError => validationError ?? string.Empty;

        /// <summary>A instrução está escondida (painel recolhido).</summary>
        public bool IsCollapsed => collapsed;

        private void Awake()
        {
            checklistTemplate.gameObject.SetActive(false);
            exitButton.onClick.AddListener(() => ExitRequested?.Invoke());
            collapseButton.onClick.AddListener(() => SetCollapsed(!collapsed));
            if (sheetDrag != null)
                sheetDrag.Swiped += up => SetCollapsed(!up);
            validateButton.onClick.AddListener(() =>
            {
                if (controller != null)
                    controller.RequestValidation();
                Refresh();
            });
            confirmButton.onClick.AddListener(() =>
            {
                if (controller == null)
                    return;
                RememberPendingItems();
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
                    toast.Show("Ajuda do observador registrada");
            });
            observerButton.OnShortPress.AddListener(() =>
                toast.Show("Botão do observador: quem acompanha a montagem segura por 1 s para registrar uma ajuda. Você não precisa tocar nele.", 3.6f));
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

        /// <summary>Esconde ou mostra a instrução (toque no puxador ou arrasto do painel).</summary>
        public void SetCollapsed(bool value)
        {
            if (collapsed == value)
                return;
            collapsed = value;
            Refresh();
        }

        /// <summary>Aviso curto acima do painel (ex.: avisos do app durante a montagem).</summary>
        public void ShowNotice(string message, float seconds = 2.4f) => toast.Show(message, seconds);

        public void HandleFlowEvent(FlowEvent flowEvent)
        {
            switch (flowEvent.Type)
            {
                case FlowEventType.StepStarted:
                    collapsed = false;
                    validationError = null;
                    break;
                case FlowEventType.ValidationStarted:
                    RebuildChecklist();
                    break;
                case FlowEventType.ValidationFailed:
                    validationError = DescribeFailure();
                    collapsed = false;
                    break;
                case FlowEventType.StepCompleted:
                    validationError = null;
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
            RefreshViewHint(demo, validating);

            stepSheet.SetActive(!validating);
            validationSheet.SetActive(validating);
            detailsGroup.SetActive(!collapsed);
            collapseLabel.text = collapsed ? "Arraste para cima (ou toque) para ver a instrução" : "Arraste para baixo para esconder a instrução";

            bool hasError = !string.IsNullOrEmpty(validationError);
            errorBox.SetActive(hasError);
            errorLabel.text = hasError ? validationError : string.Empty;

            bool locating = flow.Phase == StepPhase.Locating;
            validateButton.interactable = flow.Phase == StepPhase.Guiding;
            validateLabel.text = locating ? "Aponte para o marcador A" : hasError ? "Validar de novo" : "Validar etapa";

            orbitSurface.SetActive(demo && !validating);

            if (validating)
                RefreshValidation();
        }

        private void LateUpdate()
        {
            RectTransform sheet = ActiveSheet();
            if (sheet == null)
                return;

            sheet.GetWorldCorners(corners);
            Vector3 sheetTop = corners[1];

            // Aviso logo acima do painel visível: não cobre os cartões do topo nem o painel.
            if (toast != null && toast.transform.parent is RectTransform toastParent)
            {
                var toastRect = (RectTransform)toast.transform;
                float y = toastParent.InverseTransformPoint(sheetTop).y - toastParent.rect.yMin;
                toastRect.anchoredPosition = new Vector2(toastRect.anchoredPosition.x, y + toastGap);
            }

            // A peça 3D fica centrada no espaço entre os cartões do topo e o painel.
            if (demoStage != null && demoStage.IsVisible && topArea != null)
            {
                Canvas canvas = GetComponentInParent<Canvas>();
                if (canvas == null)
                    return;
                var root = (RectTransform)canvas.rootCanvas.transform;
                topArea.GetWorldCorners(corners);
                Vector3 topBottom = corners[0];
                float height = Mathf.Max(1f, root.rect.height);
                float top = (root.InverseTransformPoint(topBottom).y - root.rect.yMin) / height;
                float bottom = (root.InverseTransformPoint(sheetTop).y - root.rect.yMin) / height;
                // Num quadro em que o layout ainda não foi refeito (troca de painel, rotação) as medidas
                // saem trocadas: só aceita uma faixa plausível no meio da tela.
                if (top - bottom >= 0.2f && bottom <= 0.75f && top >= 0.45f)
                    demoStage.SetFreeArea(bottom, top);
            }
        }

        private RectTransform ActiveSheet()
        {
            if (validationSheet != null && validationSheet.activeInHierarchy)
                return (RectTransform)validationSheet.transform;
            if (stepSheet != null && stepSheet.activeInHierarchy)
                return (RectTransform)stepSheet.transform;
            return null;
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
            // Na validação o painel de baixo já diz "Confira antes de avançar": não repetir no topo.
            bool validating = flow.Phase == StepPhase.Validating;
            statusRow.SetActive(!validating);
            if (validating)
                return;

            if (flow.Phase == StepPhase.Locating)
            {
                SetStatus(UiTheme.StatusLocating, locatingSprite, "Aponte a câmera para o marcador A do tapete");
            }
            else if (demo)
            {
                SetStatus(UiTheme.StatusDemo, demoSprite, step.OverlayPrefab != null
                    ? "Demonstração 3D: veja na bancada virtual onde e como encaixar"
                    : "Demonstração 3D: esta etapa não tem peça para mostrar; siga a instrução");
            }
            else
            {
                SetStatus(UiTheme.StatusGuiding, guidingSprite, string.IsNullOrEmpty(step.ReferenceImageName)
                    ? "Siga a instrução abaixo"
                    : "Faça o mesmo movimento da peça fantasma sobre a placa");
            }
        }

        private void SetStatus(Color color, Sprite icon, string text)
        {
            statusChip.color = color;
            statusIcon.sprite = icon;
            statusIcon.enabled = icon != null;
            statusLabel.text = text;
        }

        private void RefreshViewHint(bool demo, bool validating)
        {
            viewHint.SetActive(demo);
            if (!demo)
                return;

            lockIcon.sprite = validating ? lockedSprite : unlockedSprite;
            lockIcon.color = validating ? UiTheme.StatusLocating : UiTheme.TextOnDark;
            viewHintBackground.color = validating ? new Color(0.09f, 0.09f, 0.1f, 0.92f) : new Color(0f, 0f, 0f, 0.5f);
            lockLabel.text = validating
                ? "<b>3D travado</b> enquanto você confere a etapa"
                : "<b>3D livre:</b> arraste para girar · pinça aproxima · toque duplo centraliza";
        }

        private void RememberPendingItems()
        {
            pendingAtConfirm.Clear();
            ChecklistValidator checklist = controller.CurrentChecklist;
            if (checklist == null)
                return;
            for (int i = 0; i < checklist.Count; i++)
            {
                if (!checklist.IsChecked(i))
                    pendingAtConfirm.Add(checklist.GetItem(i));
            }
        }

        private string DescribeFailure()
        {
            var text = new StringBuilder("<b>Etapa não validada.</b> ");
            if (pendingAtConfirm.Count == 0)
            {
                text.Append("Confira a peça e valide de novo.");
                return text.ToString();
            }

            text.Append(pendingAtConfirm.Count == 1 ? "Ficou sem confirmar:" : "Ficaram sem confirmar:");
            foreach (string item in pendingAtConfirm)
                text.Append("\n• ").Append(item);
            text.Append("\nCorrija e toque em Validar de novo.");
            return text.ToString();
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
