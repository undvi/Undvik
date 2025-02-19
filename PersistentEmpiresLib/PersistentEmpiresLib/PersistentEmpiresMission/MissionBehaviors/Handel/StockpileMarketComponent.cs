using PersistentEmpiresLib.Data;
using PersistentEmpiresLib.Helpers;
using PersistentEmpiresLib.NetworkMessages.Client;
using PersistentEmpiresLib.NetworkMessages.Server;
using PersistentEmpiresLib.SceneScripts;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace PersistentEmpiresLib.PersistentEmpiresMission.MissionBehaviors
{
    public class TradingCenterBehavior : MissionNetwork
    {
        public delegate void TradingCenterOpenHandler(PE_TradeCenter stockpileMarket, Inventory playerInventory);
        public event TradingCenterOpenHandler OnTradingCenterOpen;

        public delegate void TradingCenterUpdateHandler(PE_TradeCenter stockpileMarket, int itemIndex, int newStock);
        public event TradingCenterUpdateHandler OnTradingCenterUpdate;

        private Dictionary<MissionObject, List<NetworkCommunicator>> openedInventories;
        private Dictionary<PE_TradeCenter, long> RandomizeTimer;
        private Random randomizer;

        public override void OnBehaviorInitialize()
        {
            base.OnBehaviorInitialize();
            this.AddRemoveMessageHandlers(GameNetwork.NetworkMessageHandlerRegisterer.RegisterMode.Add);
            openedInventories = new Dictionary<MissionObject, List<NetworkCommunicator>>();
            RandomizeTimer = new Dictionary<PE_TradeCenter, long>();
            randomizer = new Random();
        }

        public override void AfterStart()
        {
            base.AfterStart();
            if (GameNetwork.IsServer)
            {
                List<PE_TradeCenter> tradeCenters = base.Mission
                    .GetActiveEntitiesWithScriptComponentOfType<PE_TradeCenter>()
                    .Select(g => g.GetFirstScriptOfType<PE_TradeCenter>())
                    .ToList();

                foreach (PE_TradeCenter tradeCenter in tradeCenters)
                {
                    RandomizeTimer[tradeCenter] = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + tradeCenter.RandomizeDelayMinutes * 60;
                    tradeCenter.Randomize(randomizer);
                }
            }
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);
            if (GameNetwork.IsClient) return;

            foreach (PE_TradeCenter tradeCenter in this.RandomizeTimer.Keys.ToList())
            {
                if (this.RandomizeTimer[tradeCenter] < DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                {
                    tradeCenter.Randomize(randomizer);
                    for (int i = 0; i < tradeCenter.MarketItems.Count; i++)
                    {
                        this.UpdateStockForPeers(tradeCenter, i);
                    }
                    this.RandomizeTimer[tradeCenter] = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + tradeCenter.RandomizeDelayMinutes * 60;
                }
            }
        }

        public void UpdateMarketPrice(PE_TradeCenter market, int itemIndex, int newPrice)
        {
            market.MarketItems[itemIndex].SetPrice(newPrice);
            this.UpdateStockForPeers(market, itemIndex);
        }

        private bool HandleRequestTradingBuyItemFromClient(NetworkCommunicator peer, RequestTradingBuyItem message)
        {
            PersistentEmpireRepresentative player = peer.GetComponent<PersistentEmpireRepresentative>();
            if (player == null) return false;

            PE_TradeCenter market = (PE_TradeCenter)message.TradingCenter;
            MarketItem item = market.MarketItems[message.ItemIndex];

            if (item.Stock == 0)
            {
                InformationComponent.Instance.SendMessage("❌ Dieses Item ist nicht mehr auf Lager.", new Color(1f, 0f, 0f).ToUnsignedInteger(), peer);
                return false;
            }

            int totalPrice = item.BuyPrice() + (int)(item.BuyPrice() * market.FactionTaxRate);

            if (!player.ReduceIfHaveEnoughGold(totalPrice))
            {
                InformationComponent.Instance.SendMessage("❌ Nicht genug Gold.", new Color(1f, 0f, 0f).ToUnsignedInteger(), peer);
                return false;
            }

            List<int> updatedSlots = player.GetInventory().AddCountedItemSynced(item.Item, 1, ItemHelper.GetMaximumAmmo(item.Item));
            foreach (int i in updatedSlots)
            {
                GameNetwork.BeginModuleEventAsServer(peer);
                GameNetwork.WriteMessage(new UpdateInventorySlot("PlayerInventory_" + i, player.GetInventory().Slots[i].Item, player.GetInventory().Slots[i].Count));
                GameNetwork.EndModuleEventAsServer();
            }

            item.UpdateReserve(item.Stock - 1);
            this.UpdateStockForPeers(market, message.ItemIndex);

            InformationComponent.Instance.SendMessage($"✅ {peer.UserName} hat {item.Item.Name} für {totalPrice} Gold gekauft.", new Color(0f, 1f, 0f).ToUnsignedInteger(), peer);
            return true;
        }

        private void UpdateStockForPeers(PE_TradeCenter market, int itemIndex)
        {
            MarketItem item = market.MarketItems[itemIndex];
            if (this.openedInventories.ContainsKey(market))
            {
                foreach (NetworkCommunicator peer in this.openedInventories[market].ToArray())
                {
                    if (peer.IsConnectionActive)
                    {
                        GameNetwork.BeginModuleEventAsServer(peer);
                        GameNetwork.WriteMessage(new UpdateTradingCenterStock(market, item.Stock, itemIndex, item.BuyPrice()));
                        GameNetwork.EndModuleEventAsServer();
                    }
                }
            }
        }

        public void OpenTradingCenterForPeer(PE_TradeCenter entity, NetworkCommunicator networkCommunicator)
        {
            PersistentEmpireRepresentative player = networkCommunicator.GetComponent<PersistentEmpireRepresentative>();
            if (player == null) return;
            if (!this.openedInventories.ContainsKey(entity))
            {
                this.openedInventories[entity] = new List<NetworkCommunicator>();
            }
            this.openedInventories[entity].Add(networkCommunicator);

            GameNetwork.BeginModuleEventAsServer(networkCommunicator);
            GameNetwork.WriteMessage(new OpenTradingCenter(entity, player.GetInventory()));
            GameNetwork.EndModuleEventAsServer();
        }
    }
}