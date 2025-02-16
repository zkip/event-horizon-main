using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using System.Text;

namespace UnityEngine.UI.Extensions
{
    [CustomEditor(typeof(FlowLayoutZ))]
    public class CustomLayoutGroupEditor : Editor
    {
        private static int IntField(string label, int value, out bool changed)
        {
            int result;

            GUIStyle labelStyle = EditorStyles.label;
            float originalLabelWidth = EditorGUIUtility.labelWidth;

            EditorGUIUtility.labelWidth = labelStyle.CalcSize(new GUIContent(label)).x + 5;
            EditorGUILayout.Space(-EditorGUIUtility.labelWidth);
            result = EditorGUILayout.IntField(label, value);

            changed = result != value;

            EditorGUIUtility.labelWidth = originalLabelWidth;
            return result;
        }

        public static RectOffset RectOffsetField(string label, RectOffset value, out bool changed)
        {

            float originalLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 40;

            float inspectorWidth = EditorGUIUtility.currentViewWidth;
            var fieldTotalWidth = inspectorWidth - originalLabelWidth;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(originalLabelWidth));
            EditorGUILayout.LabelField(" ", GUILayout.Width(fieldTotalWidth / 3));
            value.top = IntField("top", value.top, out var changed1);
            EditorGUILayout.LabelField(" ", GUILayout.Width(fieldTotalWidth / 3));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(" ", GUILayout.Width(originalLabelWidth));
            value.left = IntField("left", value.left, out var changed2);
            EditorGUILayout.LabelField(" ", GUILayout.Width(fieldTotalWidth / 3));
            value.right = IntField("right", value.right, out var changed3);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(" ", GUILayout.Width(originalLabelWidth));
            EditorGUILayout.LabelField(" ", GUILayout.Width(fieldTotalWidth / 3));
            value.bottom = IntField("bottom", value.bottom, out var changed4);
            EditorGUILayout.LabelField(" ", GUILayout.Width(fieldTotalWidth / 3));
            EditorGUILayout.EndHorizontal();

            changed = changed1 || changed2 || changed3 || changed4;

            EditorGUIUtility.labelWidth = originalLabelWidth;

            return value;
        }
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            List<string> hiddenFields = new() { "m_ChildAlignment", "m_Padding" };
            List<string> readonlyFields = new() { "m_Script" };

            SerializedProperty property = serializedObject.GetIterator();

            bool enterChildren = true;
            while (property.NextVisible(enterChildren))
            {
                enterChildren = false;

                if (hiddenFields.Contains(property.name)) continue;

                if (readonlyFields.Contains(property.name))
                {
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.PropertyField(property);
                    EditorGUI.EndDisabledGroup();
                    continue;
                }

                EditorGUILayout.PropertyField(property, true);
            }


            FlowLayoutZ layoutZ = (FlowLayoutZ)target;

            layoutZ.padding = RectOffsetField("Padding", layoutZ.padding, out var changed);
            if (changed)
                layoutZ.CalculateLayoutInputVertical();

            serializedObject.ApplyModifiedProperties();
        }
    }
}