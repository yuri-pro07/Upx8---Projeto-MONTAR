using System;
using System.Collections.Generic;

namespace MontAR
{
    /// <summary>
    /// Progresso salvo da montagem, para retomar de onde parou (Registra).
    /// Campos públicos porque o JsonUtility só serializa campos.
    /// </summary>
    [Serializable]
    public class SessionProgress
    {
        public string sessionId;
        public string participantCode;

        /// <summary>Nome opcional da montagem (ex.: "Bancada 3 · HP"), mostrado no histórico. Não vai para o CSV.</summary>
        public string label;

        public string procedureId;

        /// <summary>Etapas já aprovadas, puladas ou simuladas; é o índice da próxima etapa a executar.</summary>
        public int completedSteps;

        /// <summary>Índices das etapas puladas sem validar (teste sem todas as peças).</summary>
        public List<int> skippedSteps = new List<int>();

        /// <summary>Índices das etapas concluídas como simulação (teste sem todas as peças).</summary>
        public List<int> simulatedSteps = new List<int>();

        public int SkippedCount => skippedSteps != null ? skippedSteps.Count : 0;

        public int SimulatedCount => simulatedSteps != null ? simulatedSteps.Count : 0;

        public bool finished;

        /// <summary>Kit escolhido na tela de peças. Define quais etapas entram ao retomar.</summary>
        public List<PartSelectionEntry> parts = new List<PartSelectionEntry>();

        /// <summary>Hora da última gravação, em UTC (ISO 8601).</summary>
        public string updatedUtc;
    }
}
