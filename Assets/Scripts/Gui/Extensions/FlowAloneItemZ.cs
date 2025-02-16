using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Text;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using Gui.Extensions;

namespace UnityEngine.UI.Extensions
{

    [RequireComponent(typeof(Toggle))]
    [AddComponentMenu("Layout/Extensions/Flow Alone Item Z")]
    public class FlowAloneItemZ : UIBehaviour
    {
        protected override void Start()
        {
            GetComponent<ToggleZ>().OnToggled.AddListener(onValueChanged);
        }
        protected override void OnDestroy()
        {
            GetComponent<ToggleZ>().OnToggled.RemoveListener(onValueChanged);
        }

        private void onValueChanged(bool value)
        {
            var parent = GetComponentInParent<FlowLayoutZ>();

            if (parent != null)
            {
                parent.ActivateItem(this);
            }
        }
    }
}