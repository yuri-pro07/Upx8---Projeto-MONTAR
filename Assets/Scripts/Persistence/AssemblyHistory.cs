using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace MontAR
{
    /// <summary>Tempo e reprovações de uma etapa, no histórico. Campos públicos por causa do JsonUtility.</summary>
    [Serializable]
    public class HistoryStepEntry
    {
        public string stepId;
        public string title;
        public double seconds;
        public int failedValidations;
    }

    /// <summary>Uma montagem concluída: o que a tela de conclusão mostrou, guardado no aparelho.</summary>
    [Serializable]
    public class HistoryEntry
    {
        public string sessionId;
        public string participantCode;

        /// <summary>Nome dado à montagem na tela de início (ex.: "Bancada 3 · HP"). Pode ficar vazio.</summary>
        public string label;

        /// <summary>Fim da montagem, em UTC (ISO 8601).</summary>
        public string finishedUtc;

        public string mode;
        public string kit;
        public double elapsedSeconds;
        public int completedSteps;
        public int failedValidations;
        public int observerInterventions;

        /// <summary>A sessão foi retomada depois de o app fechar (os tempos contam só a partir da retomada).</summary>
        public bool resumed;

        public List<HistoryStepEntry> steps = new List<HistoryStepEntry>();

        public string DisplayName => string.IsNullOrEmpty(label) ? "Montagem sem nome" : label;
    }

    [Serializable]
    internal class HistoryFile
    {
        public List<HistoryEntry> entries = new List<HistoryEntry>();
    }

    /// <summary>
    /// Histórico das montagens concluídas neste aparelho (history.json no persistentDataPath),
    /// da mais recente para a mais antiga. Não substitui o CSV da pesquisa: é só para o usuário rever.
    /// </summary>
    public class AssemblyHistoryStore
    {
        public const string FileName = "history.json";

        /// <summary>Montagens guardadas; as mais antigas saem primeiro.</summary>
        public const int MaxEntries = 100;

        /// <summary>Tamanho máximo do nome da montagem.</summary>
        public const int MaxLabelLength = 40;

        private readonly string directory;

        public AssemblyHistoryStore(string directory)
        {
            if (string.IsNullOrEmpty(directory))
                throw new ArgumentException("Diretório inválido.", nameof(directory));
            this.directory = directory;
        }

        public static AssemblyHistoryStore CreateDefault() => new AssemblyHistoryStore(Application.persistentDataPath);

        public string FilePath => Path.Combine(directory, FileName);

        /// <summary>Tira espaços repetidos e das pontas e corta no tamanho máximo.</summary>
        public static string NormalizeLabel(string label)
        {
            if (string.IsNullOrWhiteSpace(label))
                return string.Empty;

            var text = new StringBuilder(label.Length);
            bool space = false;
            foreach (char c in label.Trim())
            {
                if (char.IsWhiteSpace(c))
                {
                    space = true;
                    continue;
                }
                if (char.IsControl(c))
                    continue;
                if (space && text.Length > 0)
                    text.Append(' ');
                space = false;
                text.Append(c);
            }
            string normalized = text.ToString();
            return normalized.Length > MaxLabelLength ? normalized.Substring(0, MaxLabelLength).TrimEnd() : normalized;
        }

        /// <summary>Montagens salvas, da mais recente para a mais antiga. Arquivo ausente ou ilegível = lista vazia.</summary>
        public List<HistoryEntry> Load()
        {
            if (!File.Exists(FilePath))
                return new List<HistoryEntry>();

            try
            {
                HistoryFile file = JsonUtility.FromJson<HistoryFile>(File.ReadAllText(FilePath));
                if (file?.entries == null)
                    return new List<HistoryEntry>();
                file.entries.RemoveAll(e => e == null || string.IsNullOrEmpty(e.sessionId));
                return file.entries;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[MontAR] Histórico ilegível, ignorando: {e.Message}");
                return new List<HistoryEntry>();
            }
        }

        /// <summary>
        /// Guarda a montagem no topo da lista. Uma sessão retomada e concluída de novo substitui
        /// a entrada anterior com o mesmo sessionId.
        /// </summary>
        public void Add(HistoryEntry entry)
        {
            if (entry == null)
                throw new ArgumentNullException(nameof(entry));
            if (string.IsNullOrEmpty(entry.sessionId))
                throw new ArgumentException("A montagem precisa de um sessionId.", nameof(entry));

            List<HistoryEntry> entries = Load();
            entries.RemoveAll(e => e.sessionId == entry.sessionId);
            if (string.IsNullOrEmpty(entry.finishedUtc))
                entry.finishedUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
            entries.Insert(0, entry);
            if (entries.Count > MaxEntries)
                entries.RemoveRange(MaxEntries, entries.Count - MaxEntries);

            Directory.CreateDirectory(directory);
            File.WriteAllText(FilePath, JsonUtility.ToJson(new HistoryFile { entries = entries }, true));
        }

        public void Clear()
        {
            if (File.Exists(FilePath))
                File.Delete(FilePath);
        }

        /// <summary>Monta a entrada a partir do resumo da sessão (os mesmos números da tela de conclusão).</summary>
        public static HistoryEntry CreateEntry(SessionProgress progress, SessionSummary summary, IReadOnlyList<AssemblyStep> steps,
            GuidanceMode mode, string kit)
        {
            if (progress == null)
                throw new ArgumentNullException(nameof(progress));
            if (summary == null)
                throw new ArgumentNullException(nameof(summary));

            var entry = new HistoryEntry
            {
                sessionId = progress.sessionId,
                participantCode = progress.participantCode,
                label = NormalizeLabel(progress.label),
                mode = mode.ToCsvName(),
                kit = kit ?? string.Empty,
                elapsedSeconds = summary.ElapsedSeconds,
                completedSteps = summary.CompletedSteps,
                failedValidations = summary.FailedValidations,
                observerInterventions = summary.ObserverInterventions,
                resumed = summary.StartedAtStep > 0,
            };
            foreach (StepTiming timing in summary.Steps)
            {
                bool known = steps != null && timing.StepIndex >= 0 && timing.StepIndex < steps.Count && steps[timing.StepIndex] != null;
                entry.steps.Add(new HistoryStepEntry
                {
                    stepId = timing.StepId,
                    title = known ? steps[timing.StepIndex].Title : timing.StepId,
                    seconds = timing.Seconds,
                    failedValidations = timing.FailedValidations,
                });
            }
            return entry;
        }
    }
}
