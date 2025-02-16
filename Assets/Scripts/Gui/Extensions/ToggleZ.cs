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
    [AddComponentMenu("Loggical/Toggle Z")]
    public class ToggleZ : MonoBehaviour, IPointerClickHandler
    {
        public GameObject Target;

        public ToggleGroupZ Group;

        [SerializeField] private bool _activate;
        public bool Activate {
            get => _activate;
            set { _activate = value; onSetActivated(true); }
        }

        public UnityEvent<bool> OnToggled;

        void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
        {
            Activate = !Activate;
        }

        private void onSetActivated(bool sendback)
        {
            if(sendback) OnToggled.Invoke(_activate);
            if (Target != null)
                Target.SetActive(_activate);
        }

        public void SetActivateWithoutSendback(bool activate)
        {
            _activate = activate;
            onSetActivated(false);
        }

        private void Start()
        {
            if (Group != null) Group.Register(this);
        }

        private void OnDestroy()
        {
            if (Group != null) Group.UnRegister(this);
        }

    }

}
