using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using PersistentEmpiresLib.Systems;
using PersistentEmpiresLib.Factions;

namespace PersistentEmpiresLib.Systems
{
    public class PEMarketSystem : MissionObject
    {
        private Dictionary<int, MarketStand> MarketStands = new Dictionary<int, MarketStand>();
        private Dictionary<string, float> FactionTaxRates = new Dictionary<string, float>();
        private const int BaseMarketCost = 500;
        private const int DefaultStorageLimit = 50;
        private const float DefaultTaxRate = 0.05f;

        public void InitializeSystem()
        {
            LoadMarketTypes();
        }

        private void LoadMarketTypes()
        {
            MarketStands[1] = new MarketStand(1, "Waffenmarkt", 1000, "Weapons", 50);
            MarketStands[2] = new MarketStand(2, "Rüstungsmarkt", 1500, "Armor", 50);
            MarketStands[3] = new MarketStand(3, "Pferdemarkt", 2000, "Horses", 30);
            MarketStands[4] = new MarketStand(4, "Nahrungsmarkt", 500, "Food", 100, true); // Nahrung hat Verfall
        }

        public bool CanBuyMarket(NetworkCommunicator player, int marketId)
        {
            return MarketStands.TryGetValue(marketId, out var market) && !market.IsOwned;
        }

        public void BuyMarket(NetworkCommunicator player, int marketId)
        {
            if (!MarketStands.TryGetValue(marketId, out var market))
            {
                InformationManager.DisplayMessage(new InformationMessage("⚠️ Fehler: Markt nicht gefunden!"));
                return;
            }

            var playerInventory = player.GetComponent<PlayerInventory>();
            var influenceSystem = Mission.Current.GetMissionBehavior<PEInfluenceSystem>();

            if (playerInventory == null || influenceSystem == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("⚠️ Fehler: Systemproblem - Markt konnte nicht gekauft werden!"));
                return;
            }

            if (!playerInventory.HasGold(market.Cost) || !influenceSystem.HasInfluence(player, BaseMarketCost))
            {
                InformationManager.DisplayMessage(new InformationMessage("⚠️ Nicht genug Gold oder Einfluss, um diesen Markt zu kaufen!"));
                return;
            }

            playerInventory.RemoveGold(market.Cost);
            influenceSystem.RemoveInfluence(player, BaseMarketCost);

            market.SetOwner(player);
            InformationManager.DisplayMessage(new InformationMessage($"✅ {player.UserName} hat den {market.Name} gekauft!"));
        }

        public void SellMarket(NetworkCommunicator player, int marketId)
        {
            if (!MarketStands.TryGetValue(marketId, out var market) || market.Owner != player)
            {
                InformationManager.DisplayMessage(new InformationMessage("⚠️ Fehler: Du besitzt diesen Markt nicht!"));
                return;
            }

            player.GetComponent<PlayerInventory>()?.AddGold(market.Cost / 2);
            market.RemoveOwner();
            InformationManager.DisplayMessage(new InformationMessage($"✅ {player.UserName} hat den {market.Name} verkauft!"));
        }

        public void SellItem(NetworkCommunicator player, string item, int price)
        {
            var playerInventory = player.GetComponent<PlayerInventory>();
            var faction = PEFactionManager.GetFactionOfPlayer(player);
            var market = GetMarketByOwner(player);

            if (playerInventory == null || market == null || !playerInventory.HasItem(item))
            {
                InformationManager.DisplayMessage(new InformationMessage("⚠️ Fehler: Item nicht vorhanden oder kein Marktbesitz!"));
                return;
            }

            if (market.IsFull)
            {
                InformationManager.DisplayMessage(new InformationMessage("⚠️ Dein Markt hat keinen Platz mehr für neue Waren!"));
                return;
            }

            float taxRate = faction != null && FactionTaxRates.TryGetValue(faction.Name, out float rate) ? rate : DefaultTaxRate;
            int taxAmount = (int)(price * taxRate);
            int finalPrice = price - taxAmount;

            playerInventory.RemoveItem(item);
            market.AddItem(new MarketItem(item, finalPrice));

            faction?.AddGold(taxAmount);
            InformationManager.DisplayMessage(new InformationMessage($"💰 {player.UserName} hat {item} für {finalPrice} Gold verkauft (Steuer: {taxAmount})!"));
        }

        public void UpdateItemDecay()
        {
            foreach (var market in MarketStands.Values)
            {
                market.ApplyDecay();
            }
        }

        private MarketStand GetMarketByOwner(NetworkCommunicator player) =>
            MarketStands.Values.FirstOrDefault(m => m.Owner == player);
    }

    public class MarketStand
    {
        public int StandID { get; }
        public string Name { get; }
        public int Cost { get; }
        public string MarketType { get; }
        public NetworkCommunicator Owner { get; private set; }
        public bool IsOwned => Owner != null;
        public List<MarketItem> Storage { get; } = new List<MarketItem>();
        public int StorageLimit { get; }
        private bool HasDecay { get; }

        public bool IsFull => Storage.Count >= StorageLimit;

        public MarketStand(int id, string name, int cost, string type, int storageLimit, bool hasDecay = false)
        {
            StandID = id;
            Name = name;
            Cost = cost;
            MarketType = type;
            StorageLimit = storageLimit;
            HasDecay = hasDecay;
        }

        public void SetOwner(NetworkCommunicator owner)
        {
            Owner = owner;
        }

        public void RemoveOwner()
        {
            Owner = null;
            Storage.Clear();
        }

        public void AddItem(MarketItem item)
        {
            if (!IsFull)
                Storage.Add(item);
        }

        public void ApplyDecay()
        {
            if (HasDecay)
                Storage.RemoveAll(item => item.ShouldDecay());
        }
    }

    public class MarketItem
    {
        public string Name { get; }
        public int Price { get; }
        public DateTime AddedTime { get; }

        public MarketItem(string name, int price)
        {
            Name = name;
            Price = price;
            AddedTime = DateTime.UtcNow;
        }

        public bool ShouldDecay() => (DateTime.UtcNow - AddedTime).TotalHours > 24;
    }
}
