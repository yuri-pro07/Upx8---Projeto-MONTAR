using System;
using System.Collections.Generic;
using System.Text;

namespace MontAR
{
    /// <summary>Uma linha da seleção de peças. Campos públicos porque o JsonUtility só serializa campos.</summary>
    [Serializable]
    public class PartSelectionEntry
    {
        public string partId;
        public string modelId;
        public int quantity;
    }

    /// <summary>
    /// O kit escolhido na tela de peças: modelo e quantidade de cada componente.
    /// Quantidade 0 = peça fora da montagem. A ordem segue o catálogo.
    /// </summary>
    public class PartSelection
    {
        private readonly List<PartSelectionEntry> entries = new List<PartSelectionEntry>();

        public int Count => entries.Count;

        /// <summary>Define (ou troca) o modelo e a quantidade de uma peça.</summary>
        public void Set(string partId, string modelId, int quantity)
        {
            if (string.IsNullOrEmpty(partId))
                throw new ArgumentException("A peça precisa de um identificador.", nameof(partId));
            if (quantity < 0)
                throw new ArgumentOutOfRangeException(nameof(quantity));

            PartSelectionEntry entry = Find(partId);
            if (entry == null)
            {
                entry = new PartSelectionEntry { partId = partId };
                entries.Add(entry);
            }
            entry.modelId = modelId ?? string.Empty;
            entry.quantity = quantity;
        }

        public int GetQuantity(string partId) => Find(partId)?.quantity ?? 0;

        public string GetModelId(string partId) => Find(partId)?.modelId ?? string.Empty;

        public bool Includes(string partId) => GetQuantity(partId) > 0;

        /// <summary>
        /// Resumo para o CSV, na ordem do catálogo: "peça:modelo:quantidade" separados por "|"
        /// (ex.: "cpu:intel_lga1700:1|ram:ddr5_dimm:2|ssd:ssd_m2_2280:0").
        /// </summary>
        public string Describe()
        {
            var text = new StringBuilder();
            foreach (PartSelectionEntry entry in entries)
            {
                if (text.Length > 0)
                    text.Append('|');
                text.Append(entry.partId).Append(':').Append(entry.modelId).Append(':').Append(entry.quantity);
            }
            return text.ToString();
        }

        /// <summary>Cópia das linhas, para salvar no progresso.</summary>
        public List<PartSelectionEntry> ToEntries()
        {
            var copy = new List<PartSelectionEntry>(entries.Count);
            foreach (PartSelectionEntry entry in entries)
                copy.Add(new PartSelectionEntry { partId = entry.partId, modelId = entry.modelId, quantity = entry.quantity });
            return copy;
        }

        public PartSelection Clone() => FromEntries(entries);

        /// <summary>Recria a seleção a partir do progresso salvo. Linhas inválidas são ignoradas.</summary>
        public static PartSelection FromEntries(IEnumerable<PartSelectionEntry> saved)
        {
            var selection = new PartSelection();
            if (saved == null)
                return selection;

            foreach (PartSelectionEntry entry in saved)
            {
                if (entry != null && !string.IsNullOrEmpty(entry.partId) && entry.quantity >= 0)
                    selection.Set(entry.partId, entry.modelId, entry.quantity);
            }
            return selection;
        }

        /// <summary>Kit padrão: primeiro modelo de cada peça, na quantidade padrão.</summary>
        public static PartSelection CreateDefault(IEnumerable<PartDefinition> parts)
        {
            var selection = new PartSelection();
            if (parts == null)
                return selection;

            foreach (PartDefinition part in parts)
            {
                if (part == null || string.IsNullOrEmpty(part.PartId))
                    continue;
                string modelId = part.Models.Count > 0 && part.Models[0] != null ? part.Models[0].ModelId : string.Empty;
                selection.Set(part.PartId, modelId, part.DefaultQuantity);
            }
            return selection;
        }

        private PartSelectionEntry Find(string partId)
        {
            foreach (PartSelectionEntry entry in entries)
            {
                if (entry.partId == partId)
                    return entry;
            }
            return null;
        }
    }
}
