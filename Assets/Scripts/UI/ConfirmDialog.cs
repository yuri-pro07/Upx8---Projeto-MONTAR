using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MontAR
{
    /// <summary>Caixa de diálogo modal com até dois botões (ex.: "Continuar a montagem?").</summary>
    public class ConfirmDialog : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text messageLabel;
        [SerializeField] private Button primaryButton;
        [SerializeField] private TMP_Text primaryLabel;
        [SerializeField] private Button secondaryButton;
        [SerializeField] private TMP_Text secondaryLabel;

        private Action onPrimary;
        private Action onSecondary;

        public bool IsOpen => gameObject.activeSelf;

        private void Awake()
        {
            primaryButton.onClick.AddListener(() => Close(onPrimary));
            secondaryButton.onClick.AddListener(() => Close(onSecondary));
        }

        public void Show(string title, string message, string primary, Action primaryAction, string secondary = null, Action secondaryAction = null)
        {
            titleLabel.text = title;
            messageLabel.text = message;
            primaryLabel.text = primary;
            onPrimary = primaryAction;
            onSecondary = secondaryAction;
            secondaryButton.gameObject.SetActive(!string.IsNullOrEmpty(secondary));
            secondaryLabel.text = secondary ?? string.Empty;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
        }

        /// <summary>Fecha como se o botão secundário tivesse sido tocado (botão voltar do Android).</summary>
        public void Dismiss() => Close(onSecondary);

        private void Close(Action action)
        {
            onPrimary = null;
            onSecondary = null;
            gameObject.SetActive(false);
            action?.Invoke();
        }
    }
}
