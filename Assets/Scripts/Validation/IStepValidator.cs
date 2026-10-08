namespace MontAR
{
    /// <summary>
    /// Decide se a etapa atual pode ser aprovada. O resultado vai para
    /// <see cref="StepFlow.SubmitValidation"/> junto com <see cref="Describe"/>.
    /// </summary>
    public interface IStepValidator
    {
        bool IsSatisfied { get; }

        /// <summary>Resumo curto para a coluna "detail" do CSV.</summary>
        string Describe();
    }
}
