using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MontAR
{
    /// <summary>
    /// Aba Estudo do PC: pede para apontar a mesa, mostra os pinos e a lista das peças e, ao tocar numa
    /// peça, abre o que ela faz, como encaixa, a ordem na montagem e os detalhes.
    /// O 3D e a colocação na mesa ficam no <see cref="StudyStage"/>.
    /// </summary>
    public class StudyScreen : UiScreen
    {
        [SerializeField] private TextAsset contentAsset;
        [SerializeField] private StudyInput surface;

        [Header("Topo")]
        [SerializeField] private Button exitButton;
        [SerializeField] private TMP_Text statusLabel;
        [SerializeField] private RectTransform topArea;
        [SerializeField] private Button modeArButton;
        [SerializeField] private Image modeArBackground;
        [SerializeField] private TMP_Text modeArLabel;
        [SerializeField] private Button modeDemoButton;
        [SerializeField] private Image modeDemoBackground;
        [SerializeField] private TMP_Text modeDemoLabel;
        [SerializeField] private GameObject hintChip;
        [SerializeField] private Image hintBackground;
        [SerializeField] private TMP_Text hintLabel;

        [Header("Pinos")]
        [SerializeField] private RectTransform pinsLayer;
        [SerializeField] private StudyPinView pinTemplate;

        [Header("Lista de peças")]
        [SerializeField] private RectTransform overviewCard;
        [SerializeField] private RectTransform chipsContainer;
        [SerializeField] private Button chipTemplate;
        [SerializeField] private Button explodeButton;
        [SerializeField] private TMP_Text explodeLabel;
        [SerializeField] private Button repositionButton;

        [Header("Peça aberta")]
        [SerializeField] private RectTransform partSheet;
        [SerializeField] private TMP_Text partNumberLabel;
        [SerializeField] private TMP_Text partNameLabel;
        [SerializeField] private TMP_Text partOrderLabel;
        [SerializeField] private TMP_Text taglineLabel;
        [SerializeField] private TMP_Text whatLabel;
        [SerializeField] private TMP_Text howLabel;
        [SerializeField] private GameObject detailsGroup;
        [SerializeField] private TMP_Text detailsLabel;
        [SerializeField] private ScrollRect sheetScroll;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button detailsButton;
        [SerializeField] private TMP_Text detailsButtonLabel;
        [SerializeField] private Button fitButton;
        [SerializeField] private Button previousButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private float compactHeight = 880f;
        [SerializeField] private float expandedHeight = 1400f;
        [SerializeField] private float sheetMargin = 24f;

        private const float SearchHelpSeconds = 12f;

        private readonly Dictionary<string, StudyPinView> pins = new Dictionary<string, StudyPinView>();
        private readonly Vector3[] corners = new Vector3[4];
        private StudyStage stage;
        private StudyContent content;
        private bool built;
        private bool arAvailable = true;
        private bool detailsOpen;
        private float sheetHeight;

        public event Action ExitRequested;
        public event Action<bool> ModeRequested;

        public bool UsesCamera => stage != null && stage.UsesCamera;

        /// <summary>Id da peça aberta (null na visão geral).</summary>
        public string SelectedPartId => stage != null && stage.Selected != null ? stage.Selected.Id : null;

        public bool DetailsOpen => detailsOpen;

        public StudyStage Stage => stage;

        private void Awake()
        {
            chipTemplate.gameObject.SetActive(false);
            pinTemplate.gameObject.SetActive(false);
            partSheet.gameObject.SetActive(false);

            exitButton.onClick.AddListener(() => ExitRequested?.Invoke());
            modeArButton.onClick.AddListener(() => ModeRequested?.Invoke(true));
            modeDemoButton.onClick.AddListener(() => ModeRequested?.Invoke(false));
            explodeButton.onClick.AddListener(() => stage.SetExploded(!stage.IsExploded));
            repositionButton.onClick.AddListener(() => stage.Reposition());
            closeButton.onClick.AddListener(() => stage.ClearSelection());
            fitButton.onClick.AddListener(() => stage.PlayFit());
            detailsButton.onClick.AddListener(() => SetDetailsOpen(!detailsOpen));
            previousButton.onClick.AddListener(() => Step(-1));
            nextButton.onClick.AddListener(() => Step(1));

            surface.Tapped += OnTapped;
            surface.Dragged += delta => stage.Orbit(delta);
            surface.Zoomed += factor => stage.Zoom(factor);
        }

        public void Bind(StudyStage studyStage)
        {
            stage = studyStage;
            content = StudyContent.Parse(contentAsset.text);
            stage.StateChanged += Refresh;
        }

        public void Begin(bool useCamera)
        {
            detailsOpen = false;
            stage.Begin(useCamera);
            BuildLists();
            Refresh();
        }

        public void End()
        {
            stage.End();
        }

        public void SetCameraMode(bool useCamera)
        {
            stage.SetCameraMode(useCamera);
            Refresh();
        }

        public void SetArAvailable(bool available)
        {
            arAvailable = available;
            if (IsVisible)
                Refresh();
        }

        /// <summary>Abre uma peça pelo id (chips, pinos e testes).</summary>
        public void SelectPart(string id)
        {
            StudyPart part = FindPart(id);
            if (part != null)
                stage.Select(part);
        }

        /// <summary>Botão voltar do Android: fecha os detalhes, depois a peça, depois a aba.</summary>
        public void HandleBack()
        {
            if (stage.Selected == null)
                ExitRequested?.Invoke();
            else if (detailsOpen)
                SetDetailsOpen(false);
            else
                stage.ClearSelection();
        }

        private StudyPart FindPart(string id)
        {
            foreach (StudyPart part in stage.Parts)
            {
                if (part.Id == id)
                    return part;
            }
            return null;
        }

        private void BuildLists()
        {
            if (built)
                return;
            built = true;

            foreach (StudyPartInfo info in content.parts)
            {
                string id = info.id;
                if (FindPart(id) == null)
                {
                    Debug.LogWarning($"[MontAR] Estudo: a peça '{id}' do conteúdo não existe no modelo 3D.");
                    continue;
                }

                Button chip = Instantiate(chipTemplate, chipsContainer);
                chip.name = "Chip " + id;
                chip.gameObject.SetActive(true);
                var label = chip.GetComponentInChildren<TMP_Text>();
                label.text = $"<color=#4ADE80>{info.sequence}</color>  {info.shortName}";
                chip.GetComponent<LayoutElement>().preferredWidth = label.GetPreferredValues(label.text).x + 76f;
                chip.onClick.AddListener(() => SelectPart(id));

                StudyPinView pin = Instantiate(pinTemplate, pinsLayer);
                pin.name = "Pino " + id;
                pin.Bind(info.sequence);
                pin.Button.onClick.AddListener(() => SelectPart(id));
                pins[id] = pin;
            }
        }

        private void OnTapped(Vector2 screenPoint)
        {
            if (!stage.IsPlaced)
            {
                stage.TryPlace();
                return;
            }

            StudyPart part = stage.Pick(screenPoint);
            if (part != null)
                stage.Select(part);
            else
                stage.ClearSelection();
        }

        private void Step(int direction)
        {
            StudyPartInfo current = content.Find(SelectedPartId);
            int index = content.parts.IndexOf(current) + direction;
            if (index >= 0 && index < content.parts.Count)
                SelectPart(content.parts[index].id);
        }

        private void SetDetailsOpen(bool open)
        {
            detailsOpen = open;
            detailsGroup.SetActive(open);
            detailsButtonLabel.text = open ? "Menos detalhes" : "Mais detalhes";
            if (open)
                sheetScroll.verticalNormalizedPosition = 0f;
        }

        private void Refresh()
        {
            if (stage == null || !IsVisible)
                return;

            RefreshMode();
            RefreshHint();

            StudyPart selected = stage.Selected;
            overviewCard.gameObject.SetActive(stage.IsPlaced && selected == null);
            partSheet.gameObject.SetActive(selected != null);
            explodeLabel.text = stage.IsExploded ? "Juntar peças" : "Separar peças";
            repositionButton.gameObject.SetActive(stage.UsesCamera);

            if (selected != null)
                ShowPart(content.Find(selected.Id), selected);
        }

        private void RefreshMode()
        {
            bool camera = stage.UsesCamera;
            modeArBackground.color = camera ? UiTheme.Primary : Color.clear;
            modeArLabel.color = camera ? UiTheme.TextOnDark : arAvailable ? UiTheme.TextSecondary : UiTheme.Disabled;
            modeDemoBackground.color = camera ? Color.clear : UiTheme.Primary;
            modeDemoLabel.color = camera ? UiTheme.TextSecondary : UiTheme.TextOnDark;
            modeArButton.interactable = arAvailable;
        }

        private void RefreshHint()
        {
            bool searching = !stage.IsPlaced;
            hintChip.SetActive(searching || stage.Selected == null);
            if (searching)
            {
                hintBackground.color = stage.HasSurface ? UiTheme.StatusGuiding : UiTheme.StatusLocating;
                hintLabel.text = stage.HasSurface
                    ? "Mesa encontrada: toque na tela para colocar o PC"
                    : stage.SearchSeconds > SearchHelpSeconds
                        ? "Não achou? Precisa de luz, e mesa muito lisa atrapalha: uma folha impressa em cima ajuda. Ou use o 3D."
                        : "Aponte para a mesa e mova o celular devagar";
                statusLabel.text = "Procurando a mesa";
            }
            else
            {
                hintBackground.color = UiTheme.StatusDemo;
                hintLabel.text = "Toque numa peça para aprender. Arraste para girar e use a pinça para aproximar.";
                statusLabel.text = stage.UsesCamera ? "PC na sua mesa" : "PC na mesa virtual";
            }
        }

        private void ShowPart(StudyPartInfo info, StudyPart part)
        {
            if (info == null)
                return;

            partNumberLabel.text = info.sequence.ToString();
            partNameLabel.text = info.name;
            partOrderLabel.text = info.orderLabel;
            taglineLabel.text = info.tagline;
            whatLabel.text = info.whatItDoes;
            howLabel.text = info.howItFits;
            fitButton.interactable = part.FitOffset != Vector3.zero;

            var details = new StringBuilder();
            foreach (StudyDetail detail in info.details)
            {
                if (details.Length > 0)
                    details.Append("\n\n");
                details.Append("<b><color=#4ADE80>").Append(detail.title).Append("</color></b>\n").Append(detail.text);
            }
            detailsLabel.text = details.ToString();

            int index = content.parts.IndexOf(info);
            previousButton.interactable = index > 0;
            nextButton.interactable = index < content.parts.Count - 1;

            SetDetailsOpen(false);
            sheetScroll.verticalNormalizedPosition = 1f;
        }

        private void LateUpdate()
        {
            if (stage == null)
                return;

            float target = stage.Selected != null ? (detailsOpen ? expandedHeight : compactHeight) : 0f;
            sheetHeight = Mathf.Lerp(sheetHeight, target, 1f - Mathf.Exp(-12f * Time.unscaledDeltaTime));
            partSheet.offsetMax = new Vector2(partSheet.offsetMax.x, sheetMargin + sheetHeight);

            if (!stage.IsPlaced)
                RefreshHint();
            UpdateFreeArea();
            UpdatePins();
        }

        private void UpdateFreeArea()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
                return;

            var root = (RectTransform)canvas.rootCanvas.transform;
            float height = Mathf.Max(1f, root.rect.height);
            topArea.GetWorldCorners(corners);
            float top = (root.InverseTransformPoint(corners[0]).y - root.rect.yMin) / height;

            float bottom = 0.05f;
            RectTransform lower = partSheet.gameObject.activeSelf ? partSheet : overviewCard.gameObject.activeSelf ? overviewCard : null;
            if (lower != null)
            {
                lower.GetWorldCorners(corners);
                bottom = (root.InverseTransformPoint(corners[1]).y - root.rect.yMin) / height;
            }

            // Num quadro em que o layout ainda não foi refeito as medidas saem trocadas.
            if (top - bottom >= 0.12f)
                stage.SetFreeArea(bottom, top);
        }

        private void UpdatePins()
        {
            bool show = stage.IsPlaced && stage.Selected == null;
            Vector2 size = pinsLayer.rect.size;
            foreach (StudyPart part in stage.Parts)
            {
                if (!pins.TryGetValue(part.Id, out StudyPinView pin))
                    continue;

                bool visible = show && stage.TryGetPinViewport(part, out Vector2 viewport) && part.Visibility > 0.9f;
                if (pin.gameObject.activeSelf != visible)
                    pin.gameObject.SetActive(visible);
                if (visible)
                {
                    stage.TryGetPinViewport(part, out Vector2 point);
                    ((RectTransform)pin.transform).anchoredPosition = new Vector2((point.x - 0.5f) * size.x, (point.y - 0.5f) * size.y);
                }
            }
        }
    }
}
