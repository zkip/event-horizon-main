using Economy;
using Economy.ItemType;
using GameDatabase;
using GameDatabase.Model;
using GameDatabase.DataModel;
using Gui.Theme.Wrappers;
using System;
using System.Collections.Generic;
using Services.Resources;
using Economy.Products;
using GameModel.Quests;
using System.Linq;
using UnityEngine;
using GameServices.Player;

namespace Gui.Reforge
{
    class ReforgeCostItem: MonoBehaviour
    {
        public ThemedImage Icon;
        public ThemedText Text;

        public void Initialize(IResourceLocator resourceLocator, IInventory inventory, PlayerResources playerResources) {
            _resourceLocator = resourceLocator;
            _inventory = inventory;
            _playerResources = playerResources;
        }

        public void UpdateCost(IProduct product)
        {
            var stockProducts = _inventory.Items.Where(item => item.Type.Id == product.Type.Id);
            long stock = stockProducts.Count() > 0 ? stockProducts.First().Quantity : 0;
            if (product.Type is MoneyItem)
                stock = ((long)_playerResources.Money);
            if (product.Type is StarsItem)
                stock = ((long)_playerResources.Stars);

            Icon.sprite = _resourceLocator.GetSprite(product.Type.Icon);
            Icon.color = product.Type.Color;
            Text.text = product.Quantity.ToString() + " / " + stock.ToString();

            if (stock < product.Quantity)
                Text.color = Theme.UiTheme.Current.GetColor(Theme.ThemeColor.WarningButtonText);
            else
                Text.color = Theme.UiTheme.Current.GetColor(Theme.ThemeColor.BrightText);

            _isEnough = stock >= product.Quantity;
        }
        public bool IsEnough => _isEnough;

        private bool _isEnough = false;
        private IResourceLocator _resourceLocator;
        private IInventory _inventory;
        private PlayerResources _playerResources;
    }
}
