
using System;
using System.Collections.Generic;
using UnityEditor;

namespace UnityEngine.UI.Extensions
{
    public enum FlexDirection { Horizontal = 0, Vertical = 1 }
    public enum FlexJustContent { Start = 0, Center = 1, End = 2 }
    public enum FlexAlignItems { Start = 0, Center = 1, End = 2 }

    [AddComponentMenu("Layout/Extensions/Flex Layout Z")]
    public class FlexLayoutZ : LayoutGroup
    {
        public enum Axis { Main = 0, Side = 1 }

        public Vector2 Spacing = Vector2.zero;

        public FlexDirection Direction = FlexDirection.Horizontal;
        public FlexJustContent JustContent = FlexJustContent.Start;
        public FlexAlignItems AlignItems = FlexAlignItems.Start;

        public bool SideSizeFit = true;
        public bool IgnoreWrapSpacing = true;

        private float getSize(Axis axis, RectTransform rectTransform)
        {
            switch ((axis, Direction))
            {
                case (Axis.Main, FlexDirection.Horizontal):
                case (Axis.Side, FlexDirection.Vertical):
                    return rectTransform.rect.width;

                case (Axis.Main, FlexDirection.Vertical):
                case (Axis.Side, FlexDirection.Horizontal):
                    return rectTransform.rect.height;

                default: throw new ArgumentException();
            }
        }
        private void setSize(Axis axis, RectTransform rectTransform, float size)
        {
            switch ((axis, Direction))
            {
                case (Axis.Main, FlexDirection.Horizontal):
                case (Axis.Side, FlexDirection.Vertical):
                    rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size); break;

                case (Axis.Main, FlexDirection.Vertical):
                case (Axis.Side, FlexDirection.Horizontal):
                    rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size); break;

