using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MontAR
{
    /// <summary>
    /// Botão que só dispara se ficar pressionado por um tempo. Usado no botão discreto do
    /// observador, para um toque acidental do participante não virar intervenção no CSV.
    /// Um toque curto dispara <see cref="OnShortPress"/> (ex.: explicar para que serve o botão).
    /// </summary>
    public class LongPressButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] private float holdSeconds = 1f;
        [Tooltip("Imagem preenchida (Filled) que mostra o progresso do toque longo. Opcional.")]
        [SerializeField] private Image progressFill;
        [SerializeField] private UnityEvent onLongPress = new UnityEvent();
        [SerializeField] private UnityEvent onShortPress = new UnityEvent();

        private float pressedAt = -1f;
        private bool fired;

        public UnityEvent OnLongPress => onLongPress;

        /// <summary>Soltou antes do tempo do toque longo (sem sair do botão).</summary>
        public UnityEvent OnShortPress => onShortPress;

        public void OnPointerDown(PointerEventData eventData)
        {
            pressedAt = Time.unscaledTime;
            fired = false;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            bool shortPress = pressedAt >= 0f && !fired;
            Release();
            if (shortPress)
                onShortPress.Invoke();
        }

        public void OnPointerExit(PointerEventData eventData) => Release();

        private void OnDisable() => Release();

        private void Update()
        {
            if (pressedAt < 0f || fired)
                return;

            float progress = (Time.unscaledTime - pressedAt) / Mathf.Max(0.01f, holdSeconds);
            if (progressFill != null)
                progressFill.fillAmount = Mathf.Clamp01(progress);

            if (progress >= 1f)
            {
                fired = true;
                onLongPress.Invoke();
            }
        }

        private void Release()
        {
            pressedAt = -1f;
            if (progressFill != null)
                progressFill.fillAmount = 0f;
        }
    }
}
