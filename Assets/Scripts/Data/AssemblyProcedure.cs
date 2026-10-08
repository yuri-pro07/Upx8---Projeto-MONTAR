using System.Collections.Generic;
using UnityEngine;

namespace MontAR
{
    /// <summary>
    /// Roteiro de montagem: a lista ordenada de etapas que o app vai guiar.
    /// O manual e o vídeo das condições de controle seguem este mesmo roteiro.
    /// </summary>
    [CreateAssetMenu(menuName = "MontAR/Roteiro de montagem", fileName = "Roteiro")]
    public class AssemblyProcedure : ScriptableObject
    {
        [Tooltip("Identificador do roteiro, salvo junto com o progresso.")]
        [SerializeField] private string procedureId = "";
        [SerializeField] private string title = "";
        [SerializeField] private List<AssemblyStep> steps = new List<AssemblyStep>();

        public string ProcedureId => procedureId;
        public string Title => title;
        public IReadOnlyList<AssemblyStep> Steps => steps;

        private void OnValidate()
        {
            var seen = new HashSet<string>();
            for (int i = 0; i < steps.Count; i++)
            {
                AssemblyStep step = steps[i];
                if (step == null)
                {
                    Debug.LogWarning($"[MontAR] Roteiro '{name}': a posição {i} está vazia.", this);
                    continue;
                }
                if (string.IsNullOrEmpty(step.StepId))
                    Debug.LogWarning($"[MontAR] Roteiro '{name}': a etapa '{step.name}' está sem StepId.", this);
                else if (!seen.Add(step.StepId))
                    Debug.LogWarning($"[MontAR] Roteiro '{name}': StepId repetido '{step.StepId}'.", this);
            }
        }
    }
}
