using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Constructor;
using DataModel.Technology;
using Economy.ItemType;
using Economy.Products;
using GameDatabase.Enums;
using GameModel.Quests;
using GameServices.Player;
using GameServices.Research;
using Services.Audio;
using Services.Resources;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Utils;
using Zenject;

namespace Gui.Reforge
{
    public class ReforgePanel : MonoBehaviour
    {

        [Inject] private readonly ISoundPlayer _soundPlayer;
        [Inject] private readonly IResourceLocator _resourceLocator;
        [Inject] private readonly PlayerResources _playerResources;
        [Inject] private readonly ItemTypeFactory _factory;
        [Inject] private readonly PlayerSkills _playerSkills;

        [SerializeField] private AudioClip _buySound;

        [SerializeField] private Toggle _randomAll;
        [SerializeField] private Toggle _randomModification;
        [SerializeField] private Toggle _Upgrade;

        [SerializeField] private RectTransform _costPriceContainer;
        [SerializeField] private RectTransform _costMaterialContainer;

        [SerializeField] private ReforgeButton _reforgeButton;

        [SerializeField] private RectTransform _reforgeInfo;
        [SerializeField] private RectTransform _reforgeActions;
        [SerializeField] private RectTransform _costContainer;

        [SerializeField] private RectTransform _randomAllInfo;
        [SerializeField] private RectTransform _randomModificationInfo;
        [SerializeField] private RectTransform _upgradeInfo;
        [SerializeField] private RectTransform _needMoreLevelInfo;
        [SerializeField] private RectTransform _noMoreUpgradeInfo;
        [SerializeField] private RectTransform _noMoreModificationInfo;

        [SerializeField] public UnityEvent<ComponentInfo> ItemReforged;

        public void Initialize(IInventory inventory, IReforgeCosts<ReforgeContext> reforgeCosts, int spaceLevel)
        {
            _spaceLevel = spaceLevel;
            _inventory = inventory;
            _reforgeCosts = reforgeCosts;
            _randomAll.onValueChanged.AddListener((bool isToggled) => { if (isToggled) OnReforgeStyleChanged(ReforgeStyle.RandomAll); });
            _randomModification.onValueChanged.AddListener((bool isToggled) => { if (isToggled) OnReforgeStyleChanged(ReforgeStyle.RandomModification); });
            _Upgrade.onValueChanged.AddListener((bool isToggled) => { if (isToggled) OnReforgeStyleChanged(ReforgeStyle.UpgradeThenRandom); });
            _reforgeButton.GetComponent<Button>().onClick.AddListener(() => { OnReforge(); });
        }

        public void Clean()
        {
            _targetItem = null;
            _reforgeStyle = ReforgeStyle.RandomAll;
            updateInfoPanel();
        }

        private void OnReforgeStyleChanged(ReforgeStyle reforgeStyle)
        {
            _reforgeStyle = reforgeStyle;
            _reforgeButton.UpdateReforgeStyle(reforgeStyle);

            if (_targetItem == null) { updateInfoPanel(); return; }

            updateCosts();
            updateInfoPanel();
        }

        private void randomAll()
        {
            if (_targetItem == null) return;

            if (_targetItem.Type is ComponentItem)
            {
                var componentItem = (ComponentItem)_targetItem.Type;
                var seed = DateTime.Now.Millisecond + 55 + _reforgeCount;
                var newComponent = ComponentInfo.RandomQualityAndModifications(componentItem.Component, seed, true);
                var typeItem = _factory.CreateComponentItem(newComponent);
                var product = CommonProduct.Create(typeItem);

                _targetItem.Withdraw(1);
                product.Consume(1);

                _focusComponentType = newComponent;
            }
        }

        private void randomModification()
        {
            if (_targetItem == null) return;

            if (_targetItem.Type is ComponentItem)
            {
                var componentItem = (ComponentItem)_targetItem.Type;

                if (componentItem.Component.Data.PossibleModifications.Count < 2) return;

                var seed = DateTime.Now.Millisecond + 315 + _reforgeCount;
                var newComponent = ComponentInfo.RandomModifications(componentItem.Component, seed, true);
                var typeItem = _factory.CreateComponentItem(newComponent);
                var product = CommonProduct.Create(typeItem);

                _targetItem.Withdraw(1);
                product.Consume(1);

                _focusComponentType = newComponent;
            }
        }

