using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using System.Text;
using Gui.Theme.Wrappers;
using UnityEngine.EventSystems;

namespace UnityEngine.UI.Extensions
{
    [ExecuteAlways]
    [AddComponentMenu("Layout/Extensions/TextFitter Z")]
    public class TextFitterZ : UIBehaviour
    {
        private void Update()
        {
            var text = gameObject.GetComponent<Text>();
            if (text != null)
            {
                float preferredHeight = text.preferredHeight;

                var size = gameObject.GetComponent<RectTransform>().sizeDelta;
                gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(size.x, preferredHeight);

                var parent = gameObject.transform.parent;
                var parentBoundaryZ = parent.GetComponent<BoundaryZ>();
                if (parentBoundaryZ!=null)
                {
                    parentBoundaryZ.Refresh(true);
                }
            }

        }

    }
}