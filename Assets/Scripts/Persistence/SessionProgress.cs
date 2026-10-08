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
        public string procedureId;

        /// <summary>Etapas já aprovadas; é o índice da próxima etapa a executar.</summary>
        public int completedSteps;

        public bool finished;

        /// <summary>Kit escolhido na tela de peças. Define quais etapas entram ao retomar.</summary>
        public List<PartSelectionEntry> parts = new List<PartSelectionEntry>();

        /// <summary>Hora da última gravação, em UTC (ISO 8601).</summary>
        public string updatedUtc;
    }
}
