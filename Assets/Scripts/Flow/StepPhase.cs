namespace MontAR
{
    /// <summary>Fase da etapa atual dentro do fluxo de montagem.</summary>
    public enum StepPhase
    {
        /// <summary>Aguardando a câmera encontrar a referência da etapa (Aponta).</summary>
        Locating,

        /// <summary>Overlay posicionado; o usuário executa a etapa (Guia).</summary>
        Guiding,

        /// <summary>O usuário pediu para validar a etapa (Valida).</summary>
        Validating,

        /// <summary>Etapa aprovada.</summary>
        Completed
    }
}