                default: throw new ArgumentException();
            }
        }
        private void setChildAlongSize(Axis axis, RectTransform rectTransform, float size)
        {
            switch ((axis, Direction))
            {
                case (Axis.Main, FlexDirection.Horizontal):
                case (Axis.Side, FlexDirection.Vertical):
                    SetChildAlongAxis(rectTransform, 0, size); break;

                case (Axis.Main, FlexDirection.Vertical):
                case (Axis.Side, FlexDirection.Horizontal):
                    SetChildAlongAxis(rectTransform, 1, size); break;

                default: throw new ArgumentException();
            }
        }
        private (float, float, float) getSpacingAndPadding(Axis axis)
        {
            switch ((axis, Direction))
            {
                case (Axis.Main, FlexDirection.Horizontal):
                case (Axis.Side, FlexDirection.Vertical):
                    return (padding.left, padding.right, Spacing.x);

                case (Axis.Main, FlexDirection.Vertical):
                case (Axis.Side, FlexDirection.Horizontal):
                    return (padding.top, padding.bottom, Spacing.y);

                default: throw new ArgumentException();
            }
        }

        private float getTrackerOffset(float remains)
        {
            switch (JustContent)
            {
                case FlexJustContent.Start: return remains * 0;
                case FlexJustContent.Center: return remains * 0.5f;
                case FlexJustContent.End: return remains * 1;

                default: throw new ArgumentException();
            }
        }
        private float getItemOffsetSide(RectTransform rectTransform, float trackerSizeSide)
        {
            var remains = trackerSizeSide - getSize(Axis.Side, rectTransform);
            switch (AlignItems)
            {
                case FlexAlignItems.Start: return remains * 0;
                case FlexAlignItems.Center: return remains * 0.5f;
                case FlexAlignItems.End: return remains * 1;

                default: throw new ArgumentException();
            }
        }

        public override void CalculateLayoutInputVertical()
        {
            var advanced = 0f;
            var advancedSide = 0f;
            var maxSideSize = 0f;

            var (paddingLow, paddingHigh, spacing) = getSpacingAndPadding(Axis.Main);
            var (paddingLowSide, paddingHighSide, spacingSide) = getSpacingAndPadding(Axis.Side);

            var workingSize = getSize(Axis.Main, rectTransform) - (paddingLow + paddingHigh);

            var placeCount = 0;
            var sideCount = 0;

            var skipFirst = true;

            for (var i = 0; i < rectChildren.Count; i++)
            {
                var child = rectChildren[i];
                var layoutElement = child.GetComponent<LayoutElement>();
                if (layoutElement != null && layoutElement.ignoreLayout) continue;

                //var stepSize = LayoutUtility.GetPreferredSize(child, 0);
                //var stepSizeSide = LayoutUtility.GetPreferredSize(child, 1);
                var childRectTransform = child.GetComponent<RectTransform>();

                var stepSize = getSize(Axis.Main, childRectTransform);
                var stepSizeSide = getSize(Axis.Side, childRectTransform);

                var overflow = advanced + stepSize + (IgnoreWrapSpacing ? 0 : spacing) > workingSize;

                if (!overflow) maxSideSize = Mathf.Max(maxSideSize, stepSizeSide);

                var isTail = i == rectChildren.Count - 1;
                var isSkip = skipFirst && i > 0;

                placeCount++;
                if ((overflow || isTail) && isSkip)
                {
                    var trackerSize = advanced - spacing + (isTail && !overflow ? stepSize + spacing : 0);
                    var remains = workingSize - trackerSize;
                    var trackerOffset = getTrackerOffset(remains);
                    //var trackerOffset = 0;
                    //var remains = 0;

                    var subAdvanced = advanced;
                    for (var placeIndex = placeCount - 1 - 1; placeIndex >= 0; placeIndex--)
                    {
                        var childIndex = placeIndex + sideCount;
                        var subChildRectTransform = rectChildren[childIndex].GetComponent<RectTransform>();

                        var itemOffsetSide = getItemOffsetSide(rectChildren[childIndex], maxSideSize);
                        var subStepSize = getSize(Axis.Main, subChildRectTransform);
                        subAdvanced -= (subStepSize + spacing);
                        setChildAlongSize(Axis.Main, rectChildren[childIndex], subAdvanced + trackerOffset + paddingLow);
                        setChildAlongSize(Axis.Side, rectChildren[childIndex], advancedSide + itemOffsetSide + paddingLowSide);
                    }

                    var wrapChildRectTransform = rectChildren[sideCount + placeCount - 1].GetComponent<RectTransform>();
                    var firstStepSize = getSize(Axis.Main, wrapChildRectTransform);
                    var firstStepSideSide = getSize(Axis.Side, wrapChildRectTransform);

                    sideCount += placeCount - 1;
                    placeCount = 0 + 1;

                    if (isTail)
                    {
                        var tailAdvance = overflow ? 0 : advanced;
                        var tailTrackerOffset = getTrackerOffset(overflow ? workingSize - stepSize : remains);
                        var tailAdvanceSide = overflow ? advancedSide + maxSideSize + spacingSide : advancedSide;

                        var itemOffsetSide = overflow ? 0 : getItemOffsetSide(rectChildren[sideCount], maxSideSize);

                        setChildAlongSize(Axis.Main, rectChildren[sideCount], tailAdvance + tailTrackerOffset + paddingLow);
                        setChildAlongSize(Axis.Side, rectChildren[sideCount], tailAdvanceSide + itemOffsetSide + paddingLowSide);
                    }

                    advancedSide += maxSideSize + (isTail && overflow ? stepSizeSide + spacingSide : 0) + spacingSide;
                    maxSideSize = firstStepSideSide;
                    advanced = firstStepSize + spacing;
                }
                else
                {
                    advanced += (stepSize + spacing);
                }

            }

            advancedSide += paddingLowSide + paddingHighSide - spacingSide;
            if (SideSizeFit) setSize(Axis.Side, rectTransform, advancedSide);

        }
        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();
        }
        public override void SetLayoutHorizontal()
        {
        }

        public override void SetLayoutVertical()
        {
        }
    }

}