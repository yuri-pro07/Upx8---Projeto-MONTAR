using System;
using System.Globalization;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MontAR
{
    /// <summary>
    /// Cartão de uma montagem no histórico: nome, data, participante, tempo, reprovações e,
    /// ao tocar em "Ver etapas", o tempo e os erros de cada etapa.
    /// </summary>
    public class HistoryEntryView : MonoBehaviour
    {
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private TMP_Text metaLabel;
        [SerializeField] private TMP_Text statsLabel;
        [SerializeField] private TMP_Text stepsLabel;
        [SerializeField] private Button toggleButton;
        [SerializeField] private TMP_Text toggleLabel;
        [Tooltip("Faixa \"recém-concluída\" mostrada na montagem que acabou de terminar.")]
        [SerializeField] private GameObject latestBadge;

        private bool expanded;

        public void Bind(HistoryEntry entry, bool latest)
        {
            nameLabel.text = entry.DisplayName;
            metaLabel.text = $"{FormatDate(entry.finishedUtc)} · participante {entry.participantCode} · {(entry.mode == "demo_3d" ? "demonstração 3D" : "RA")}";

            var stats = new StringBuilder();
            stats.Append("<b>").Append(SessionSummary.FormatDuration(entry.elapsedSeconds)).Append("</b> no total · ")
                .Append(entry.completedSteps).Append(entry.completedSteps == 1 ? " etapa" : " etapas").Append(" · ");
            stats.Append(entry.failedValidations == 0
                ? "<color=#16A34A>nenhum erro</color>"
                : $"<color=#DC2626>{entry.failedValidations} {(entry.failedValidations == 1 ? "erro apontado" : "erros apontados")}</color>");
            if (entry.observerInterventions > 0)
                stats.Append(" · ").Append(entry.observerInterventions).Append(entry.observerInterventions == 1 ? " ajuda do observador" : " ajudas do observador");
            if (entry.resumed)
                stats.Append(" · retomada");
            statsLabel.text = stats.ToString();

            var steps = new StringBuilder();
            for (int i = 0; i < entry.steps.Count; i++)
            {
                HistoryStepEntry step = entry.steps[i];
                if (steps.Length > 0)
                    steps.Append('\n');
                steps.Append(i + 1).Append(". ").Append(step.title)
                    .Append("  <color=#71717A>").Append(SessionSummary.FormatDuration(step.seconds)).Append("</color>");
                if (step.failedValidations > 0)
                    steps.Append(step.failedValidations == 1 ? "  <color=#DC2626>· 1 erro</color>" : $"  <color=#DC2626>· {step.failedValidations} erros</color>");
            }
            stepsLabel.text = steps.Length > 0 ? steps.ToString() : "Sem etapas registradas.";

            if (latestBadge != null)
                latestBadge.SetActive(latest);

            toggleButton.onClick.RemoveAllListeners();
            toggleButton.onClick.AddListener(() => SetExpanded(!expanded));
            SetExpanded(latest);
        }

        private void SetExpanded(bool value)
        {
            expanded = value;
            stepsLabel.gameObject.SetActive(expanded);
            toggleLabel.text = expanded ? "Esconder etapas" : "Ver etapas";
            LayoutRebuilder.MarkLayoutForRebuild((RectTransform)transform);
        }

        /// <summary>Data local curta (dd/MM/aaaa HH:mm) a partir do UTC salvo.</summary>
        public static string FormatDate(string utc)
        {
            if (string.IsNullOrEmpty(utc)
                || !DateTime.TryParse(utc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime when))
                return "data desconhecida";
            return when.ToLocalTime().ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
        }
    }
}
