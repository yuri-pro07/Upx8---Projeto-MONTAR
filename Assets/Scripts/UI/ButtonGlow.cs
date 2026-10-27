using UnityEngine;
using UnityEngine.UI;

namespace MontAR
{
    /// <summary>Brilho colorido atrás do botão principal: some quando o botão está desabilitado.</summary>
    public class ButtonGlow : MonoBehaviour
    {
        [SerializeField] private Selectable selectable;
        [SerializeField] private Graphic glow;
        [SerializeField] private float alpha = 0.5f;

        private void LateUpdate()
        {
            if (selectable == null || glow == null)
                return;
            float target = selectable.IsInteractable() ? alpha : 0f;
            Color color = glow.color;
            if (Mathf.Approximately(color.a, target))
                return;
            color.a = target;
            glow.color = color;
        }
    }
}
