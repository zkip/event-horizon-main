using Economy.ItemType;
using Economy.Products;
using Gui.Theme.Wrappers;
using Services.Resources;
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;
using Zenject;

namespace Gui.Reforge
{
    public class ReforgeItem : MonoBehaviour
    {
        public Toggle Toggle;
        public ThemedImage Icon;
        public ThemedText Quantity;

        [HideInInspector]
        public IProduct Product;

        [Serializable]
        public class ReforgeItemSelectionEvent : UnityEvent<IProduct> { }
        public ReforgeItemSelectionEvent OnItemSelectedEvent = new ReforgeItemSelectionEvent();
        public ReforgeItemSelectionEvent OnItemDeselectedEvent = new ReforgeItemSelectionEvent();

        public bool UseQuality = false;
        public ThemedImage Frame;
        public ThemedImage QuantityBackground;

        public void InitializeForReforge(IResourceLocator _resourceLocator, IProduct item, int overridedQuantity = 0)
        {
            Product = item;
            _item = item;
            Icon.sprite = _resourceLocator.GetSprite(item.Type.Icon);
            Quantity.text = overridedQuantity != 0 ? overridedQuantity.ToString() : item.Quantity.ToString();
            Icon.color = item.Type.Color;
            if (UseQuality)
            {
                Frame.color = Theme.UiTheme.Current.GetQualityColor(item.Type.Quality);
                Quantity.color = Theme.UiTheme.Current.GetQualityColor(item.Type.Quality);
                QuantityBackground.color = Theme.UiTheme.Current.GetQualityColor(item.Type.Quality);
            }
            else
            {
                Frame.color = Theme.UiTheme.Current.GetColor(Theme.ThemeColor.Button);
                Quantity.color = Theme.UiTheme.Current.GetColor(Theme.ThemeColor.ButtonText);
                QuantityBackground.color = Theme.UiTheme.Current.GetColor(Theme.ThemeColor.ButtonText);
            }
        }

        private void Start()
        {
            Toggle.onValueChanged.AddListener(onToggle);
        }
        private void OnDestroy()
        {
            Toggle.onValueChanged.RemoveListener(onToggle);
        }

        private void onToggle(bool toggle)
        {
            if (toggle)
                OnItemSelectedEvent.Invoke(_item);
            else
                OnItemDeselectedEvent.Invoke(_item);
        }

        private IProduct _item;
    }
}
