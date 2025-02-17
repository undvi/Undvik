using System;
using System.Collections.Generic;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using PersistentEmpiresLib.Systems;
using PersistentEmpiresLib.Factions;

namespace PersistentEmpiresLib.Systems
{
    public class PEMarketSystem : MissionObject
    {
        private Dictionary<int, MarketStand> MarketStands = new Dictionary<int, MarketStand>();
        private Dictionary<string, float> FactionTaxRates = new Dictionary<string, float>();

        private const int BaseRentCost = 100;
        private const float DefaultTaxRate = 0.05f;
        private const int MaxStoragePerMarket = 50; // Begrenzte Lagerkapazität für Marktstände

        public void InitializeSystem()
        {
            // Lade Marktstände aus der Datenbank oder aus gespeicherten Daten
        }

        public bool CanRentMarket(NetworkCommunicator player, int standId)
        {
            return MarketStands.ContainsKey(standId) && !MarketStands[standId].IsOccupied;
        }

        public void RentMarket(NetworkCommunicator player, int standId)
        {
            var influenceSystem = Mission.Current.GetMissionBehavior<PEInfluenceSystem>();
            var playerInventory = player.GetComponent<PlayerInventory>();

            if (playerInventory == null || !MarketStands.ContainsKey(standId))
            {
                InformationManager.DisplayMessage(new InformationMessage("⚠️ Fehler: Marktstand nicht gefunden!"));
                return;
            }

            if (!playerInventory.HasGold(BaseRentCost))
            {
                InformationManager.DisplayMessage(new InformationMessage("⚠️ Nicht genug Gold, um diesen Marktstand zu mieten!"));
                return;
            }

            playerInventory.RemoveGold(BaseRentCost);
            MarketStands[standId].Owner = player;
            MarketStands[standId].IsOccupied = true;

            InformationManager.DisplayMessage(new InformationMessage($"✅ {player.UserName} hat einen Marktstand gemietet!"));
        }

        public void SellItem(NetworkCommunicator player, string item, int price)
        {
            var playerInventory = player.GetComponent<PlayerInventory>();
            var faction = PEFactionManager.GetFactionOfPlayer(player);
            var market = GetMarketByOwner(player);

            if (playerInventory == null || market == null || !playerInventory.HasItem(item))
            {
                InformationManager.DisplayMessage(new InformationMessage("⚠️ Fehler: Item nicht vorhanden oder kein Marktstand-Besitz!"));
                return;
            }

            if (market.Storage.Count >= MaxStoragePerMarket)
            {
                InformationManager.DisplayMessage(new InformationMessage("⚠️ Dein Markt hat keinen Platz mehr für neue Waren!"));
                return;
            }

            float taxRate = faction != null && FactionTaxRates.ContainsKey(faction.Name) ? FactionTaxRates[faction.Name] : DefaultTaxRate;
            int taxAmount = (int)(price * taxRate);
            int finalPrice = price - taxAmount;

            playerInventory.RemoveItem(item);
            market.Storage.Add(new MarketItem(item, finalPrice));

            if (faction != null)
            {
                faction.AddGold(taxAmount);
            }

            InformationManager.DisplayMessage(new InformationMessage($"💰 {player.UserName} hat {item} für {finalPrice} verkauft (Steuer: {taxAmount})!"));
        }

        public void BuyItem(NetworkCommunicator player, string item, int marketId)
        {
            var playerInventory = player.GetComponent<PlayerInventory>();
            var market = MarketStands.ContainsKey(marketId) ? MarketStands[marketId] : null;

            if (playerInventory == null || market == null || !market.HasItem(item))
            {
                InformationManager.DisplayMessage(new InformationMessage("⚠️ Fehler: Item nicht verfügbar!"));
                return;
            }

            MarketItem marketItem = market.GetItem(item);

            if (!playerInventory.HasGold(marketItem.Price))
            {
                InformationManager.DisplayMessage(new InformationMessage("⚠️ Nicht genug Gold zum Kauf!"));
                return;
            }

            playerInventory.RemoveGold(marketItem.Price);
            market.Storage.Remove(marketItem);
            playerInventory.AddItem(item);

            InformationManager.DisplayMessage(new InformationMessage($"✅ {player.UserName} hat {item} für {marketItem.Price} Gold gekauft!"));
        }

        public void SetFactionTax(NetworkCommunicator leader, string factionName, float taxRate)
        {
            if (!PEFactionManager.IsFactionLeader(leader, factionName))
            {
                InformationManager.DisplayMessage(new InformationMessage("⚠️ Nur Fraktionsleiter können Steuern ändern!"));
                return;
            }

            FactionTaxRates[factionName] = taxRate;
            InformationManager.DisplayMessage(new InformationMessage($"📢 Steuer für {factionName} wurde auf {taxRate * 100}% gesetzt!"));
        }

        public void SetItemPrice(NetworkCommunicator player, int standId, string item, int price)
        {
            if (!MarketStands.ContainsKey(standId) || MarketStands[standId].Owner != player)
            {
                InformationManager.DisplayMessage(new InformationMessage("⚠️ Fehler: Kein Zugriff auf diesen Marktstand!"));
                return;
            }

            MarketStands[standId].ItemPrices[item] = price;
            InformationManager.DisplayMessage(new InformationMessage($"💰 Preis für {item} wurde auf {price} Gold gesetzt!"));
        }

        public void UpdatePerishableGoods()
        {
            foreach (var market in MarketStands.Values)
            {
                for (int i = market.Storage.Count - 1; i >= 0; i--)
                {
                    if (market.Storage[i].IsPerishable && market.Storage[i].TimeSinceAdded > market.Storage[i].MaxLifetime)
                    {
                        market.Storage.RemoveAt(i);
                        InformationManager.DisplayMessage(new InformationMessage($"⚠️ Ein verderbliches Item in Markt {market.StandID} ist verfault!"));
                    }
                }
            }
        }

        private MarketStand GetMarketByOwner(NetworkCommunicator player)
        {
            return MarketStands.Values.Find(m => m.Owner == player);
        }
    }

    public class MarketStand
    {
        public int StandID { get; set; }
        public NetworkCommunicator Owner { get; set; }
        public bool IsOccupied { get; set; }
        public List<MarketItem> Storage { get; set; } = new List<MarketItem>();
        public Dictionary<string, int> ItemPrices { get; set; } = new Dictionary<string, int>();

        public bool HasItem(string itemName)
        {
            return Storage.Exists(i => i.Name == itemName);
        }

        public MarketItem GetItem(string itemName)
        {
            return Storage.Find(i => i.Name == itemName);
        }
    }

    public class MarketItem
    {
        public string Name { get; private set; }
        public int Price { get; private set; }
        public bool IsPerishable { get; private set; }
        public float TimeSinceAdded { get; private set; }
        public float MaxLifetime { get; private set; }

        public MarketItem(string name, int price, bool isPerishable = false, float maxLifetime = 0)
        {
            Name = name;
            Price = price;
            IsPerishable = isPerishable;
            MaxLifetime = maxLifetime;
            TimeSinceAdded = 0;
        }

        public void UpdateTime(float deltaTime)
        {
            if (IsPerishable)
            {
                TimeSinceAdded += deltaTime;
            }
        }
    }
}
