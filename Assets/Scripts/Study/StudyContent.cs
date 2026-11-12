using System;
using System.Collections.Generic;
using UnityEngine;

namespace MontAR
{
    /// <summary>Um bloco do "Mais detalhes" de uma peça.</summary>
    [Serializable]
    public class StudyDetail
    {
        public string title;
        public string text;
    }

    /// <summary>O que o Estudo do PC ensina sobre uma peça. Campos públicos por causa do JsonUtility.</summary>
    [Serializable]
    public class StudyPartInfo
    {
        public string id;
        public string name;
        public string shortName;

        /// <summary>Posição na lista e nos pinos, na ordem da montagem.</summary>
        public int sequence;

        /// <summary>Em que ponto da montagem a peça entra (ex.: "Etapa 2 de 9 da montagem").</summary>
        public string orderLabel;

        public string tagline;
        public string whatItDoes;
        public string howItFits;
        public List<StudyDetail> details = new List<StudyDetail>();
    }

    /// <summary>Textos do Estudo do PC, lidos de Assets/Data/Estudo/estudo_pc.json (editável sem mexer no código).</summary>
    [Serializable]
    public class StudyContent
    {
        public List<StudyPartInfo> parts = new List<StudyPartInfo>();

        public static StudyContent Parse(string json)
        {
            StudyContent content = JsonUtility.FromJson<StudyContent>(json) ?? new StudyContent();
            content.parts.Sort((a, b) => a.sequence.CompareTo(b.sequence));
            return content;
        }

        public StudyPartInfo Find(string id) => parts.Find(part => part.id == id);

        /// <summary>O que falta ou está repetido no conteúdo (lista vazia quando está tudo certo).</summary>
        public List<string> Validate()
        {
            var problems = new List<string>();
            var ids = new HashSet<string>();
            var sequences = new HashSet<int>();
            foreach (StudyPartInfo part in parts)
            {
                string label = string.IsNullOrEmpty(part.id) ? "(sem id)" : part.id;
                if (!ids.Add(part.id))
                    problems.Add($"{label}: id repetido");
                if (!sequences.Add(part.sequence))
                    problems.Add($"{label}: sequence repetida ({part.sequence})");
                foreach ((string field, string value) in new[]
                {
                    ("id", part.id), ("name", part.name), ("shortName", part.shortName), ("orderLabel", part.orderLabel),
                    ("tagline", part.tagline), ("whatItDoes", part.whatItDoes), ("howItFits", part.howItFits)
                })
                {
                    if (string.IsNullOrWhiteSpace(value))
                        problems.Add($"{label}: falta {field}");
                }

                if (part.details == null || part.details.Count == 0)
                    problems.Add($"{label}: falta details");
                else if (part.details.Exists(d => string.IsNullOrWhiteSpace(d.title) || string.IsNullOrWhiteSpace(d.text)))
                    problems.Add($"{label}: detalhe sem título ou texto");
            }
            return problems;
        }
    }
}
