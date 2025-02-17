using PersistentEmpires.Views.ViewsVM;
using PersistentEmpiresLib.Data;
using System;
using System.Collections.Generic;
using TaleWorlds.Core.ViewModelCollection.Selector;
using TaleWorlds.Library;

namespace PersistentEmpiresClient.ViewsVM
{
    public class PEBaseItemListVM<T, U> : ViewModel
    {
        private List<U> _items;
        private readonly Action<PEItemVM> _handleClickItem;
        private PEInventoryVM _playerInventory;
        private string _nameFilter;
        private MBBindingList<T> _filteredItemList;
        private SelectorVM<SelectorItemVM> _stockFilter;
        private SelectorVM<SelectorItemVM> _cultureFilter;
        private SelectorVM<SelectorItemVM> _tierFilter;
        private SelectorVM<SelectorItemVM> _itemTypeFilter;
        private int _lastRenderedIndex;

        private bool? _stockFilterValue;
        private string _cultureFilterValue;
        private int? _tierFilterValue;
        private TaleWorlds.Core.ItemObject.ItemTypeEnum? _itemTypeFilterValue;

        private const int MaxItemsRenderedPerTick = 25;

        private static readonly Dictionary<string, bool?> StockFilterDict = new()
        {
            {"All", null},
            {"Available Stock", true},
            {"No Stock", false}
        };

        private static readonly Dictionary<string, string> CultureFilterDict = new()
        {
            {"All", null},
            {"Vlandia", "vlandia"},
            {"Khuzait", "khuzait"},
            {"Aserai", "aserai"},
            {"Sturgia", "sturgia"},
            {"Battania", "battania"},
            {"Empire", "empire"},
            {"Neutral Culture", "neutral_culture"}
        };

        private static readonly Dictionary<string, int?> TierFilterDict = new()
        {
            {"All", null},
            {"Tier 1", 1},
            {"Tier 2", 2},
            {"Tier 3", 3}
        };

        private static readonly Dictionary<string, TaleWorlds.Core.ItemObject.ItemTypeEnum?> ItemTypeDict = new()
        {
            {"All", null},
            {"One Handed Weapon", TaleWorlds.Core.ItemObject.ItemTypeEnum.OneHandedWeapon},
            {"Two Handed Weapon", TaleWorlds.Core.ItemObject.ItemTypeEnum.TwoHandedWeapon},
            {"Polearm", TaleWorlds.Core.ItemObject.ItemTypeEnum.Polearm},
            {"Bow", TaleWorlds.Core.ItemObject.ItemTypeEnum.Bow},
            {"Crossbow", TaleWorlds.Core.ItemObject.ItemTypeEnum.Crossbow},
            {"Thrown", TaleWorlds.Core.ItemObject.ItemTypeEnum.Thrown},
            {"Goods", TaleWorlds.Core.ItemObject.ItemTypeEnum.Goods},
            {"Armor", TaleWorlds.Core.ItemObject.ItemTypeEnum.BodyArmor},
            {"Horse", TaleWorlds.Core.ItemObject.ItemTypeEnum.Horse}
        };

        public PEBaseItemListVM(Action<PEItemVM> handleClickItem)
        {
            _handleClickItem = handleClickItem ?? throw new ArgumentNullException(nameof(handleClickItem));
            _lastRenderedIndex = 0;
            _filteredItemList = new MBBindingList<T>();
            _nameFilter = string.Empty;

            StockFilter = new SelectorVM<SelectorItemVM>(new List<string>(StockFilterDict.Keys), 0, OnStockFilterSelected);
            CultureFilter = new SelectorVM<SelectorItemVM>(new List<string>(CultureFilterDict.Keys), 0, OnCultureFilterSelected);
            TierFilter = new SelectorVM<SelectorItemVM>(new List<string>(TierFilterDict.Keys), 0, OnTierFilterSelected);
            ItemTypeFilter = new SelectorVM<SelectorItemVM>(new List<string>(ItemTypeDict.Keys), 0, OnItemTypeFilterSelected);
        }

