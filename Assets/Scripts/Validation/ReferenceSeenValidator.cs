using System;

namespace MontAR
{
    /// <summary>
    /// Validação visual: a etapa é aprovada quando a câmera rastreia o marcador esperado
    /// (por exemplo, o marcador do componente certo antes de instalá-lo).
    /// </summary>
    public class ReferenceSeenValidator : IStepValidator
    {
        private readonly Func<string, bool> isTracked;
        private readonly string referenceName;

        public ReferenceSeenValidator(string referenceName, Func<string, bool> isTracked)
        {
            if (string.IsNullOrEmpty(referenceName))
                throw new ArgumentException("A validação visual precisa do nome do marcador.", nameof(referenceName));
            this.referenceName = referenceName;
            this.isTracked = isTracked ?? throw new ArgumentNullException(nameof(isTracked));
        }

        public string ReferenceName => referenceName;

        public bool IsSatisfied => isTracked(referenceName);

        public string Describe() => $"seen={referenceName}";
    }
}
