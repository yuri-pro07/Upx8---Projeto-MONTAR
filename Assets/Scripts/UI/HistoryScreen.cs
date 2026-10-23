using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MontAR
{
    /// <summary>
    /// Histórico de montagens do aparelho: uma lista com o nome dado na tela de início, o tempo,
    /// os erros apontados e as etapas de cada montagem concluída. É para onde a conclusão leva.
    /// </summary>
    public class HistoryScreen : UiScreen
    {
        [SerializeField] private RectTransform listContainer;
        [SerializeField] private HistoryEntryView entryTemplate;
        [SerializeField] private TMP_Text emptyLabel;
        [SerializeField] private TMP_Text summaryLabel;
        [SerializeField] private Button backButton;
        [SerializeField] private Button newSessionButton;
        [SerializeField] private ScrollRect scrollRect;

        private readonly List<HistoryEntryView> views = new List<HistoryEntryView>();

        public event Action Back;
        public event Action NewSessionRequested;

        /// <summary>Quantos cartões estão na lista (para os testes).</summary>
        public int EntryCount => views.Count;

        private void Awake()
        {
            backButton.onClick.AddListener(() => Back?.Invoke());
            newSessionButton.onClick.AddListener(() => NewSessionRequested?.Invoke());
            entryTemplate.gameObject.SetActive(false);
        }

        /// <summary>Mostra as montagens (da mais recente para a mais antiga). <paramref name="latestSessionId"/> fica aberta e marcada.</summary>
        public void Bind(IReadOnlyList<HistoryEntry> entries, string latestSessionId)
        {
            if (entryTemplate.gameObject.activeSelf)
                entryTemplate.gameObject.SetActive(false);

            foreach (HistoryEntryView view in views)
                Destroy(view.gameObject);
            views.Clear();

            int count = entries != null ? entries.Count : 0;
            for (int i = 0; i < count; i++)
            {
                HistoryEntryView view = Instantiate(entryTemplate, listContainer);
                view.gameObject.SetActive(true);
                view.name = $"Montagem {i + 1}";
                view.Bind(entries[i], !string.IsNullOrEmpty(latestSessionId) && entries[i].sessionId == latestSessionId);
                views.Add(view);
            }

            emptyLabel.gameObject.SetActive(count == 0);
            summaryLabel.text = count == 0
                ? "Nenhuma montagem concluída neste aparelho ainda."
                : count == 1 ? "1 montagem concluída neste aparelho." : $"{count} montagens concluídas neste aparelho.";

            if (scrollRect != null)
                scrollRect.verticalNormalizedPosition = 1f;
        }
    }
}