        public virtual void AddItem(object obj, int index) { }

        public void RefreshValues(List<U> items, Inventory playerInventory)
        {
            ItemsList = items ?? new List<U>();
            PlayerInventory = new PEInventoryVM(_handleClickItem);
            PlayerInventory.SetItems(playerInventory);
            ReRender();
        }

        private void ReRender()
        {
            _lastRenderedIndex = 0;
            _filteredItemList.Clear();
        }

        private void OnStockFilterSelected(SelectorVM<SelectorItemVM> obj)
        {
            _stockFilterValue = StockFilterDict[obj.SelectedItem.StringItem];
            ReRender();
        }

        private void OnCultureFilterSelected(SelectorVM<SelectorItemVM> obj)
        {
            _cultureFilterValue = CultureFilterDict[obj.SelectedItem.StringItem];
            ReRender();
        }

        private void OnTierFilterSelected(SelectorVM<SelectorItemVM> obj)
        {
            _tierFilterValue = TierFilterDict[obj.SelectedItem.StringItem];
            ReRender();
        }

        private void OnItemTypeFilterSelected(SelectorVM<SelectorItemVM> obj)
        {
            _itemTypeFilterValue = ItemTypeDict[obj.SelectedItem.StringItem];
            ReRender();
        }

        public void OnTick()
        {
            if (ItemsList == null || _lastRenderedIndex >= ItemsList.Count) return;

            int maxIndex = Math.Min(_lastRenderedIndex + MaxItemsRenderedPerTick, ItemsList.Count);

            for (int i = _lastRenderedIndex; i < maxIndex; i++)
            {
                dynamic item = ItemsList[i];
                if (!string.IsNullOrEmpty(_nameFilter) && !item.Item.Name.ToString().ToLower().Contains(_nameFilter.ToLower())) continue;
                if (_stockFilterValue != null && (_stockFilterValue.Value ? item.Stock <= 0 : item.Stock > 0)) continue;
                if (_cultureFilterValue != null && item.Item.Culture.StringId != _cultureFilterValue) continue;
                if (_tierFilterValue != null && item.Tier != _tierFilterValue) continue;
                if (_itemTypeFilterValue != null && item.Item.Type != _itemTypeFilterValue) continue;

                AddItem(item, i);
            }

            _lastRenderedIndex = maxIndex;
        }

        public List<U> ItemsList
        {
            get => _items;
            set => _items = value ?? new List<U>();
        }

        [DataSourceProperty]
        public MBBindingList<T> FilteredItemList
        {
            get => _filteredItemList;
            set => SetProperty(ref _filteredItemList, value, nameof(FilteredItemList));
        }

        [DataSourceProperty]
        public PEInventoryVM PlayerInventory
        {
            get => _playerInventory;
            set => SetProperty(ref _playerInventory, value, nameof(PlayerInventory));
        }

        [DataSourceProperty]
        public string NameFilter
        {
            get => _nameFilter;
            set
            {
                if (SetProperty(ref _nameFilter, value, nameof(NameFilter)))
                {
                    ReRender();
                }
            }
        }

        [DataSourceProperty]
        public SelectorVM<SelectorItemVM> StockFilter { get; set; }

        [DataSourceProperty]
        public SelectorVM<SelectorItemVM> CultureFilter { get; set; }

        [DataSourceProperty]
        public SelectorVM<SelectorItemVM> TierFilter { get; set; }

        [DataSourceProperty]
        public SelectorVM<SelectorItemVM> ItemTypeFilter { get; set; }

        private bool SetProperty<TValue>(ref TValue field, TValue newValue, string propertyName)
        {
            if (EqualityComparer<TValue>.Default.Equals(field, newValue)) return false;
            field = newValue;
            base.OnPropertyChanged(propertyName);
            return true;
        }
    }
}
