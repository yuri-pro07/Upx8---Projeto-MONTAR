using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MontAR
{
    /// <summary>
    /// Última tela antes da montagem: código anônimo do participante, nome opcional da montagem
    /// (para achar no histórico), modo de orientação (RA ou demonstração 3D) e o roteiro que o kit gerou.
    /// </summary>
    public class StartScreen : UiScreen
    {
        [SerializeField] private TMP_InputField participantInput;
        [SerializeField] private TMP_Text participantError;
        [Tooltip("Nome opcional da montagem (ex.: \"Bancada 3 · HP\"). Fica só no histórico do aparelho.")]
        [SerializeField] private TMP_InputField labelInput;
        [SerializeField] private Button modeArButton;
        [SerializeField] private Image modeArBackground;
        [SerializeField] private TMP_Text modeArLabel;
        [SerializeField] private Button modeDemoButton;
        [SerializeField] private Image modeDemoBackground;
        [SerializeField] private TMP_Text modeDemoLabel;
        [SerializeField] private TMP_Text modeNote;
        [Header("Peças reais ou virtuais")]
        [SerializeField] private Button partsRealButton;
        [SerializeField] private Image partsRealBackground;
        [SerializeField] private TMP_Text partsRealLabel;
        [SerializeField] private Button partsVirtualButton;
        [SerializeField] private Image partsVirtualBackground;
        [SerializeField] private TMP_Text partsVirtualLabel;
        [SerializeField] private TMP_Text partsNote;
        [SerializeField] private TMP_Text stepsHeader;
        [SerializeField] private TMP_Text stepsList;
        [SerializeField] private Button startButton;
        [SerializeField] private Button backButton;

        private const string SimulatedPartsKey = "montar.pecas_virtuais";

        private GuidanceMode mode;
        private bool arAvailable;
        private bool simulatedParts = true;

        /// <summary>Peças virtuais (sem o kit): a escolha fica salva no aparelho. Padrão: ligada.</summary>
        public static bool SavedSimulatedParts => PlayerPrefs.GetInt(SimulatedPartsKey, 1) == 1;

        /// <summary>Montagem com peças virtuais escolhida nesta tela.</summary>
        public bool SimulatedParts => simulatedParts;

        /// <summary>Código já validado, nome da montagem (pode ser vazio) e modo escolhido.</summary>
        public event Action<string, string, GuidanceMode> StartRequested;
        public event Action Back;

        private void Awake()
        {
            startButton.onClick.AddListener(TryStart);
            backButton.onClick.AddListener(() => Back?.Invoke());
            modeArButton.onClick.AddListener(() => SetMode(GuidanceMode.AugmentedReality));
            modeDemoButton.onClick.AddListener(() => SetMode(GuidanceMode.Demo3D));
            if (partsRealButton != null)
                partsRealButton.onClick.AddListener(() => SetSimulatedParts(false));
            if (partsVirtualButton != null)
                partsVirtualButton.onClick.AddListener(() => SetSimulatedParts(true));
            participantInput.characterLimit = ParticipantCode.MaxLength;
            // Maiúsculas enquanto digita e só os caracteres aceitos no código (o resto é ignorado).
            participantInput.onValidateInput = (text, index, c) =>
            {
                c = char.ToUpperInvariant(c);
                bool allowed = (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '-' || c == '_';
                return allowed ? c : '\0';
            };
            participantInput.onValueChanged.AddListener(_ => participantError.gameObject.SetActive(false));
            participantInput.onSubmit.AddListener(_ => TryStart());
            if (labelInput != null)
                labelInput.characterLimit = AssemblyHistoryStore.MaxLabelLength;
        }

        public void Bind(IReadOnlyList<AssemblyStep> steps, bool isArAvailable, GuidanceMode preferredMode, string lastCode)
        {
            arAvailable = isArAvailable;
            participantInput.text = lastCode ?? string.Empty;
            participantError.gameObject.SetActive(false);
            if (labelInput != null)
                labelInput.text = string.Empty;

            stepsHeader.text = $"Roteiro ({steps.Count} etapas)";
            var text = new StringBuilder();
            for (int i = 0; i < steps.Count; i++)
            {
                if (i > 0)
                    text.Append('\n');
                text.Append(i + 1).Append(". ").Append(steps[i].Title);
            }
            stepsList.text = text.ToString();

            SetSimulatedParts(SavedSimulatedParts);
            SetMode(arAvailable ? preferredMode : GuidanceMode.Demo3D);
        }

        private void SetSimulatedParts(bool value)
        {
            simulatedParts = value;
            PlayerPrefs.SetInt(SimulatedPartsKey, value ? 1 : 0);
            if (partsRealBackground == null)
                return;

            partsRealBackground.color = value ? Color.clear : UiTheme.Primary;
            partsRealLabel.color = value ? UiTheme.TextSecondary : UiTheme.TextOnDark;
            partsVirtualBackground.color = value ? UiTheme.Primary : Color.clear;
            partsVirtualLabel.color = value ? UiTheme.TextOnDark : UiTheme.TextSecondary;
            partsNote.text = value
                ? "Sem o kit: a placa-mãe e as peças aparecem em 3D (na RA, sobre o marcador A). Em cada etapa, <b>Simular encaixe</b> mostra a peça entrando no lugar, e ela fica montada."
                : "Com o kit de verdade: a peça fantasma aparece sobre a placa real e você encaixa a peça com as mãos.";
        }

        /// <summary>Atualiza a disponibilidade da RA (o ARCore pode responder depois que a tela abriu).</summary>
        public void SetArAvailable(bool available)
        {
            arAvailable = available;
            SetMode(available ? mode : GuidanceMode.Demo3D);
        }

        private void SetMode(GuidanceMode newMode)
        {
            if (newMode == GuidanceMode.AugmentedReality && !arAvailable)
                newMode = GuidanceMode.Demo3D;
            mode = newMode;

            bool ar = mode == GuidanceMode.AugmentedReality;
            modeArBackground.color = ar ? UiTheme.Primary : Color.clear;
            modeArLabel.color = ar ? UiTheme.TextOnDark : (arAvailable ? UiTheme.TextSecondary : UiTheme.Disabled);
            modeDemoBackground.color = ar ? Color.clear : UiTheme.Primary;
            modeDemoLabel.color = ar ? UiTheme.TextSecondary : UiTheme.TextOnDark;
            modeArButton.interactable = arAvailable;

            modeNote.text = !arAvailable
                ? "Este aparelho não está com a RA disponível (sem ARCore ou ainda verificando). A demonstração 3D mostra cada encaixe numa bancada virtual."
                : ar
                    ? "Aponte a câmera para o marcador A do tapete: a peça fantasma aparece sobre a placa real."
                    : "Bancada virtual em 3D, sem câmera. Use para ver como faz; a condição do experimento é a RA.";
        }

        private void TryStart()
        {
            string code = participantInput.text;
            if (!ParticipantCode.IsValid(code))
            {
                participantError.text = "Use de 1 a 16 letras sem acento, números, - ou _ (ex.: P01). Nada de nome ou RA.";
                participantError.gameObject.SetActive(true);
                return;
            }
            string label = labelInput != null ? AssemblyHistoryStore.NormalizeLabel(labelInput.text) : string.Empty;
            StartRequested?.Invoke(ParticipantCode.Normalize(code), label, mode);
        }
    }
}
