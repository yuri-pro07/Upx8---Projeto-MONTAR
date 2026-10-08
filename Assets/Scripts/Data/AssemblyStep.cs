using System.Collections.Generic;
using UnityEngine;

namespace MontAR
{
    /// <summary>Como a etapa é confirmada antes de liberar a próxima.</summary>
    public enum StepValidationType
    {
        /// <summary>O usuário marca todos os itens de verificação da etapa.</summary>
        Checklist,

        /// <summary>A câmera precisa ver o marcador esperado (ex.: o do componente certo).</summary>
        ReferenceSeen
    }

    /// <summary>
    /// Uma etapa do roteiro de montagem, editável no Inspector sem mexer em código.
    /// </summary>
    [CreateAssetMenu(menuName = "MontAR/Etapa de montagem", fileName = "Etapa")]
    public class AssemblyStep : ScriptableObject, IStepDefinition, IPartRequirement
    {
        [Header("Identificação")]
        [Tooltip("Identificador estável usado no CSV. Não mudar depois que a coleta começar.")]
        [SerializeField] private string stepId = "";
        [SerializeField] private string title = "";
        [SerializeField] private string componentName = "";

        [Header("Peça (decide se a etapa entra no roteiro do kit escolhido)")]
        [Tooltip("PartId do catálogo de peças. Vazio = a etapa sempre entra (ex.: verificação final).")]
        [SerializeField] private string requiredPartId = "";
        [Tooltip("Modelo exigido. Vazio = qualquer modelo da peça.")]
        [SerializeField] private string requiredModelId = "";
        [Tooltip("Quantidade mínima da peça para a etapa entrar (ex.: 2 para \"RAM nos slots A2 e B2\").")]
        [Min(1)]
        [SerializeField] private int minPartQuantity = 1;
        [Tooltip("Quantidade máxima. 0 = sem limite.")]
        [Min(0)]
        [SerializeField] private int maxPartQuantity = 0;

        [Header("Instrução")]
        [TextArea(3, 8)]
        [SerializeField] private string instruction = "";
        [Tooltip("Alerta do erro mais comum nesta etapa (ex.: pinos tortos, conector trocado).")]
        [TextArea(2, 5)]
        [SerializeField] private string commonErrorAlert = "";

        [Header("Realidade aumentada")]
        [Tooltip("Nome da imagem na XRReferenceImageLibrary que ancora o overlay. Vazio = etapa sem rastreamento.")]
        [SerializeField] private string referenceImageName = "";
        [SerializeField] private GameObject overlayPrefab;
        [Tooltip("Posição do overlay em metros, relativa ao centro da imagem de referência.")]
        [SerializeField] private Vector3 overlayPositionOffset = Vector3.zero;
        [Tooltip("Rotação do overlay em graus, relativa à imagem de referência.")]
        [SerializeField] private Vector3 overlayRotationOffset = Vector3.zero;

        [Header("Validação")]
        [SerializeField] private StepValidationType validationType = StepValidationType.Checklist;
        [SerializeField] private List<string> checklistItems = new List<string>();
        [Tooltip("Usado quando a validação é ReferenceSeen: nome do marcador que precisa ser visto.")]
        [SerializeField] private string validationReferenceName = "";

        public string StepId => stepId;
        public string Title => title;
        public string ComponentName => componentName;
        public string Instruction => instruction;
        public string CommonErrorAlert => commonErrorAlert;
        public string ReferenceImageName => referenceImageName;
        public GameObject OverlayPrefab => overlayPrefab;
        public Vector3 OverlayPositionOffset => overlayPositionOffset;
        public Quaternion OverlayRotationOffset => Quaternion.Euler(overlayRotationOffset);
        public StepValidationType ValidationType => validationType;
        public IReadOnlyList<string> ChecklistItems => checklistItems;
        public string ValidationReferenceName => validationReferenceName;
        public string RequiredPartId => requiredPartId;
        public string RequiredModelId => requiredModelId;
        public int MinPartQuantity => minPartQuantity;
        public int MaxPartQuantity => maxPartQuantity;
    }
}
