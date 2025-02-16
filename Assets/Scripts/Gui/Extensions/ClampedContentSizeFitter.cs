using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Gui.Extensions
{
    [ExecuteAlways]
    [AddComponentMenu("Layout/Extensions/Clamped Content Size Fitter Z")]

    public class ClampedContentSizeFitter : ContentSizeFitter
    {
        [SerializeField] private float m_MaxHeight = -1;
        [SerializeField] private float m_MaxWidth = -1;
        public float maxHeight { get => m_MaxHeight; set { m_MaxHeight = value; SetDirty(); } }

        private DrivenRectTransformTracker m_Tracker;

        protected override void OnDisable()
        {
            m_Tracker.Clear();
            LayoutRebuilder.MarkLayoutForRebuild(GetComponent<RectTransform>());
        }

        private void HandleSelfFittingAlongAxis(int axis)
        {
            var rectTransform = GetComponent<RectTransform>();
            FitMode fitting = (axis == 0 ? horizontalFit : verticalFit);
            if (fitting == FitMode.Unconstrained)
            {
                m_Tracker.Add(this, rectTransform, DrivenTransformProperties.None);
                return;
            }
            m_Tracker.Add(this, rectTransform, (axis == 0 ? DrivenTransformProperties.SizeDeltaX : DrivenTransformProperties.SizeDeltaY));
            var maxValue = axis == 0 ? m_MaxWidth : m_MaxHeight;
            if (fitting == FitMode.MinSize)
                rectTransform.SetSizeWithCurrentAnchors((RectTransform.Axis)axis, maxValue >= 0 ? Mathf.Min(LayoutUtility.GetMinSize(rectTransform, axis), maxValue) : LayoutUtility.GetMinSize(rectTransform, axis));
            else
                rectTransform.SetSizeWithCurrentAnchors((RectTransform.Axis)axis, maxValue >= 0 ? Mathf.Min(LayoutUtility.GetPreferredSize(rectTransform, axis), maxValue) : LayoutUtility.GetPreferredSize(rectTransform, axis));
        }

        public override void SetLayoutHorizontal()
        {
            m_Tracker.Clear();
            HandleSelfFittingAlongAxis(0);
        }

        public override void SetLayoutVertical()
        {
            HandleSelfFittingAlongAxis(1);
        }
    }

}
