using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace MontAR
{
    /// <summary>
    /// Superfície invisível atrás da interface do estudo: toque escolhe, arrasto gira,
    /// pinça (ou roda do mouse) aproxima.
    /// </summary>
    public class StudyInput : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler, IPointerClickHandler
    {
        private const int NoPointer = int.MinValue;

        [SerializeField] private float scrollZoomStep = 1.12f;

        private int dragPointer = NoPointer;
        private float lastPinchDistance = -1f;

        public event Action<Vector2> Tapped;
        public event Action<Vector2> Dragged;
        public event Action<float> Zoomed;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!eventData.dragging && lastPinchDistance <= 0f)
                Tapped?.Invoke(eventData.position);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (dragPointer == NoPointer)
                dragPointer = eventData.pointerId;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId == dragPointer && lastPinchDistance <= 0f)
                Dragged?.Invoke(eventData.delta);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (eventData.pointerId == dragPointer)
                dragPointer = NoPointer;
        }

        public void OnScroll(PointerEventData eventData)
        {
            if (!Mathf.Approximately(eventData.scrollDelta.y, 0f))
                Zoomed?.Invoke(eventData.scrollDelta.y > 0f ? scrollZoomStep : 1f / scrollZoomStep);
        }

        private void OnDisable()
        {
            dragPointer = NoPointer;
            lastPinchDistance = -1f;
        }

        private void Update()
        {
            Touchscreen touchscreen = Touchscreen.current;
            if (touchscreen == null)
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
                Zoomed?.Invoke(pinchDistance / lastPinchDistance);
            lastPinchDistance = pinchDistance;
        }
    }
}
