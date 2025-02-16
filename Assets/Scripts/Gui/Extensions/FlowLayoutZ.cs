
using Gui.Extensions;
using UnityEditor;

namespace UnityEngine.UI.Extensions
{
	[AddComponentMenu("Layout/Extensions/Flow Layout Z")]
	public class FlowLayoutZ : LayoutGroup
	{
		public enum Axis { Horizontal = 0, Vertical = 1 }

        [SerializeField] private FlowLayoutZ aloneContainer;
        [SerializeField] private AloneBackgroundZ aloneBackground;

        public Vector2 Spacing = Vector2.zero;

		public Axis Direction = Axis.Horizontal;

		public bool SideSizeFit = true;

        public void ActivateItem(FlowAloneItemZ item)
        {
            if (item.GetComponent<ToggleZ>().Activate)
            {
                _activateTarget = item;
            }
            else
            {
                if(aloneContainer != null) aloneContainer.gameObject.SetActive(false);
                _activateTarget = null;
            }

            CalculateLayoutInputVertical();
            LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
        }

        public void UnActivateItem()
        {
            _activateTarget = null;
        }

        private void layoutAloneItem(float finalAdvancedSide)
        {
            aloneContainer.gameObject.SetActive(true);
            aloneContainer.rectTransform.SetParent(rectTransform);
            aloneContainer.CalculateLayoutInputVertical();
            SetChildAlongAxis(aloneContainer.rectTransform, 0, 0);
            SetChildAlongAxis(aloneContainer.rectTransform, 1, finalAdvancedSide + padding.top);
        }

        public override void CalculateLayoutInputVertical()
        {
            var advanced = 0f;
            var advancedSide = 0f;
            var maxSideSize = 0f;
            var workingSize = rectTransform.sizeDelta.x - padding.horizontal;
            var aloneAdvanced = 0f;
            var aloneSize = 0f;
            var startRecordAloneAdvanced = false;

            for (var i = 0; i < rectChildren.Count; i++) {
                var child = rectChildren[i];

                if (child.GetComponent<FlowAloneItemZ>() == null) continue;

                var stepSize = LayoutUtility.GetPreferredSize(child, 0);
                var stepSizeSide = LayoutUtility.GetPreferredSize(child, 1);

                var willAdvanced = advanced + stepSize + Spacing.x;
                var overflow = willAdvanced > workingSize;
                maxSideSize = Mathf.Max(maxSideSize, stepSizeSide);

                var willAdvancedSide = advancedSide + maxSideSize + Spacing.y;

                var finalAdvanced = overflow ? 0 : advanced;
                var finalAdvancedSide = overflow ? willAdvancedSide : advancedSide;

                if (startRecordAloneAdvanced && overflow && aloneContainer != null)
                {
                    startRecordAloneAdvanced = false;
                    layoutAloneItem(finalAdvancedSide);
                    aloneSize = aloneContainer.rectTransform.sizeDelta.y;
                }

                if (_activateTarget != null && _activateTarget == child.GetComponent<FlowAloneItemZ>())
                {
                    startRecordAloneAdvanced = true;
                    if (aloneBackground)
                    {
                        aloneBackground.AdvancedStart = finalAdvanced;
                    }
                    aloneAdvanced = finalAdvancedSide;
                }

                var applyAloneSize = finalAdvancedSide >= aloneAdvanced ? aloneSize : 0;

                SetChildAlongAxis(child, 0, finalAdvanced + padding.left);
                SetChildAlongAxis(child, 1, finalAdvancedSide + applyAloneSize + padding.top);

                advanced = overflow ? (stepSize + Spacing.x) : willAdvanced;
                advancedSide = overflow ? willAdvancedSide : advancedSide;
                maxSideSize = overflow ? stepSizeSide : maxSideSize;
            }

            advancedSide += maxSideSize;
            if (startRecordAloneAdvanced && aloneContainer != null)
            {
                layoutAloneItem(advancedSide);
                aloneSize = aloneContainer.rectTransform.sizeDelta.y;
            }

            advancedSide += aloneSize + padding.bottom + padding.top;
            rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, advancedSide);

            if (aloneBackground) aloneBackground.GetComponent<AloneBackgroundZ>().CalculateLayoutInputVertical();
        }

        public override void SetLayoutHorizontal()
        {
        }

        public override void SetLayoutVertical()
        {
        }

        private FlowAloneItemZ _activateTarget;
    }
}