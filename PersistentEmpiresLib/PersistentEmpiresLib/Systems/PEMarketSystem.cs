using System.Collections.Generic;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using PersistentEmpiresLib.Systems;
using PersistentEmpiresLib.Factions;
using PersistentEmpiresLib.Helpers;
using TaleWorlds.Core;

namespace PersistentEmpiresLib.Systems
{
    public class PEMarketSystem : MissionObject
    {
        private Dictionary<string, List<MarketStand>> MarketStands = new Dictionary<string, List<MarketStand>>();
        private const int BaseInfluenceCost = 300;
        private const int RentDurationInDays = 7;
        private const int PurchaseCostGold = 1000;

        public void InitializeSystem()
        {
            // Lade bestehende Marktstände oder initialisiere Standardwerte
        }

        public bool CanUseMarket(NetworkCommunicator player, MarketStand stand)
        {
            return stand.IsPublic || stand.Owner == player;
        }

        public void ConstructMarket(NetworkCommunicator player, BuildingZone zone)
        {
            var influenceSystem = Mission.Current.GetMissionBehavior<PEInfluenceSystem>();
            var faction = PEFactionManager.GetFactionOfPlayer(player);

            if (faction == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("⚠️ Fehler: Keine Fraktion gefunden!"));
                return;
            }

            if (!CanBuildMarket(player, faction.Name, zone))
            {
                InformationManager.DisplayMessage(new InformationMessage("⚠️ Fehler: Bauplatz nicht verfügbar!"));
                return;
            }

            int influenceCost = BaseInfluenceCost;
            if (!influenceSystem.HasInfluence(player, influenceCost))
            {
                InformationManager.DisplayMessage(new InformationMessage($"⚠️ {player.UserName} hat nicht genug Einfluss!"));
                return;
            }

            influenceSystem.RemoveInfluence(player, influenceCost);
            MarketStands[faction.Name].Add(new MarketStand(player, zone, true));

            GameNetwork.BeginBroadcastModuleEvent();
            GameNetwork.WriteMessage(new PEBuildingInfluenceUpdated(player, faction.Name, influenceCost));
            GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None, null);

            InformationManager.DisplayMessage(new InformationMessage($"✅ {player.UserName} hat einen öffentlichen Marktstand gebaut!"));
        }

        public bool CanBuildMarket(NetworkCommunicator player, string factionName, BuildingZone zone)
        {
            if (!MarketStands.ContainsKey(factionName))
                return true;

            return !MarketStands[factionName].Exists(m => m.Zone == zone);
        }

        public void RentMarketStand(NetworkCommunicator player, MarketStand stand)
        {
            if (stand.IsOwned)
            {
                InformationManager.DisplayMessage(new InformationMessage("⚠️ Dieser Marktstand wurde bereits gekauft!"));
                return;
            }

            stand.Rent(player, RentDurationInDays);
            InformationManager.DisplayMessage(new InformationMessage($"✅ {player.UserName} hat diesen Marktstand für {RentDurationInDays} Tage gemietet!"));
        }

        public void PurchaseMarketStand(NetworkCommunicator player, MarketStand stand)
        {
            var playerInventory = player.GetComponent<PlayerInventory>();
            if (playerInventory == null || !playerInventory.HasGold(PurchaseCostGold))
            {
                InformationManager.DisplayMessage(new InformationMessage("⚠️ Nicht genug Gold, um diesen Marktstand zu kaufen!"));
                return;
            }

            playerInventory.RemoveGold(PurchaseCostGold);
            stand.Purchase(player);
            InformationManager.DisplayMessage(new InformationMessage($"✅ {player.UserName} hat den Marktstand für {PurchaseCostGold} Gold gekauft!"));
        }

        public void SetItemPrice(NetworkCommunicator player, MarketStand stand, string itemName, int price)
        {
            if (stand.Owner != player)
            {
                InformationManager.DisplayMessage(new InformationMessage("⚠️ Du besitzt diesen Marktstand nicht!"));
                return;
            }

            stand.SetItemPrice(itemName, price);
            InformationManager.DisplayMessage(new InformationMessage($"✅ Preis für {itemName} wurde auf {price} Gold gesetzt!"));
        }

        public void BuyItem(NetworkCommunicator buyer, MarketStand stand, string itemName)
        {
            var playerInventory = buyer.GetComponent<PlayerInventory>();
            if (!stand.ItemPrices.ContainsKey(itemName))
            {
                InformationManager.DisplayMessage(new InformationMessage("⚠️ Dieses Item wird hier nicht verkauft!"));
                return;
            }

            int price = stand.ItemPrices[itemName];
            int tax = CalculateTax(stand, price);
            int totalCost = price + tax;

            if (!playerInventory.HasGold(totalCost))
            {
                InformationManager.DisplayMessage(new InformationMessage("⚠️ Nicht genug Gold für diesen Kauf!"));
                return;
            }

            playerInventory.RemoveGold(totalCost);
            stand.Owner.GetComponent<PlayerInventory>().AddGold(price); // Verkäufer bekommt Gold
            PEFactionManager.GetFactionOfPlayer(stand.Owner)?.AddFactionGold(tax); // Steuer geht an Fraktion

            InformationManager.DisplayMessage(new InformationMessage($"🛒 {buyer.UserName} hat {itemName} für {price} Gold gekauft (+{tax} Steuer)!"));
        }

        private int CalculateTax(MarketStand stand, int price)
        {
            var faction = PEFactionManager.GetFactionOfPlayer(stand.Owner);
            if (faction == null) return 0;

            int taxRate = faction.GetMarketTaxRate();
            return (price * taxRate) / 100;
        }
    }

    public class MarketStand
    {
        public NetworkCommunicator Owner { get; private set; }
        public BuildingZone Zone { get; private set; }
        public bool IsPublic { get; private set; }
        public bool IsOwned { get; private set; }
        public int RentDuration { get; private set; }
        public Dictionary<string, int> ItemPrices { get; private set; }

        public MarketStand(NetworkCommunicator owner, BuildingZone zone, bool isPublic)
        {
            Owner = owner;
            Zone = zone;
            IsPublic = isPublic;
            IsOwned = false;
            RentDuration = 0;
            ItemPrices = new Dictionary<string, int>();
        }

        public void Rent(NetworkCommunicator player, int days)
        {
            Owner = player;
            RentDuration = days;
        }

        public void Purchase(NetworkCommunicator player)
        {
            Owner = player;
            IsOwned = true;
        }

        public void SetItemPrice(string itemName, int price)
        {
            if (ItemPrices.ContainsKey(itemName))
                ItemPrices[itemName] = price;
            else
                ItemPrices.Add(itemName, price);
        }
    }
}
