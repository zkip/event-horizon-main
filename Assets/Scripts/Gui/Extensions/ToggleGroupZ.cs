using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Gui.Extensions
{

    [AddComponentMenu("Layout/Extensions/Toggle Group Z")]
    public class ToggleGroupZ : MonoBehaviour
    {
        public UnityEvent<ToggleZ?> OnToggleChanged;

        private void OnDestroy()
        {
            foreach(var toggle in _registerToggles)
                if(_registerActions.TryGetValue(toggle, out var listener))
                    toggle.OnToggled.RemoveListener(listener);
        }

        public void Register(ToggleZ toggle)
        {
            _registerActions[toggle] = (toggled) => { onToggle(toggled ? toggle : null); };
            toggle.OnToggled.AddListener(_registerActions[toggle]);
            _registerToggles.Add(toggle);

        }

        public void UnRegister(ToggleZ toggle)
        {
            if(_registerActions.TryGetValue(toggle, out var listener))
            {
                toggle.OnToggled.RemoveListener(listener);
                _registerToggles.Remove(toggle);
                if (_prev == toggle) _prev = null;
            }
        }

        private void onToggle(ToggleZ? toggle)
        {
            if (_prev != null && _prev != toggle) _prev.SetActivateWithoutSendback(false);

            if (_prev != toggle)
                OnToggleChanged.Invoke(toggle);
            _prev = toggle;
        }

        public void ToggleOn(ToggleZ? toggle, bool forceSendback = false)
        {
            if (_prev != null && _prev != toggle) _prev.SetActivateWithoutSendback(false);

            if (_prev != toggle) 
            {
                toggle.SetActivateWithoutSendback(true);
                if(!forceSendback) OnToggleChanged.Invoke(toggle);
            }
            if(forceSendback) OnToggleChanged.Invoke(toggle);
            _prev = toggle;
        }

        public void OffAll(bool sendback)
        {
            foreach(var toggle in _registerToggles)
            {
                if (sendback) toggle.Activate = false;
                else toggle.SetActivateWithoutSendback(false);
            }
        }

        public IEnumerable<ToggleZ> GetToggles(bool toggled) {
            return _registerToggles.Where(toggle => toggle.Activate == toggled);
        }

        private List<ToggleZ> _registerToggles = new List<ToggleZ>();
        private Dictionary<ToggleZ, UnityAction<bool>> _registerActions = new Dictionary<ToggleZ, UnityAction<bool>>();
        private ToggleZ _prev;
    }
}
