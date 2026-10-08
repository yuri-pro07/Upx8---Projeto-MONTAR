using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace MontAR
{
    /// <summary>
    /// Superfície invisível da demonstração 3D: arrastar com um dedo gira a bancada virtual,
    /// pinça (ou roda do mouse) aproxima, toque duplo volta ao enquadramento da etapa.
    /// </summary>
    public class DemoOrbitInput : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler, IPointerClickHandler
    {
        private const int NoPointer = int.MinValue;

        [SerializeField] private DemoStage stage;
        [SerializeField] private float scrollZoomStep = 1.12f;

        private int dragPointer = NoPointer;
        private float lastPinchDistance = -1f;

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (dragPointer == NoPointer)
                dragPointer = eventData.pointerId;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (stage == null || eventData.pointerId != dragPointer || lastPinchDistance > 0f)
                return;
            stage.Orbit(eventData.delta);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (eventData.pointerId == dragPointer)
                dragPointer = NoPointer;
        }

        public void OnScroll(PointerEventData eventData)
        {
            if (stage == null || Mathf.Approximately(eventData.scrollDelta.y, 0f))
                return;
            stage.Zoom(eventData.scrollDelta.y > 0f ? scrollZoomStep : 1f / scrollZoomStep);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (stage != null && eventData.clickCount >= 2)
                stage.ResetView();
        }

        private void OnDisable()
        {
            dragPointer = NoPointer;
            lastPinchDistance = -1f;
        }

        private void Update()
        {
            Touchscreen touchscreen = Touchscreen.current;
            if (stage == null || touchscreen == null)
                return;

            int count = 0;
            Vector2 first = default;
            Vector2 second = default;
            foreach (var touch in touchscreen.touches)
            {
                if (!touch.press.isPressed)
                    continue;
                if (count == 0)
                    first = touch.position.ReadValue();
                else if (count == 1)
                    second = touch.position.ReadValue();
                count++;
            }

            if (count < 2)
            {
                lastPinchDistance = -1f;
                return;
            }

            float pinchDistance = Vector2.Distance(first, second);
            if (lastPinchDistance > 0f && pinchDistance > 1f)
                stage.Zoom(pinchDistance / lastPinchDistance);
            lastPinchDistance = pinchDistance;
        }
    }
}
