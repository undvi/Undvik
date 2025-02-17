using PersistentEmpires.Views.ViewsVM;
using PersistentEmpires.Views.ViewsVM.StockpileMarket;
using PersistentEmpiresClient.Views;
using PersistentEmpiresLib.Data;
using PersistentEmpiresLib.NetworkMessages.Client;
using PersistentEmpiresLib.PersistentEmpiresMission.MissionBehaviors;
using PersistentEmpiresLib.SceneScripts;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace PersistentEmpires.Views.Views
{
    public class PEStockpileMarketScreen : PEBaseItemList<PEStockpileMarketVM, PEStockpileMarketItemVM, MarketItem>
    {
        private StockpileMarketComponent _stockpileMarketComponent;
        private PE_StockpileMarket _activeEntity;

        public override void OnMissionScreenInitialize()
        {
            base.OnMissionScreenInitialize();
            _stockpileMarketComponent = Mission.GetMissionBehavior<StockpileMarketComponent>();

            _stockpileMarketComponent.OnStockpileMarketOpen += OnOpen;
            _stockpileMarketComponent.OnStockpileMarketUpdate += OnUpdate;
            _stockpileMarketComponent.OnStockpileMarketUpdateMultiHandler += OnUpdateMulti;

            _dataSource = new PEStockpileMarketVM(HandleClickItem);
        }

        private void OnUpdateMulti(PE_StockpileMarket stockpileMarket, List<int> indexes, List<int> stocks)
        {
            if (!IsActive || indexes.Count != stocks.Count) return;

            for (int i = 0; i < indexes.Count; i++)
            {
                if (indexes[i] < 0 || indexes[i] >= _dataSource.ItemsList.Count) continue;
                _dataSource.ItemsList[indexes[i]].Stock = stocks[i];
            }

            _dataSource.OnPropertyChanged(nameof(_dataSource.FilteredItemList));
        }

        private void OnUpdate(PE_StockpileMarket stockpileMarket, int itemIndex, int newStock)
        {
            if (!IsActive || itemIndex < 0 || itemIndex >= _dataSource.ItemsList.Count) return;
            _dataSource.ItemsList[itemIndex].Stock = newStock;
            _dataSource.OnPropertyChanged(nameof(_dataSource.FilteredItemList));
        }

        public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
        {
            if (affectedAgent.IsMine) Close();
        }

        public override void HandleClickItem(PEItemVM clickedSlot)
        {
            if (_gauntletLayer.Input.IsShiftDown())
            {
                SendNetworkMessage(new InventoryHotkey(clickedSlot.DropTag));
            }
            else if (_gauntletLayer.Input.IsControlDown() && clickedSlot.Item != null && clickedSlot.Count > 0)
            {
                var item = _dataSource.FilteredItemList.FirstOrDefault(i => i.MarketItem.Item.StringId == clickedSlot.Item.StringId);
                if (item != null) Sell(item);
            }
        }

        public override void Close()
        {
            if (!IsActive || _activeEntity == null) return;
            SendNetworkMessage(new RequestCloseStockpileMarket(_activeEntity));
            CloseAux();
        }

        private void OnOpen(PE_StockpileMarket stockpileMarket, Inventory playerInventory)
        {
            if (IsActive) return;

            _activeEntity = stockpileMarket;
            _dataSource.StockpileMarket = stockpileMarket;
            _dataSource.Buy = Buy;
            _dataSource.Sell = Sell;
            _dataSource.UnpackBoxes = UnpackBoxes;

            base.OnOpen(stockpileMarket.MarketItems, playerInventory, "PEStockpileMarket");
        }

        public void Buy(PEStockpileMarketItemVM stockpileMarketItemVM)
        {
            if (_activeEntity != null)
                SendNetworkMessage(new RequestBuyItem(_activeEntity, stockpileMarketItemVM.ItemIndex));
        }

        public void Sell(PEStockpileMarketItemVM stockpileMarketItemVM)
        {
            if (_activeEntity != null)
                SendNetworkMessage(new RequestSellItem(_activeEntity, stockpileMarketItemVM.ItemIndex));
        }

        public void UnpackBoxes()
        {
            if (_activeEntity == null) return;

            var boxItem = _dataSource.PlayerInventory.InventoryItems
                .FirstOrDefault(item => item.Count > 0 && _activeEntity.CraftingBoxes.Any(c => c.BoxItem.StringId == item.Item.StringId));

            if (boxItem != null)
            {
                SendNetworkMessage(new StockpileUnpackBox(boxItem.Index, _activeEntity));
            }
        }

        private void SendNetworkMessage(GameNetworkMessage message)
        {
            GameNetwork.BeginModuleEventAsClient();
            GameNetwork.WriteMessage(message);
            GameNetwork.EndModuleEventAsClient();
        }
    }
}
