using System.Collections;
using UnityEngine;

namespace MontAR
{
    /// <summary>Base das telas do app: mostra e esconde com um fade curto.</summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class UiScreen : MonoBehaviour
    {
        [SerializeField] private float fadeSeconds = 0.15f;

        private CanvasGroup group;
        private Coroutine fade;

        public bool IsVisible => gameObject.activeSelf;

        protected CanvasGroup Group => group != null ? group : group = GetComponent<CanvasGroup>();

        public virtual void Show()
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            if (fade != null)
                StopCoroutine(fade);
            fade = StartCoroutine(FadeIn());
        }

        public virtual void Hide()
        {
            if (fade != null)
                StopCoroutine(fade);
            fade = null;
            Group.alpha = 1f;
            gameObject.SetActive(false);
        }

        private IEnumerator FadeIn()
        {
            CanvasGroup canvasGroup = Group;
            canvasGroup.alpha = 0f;
            for (float t = 0f; t < fadeSeconds; t += Time.unscaledDeltaTime)
            {
                canvasGroup.alpha = t / fadeSeconds;
                yield return null;
            }
            canvasGroup.alpha = 1f;
            fade = null;
        }
    }
}
