using UnityEngine;

namespace MontAR
{
    /// <summary>Ajusta o RectTransform à área segura da tela (entalhe da câmera, barra de gestos).</summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaFitter : MonoBehaviour
    {
        private Rect appliedArea;
        private Vector2Int appliedScreen;

        private void OnEnable()
        {
            Apply();
        }

        private void Update()
        {
            if (Screen.safeArea != appliedArea || Screen.width != appliedScreen.x || Screen.height != appliedScreen.y)
                Apply();
        }

        private void Apply()
        {
            appliedArea = Screen.safeArea;
            appliedScreen = new Vector2Int(Screen.width, Screen.height);
            if (Screen.width <= 0 || Screen.height <= 0)
                return;

            var rect = (RectTransform)transform;
            var size = new Vector2(Screen.width, Screen.height);
            rect.anchorMin = appliedArea.position / size;
            rect.anchorMax = (appliedArea.position + appliedArea.size) / size;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
