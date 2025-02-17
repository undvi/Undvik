using PersistentEmpires.Views.ViewsVM.StockpileMarket;
using PersistentEmpiresClient.ViewsVM;
using PersistentEmpiresLib.Data;
using PersistentEmpiresLib.SceneScripts;
using System;
using TaleWorlds.Library;

namespace PersistentEmpires.Views.ViewsVM
{
    public class PEStockpileMarketVM : PEBaseItemListVM<PEStockpileMarketItemVM, MarketItem>
    {
        private PE_StockpileMarket _stockpileMarket;
        private PEStockpileMarketItemVM _selectedItem;
        private Action<PEStockpileMarketItemVM> _buy;
        private Action<PEStockpileMarketItemVM> _sell;
        private Action _unpackBoxes;

        public PEStockpileMarketVM(Action<PEItemVM> handleClickItem) : base(handleClickItem)
        {
        }

        public override void AddItem(object obj, int index)
        {
            if (obj is MarketItem item)
            {
                this.FilteredItemList.Add(new PEStockpileMarketItemVM(item, index, selected =>
                {
                    this.SelectedItem = selected;
                }));
            }
        }

        public void RefreshValues(PE_StockpileMarket stockpileMarket, Inventory inventory, Action<PEStockpileMarketItemVM> buy, Action<PEStockpileMarketItemVM> sell, Action unpackBoxes)
        {
            this.StockpileMarket = stockpileMarket;
            this.Buy = buy ?? throw new ArgumentNullException(nameof(buy));
            this.Sell = sell ?? throw new ArgumentNullException(nameof(sell));
            this.UnpackBoxes = unpackBoxes ?? throw new ArgumentNullException(nameof(unpackBoxes));
        }

        public void ExecuteBuy()
        {
            if (this.Buy != null && this.SelectedItem != null)
            {
                this.Buy(this.SelectedItem);
            }
            else
            {
                InformationManager.DisplayMessage(new InformationMessage("⚠️ Kaufaktion fehlgeschlagen: Kein Item ausgewählt oder Aktion nicht gesetzt."));
            }
        }

        public void ExecuteSell()
        {
            if (this.Sell != null && this.SelectedItem != null)
            {
                this.Sell(this.SelectedItem);
            }
            else
            {
                InformationManager.DisplayMessage(new InformationMessage("⚠️ Verkaufsaktion fehlgeschlagen: Kein Item ausgewählt oder Aktion nicht gesetzt."));
            }
        }

        public void ExecuteUnpackBoxes()
        {
            if (this.UnpackBoxes != null)
            {
                this.UnpackBoxes();
            }
            else
            {
                InformationManager.DisplayMessage(new InformationMessage("⚠️ Keine Aktion für das Entpacken von Boxen gesetzt."));
            }
        }

        [DataSourceProperty]
        public bool CanImport => this.SelectedItem != null;

        [DataSourceProperty]
        public bool CanExport => this.SelectedItem != null;

        [DataSourceProperty]
        public PEStockpileMarketItemVM SelectedItem
        {
            get => this._selectedItem;
            set
            {
                if (value != this._selectedItem)
                {
                    if (this._selectedItem != null)
                    {
                        this._selectedItem.IsSelected = false;
                    }
                    this._selectedItem = value;
                    if (this._selectedItem != null)
                    {
                        this._selectedItem.IsSelected = true;
                    }
                    base.OnPropertyChangedWithValue(value, nameof(SelectedItem));
                    base.OnPropertyChanged(nameof(CanExport));
                    base.OnPropertyChanged(nameof(CanImport));
                }
            }
        }

        [DataSourceProperty]
        public PE_StockpileMarket StockpileMarket
        {
            get => _stockpileMarket;
            set
            {
                if (_stockpileMarket != value)
                {
                    _stockpileMarket = value;
                    base.OnPropertyChanged(nameof(StockpileMarket));
                }
            }
        }

        [DataSourceProperty]
        public Action<PEStockpileMarketItemVM> Buy
        {
            get => _buy;
            set
            {
                if (_buy != value)
                {
                    _buy = value;
                    base.OnPropertyChanged(nameof(Buy));
                }
            }
        }

        [DataSourceProperty]
        public Action<PEStockpileMarketItemVM> Sell
        {
            get => _sell;
            set
            {
                if (_sell != value)
                {
                    _sell = value;
                    base.OnPropertyChanged(nameof(Sell));
                }
            }
        }

        [DataSourceProperty]
        public Action UnpackBoxes
        {
            get => _unpackBoxes;
            set
            {
                if (_unpackBoxes != value)
                {
                    _unpackBoxes = value;
                    base.OnPropertyChanged(nameof(UnpackBoxes));
                }
            }
        }
    }
}
