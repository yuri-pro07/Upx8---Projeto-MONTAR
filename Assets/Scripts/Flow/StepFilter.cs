using System.Collections.Generic;

namespace MontAR
{
    /// <summary>De qual peça (e de quantas unidades) uma etapa depende para entrar no roteiro.</summary>
    public interface IPartRequirement
    {
        /// <summary>Peça exigida (ex.: "gpu"). Vazio = a etapa sempre entra.</summary>
        string RequiredPartId { get; }

        /// <summary>Modelo exigido. Vazio = qualquer modelo da peça.</summary>
        string RequiredModelId { get; }

        /// <summary>Quantidade mínima da peça (valores abaixo de 1 contam como 1).</summary>
        int MinPartQuantity { get; }

        /// <summary>Quantidade máxima da peça. 0 = sem limite.</summary>
        int MaxPartQuantity { get; }
    }

    /// <summary>
    /// Monta o roteiro da sessão a partir do kit escolhido: etapas de peças que ficaram
    /// de fora (ou com outra quantidade, como 1 ou 2 pentes de RAM) não entram.
    /// </summary>
    public static class StepFilter
    {
        public static bool Applies(IPartRequirement requirement, PartSelection selection)
        {
            if (requirement == null)
                return false;
            if (string.IsNullOrEmpty(requirement.RequiredPartId))
                return true;
            if (selection == null)
                return false;

            int quantity = selection.GetQuantity(requirement.RequiredPartId);
            int min = requirement.MinPartQuantity < 1 ? 1 : requirement.MinPartQuantity;
            if (quantity < min)
                return false;
            if (requirement.MaxPartQuantity > 0 && quantity > requirement.MaxPartQuantity)
                return false;

            return string.IsNullOrEmpty(requirement.RequiredModelId)
                || requirement.RequiredModelId == selection.GetModelId(requirement.RequiredPartId);
        }

        /// <summary>Etapas que valem para o kit, na ordem original do roteiro.</summary>
        public static List<T> Filter<T>(IEnumerable<T> steps, PartSelection selection) where T : IPartRequirement
        {
            var result = new List<T>();
            if (steps == null)
                return result;

            foreach (T step in steps)
            {
                if (step != null && Applies(step, selection))
                    result.Add(step);
            }
            return result;
        }
    }
}
