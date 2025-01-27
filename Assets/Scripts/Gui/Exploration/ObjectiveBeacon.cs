using UnityEngine;
using UnityEngine.UI;
using Services.Resources;
using Game.Exploration;
using Gui.Combat;

namespace Gui.Exploration
{
    public class ObjectiveBeacom : MonoBehaviour
    {
        [SerializeField] private Image Image;
        [SerializeField] private Image Background;
        [SerializeField] private Color StarbaseColor;
        [SerializeField] private Color NormalColor;

        private ObjectiveType _objectiveType;
        private IResourceLocator _resourceLocator;
        public bool Visited = false;

        public void Initialize(ObjectiveType objectiveType, bool visited, IResourceLocator resourceLocator) {
            _objectiveType = objectiveType;
            _resourceLocator = resourceLocator;
            Visited = visited;

            Image.sprite = _resourceLocator.GetSprite(BeaconRadar.ObjectiveIconDict[objectiveType]);
            var color = _objectiveType == ObjectiveType.Outpost ? StarbaseColor : NormalColor;
            if (Visited)
            {
                Image.color = color;
                Background.color = new Color(0,0,0,0);
            }
            else
            {
                Image.color = new Color(0, 0, 0, 1);
                Background.color = color;
            }
            gameObject.SetActive(true);
        }
    }
}
