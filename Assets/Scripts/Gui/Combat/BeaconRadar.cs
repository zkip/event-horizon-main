using UnityEngine;
using Combat.Component.Unit;
using Combat.Scene;
using Combat.Unit;
using UnityEngine.UI;
using Services.Resources;
using Game.Exploration;
using System.Collections.Generic;

namespace Gui.Combat
{
    public class BeaconRadar : MonoBehaviour
    {
        public static Dictionary<ObjectiveType, string> ObjectiveIconDict = new Dictionary<ObjectiveType, string>
        {
            [ObjectiveType.Outpost] = "Textures/GUI/star_icon",
            [ObjectiveType.Hive] = "Textures/Icons/icon_virus",
            [ObjectiveType.Container] = "Textures/Icons/icon_cargo",
            [ObjectiveType.ShipWreck] = "Textures/GUI/ship",
            [ObjectiveType.MineralsRare] = "Textures/Artifacts/stone",
            [ObjectiveType.Minerals] = "Textures/GUI/nuclear",
            [ObjectiveType.Meteorite] = "Textures/GUI/meteorite",
            // TODO: XmasBox,
        };

        [SerializeField] public Image Image;
        [SerializeField] public Image Background;
        [SerializeField] public Color StarbaseColor;
        [SerializeField] private float Size = 24;
        
        private ObjectiveInfo _objectiveInfo;
        public void Open(IUnit unit, IScene scene, IResourceLocator resourceLocator, ObjectiveInfo objectiveInfo)
        {
            _scene = scene;
            _unit = unit;
            _objectiveInfo = objectiveInfo;

            Initialize(resourceLocator);
            Update();
            gameObject.SetActive(true);
        }

        public IUnit Unit => _unit;

        private void Update()
        {
            if (!_unit.IsActive())
            {
                Close();
                return;
            }

            var itemPosition = _unit.Body.Position;
            var position = _scene.ViewPoint.Direction(itemPosition);
            var cameraHeight = Camera.main.orthographicSize;
            var cameraWidth = cameraHeight * Camera.main.aspect;

            var x = position.x / cameraWidth;
            var y = position.y / cameraHeight;

            if (x > -1 && x < 1 && y > -1 && y < 1)
            {
                Image.enabled = false;
                Background.enabled = false;
                return;
            }

            Image.enabled = true;
            Background.enabled = true;

            var width = _scene.Settings.AreaWidth/2;
            var height = _scene.Settings.AreaHeight/2;
            var dx = ((position.x > 0 ? position.x : -position.x) - cameraWidth) / (width - cameraWidth);
            var dy = ((position.y > 0 ? position.y : -position.y) - cameraHeight) / (height - cameraHeight);
            var scale = Mathf.Max(1 - Mathf.Max(dx, dy), 0.25f);

            var max = Mathf.Max(x > 0 ? x : -x, y > 0 ? y : -y);
            var offset = scale * Size;

            x = offset + 0.5f * (x / max + 1) * (_screenSize.x - 2 * offset);
            y = offset + 0.5f * (y / max + 1) * (_screenSize.y - 2 * offset);

            gameObject.SetActive(true);

            RectTransform.anchoredPosition = new Vector2(x, y);
            RectTransform.localScale = Vector3.one * scale;
        }

        public void Close()
        {
            _unit = null;

            if (this)
                gameObject.SetActive(false);
        }

        public RectTransform RectTransform
        {
            get
            {
                if (_rectTransform == null)
                    _rectTransform = GetComponent<RectTransform>();
                return _rectTransform;
            }
        }

        private void Initialize(IResourceLocator resourceLocator)
        {
            _screenSize = RectTransform.parent.GetComponent<RectTransform>().rect.size;
            RectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Size * 2);
            RectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Size * 2);

            Image.sprite = resourceLocator.GetSprite(ObjectiveIconDict[_objectiveInfo.Type]);
            if (_objectiveInfo.Type == ObjectiveType.Outpost)
            {
                Background.color = StarbaseColor;
            }

        }

        private Vector2 _screenSize;
        private RectTransform _rectTransform;
        private IUnit _unit;
        private IScene _scene;
    }
}
