using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using System.Text;
using UnityEngine.EventSystems;

namespace UnityEngine.UI.Extensions
{
    [ExecuteAlways]
    [AddComponentMenu("Layout/Extensions/Boundary Z")]
    public class BoundaryZ : UIBehaviour
    {
        public void Refresh(bool bubbling = false)
        {
            var changed = false;
            for (var i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                changed = child.hasChanged || changed;
            }

            if (changed)
            {
                for (var i = 0; i < transform.childCount; i++)
                {
                    var child = transform.GetChild(i);
                    changed = child.hasChanged || changed;
                }

                var step = 0f;
                var max = 0f;
                for (var i = 0; i < transform.childCount; i++)
                {
                    var child = transform.GetChild(i);
                    if (!child.gameObject.activeSelf) continue;
                    var layoutElement = gameObject.GetComponent<LayoutElement>();
                    if (layoutElement != null && layoutElement.ignoreLayout) continue;

                    var rectTransform = child.GetComponent<RectTransform>();
                    var y = rectTransform.localPosition.y;
                    var h = rectTransform.sizeDelta.y;

                    max = Mathf.Max(max, Mathf.Abs(y)+h*rectTransform.pivot.y);
                }

                var hGroup = gameObject.GetComponent<HorizontalLayoutGroup>();
                var vGroup = gameObject.GetComponent<VerticalLayoutGroup>();
                var rectTransformSelf = gameObject.GetComponent<RectTransform>();

                if (hGroup!=null) step += hGroup.padding.bottom;
                if (vGroup!=null) step += vGroup.padding.bottom;

                gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(rectTransformSelf.sizeDelta.x, max + step);
            }

            if (bubbling)
            {
                var parent = transform.parent;
                var parentBoundaryZ = parent.GetComponent<BoundaryZ>();
                if (parentBoundaryZ!=null)
                {
                    parentBoundaryZ.Refresh(true);
                    var vLayoutGroup= parent.GetComponent<VerticalLayoutGroup>();
                    if (vLayoutGroup!=null)
                    {
                        vLayoutGroup.SetLayoutVertical();
                    }
                }
            }
        }
        private void Update()
        {
            Refresh(false);
        }
    }
}