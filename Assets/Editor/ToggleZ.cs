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
    [CustomEditor(typeof(ToggleZ))]
    public class ToggleZEditor : Editor
    {
        private SerializedProperty _myValueProp;
        private SerializedProperty _groupProp;

        private void OnEnable()
        {
            _myValueProp = serializedObject.FindProperty("_activate");
            _groupProp = serializedObject.FindProperty("_group");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            List<string> hiddenFields = new() { };
            List<string> readonlyFields = new() { "m_Script" };

            ToggleZ toggleZ = (ToggleZ)target;
            SerializedProperty property = serializedObject.GetIterator();

            var groupProp = serializedObject.FindProperty("Group");
            var activateProp = serializedObject.FindProperty("_activate");

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


            if (activateProp.boolValue != toggleZ.Activate) toggleZ.Activate = activateProp.boolValue;

            if (groupProp.objectReferenceValue != toggleZ.Group) 
            {
                if(toggleZ.Group != null) toggleZ.Group.UnRegister(toggleZ);
                toggleZ.Group = groupProp.objectReferenceValue as ToggleGroupZ;
                if (toggleZ.Group != null) toggleZ.Group.Register(toggleZ);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}