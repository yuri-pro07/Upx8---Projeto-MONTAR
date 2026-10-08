using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MontAR
{
    /// <summary>
    /// Última tela antes da montagem: código anônimo do participante, modo de orientação
    /// (RA ou demonstração 3D) e o roteiro que o kit gerou.
    /// </summary>
    public class StartScreen : UiScreen
    {
        [SerializeField] private TMP_InputField participantInput;
        [SerializeField] private TMP_Text participantError;
        [SerializeField] private Button modeArButton;
        [SerializeField] private Image modeArBackground;
        [SerializeField] private TMP_Text modeArLabel;
        [SerializeField] private Button modeDemoButton;
        [SerializeField] private Image modeDemoBackground;
        [SerializeField] private TMP_Text modeDemoLabel;
        [SerializeField] private TMP_Text modeNote;
        [SerializeField] private TMP_Text stepsHeader;
        [SerializeField] private TMP_Text stepsList;
        [SerializeField] private Button startButton;
        [SerializeField] private Button backButton;

        private GuidanceMode mode;
        private bool arAvailable;

        /// <summary>Código já validado e modo escolhido.</summary>
        public event Action<string, GuidanceMode> StartRequested;
        public event Action Back;

        private void Awake()
        {
            startButton.onClick.AddListener(TryStart);
            backButton.onClick.AddListener(() => Back?.Invoke());
            modeArButton.onClick.AddListener(() => SetMode(GuidanceMode.AugmentedReality));
            modeDemoButton.onClick.AddListener(() => SetMode(GuidanceMode.Demo3D));
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
        }

        public void Bind(IReadOnlyList<AssemblyStep> steps, bool isArAvailable, GuidanceMode preferredMode, string lastCode)
        {
            arAvailable = isArAvailable;
            participantInput.text = lastCode ?? string.Empty;
            participantError.gameObject.SetActive(false);

            stepsHeader.text = $"Roteiro ({steps.Count} etapas)";
            var text = new StringBuilder();
            for (int i = 0; i < steps.Count; i++)
            {
                if (i > 0)
                    text.Append('\n');
                text.Append(i + 1).Append(". ").Append(steps[i].Title);
            }
            stepsList.text = text.ToString();

            SetMode(arAvailable ? preferredMode : GuidanceMode.Demo3D);
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
            modeArBackground.color = ar ? UiTheme.Primary : UiTheme.Surface;
            modeArLabel.color = ar ? UiTheme.TextOnDark : (arAvailable ? UiTheme.TextPrimary : UiTheme.Disabled);
            modeDemoBackground.color = ar ? UiTheme.Surface : UiTheme.Primary;
            modeDemoLabel.color = ar ? UiTheme.TextPrimary : UiTheme.TextOnDark;
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
            StartRequested?.Invoke(ParticipantCode.Normalize(code), mode);
        }
    }
}
