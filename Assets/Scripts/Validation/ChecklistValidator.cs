using System;
using System.Collections.Generic;
using System.Text;

namespace MontAR
{
    /// <summary>
    /// Validação por checklist: a etapa só é aprovada com todos os itens marcados.
    /// Etapa sem itens é aprovada com uma confirmação simples.
    /// </summary>
    public class ChecklistValidator : IStepValidator
    {
        private readonly IReadOnlyList<string> items;
        private readonly bool[] checkedItems;

        public ChecklistValidator(IReadOnlyList<string> items)
        {
            this.items = items ?? Array.Empty<string>();
            checkedItems = new bool[this.items.Count];
        }

        public int Count => items.Count;

        public string GetItem(int index) => items[index];

        public bool IsChecked(int index) => checkedItems[index];

        public void SetChecked(int index, bool value) => checkedItems[index] = value;

        public bool IsSatisfied
        {
            get
            {
                foreach (bool isChecked in checkedItems)
                {
                    if (!isChecked)
                        return false;
                }
                return true;
            }
        }

        /// <summary>Ex.: "checked=2/3;pending=1" (índices base 0 dos itens pendentes).</summary>
        public string Describe()
        {
            int done = 0;
            var pending = new StringBuilder();
            for (int i = 0; i < checkedItems.Length; i++)
            {
                if (checkedItems[i])
                {
                    done++;
                    continue;
                }
                if (pending.Length > 0)
                    pending.Append('|');
                pending.Append(i);
            }

            string summary = $"checked={done}/{checkedItems.Length}";
            return pending.Length > 0 ? $"{summary};pending={pending}" : summary;
        }
    }
}
