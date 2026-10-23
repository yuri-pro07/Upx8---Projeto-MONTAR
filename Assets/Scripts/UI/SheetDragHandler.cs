using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MontAR
{
    /// <summary>
    /// Arrastar o painel da etapa para cima ou para baixo (como um bottom sheet): para cima mostra
    /// a instrução, para baixo esconde. Os botões do painel continuam funcionando, porque um toque
    /// sem arrasto não chega aqui.
    /// </summary>
    public class SheetDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Tooltip("Arrasto vertical mínimo, em unidades do Canvas (referência 1080 x 1920).")]
        [SerializeField] private float threshold = 60f;

        private bool vertical;

        /// <summary>true = arrastou para cima (mostrar), false = para baixo (esconder).</summary>
        public event Action<bool> Swiped;

        public void OnBeginDrag(PointerEventData eventData)
        {
            Vector2 delta = eventData.position - eventData.pressPosition;
            vertical = Mathf.Abs(delta.y) >= Mathf.Abs(delta.x);
        }

        public void OnDrag(PointerEventData eventData)
        {
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!vertical)
                return;
            float dy = eventData.position.y - eventData.pressPosition.y;
            float scale = Mathf.Max(0.01f, transform.lossyScale.y);
            if (Mathf.Abs(dy) / scale >= threshold)
                Swiped?.Invoke(dy > 0f);
        }
    }
}
