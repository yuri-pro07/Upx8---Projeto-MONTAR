namespace MontAR
{
    /// <summary>Como a instrução 3D é mostrada durante a montagem.</summary>
    public enum GuidanceMode
    {
        /// <summary>Câmera do celular: a peça fantasma aparece sobre a placa real (condição do experimento).</summary>
        AugmentedReality,

        /// <summary>
        /// Bancada virtual em 3D, sem câmera: mostra como fazer quando o marcador não está à vista
        /// ou o aparelho não tem ARCore. A etapa não espera a câmera achar a referência.
        /// </summary>
        Demo3D
    }

    /// <summary>Nomes do modo usados no CSV.</summary>
    public static class GuidanceModeExtensions
    {
        public static string ToCsvName(this GuidanceMode mode)
        {
            return mode == GuidanceMode.Demo3D ? "demo_3d" : "ar";
        }
    }
}
