using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MontAR
{
    /// <summary>Item do checklist de validação: caixa de marcar + texto.</summary>
    public class ChecklistItemView : MonoBehaviour
    {
        [SerializeField] private Toggle toggle;
        [SerializeField] private TMP_Text label;
        [Tooltip("Caixa verde com o check, ligada só quando o item está marcado.")]
        [SerializeField] private GameObject checkMark;

        public void Bind(string text, bool isChecked, Action<bool> onChanged)
        {
            label.text = text;
            toggle.onValueChanged.RemoveAllListeners();
            toggle.SetIsOnWithoutNotify(isChecked);
            checkMark.SetActive(isChecked);
            toggle.onValueChanged.AddListener(value =>
            {
                checkMark.SetActive(value);
                onChanged?.Invoke(value);
            });
        }
    }
}
