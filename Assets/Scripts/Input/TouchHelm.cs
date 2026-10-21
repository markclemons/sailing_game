using UnityEngine;
using UnityEngine.EventSystems;

namespace Sailing
{
    public class TouchHelm : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public RectTransform knob;
        public float range = 70f;
        public Vector2 Value { get; private set; }

        public void OnPointerDown(PointerEventData eventData) => Drag(eventData);
        public void OnDrag(PointerEventData eventData) => Drag(eventData);

        public void OnPointerUp(PointerEventData eventData)
        {
            Value = Vector2.zero;
            if (knob != null) knob.anchoredPosition = Vector2.zero;
        }

        void Drag(PointerEventData eventData)
        {
            var rect = (RectTransform)transform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, eventData.position, eventData.pressEventCamera, out Vector2 local);
            Vector2 clamped = Vector2.ClampMagnitude(local, range);
            Value = clamped / range;
            if (knob != null) knob.anchoredPosition = clamped;
        }
    }
}
