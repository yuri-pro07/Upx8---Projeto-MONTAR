using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MontAR
{
    /// <summary>
    /// Seleção das peças: qual modelo de cada componente e quantas unidades. O kit decide
    /// quais etapas entram no roteiro (ex.: sem placa de vídeo, a etapa do PCIe some).
    /// </summary>
    public class PartsScreen : UiScreen
    {
        [SerializeField] private RectTransform listContainer;
        [SerializeField] private PartRowView rowTemplate;
        [SerializeField] private TMP_Text summaryLabel;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button backButton;
        [SerializeField] private ScrollRect scrollRect;

        private readonly List<PartRowView> rows = new List<PartRowView>();
        private PartSelection selection;
        private Func<PartSelection, int> countSteps;

        public event Action Continued;
        public event Action Back;

        private void Awake()
        {
            continueButton.onClick.AddListener(() => Continued?.Invoke());
            backButton.onClick.AddListener(() => Back?.Invoke());
            rowTemplate.gameObject.SetActive(false);
        }

        /// <summary>Monta a lista a partir do catálogo. A seleção é alterada no lugar.</summary>
        public void Bind(PartCatalog catalog, PartSelection kit, Func<PartSelection, int> stepCounter)
        {
            if (rowTemplate.gameObject.activeSelf)
                rowTemplate.gameObject.SetActive(false);

            selection = kit;
            countSteps = stepCounter;

            foreach (PartRowView row in rows)
                Destroy(row.gameObject);
            rows.Clear();

            foreach (PartDefinition part in catalog.Parts)
            {
                if (part == null)
                    continue;
                PartRowView row = Instantiate(rowTemplate, listContainer);
                row.gameObject.SetActive(true);
                row.Bind(part, selection, RefreshSummary);
                rows.Add(row);
            }

            if (scrollRect != null)
                scrollRect.verticalNormalizedPosition = 1f;
            RefreshSummary();
        }

        private void RefreshSummary()
        {
            int steps = countSteps != null ? countSteps(selection) : 0;
            summaryLabel.text = steps == 1 ? "O roteiro terá 1 etapa" : $"O roteiro terá {steps} etapas";
            continueButton.interactable = steps > 0;
        }
    }
}
