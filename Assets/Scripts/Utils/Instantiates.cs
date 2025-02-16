using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Utils
{
    public static class Instantiates
    {
        public static Tc ActivateInRange<Tp, Tc>(Tp parent, int childIndex, Tc instOriginal) where Tp : Component where Tc : Component
        {
            if (childIndex < parent.transform.childCount)
            {
                var child = parent.transform.GetChild(childIndex).GetComponent<Tc>();
                child.gameObject.SetActive(true);
                return child;
            }

            var inst = UnityEngine.Object.Instantiate(instOriginal, parent.transform);
            inst.gameObject.SetActive(true);
            return inst;
        }
        public static void DeactivateOutOfRange<T>(T parent, int expectCount) where T : Component
        {
            var pcount = parent.transform.childCount;
            for (var i = 0; i < pcount - expectCount; i++)
            {
                parent.transform.GetChild(i + expectCount).gameObject.SetActive(false);
            }
        }
    }

}
