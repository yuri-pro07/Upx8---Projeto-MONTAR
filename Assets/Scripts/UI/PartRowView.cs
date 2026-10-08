using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MontAR
{
    /// <summary>Linha da tela de peças: componente, modelo escolhido e quantidade.</summary>
    public class PartRowView : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private TMP_Text modelLabel;
        [SerializeField] private TMP_Text descriptionLabel;
        [SerializeField] private TMP_Text requirementLabel;
        [SerializeField] private Button previousModelButton;
        [SerializeField] private Button nextModelButton;
        [SerializeField] private Button minusButton;
        [SerializeField] private Button plusButton;
        [SerializeField] private TMP_Text quantityLabel;
        [Tooltip("Grupo dos botões – e +. Fica invisível (sem sair do layout) nas peças de quantidade fixa.")]
        [SerializeField] private CanvasGroup quantityButtons;
        [SerializeField] private CanvasGroup dimmedGroup;

        private PartDefinition part;
        private PartSelection selection;
        private Action changed;

        public void Bind(PartDefinition definition, PartSelection kit, Action onChanged)
        {
            part = definition;
            selection = kit;
            changed = onChanged;
            name = "Peça " + definition.PartId;

            icon.sprite = definition.Icon;
            icon.enabled = definition.Icon != null;
            nameLabel.text = definition.DisplayName;
            descriptionLabel.text = definition.Description;
            descriptionLabel.gameObject.SetActive(!string.IsNullOrEmpty(definition.Description));
            requirementLabel.text = definition.IsRequired ? "obrigatória" : "opcional";

            bool severalModels = definition.Models.Count > 1;
            previousModelButton.gameObject.SetActive(severalModels);
            nextModelButton.gameObject.SetActive(severalModels);

            previousModelButton.onClick.RemoveAllListeners();
            nextModelButton.onClick.RemoveAllListeners();
            minusButton.onClick.RemoveAllListeners();
            plusButton.onClick.RemoveAllListeners();
            previousModelButton.onClick.AddListener(() => CycleModel(-1));
            nextModelButton.onClick.AddListener(() => CycleModel(1));
            minusButton.onClick.AddListener(() => ChangeQuantity(-1));
            plusButton.onClick.AddListener(() => ChangeQuantity(1));

            Refresh();
        }

        private void CycleModel(int direction)
        {
            int count = part.Models.Count;
            if (count < 2)
                return;

            int index = Mathf.Max(0, IndexOfModel(selection.GetModelId(part.PartId)));
            index = (index + direction + count) % count;
            selection.Set(part.PartId, part.Models[index].ModelId, selection.GetQuantity(part.PartId));
            Refresh();
            changed?.Invoke();
        }

        private void ChangeQuantity(int delta)
        {
            int quantity = part.ClampQuantity(selection.GetQuantity(part.PartId) + delta);
            selection.Set(part.PartId, selection.GetModelId(part.PartId), quantity);
            Refresh();
            changed?.Invoke();
        }

        private void Refresh()
        {
            int quantity = selection.GetQuantity(part.PartId);
            PartModel model = part.FindModel(selection.GetModelId(part.PartId));
            modelLabel.text = model != null ? model.DisplayName : "—";
            quantityLabel.text = quantity == 0 ? "não vai" : $"{quantity} {part.UnitFor(quantity)}";

            // Quantidade fixa (ex.: 1 placa-mãe): botões invisíveis, mas no layout (desativá-los encolhe a coluna).
            // Senão, o botão no limite fica apagado.
            bool adjustable = part.MinQuantity != part.MaxQuantity;
            if (quantityButtons != null)
            {
                quantityButtons.alpha = adjustable ? 1f : 0f;
                quantityButtons.blocksRaycasts = adjustable;
                quantityButtons.interactable = adjustable;
            }
            SetEnabled(minusButton, quantity > part.MinQuantity);
            SetEnabled(plusButton, quantity < part.MaxQuantity);
            if (dimmedGroup != null)
                dimmedGroup.alpha = quantity == 0 ? 0.45f : 1f;
        }

        private static void SetEnabled(Button button, bool enabled)
        {
            button.interactable = enabled;
            var glyph = button.GetComponentInChildren<TMP_Text>(true);
            if (glyph != null)
                glyph.alpha = enabled ? 1f : 0.25f;
        }

        private int IndexOfModel(string modelId)
        {
            for (int i = 0; i < part.Models.Count; i++)
            {
                if (part.Models[i].ModelId == modelId)
                    return i;
            }
            return -1;
        }
    }
}
