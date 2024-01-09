﻿using Constructor.Ships;
using GameDatabase.DataModel;
using GameServices.Research;
using Services.Audio;
using Services.Localization;
using UnityEngine;
using Services.Resources;
using UnityEngine.UI;
using Zenject;

namespace Gui.ShipService
{
    public class PaintingPanel : MonoBehaviour
    {
        [SerializeField] private Button _paintButton;
        [SerializeField] private GameObject _paintNotEnoughMoney;
        [SerializeField] private Slider _colorSlider;
        [SerializeField] private Slider _contrastSlider;
        [SerializeField] private Image _shipIcon;
        [SerializeField] private Image _techIcon;
        [SerializeField] private AudioClip _buySound;
        [SerializeField] private Text _nameText;
        [SerializeField] private Text _levelText;

        [Inject] private readonly Research _research;
        [Inject] private readonly IResourceLocator _resourceLocator;
        [Inject] private readonly ISoundPlayer _soundPlayer;
        [Inject] private readonly ILocalization _localization;

        public void Initialize(IShip ship, Faction faction)
        {
            _ship = ship;
            _faction = faction;
            _shipIcon.sprite = _resourceLocator.GetSprite(_ship.Model.ModelImage);
            _nameText.text = _localization.GetString(_ship.Name);
            _levelText.text = _ship.Experience.Level.ToString();
            _techIcon.color = _faction.Color;

            _color = new Color(ship.ColorScheme.Hue, ship.ColorScheme.Saturation, 0);
            _colorSlider.value = _color.r;
            _contrastSlider.value = _color.g * 2f;
            UpdateColorPanel();
        }

        public void OnColorValueChanged(float value)
        {
            _color.r = value;
            UpdateColorPanel();
        }

        public void OnContrastValueChanged(float value)
        {
            _color.g = value * 0.5f;
            UpdateColorPanel();
        }

        public void OnPaintButtonClicked()
        {
            if (!TryTakeMoney()) return;
            _ship.ColorScheme.Hue = _color.r;
            _ship.ColorScheme.Saturation = _color.g;
            _ship.ColorScheme.Type = ShipColorScheme.SchemeType.Hsv;
            _soundPlayer.Play(_buySound);
            UpdateColorPanel();
        }

        private void UpdateColorPanel()
        {
            var isChanged = Mathf.Abs(_color.r - _ship.ColorScheme.Hue) > 0.01f || Mathf.Abs(_color.g - _ship.ColorScheme.Saturation) > 0.01f;
            _paintButton.interactable = isChanged && HaveMoney;
            _paintNotEnoughMoney.SetActive(!HaveMoney);
            _shipIcon.color = _color;
        }

        private bool HaveMoney
        {
            get
            {
                return _research.GetAvailablePoints(_faction) > 0;
            }
        }

        private bool TryTakeMoney()
        {
            if (_research.GetAvailablePoints(_faction) < 1)
                return false;

            _research.AddResearchPoints(_faction, -1);
            return true;
        }

        private Color _color;
        private IShip _ship;
        private Faction _faction;
    }
}
