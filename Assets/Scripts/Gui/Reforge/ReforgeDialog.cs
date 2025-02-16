using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using DataModel.Technology;
using Economy.ItemType;
using Economy.Products;
using Services.Gui;
using GameDatabase;
using GameDatabase.DataModel;
using GameDatabase.Model;
using GameServices.Database;
using GameServices.Gui;
using GameServices.Player;
using GameServices.Random;
using GameServices.Research;
using Services.Audio;
using Services.Localization;
using Services.Messenger;
using Services.ObjectPool;
using Zenject;
using CommonComponents;
using UnityEngine.UI.Extensions;
using GameModel.Quests;
using Services.Resources;
using System.Collections.Generic;
using Utils;
using GameDatabase.Enums;
using Gui.Theme.Wrappers;
using Constructor;
using System.Collections;
using Gui.Extensions;

namespace Gui.Reforge
{
    public enum ReforgeStyle
    {
        RandomAll,
        RandomModification,
        UpgradeThenRandom,
    }

    public class ReforgeDialog : MonoBehaviour
    {
        [Inject] private readonly ISoundPlayer _soundPlayer;
        [Inject] private readonly PlayerResources _playerResources;
        [Inject] private readonly IRandom _random;
        [Inject] private readonly ItemTypeFactory _factory;
        [Inject] private readonly ILocalization _localization;
        [Inject] private readonly Research _research;
        [Inject] private readonly ITechnologies _technologies;
        [Inject] private readonly MotherShip _motherShip;
        [Inject] private readonly IGameObjectFactory _gameObjectFactory;
        [Inject] private readonly GuiHelper _helper;
        [Inject] private readonly InventoryFactory _inventoryFactory;
        [Inject] private readonly IResourceLocator _resourceLocator;
        [Inject] private readonly IDatabase _database;

        [SerializeField] private Text _factionText;
        [SerializeField] private Image _factionIcon;
        [SerializeField] private Text _levelText;
        [SerializeField] private Text _creditsText;
        [SerializeField] private Text _starsText;

        [SerializeField] private ToggleGroupZ _reforgeItemList;
        [SerializeField] private Toggle _defaultReforgeStyle;
        
        [SerializeField] private ThemedImage _currentImage;
        [SerializeField] private ThemedImage _availableImage;
        [SerializeField] private ThemedText _name;
        [SerializeField] private ThemedText _modification;

        [SerializeField] private FlowLayoutZ _weaponsGroup;
        [SerializeField] private FlowLayoutZ _armorsGroup;
        [SerializeField] private FlowLayoutZ _energyGroup;
        [SerializeField] private FlowLayoutZ _dronesGroup;
        [SerializeField] private FlowLayoutZ _enginesGroup;
        [SerializeField] private FlowLayoutZ _specialGroup;
        [SerializeField] private FlowLayoutZ _othersGroup;

        [SerializeField] private FlowLayoutZ _aloneGroup;
        [SerializeField] private ReforgeButton _reforgeButton;

        [SerializeField] private RectTransform _costPriceContainer;
        [SerializeField] private RectTransform _costMaterialContainer;
        [SerializeField] private RectTransform _reforgeIntroduction;
        [SerializeField] private RectTransform _reforgeItemInfo;

        [SerializeField] private RectTransform _currentModifications;
        [SerializeField] private RectTransform _availableModifications;
        [SerializeField] private RectTransform _availableModificationContainer;

        [SerializeField] private Image _componentCategoryWidgetIcon;
        [SerializeField] private Image _slotWidgetIcon;
        [SerializeField] private Text _slotWidgetText;

        [SerializeField] private ReforgePanel _reforgePanel;

        [SerializeField] private Color _weaponCellColor;
        [SerializeField] private Color _outerCellColor;
        [SerializeField] private Color _innerCellColor;
        [SerializeField] private Color _engineCellColor;
        [SerializeField] private Color _emptyCellColor;

        [SerializeField] private ReforgeItem _itemTemplate;
        [SerializeField] private ScrollRect _scrollRect;
        [SerializeField] private ScrollRect _infoScrollRect;

