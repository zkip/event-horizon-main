using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine;

namespace Gui.Reforge
{
    [RequireComponent(typeof(Toggle))]
    class ReforgeStylePanel: MonoBehaviour
    {

        public UnityEvent<ReforgeStyle> OnSelected;
        public ReforgeStyle Style;

        private void Start()
        {
            var toggle = GetComponent<Toggle>();
            toggle.onValueChanged.AddListener(OnToggled);
        }

        public void OnToggled(bool toggled) {
            if (toggled)
                OnSelected.Invoke(Style);
        }
    }
}
