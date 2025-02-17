using PersistentEmpiresLib.SceneScripts;
using System;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace PersistentEmpires.Views.ViewsVM.StockpileMarket
{
    public class PEStockpileMarketItemVM : ViewModel
    {
        public MarketItem MarketItem { get; private set; }
        public int ItemIndex { get; private set; }

        private ImageIdentifierVM _imageIdentifier;
        private bool _isSelected;
        private string _itemName;
        private int _stock;
        private int _constant;
        private readonly Action<PEStockpileMarketItemVM> _executeSelect;

        public PEStockpileMarketItemVM(MarketItem marketItem, int itemIndex, Action<PEStockpileMarketItemVM> executeSelect)
        {
            MarketItem = marketItem ?? throw new ArgumentNullException(nameof(marketItem));
            _executeSelect = executeSelect ?? throw new ArgumentNullException(nameof(executeSelect));

            ImageIdentifier = new ImageIdentifierVM(marketItem.Item);
            ItemIndex = itemIndex;
            Stock = marketItem.Stock;
            Constant = marketItem.Constant;
            ItemName = marketItem.Item.Name.ToString();
        }

        [DataSourceProperty]
        public int BuyPrice => MarketItem.BuyPrice();

        [DataSourceProperty]
        public int SellPrice => MarketItem.SellPrice();

        [DataSourceProperty]
        public ImageIdentifierVM ImageIdentifier
        {
            get => _imageIdentifier;
            set => SetProperty(ref _imageIdentifier, value, nameof(ImageIdentifier));
        }

        [DataSourceProperty]
        public int Stock
        {
            get => _stock;
            set
            {
                if (SetProperty(ref _stock, value, nameof(Stock)))
                {
                    base.OnPropertyChanged(nameof(BuyPrice));
                    base.OnPropertyChanged(nameof(SellPrice));
                }
            }
        }

        [DataSourceProperty]
        public int Constant
        {
            get => _constant;
            set
            {
                if (SetProperty(ref _constant, value, nameof(Constant)))
                {
                    base.OnPropertyChanged(nameof(BuyPrice));
                    base.OnPropertyChanged(nameof(SellPrice));
                }
            }
        }

        [DataSourceProperty]
        public string ItemName
        {
            get => _itemName;
            set => SetProperty(ref _itemName, value, nameof(ItemName));
        }

        [DataSourceProperty]
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value, nameof(IsSelected));
        }

        public void ExecuteSelect() => _executeSelect(this);

        public void ExecuteHoverStart()
        {
            if (MarketItem?.Item != null)
            {
                InformationManager.ShowTooltip(typeof(ItemObject), new object[] { new EquipmentElement(MarketItem.Item) });
            }
        }

        public void ExecuteHoverEnd() => InformationManager.HideTooltip();

        private bool SetProperty<T>(ref T field, T newValue, string propertyName)
        {
            if (EqualityComparer<T>.Default.Equals(field, newValue)) return false;
            field = newValue;
            base.OnPropertyChanged(propertyName);
            return true;
        }
    }
}