        private void upgradeThenRandom()
        {
            if (_targetItem == null) return;

            if (_targetItem.Type is ComponentItem)
            {
                var componentItem = (ComponentItem)_targetItem.Type;
                var seed = DateTime.Now.Millisecond + 126 + _reforgeCount;
                var newComponent = ComponentInfo.UpgradeThenRandomModification(componentItem.Component, seed);
                var typeItem = _factory.CreateComponentItem(newComponent);
                var product = CommonProduct.Create(typeItem);

                _targetItem.Withdraw(1);
                product.Consume(1);

                _focusComponentType = newComponent;
            }
        }

        public void OnReforge()
        {
            if (!_isMaterialsEnough) return;

            var pricesCosts = _priceCosts.GetEnumerator();
            var materialCosts = _materialCosts.GetEnumerator();

            while (pricesCosts.MoveNext()) pricesCosts.Current.Withdraw();
            while (materialCosts.MoveNext()) materialCosts.Current.Withdraw();

            switch (_reforgeStyle)
            {
                case ReforgeStyle.RandomAll: randomAll(); break;
                case ReforgeStyle.RandomModification: randomModification(); break;
                case ReforgeStyle.UpgradeThenRandom: upgradeThenRandom(); break;
            }

            _soundPlayer.Play(_buySound);
            _reforgeCount++;
            ItemReforged.Invoke(_focusComponentType);
        }

        public void UpdateTargetItem(IProduct item)
        {
            _targetItem = item;
            if (item == null) { updateInfoPanel(); return; }

            updateCosts();
            updateInfoPanel();
        }

        private Tuple<int,int> getCraftSkillLevel()
        {
            var craftingPrice = _playerSkills.GetSkillLevel(GameModel.Skills.SkillType.CraftingPrice);
            var craftingLevel = _playerSkills.GetSkillLevel(GameModel.Skills.SkillType.CraftingLevel);
            return new Tuple<int, int>(craftingPrice, craftingLevel);
        }

        private void updateCosts()
        {
            _isMaterialsEnough = true;

            var discount = 1f - _playerSkills.CraftingPriceScale;
            var (craftingPrice , craftingLevel) = getCraftSkillLevel();

            var reforgeContext = new ReforgeContext {
                target = _targetItem.Type,
                spaceLevel = _spaceLevel,
                discount = discount,
                notRequiredStars = craftingPrice >= 6 ? true : false,
                ignoreLevel = craftingLevel >= 6 ? -1 : craftingLevel * 15,
            };

            var costs = _reforgeCosts.GetCosts(_reforgeStyle, reforgeContext).Where(isSupportedCostType).Where(cost=>cost.Quantity>0);

            var priceTemplate = _costPriceContainer.GetChild(0);
            var materialTemplate = _costMaterialContainer.GetChild(0);

            var costsGroup = costs.GroupBy(isPrice);
            _priceCosts = costsGroup.FirstOrDefault()?.ToList() ?? new List<IProduct> { };
            _materialCosts = costsGroup.Skip(1).FirstOrDefault()?.ToList() ?? new List<IProduct> { };

            for (var i = 0; i < _priceCosts.Count(); i++)
            {
                var cost = _priceCosts[i];
                var costItem = Instantiates.ActivateInRange(_costPriceContainer, i, priceTemplate);
                var reforgeCostItem = costItem.GetComponent<ReforgeCostItem>();

                reforgeCostItem.Initialize(_resourceLocator, _inventory, _playerResources);
                reforgeCostItem.UpdateCost(cost);
                _isMaterialsEnough = _isMaterialsEnough && reforgeCostItem.IsEnough;
            }
            Instantiates.DeactivateOutOfRange(_costPriceContainer, _priceCosts.Count());
            _costPriceContainer.gameObject.SetActive(_priceCosts.Count() > 0);

            for (var i = 0; i < _materialCosts.Count(); i++)
            {
                var cost = _materialCosts[i];
                var costItem = Instantiates.ActivateInRange(_costMaterialContainer, i, materialTemplate);
                var reforgeCostItem = costItem.GetComponent<ReforgeCostItem>();

                reforgeCostItem.Initialize(_resourceLocator, _inventory, _playerResources);
                reforgeCostItem.UpdateCost(cost);
                _isMaterialsEnough = _isMaterialsEnough && reforgeCostItem.IsEnough;
            }
            Instantiates.DeactivateOutOfRange(_costMaterialContainer, _materialCosts.Count());
            _costMaterialContainer.gameObject.SetActive(_materialCosts.Count() > 0);
        }

