using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using System.Text;
using Gui.Theme.Wrappers;

namespace UnityEngine.UI.Extensions
{
	[AddComponentMenu("Layout/Extensions/Alone Background Z")]
	public class AloneBackgroundZ : LayoutGroup
    {
        public ThemedImage L;
        public ThemedImage R;
        public ThemedImage C1;
        public ThemedImage C2;
        public Vector2 C1Offset;

        public float HeaderWidth;
        public float HeaderHeight;

        public float AdvancedStart;
        public float AdvancedOffset;

        public override void CalculateLayoutInputVertical()
        {
            var containerWidth = rectTransform.rect.width;
            var containerHeight = rectTransform.rect.height;

            var advancedStart = AdvancedStart + AdvancedOffset + 1;

            setW(R, containerWidth - advancedStart - HeaderWidth);
            setH(R, containerHeight);
            setX(R, advancedStart + HeaderWidth);
            setY(R, 0);

            setW(L, advancedStart);
            setH(L, containerHeight);
            setX(L, 0);
            setY(L, 0);

            setX(C2, advancedStart);
            setY(C2, -HeaderHeight);
            setW(C2, HeaderWidth);
            setH(C2, HeaderHeight);

            setX(C1, advancedStart + C1Offset.x);
            setY(C1, 0 + C1Offset.y);
            setW(C1, HeaderWidth - C1Offset.x * 2);
            setH(C1, containerHeight - C1Offset.y);

            var parentRect = transform.parent.GetComponent<RectTransform>();
            setH(rectTransform, parentRect.sizeDelta.y);
        }
        private void setX(Component component, float x)
        {
            SetChildAlongAxis(component.GetComponent<RectTransform>(), 0, x);
        }
        private void setY(Component component, float y)
        {
            SetChildAlongAxis(component.GetComponent<RectTransform>(), 1, y);
        }
        private void setW(Component component, float w)
        {
            var size = component.GetComponent<RectTransform>().sizeDelta;
            component.GetComponent<RectTransform>().sizeDelta = new Vector2(w, size.y);
        }
        private void setH(Component component, float h)
        {
            var size = component.GetComponent<RectTransform>().sizeDelta;
            component.GetComponent<RectTransform>().sizeDelta = new Vector2(size.x, h);
        }

        public override void SetLayoutHorizontal()
        {
        }

        public override void SetLayoutVertical()
        {
        }
    }

}