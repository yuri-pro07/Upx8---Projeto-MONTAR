using System;
using System.Collections.Generic;
using UnityEngine;

namespace MontAR
{
    /// <summary>Um modelo de peça que o roteiro sabe guiar (ex.: "Intel, soquete LGA1700").</summary>
    [Serializable]
    public class PartModel
    {
        [Tooltip("Identificador estável, gravado no CSV e no progresso salvo.")]
        [SerializeField] private string modelId = "";
        [SerializeField] private string displayName = "";
        [TextArea(1, 3)]
        [SerializeField] private string detail = "";

        public PartModel()
        {
        }

        public PartModel(string modelId, string displayName, string detail = "")
        {
            this.modelId = modelId ?? string.Empty;
            this.displayName = displayName ?? string.Empty;
            this.detail = detail ?? string.Empty;
        }

        public string ModelId => modelId;
        public string DisplayName => displayName;
        public string Detail => detail;
    }

    /// <summary>
    /// Um componente do PC na tela de seleção de peças: os modelos disponíveis e
    /// quantas unidades o roteiro aceita.
    /// </summary>
    [Serializable]
    public class PartDefinition
    {
        [Tooltip("Identificador estável (ex.: \"ram\"). As etapas apontam para ele em Required Part Id.")]
        [SerializeField] private string partId = "";
        [SerializeField] private string displayName = "";
        [TextArea(2, 4)]
        [SerializeField] private string description = "";
        [SerializeField] private Sprite icon;
        [Tooltip("Unidade mostrada ao lado da quantidade (ex.: pente / pentes).")]
        [SerializeField] private string unitSingular = "unidade";
        [SerializeField] private string unitPlural = "unidades";
        [Tooltip("0 = peça opcional (pode ficar de fora da montagem).")]
        [Min(0)]
        [SerializeField] private int minQuantity = 1;
        [Min(0)]
        [SerializeField] private int maxQuantity = 1;
        [Min(0)]
        [SerializeField] private int defaultQuantity = 1;
        [Tooltip("Por enquanto um modelo por peça; novos modelos entram aqui.")]
        [SerializeField] private List<PartModel> models = new List<PartModel>();

        public PartDefinition()
        {
        }

        public PartDefinition(string partId, string displayName, int minQuantity, int maxQuantity, int defaultQuantity, params PartModel[] models)
        {
            this.partId = partId ?? string.Empty;
            this.displayName = displayName ?? string.Empty;
            this.minQuantity = minQuantity;
            this.maxQuantity = maxQuantity;
            this.defaultQuantity = defaultQuantity;
            this.models = new List<PartModel>(models ?? Array.Empty<PartModel>());
        }

        public string PartId => partId;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public int MinQuantity => minQuantity;
        public int MaxQuantity => Mathf.Max(minQuantity, maxQuantity);
        public int DefaultQuantity => ClampQuantity(defaultQuantity);
        public IReadOnlyList<PartModel> Models => models;

        /// <summary>Peça obrigatória: a montagem não faz sentido sem ela.</summary>
        public bool IsRequired => minQuantity > 0;

        public int ClampQuantity(int quantity) => Mathf.Clamp(quantity, minQuantity, MaxQuantity);

        public string UnitFor(int quantity) => quantity == 1 ? unitSingular : unitPlural;

        public PartModel FindModel(string modelId)
        {
            foreach (PartModel model in models)
            {
                if (model != null && model.ModelId == modelId)
                    return model;
            }
            return null;
        }
    }

    /// <summary>
    /// Catálogo de peças que o app sabe montar. A tela de seleção mostra estas peças, e a
    /// escolha (modelo e quantidade) decide quais etapas do roteiro entram na sessão.
    /// </summary>
    [CreateAssetMenu(menuName = "MontAR/Catálogo de peças", fileName = "Catalogo_pecas")]
    public class PartCatalog : ScriptableObject
    {
        [SerializeField] private List<PartDefinition> parts = new List<PartDefinition>();

        public IReadOnlyList<PartDefinition> Parts => parts;

        public PartDefinition Find(string partId)
        {
            foreach (PartDefinition part in parts)
            {
                if (part != null && part.PartId == partId)
                    return part;
            }
            return null;
        }

        /// <summary>Kit padrão: primeiro modelo de cada peça, na quantidade padrão.</summary>
        public PartSelection CreateDefaultSelection() => PartSelection.CreateDefault(parts);

        private void OnValidate()
        {
            var seen = new HashSet<string>();
            foreach (PartDefinition part in parts)
            {
                if (part == null)
                    continue;
                if (string.IsNullOrEmpty(part.PartId))
                    Debug.LogWarning($"[MontAR] Catálogo '{name}': a peça '{part.DisplayName}' está sem PartId.", this);
                else if (!seen.Add(part.PartId))
                    Debug.LogWarning($"[MontAR] Catálogo '{name}': PartId repetido '{part.PartId}'.", this);
                if (part.Models.Count == 0)
                    Debug.LogWarning($"[MontAR] Catálogo '{name}': a peça '{part.PartId}' não tem nenhum modelo.", this);
            }
        }
    }
}
