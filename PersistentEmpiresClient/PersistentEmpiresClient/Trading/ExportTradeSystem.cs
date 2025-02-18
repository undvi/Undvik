using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace PersistentEmpiresMod.Trading
{
    public class ExportTradeSystem
    {
        private static List<TradeOrder> activeTradeOrders = new List<TradeOrder>();
        private static Random random = new Random();
        private static readonly string savePath = "export_trade_orders.json";
        private static readonly string logPath = "trade_log.txt";

        static ExportTradeSystem()
        {
            LoadTradeOrders();
        }

        public static void GenerateTradeOrder(Faction faction)
        {
            if (faction == null) return;

            string[] possibleGoods = { "Grain", "Iron", "Wood", "Silk", "Spices", "Wine", "Horses" };
            string selectedGood = possibleGoods[random.Next(possibleGoods.Length)];
            int requiredAmount = random.Next(50, 300);
            int baseGoldReward = requiredAmount * 3;
            int baseInfluenceReward = requiredAmount / 10;

            // Skalierte Belohnung abhängig von der Warenart
            if (selectedGood == "Spices" || selectedGood == "Wine" || selectedGood == "Horses")
            {
                baseGoldReward *= 2; // Seltene Waren bringen mehr Geld
                baseInfluenceReward *= 2;
            }

            TradeOrder newOrder = new TradeOrder(faction.StringId, selectedGood, requiredAmount, baseGoldReward, baseInfluenceReward, DateTime.UtcNow);
            activeTradeOrders.Add(newOrder);
            SaveTradeOrders();
            LogTradeAction($"[GENERATED] Handelsauftrag für {faction.Name}: {requiredAmount}x {selectedGood}");

            InformationManager.DisplayMessage(new InformationMessage($"Neue Handelsmission: {faction.Name} soll {requiredAmount}x {selectedGood} exportieren!"));
        }

        public static void AdminGenerateTradeOrder(string factionId)
        {
            Faction faction = Campaign.Current.Factions.Find(f => f.StringId == factionId);
            if (faction != null)
            {
                GenerateTradeOrder(faction);
                InformationManager.DisplayMessage(new InformationMessage($"🛠️ Admin: Handelsauftrag für {faction.Name} erstellt."));
            }
            else
            {
                InformationManager.DisplayMessage(new InformationMessage("⚠️ Ungültige Fraktion!"));
            }
        }

        public static void AdminClearTradeOrders()
        {
            activeTradeOrders.Clear();
            SaveTradeOrders();
            LogTradeAction("[ADMIN] Alle Handelsaufträge wurden zurückgesetzt.");
            InformationManager.DisplayMessage(new InformationMessage("🛠️ Admin: Alle Handelsaufträge wurden gelöscht."));
        }

        private static void LogTradeAction(string logMessage)
        {
            string logEntry = $"[{DateTime.UtcNow}] {logMessage}";
            File.AppendAllText(logPath, logEntry + "\n");
        }

        private static void SaveTradeOrders()
        {
            File.WriteAllText(savePath, JsonSerializer.Serialize(activeTradeOrders));
        }

        private static void LoadTradeOrders()
        {
            if (File.Exists(savePath))
            {
                string json = File.ReadAllText(savePath);
                activeTradeOrders = JsonSerializer.Deserialize<List<TradeOrder>>(json) ?? new List<TradeOrder>();
            }
        }
    }

    public class TradeOrder
    {
        public string FactionId { get; }
        public string Good { get; }
        public int RequiredAmount { get; }
        public int GoldReward { get; }
        public int InfluenceReward { get; }
        public DateTime Timestamp { get; }

        public TradeOrder(string factionId, string good, int requiredAmount, int goldReward, int influenceReward, DateTime timestamp)
        {
            FactionId = factionId;
            Good = good;
            RequiredAmount = requiredAmount;
            GoldReward = goldReward;
            InfluenceReward = influenceReward;
            Timestamp = timestamp;
        }
    }
}
