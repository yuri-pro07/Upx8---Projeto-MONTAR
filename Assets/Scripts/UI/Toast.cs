using System.Collections;
using TMPro;
using UnityEngine;

namespace MontAR
{
    /// <summary>Aviso curto no rodapé da tela (ex.: "Etapa concluída", "Intervenção registrada").</summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class Toast : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private float fadeSeconds = 0.2f;

        private CanvasGroup group;
        private Coroutine routine;

        private void Awake()
        {
            group = GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
        }

        public void Show(string message, float seconds = 2.4f)
        {
            if (group == null)
                Awake();
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            label.text = message;
            if (routine != null)
                StopCoroutine(routine);
            routine = StartCoroutine(Run(seconds));
        }

        private IEnumerator Run(float seconds)
        {
            for (float t = 0f; t < fadeSeconds; t += Time.unscaledDeltaTime)
            {
                group.alpha = t / fadeSeconds;
                yield return null;
            }
            group.alpha = 1f;
            yield return new WaitForSecondsRealtime(seconds);
            for (float t = 0f; t < fadeSeconds; t += Time.unscaledDeltaTime)
            {
                group.alpha = 1f - t / fadeSeconds;
                yield return null;
            }
            group.alpha = 0f;
            routine = null;
        }
    }
}
