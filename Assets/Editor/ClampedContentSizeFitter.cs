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
    [CustomEditor(typeof(ClampedContentSizeFitter))]
    class CustomClampedContentSizeFitterEditorf: Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            List<string> hiddenFields = new() { };
            List<string> readonlyFields = new() { "m_Script" };

            var fitter = (ClampedContentSizeFitter)target;
            SerializedProperty property = serializedObject.GetIterator();

            var heightProp = serializedObject.FindProperty("m_MaxHeight");
            if (heightProp.floatValue != fitter.maxHeight) {
                fitter.maxHeight = heightProp.floatValue;
            }

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

            serializedObject.ApplyModifiedProperties();
        }
    }
}