        [Inject]
        private void Initialize(IMessenger messenger)
        {
            _inventory = _inventoryFactory.CreateCargoHoldInventory();
            messenger.AddListener<Money>(EventType.MoneyValueChanged, value => UpdateResources());
            messenger.AddListener<Money>(EventType.StarsValueChanged, value => UpdateResources());
            messenger.AddListener(EventType.TechPointsChanged, UpdateResources);

            shownCategoryUINodes = new Dictionary<ComponentCategory, FlowLayoutZ> {
                {ComponentCategory.Weapon, _weaponsGroup },
                {ComponentCategory.Defense, _armorsGroup},
                {ComponentCategory.Energy, _energyGroup},
                {ComponentCategory.Drones, _dronesGroup},
                {ComponentCategory.Engine, _enginesGroup},
                {ComponentCategory.Special, _specialGroup},
                {ComponentCategory.Undefined, _othersGroup},
            };

            _reforgeCosts = new ReforgeCostsForCommodity(_database, _factory);
            var star = _motherShip.CurrentStar;
            _level = Mathf.Max(5, star.Level);
            _reforgePanel.Initialize(_inventory, _reforgeCosts, _level);
        }

        IEnumerator ApplyScrollPosition(ScrollRect sr, float verticalPos)
        {
            yield return new WaitForEndOfFrame();
            sr.verticalNormalizedPosition = verticalPos;
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)sr.transform);
        }
        public void InitializeWindow(WindowArgs args)
        {
            if (args.Count >= 2)
            {
                _faction = args.Get<Faction>(0);
                _level = args.Get<int>(1);
            }
            else
            {
                var star = _motherShip.CurrentStar;
                _faction = star.Region.Faction;
                _level = Mathf.Max(5, star.Level);
            }

            syncItems();
            UpdateItemList();
            updateAloneItems();
            updateInfoWindow();
            _reforgePanel.Clean();

            _factionText.text = _localization.GetString(_faction.Name);
            _factionIcon.color = _faction.Color;
            _levelText.text = _level.ToString();
            UpdateResources();
        }

        public void DeinitializedWindow()
        {
            _defaultReforgeStyle.SetIsOnWithoutNotify(true);
            _reforgeItemList.OffAll(false);
            OnItemChanged(null);
        }

        public void OnItemChanged(ToggleZ toggle)
        {
            var product = toggle == null ? null : toggle.GetComponent<ReforgeItem>().Product;
            _currentTypeItem = product;
            _currentItem = null;
            _aloneGroup.GetComponent<ToggleGroupZ>().OffAll(true);
            _reforgePanel.UpdateTargetItem(null);

            if (product != null) {
                var componentItem = (ComponentItem)product.Type;
                var category = componentItem.Component.Data.DisplayCategory;

                if (prevCategory != category)
                {
                    shownCategoryUINodes[prevCategory]?.UnActivateItem();
                    prevCategory = category;
                }
            }
            else
            {
                shownCategoryUINodes[prevCategory]?.UnActivateItem();
            }


            if (product != null) updateAloneItems();
            updateInfoWindow();
            if (product != null) updateDescription();

            if(product != null)
            {
                if (_scrollRect.verticalNormalizedPosition * _scrollRect.content.rect.height < 20)
                {
                    float backup = _scrollRect.verticalNormalizedPosition;
                    StartCoroutine(ApplyScrollPosition(_scrollRect, backup));
                }
            }
        }

        public void OnAloneItemChanged(ToggleZ toggle)
        {
            var product = toggle == null ? null : toggle.GetComponent<ReforgeItem>().Product;
            _currentItem = product;
            _reforgePanel.UpdateTargetItem(product);
            updateDescription();
        }

        private void updateInfoWindow()
        {
            var nothingToSelected = _currentTypeItem == null && _currentItem == null;
            _reforgeIntroduction.gameObject.SetActive(nothingToSelected);
            _reforgeItemInfo.gameObject.SetActive(!nothingToSelected);
        }

        private void updateAvailableModifications()
        {
            var componentItem = (ComponentItem)_currentTypeItem.Type;
            var modifications = componentItem.Component.Data.PossibleModifications;

            var template = _availableModificationContainer.GetChild(0);
            for (var i = 0; i < modifications.Count; i++)
            {
                var inst = Instantiates.ActivateInRange(_availableModificationContainer, i, template);
                var text = inst.GetComponent<ThemedText>();
                text.text = _localization.GetString(modifications[i].Description, "", "", "", "");
            }

            Instantiates.DeactivateOutOfRange(_availableModificationContainer, modifications.Count);
        }

        private static Dictionary<ComponentCategory, SpriteId> categorySpriteId = new Dictionary<ComponentCategory, SpriteId> {
            {ComponentCategory.Weapon,  new SpriteId("icons/icon_swords", SpriteId.Type.Default)},
            {ComponentCategory.Energy,  new SpriteId("icons/icon_battery", SpriteId.Type.Default)},
            {ComponentCategory.Drones,  new SpriteId("icons/icon_drone", SpriteId.Type.Default)},
            {ComponentCategory.Engine,  new SpriteId("icons/icon_engine", SpriteId.Type.Default)},
            {ComponentCategory.Defense,  new SpriteId("icons/icon_shield", SpriteId.Type.Default)},
            {ComponentCategory.Special,  new SpriteId("icons/icon_gear", SpriteId.Type.Default)},
            {ComponentCategory.Undefined,  new SpriteId("icons/icon_remove", SpriteId.Type.Default)},
        };

        private void updateWidgets() {
            var shouldShown = _currentTypeItem != null && _currentTypeItem.Type is ComponentItem;
            if (shouldShown)
            {
                var componentItem = (ComponentItem)_currentTypeItem.Type;
                var category = componentItem.Component.Data.DisplayCategory;
                _componentCategoryWidgetIcon.sprite = _resourceLocator.GetSprite(categorySpriteId[category]);
                _slotWidgetIcon.color = getCellColor(componentItem.Component.Data.CellType);
                _slotWidgetText.text = slotTypeToString(componentItem.Component);
            }
            _componentCategoryWidgetIcon.gameObject.SetActive(shouldShown);
            _slotWidgetIcon.gameObject.SetActive(shouldShown);
        }

        private static string slotTypeToString(ComponentInfo componentInfo)
        {
            var cellType = componentInfo.Data.CellType;
            if(cellType == CellType.Weapon)
            {
                var weaponType = componentInfo.Data.WeaponSlotType;
                return ((char)weaponType).ToString();
            }
            return string.Empty;
        }
        private Color getCellColor(CellType cellType)
        {
            switch (cellType)
            {
                case CellType.Weapon: return _weaponCellColor;
                case CellType.Outer: return _outerCellColor;
                case CellType.Inner: return _innerCellColor;
                case CellType.InnerOuter: return Color.Lerp(_innerCellColor, _outerCellColor, 0.5f);
                case CellType.Engine: return _engineCellColor;
                case CellType.Empty:
                default:
                    return _emptyCellColor;
            }
        }

        private void updateDescription()
        {
            var showInfo = _currentTypeItem != null && _currentItem != null;
            var showAvailables = _currentTypeItem != null && _currentItem == null;
            if (showAvailables) updateAvailableModifications();
            if (showAvailables) updateWidgets();

            _currentModifications.gameObject.SetActive(showInfo);
            _availableModifications.gameObject.SetActive(showAvailables);

            if (_currentTypeItem != null)
            {
                var typeItemType = (ComponentItem)_currentTypeItem.Type;
                var typeItemComponent = typeItemType.Component;
                var typeItemColor = Theme.UiTheme.Current.GetQualityColor(typeItemComponent.ItemQuality);

                _currentModifications.GetChild(0).GetComponent<ThemedText>().text = typeItemType.Name;
                _availableModifications.GetChild(0).GetComponent<ThemedText>().text = typeItemType.Name;
                _name.text = typeItemType.Name;
                _name.color = typeItemColor;
                _currentImage.sprite = _resourceLocator.GetSprite(typeItemComponent.Data.Icon);
                _availableImage.sprite= _resourceLocator.GetSprite(typeItemComponent.Data.Icon);
                _currentImage.color = typeItemType.Color;
                _availableImage.color = typeItemType.Color;
            }

            if (_currentItem != null)
            {
                var coomponentInfo = (ComponentItem)_currentItem.Type;
                var component = coomponentInfo.Component;
                var color = Theme.UiTheme.Current.GetQualityColor(component.ItemQuality);
                var descriptionText = component.CreateModification().GetDescription(_localization);
                _modification.text = descriptionText;
                _modification.color = color;
                _name.color = color;
                _modification.gameObject.SetActive(true);
            }
            else
            {
                _modification.gameObject.SetActive(false);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(_infoScrollRect.GetComponent<RectTransform>());
        }

        private void updateAloneItems()
        {
            if (_currentTypeItem == null) return;
            if (!collections.TryGetValue(_currentTypeItem.Type.Name, out var items))
                return;

            var template = _aloneGroup.transform.GetChild(1).GetComponent<ReforgeItem>();
            for(var i = 0; i < items.Count; i++)
            {
                var itemType = items[i].Type;
                var reforgeItem = Instantiates.ActivateInRange(_aloneGroup, 1 + i, template);
                reforgeItem.InitializeForReforge(_resourceLocator, items[i]);

                if (itemType is ComponentItem) {
                    var componentItem = (ComponentItem)itemType;

                    if (componentItem.Component == _focusComponentType)
                        _aloneGroup.GetComponent<ToggleGroupZ>().ToggleOn(reforgeItem.GetComponent<ToggleZ>(), true);
                }
            }

            Instantiates.DeactivateOutOfRange(_aloneGroup, 1 + items.Count);

            _focusComponentType = ComponentInfo.Empty;
        }

        private void UpdateItemList()
        {
            var shownCategories = new[] { ComponentCategory.Weapon, ComponentCategory.Defense, ComponentCategory.Energy, ComponentCategory.Drones, ComponentCategory.Engine, ComponentCategory.Special, ComponentCategory.Undefined };

            foreach (var category in shownCategories)
            {
                var uiRoot = shownCategoryUINodes[category];
                if (!kinds.TryGetValue(category, out var kind))
                {
                    uiRoot.gameObject.SetActive(false);
                    continue;
                }

                uiRoot.gameObject.SetActive(true);

                for (var i = 0; i< kind.Count; i++)
                {
                    var reforgeItem = Instantiates.ActivateInRange(uiRoot, 1 + i, _itemTemplate);

                    var name = kind[i];
                    var collection = collections[name];
                    var totalQuantity = kindTotals.GetValueOrDefault(name);

                    if (collection.Count > 0) reforgeItem.InitializeForReforge(_resourceLocator, collection.First(), totalQuantity);
                }

                var addtionCount = 0;
                if (_currentTypeItem != null)
                {
                    var componentItem = (ComponentItem)_currentTypeItem.Type;
                    var isCurrentCategory = componentItem.Component.Data.DisplayCategory == category;
                    addtionCount = isCurrentCategory ? 1 : 0;
                }
                Instantiates.DeactivateOutOfRange(uiRoot, 1 + kind.Count + addtionCount);
            }
        }

        private void syncItems()
        {
            collections.Clear();
            kinds.Clear();
            kindTotals.Clear();
            foreach (var item in _inventory.Items) {
                if(item.Type is ComponentItem)
                {
                    var componentItem = (ComponentItem)item.Type;
                    var category = componentItem.Component.Data.DisplayCategory;
                    var name = item.Type.Name;
                    var isExistCollection = collections.TryGetValue(name, out var collection);
                    if (!isExistCollection)
                    {
                        collection = new List<IProduct>();

                        var kind = kinds.GetValueOrDefault(category, new List<string>());
                        kind.Add(item.Type.Name);
                        kinds[category] = kind;
                    }

                    collection.Add(item);
                    collections[name] = collection;
                    kindTotals[name] = kindTotals.GetValueOrDefault(name, 0) + item.Quantity;
                }
            }
        }

        public void OnReforge(ComponentInfo focusComponentType)
        {
            _focusComponentType = focusComponentType;
            syncItems();
            UpdateItemList();
            updateAloneItems();
            _reforgePanel.UpdateTargetItem(_currentItem);
            updateDescription();
        }

        private void UpdateResources()
        {
            if (!gameObject.activeSelf)
                return;

            _creditsText.text = _playerResources.Money.ToString();
            _starsText.text = _playerResources.Stars.ToString();
        }

        private Dictionary<ComponentCategory, FlowLayoutZ> shownCategoryUINodes;

        private IReforgeCosts<ReforgeContext> _reforgeCosts;
        private ComponentInfo _focusComponentType = ComponentInfo.Empty;
        private IProduct _currentTypeItem;
        private IProduct _currentItem;
        private ComponentCategory prevCategory;

        private Dictionary<string, List<IProduct>> collections = new();
        private Dictionary<string, int> kindTotals = new();
        private Dictionary<ComponentCategory, List<string>> kinds = new();

        private IInventory _inventory;
        private Faction _faction;
        private ObscuredInt _level;
        private bool isAloneToggling = false;
        private bool isToggling = false;
    }
}
