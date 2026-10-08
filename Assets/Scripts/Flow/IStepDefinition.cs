namespace MontAR
{
    /// <summary>
    /// O mínimo que o <see cref="StepFlow"/> precisa saber de uma etapa.
    /// Separado do ScriptableObject para que o fluxo possa ser testado sem assets.
    /// </summary>
    public interface IStepDefinition
    {
        /// <summary>Identificador estável da etapa, usado no CSV (ex.: "cpu_soquete").</summary>
        string StepId { get; }

        /// <summary>
        /// Nome da imagem de referência que ancora o overlay desta etapa.
        /// Vazio quando a etapa não depende de rastreamento.
        /// </summary>
        string ReferenceImageName { get; }
    }
}