        private void updateInfoPanel()
        {
            var button = _reforgeButton.GetComponent<Button>();
            var currentComponentItem = _targetItem != null ? ((ComponentItem)_targetItem.Type) : null;
            var isTipNeed = currentComponentItem != null;

            InfoPanelType visibleInfoPanelType = InfoPanelType.ReforgeRandomAll;
            var showReforgePanel = _targetItem != null;
            var showTips = false;

            var (_, craftingLevel) = getCraftSkillLevel();
            if (!showTips && isTipNeed && craftingLevel < 6 && _spaceLevel + craftingLevel < currentComponentItem.Component.Data.Level)
            {
                visibleInfoPanelType = InfoPanelType.NeedMoreLevels;
                showTips = true;
            }

            var isModifications = _reforgeStyle == ReforgeStyle.RandomModification;
            if (!showTips && isTipNeed && isModifications && currentComponentItem.Component.Data.PossibleModifications.Count < 2)
            {
                visibleInfoPanelType = InfoPanelType.NoMoreModifications;
                showTips = true;
            }

            var isUpgrade = _reforgeStyle == ReforgeStyle.UpgradeThenRandom;
            if (!showTips && isTipNeed && isUpgrade && currentComponentItem.Component.ModificationQuality == ModificationQuality.P3)
            {
                visibleInfoPanelType = InfoPanelType.NoMoreUpgrades;
                showTips = true;
            }

            if (!showReforgePanel && !showTips)
            {
                switch (_reforgeStyle)
                {
                    case ReforgeStyle.RandomAll:
                        visibleInfoPanelType = InfoPanelType.ReforgeRandomAll; break;
                    case ReforgeStyle.RandomModification:
                        visibleInfoPanelType = InfoPanelType.ReforgeRandomModifications; break;
                    case ReforgeStyle.UpgradeThenRandom:
                        visibleInfoPanelType = InfoPanelType.ReforgeUpgrade; break;
                    default: throw new ArgumentException();
                }

            }

            if (showReforgePanel) button.interactable = _isMaterialsEnough;

            _costContainer.gameObject.SetActive(!showTips && showReforgePanel);
            _reforgeActions.gameObject.SetActive(!showTips && showReforgePanel);
            _reforgeInfo.gameObject.SetActive(showTips || !showReforgePanel);

            _randomAllInfo.gameObject.SetActive(visibleInfoPanelType == InfoPanelType.ReforgeRandomAll);
            _randomModificationInfo.gameObject.SetActive(visibleInfoPanelType == InfoPanelType.ReforgeRandomModifications);
            _upgradeInfo.gameObject.SetActive(visibleInfoPanelType == InfoPanelType.ReforgeUpgrade);

            _needMoreLevelInfo.gameObject.SetActive(visibleInfoPanelType == InfoPanelType.NeedMoreLevels);
            _noMoreModificationInfo.gameObject.SetActive(visibleInfoPanelType == InfoPanelType.NoMoreModifications);
            _noMoreUpgradeInfo.gameObject.SetActive(visibleInfoPanelType == InfoPanelType.NoMoreUpgrades);
        }

        private static bool isPrice(IProduct product)
        {
            return product.Type is MoneyItem || product.Type is StarsItem;
        }
        private static bool isSupportedCostType(IProduct product)
        {
            return product.Type is ArtifactItem || product.Type is MoneyItem || product.Type is StarsItem;
        }

        private ReforgeStyle _reforgeStyle = ReforgeStyle.RandomAll;
        private IProduct _targetItem;
        private bool _isMaterialsEnough = true;
        private int _reforgeCount = 0;
        private ComponentInfo _focusComponentType = ComponentInfo.Empty;
        private List<IProduct> _priceCosts;
        private List<IProduct> _materialCosts;

        private int _spaceLevel;
        IReforgeCosts<ReforgeContext> _reforgeCosts;
        IInventory _inventory;

        private enum InfoPanelType
        {
            ReforgeRandomAll,
            ReforgeRandomModifications,
            ReforgeUpgrade,
            NeedMoreLevels,
            NoMoreUpgrades,
            NoMoreModifications,
        }
    }
}
